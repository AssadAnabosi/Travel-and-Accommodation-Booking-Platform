# TABP API — End-to-End Walkthrough

A numbered, payload-by-payload walkthrough of the whole platform from three points of view:
an **Admin**, a **Hotel Owner**, and a **Customer** who searches, books, pays, and reviews.

Everything here has been exercised against the running stack.

---

## 0. Setup

```bash
docker compose up --build -d
```

- **Base URL:** `http://localhost:8080`
- **Swagger UI:** `http://localhost:8080/swagger` (use the green **Authorize** button to paste an access token)
- **Sent emails (MailHog):** `http://localhost:8025` (booking confirmations, hotel approved/rejected notices)
- **Health:** `GET /api/health` → JSON `{ "status": "Healthy", "totalDurationMs": …, "components": { "database": {…}, "redis": {…} } }`.
  **200** `Healthy`; **200** `Degraded` if only Redis is down (rate limiting then fails open); **503** `Unhealthy` if the DB is down.
- **Reset seed data:** `docker compose down -v && docker compose up -d`

### Seeded accounts (password `Password123!` for all)

| Email | Role |
|---|---|
| `admin@tabp.dev` | Admin |
| `owner@tabp.dev` | HotelOwner |
| `customer@tabp.dev` | Customer |

### How auth works

- **Login/Register** return an `accessToken` (JWT, ~60 min) in the JSON body and set the **refresh token
  as an HttpOnly cookie** (`SameSite=Strict`, `Path=/api/auth`) — it is *not* in the body.
- Send the access token on protected calls: `Authorization: Bearer <accessToken>`.
- `POST /api/auth/refresh` (no body — it reads the cookie) issues a new access token + rotates the cookie.
- The current user is `GET /api/auth`.
- Rate limits: **10 requests/min per IP** on everything under `/api/auth` (including refresh), and 100/min
  per user (or IP) overall. Going over returns **429** `application/problem+json`, so pace repeated logins.

### Enums (sent/received as strings)

- `UserRole`: `Customer`, `HotelOwner`, `Admin`
- `RoomType`: `Standard`, `Budget`, `Deluxe`, `Suite`, `Luxury`, `Boutique`
- `DiscountType`: `Percentage`, `FixedAmount`
- `HotelApprovalStatus`: `Pending`, `Approved`, `Rejected`
- `BookingStatus`: `Pending`, `Confirmed`, `CheckedIn`, `CheckedOut`

---

## Part A — Admin

The admin curates reference data (cities, amenities), manages users and roles, and runs the hotel
approval workflow.

### A1. Log in as Admin

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@tabp.dev", "password": "Password123!" }
```

**200** →
```json
{
  "userId": "…", "email": "admin@tabp.dev", "firstName": "Site", "lastName": "Admin",
  "role": "Admin", "accessToken": "eyJhbGciOi…"
}
```
Use this `accessToken` as `Authorization: Bearer …` for every step below.

### A2. Create a city

```http
POST /api/cities
Authorization: Bearer <admin token>
Content-Type: application/json

{ "name": "Barcelona", "country": "Spain", "postOffice": "08001",
  "thumbnailUrl": "https://picsum.photos/seed/city-barcelona/800/600" }
```
**201 Created** → `CityDto` `{ "id": 1003, "name": "Barcelona", "country": "Spain", "postOffice": "08001", "thumbnailUrl": "https://…", "hotelsCount": 0, … }`

> `thumbnailUrl` is optional (shown on "Trending destinations"); it must be an absolute **http(s)** URL (**400** otherwise).
> `PUT /api/cities/{id}` replaces the whole city, so **leaving `thumbnailUrl` out clears it**.

> List/read cities publicly: `GET /api/cities?keyword=barc&pageNumber=1&pageSize=20`, `GET /api/cities/{id}`.
> Update/delete: `PUT /api/cities/{id}`, `DELETE /api/cities/{id}` (blocked with **409** if the city still has hotels).

### A3. Create an amenity

```http
POST /api/amenities
Authorization: Bearer <admin token>
Content-Type: application/json

{ "name": "Rooftop Pool" }
```
**201 Created** → `{ "id": 1007, "name": "Rooftop Pool", … }`

> `GET /api/amenities` is public. `DELETE /api/amenities/{id}` is blocked with **409** if it's assigned to any hotel.

### A4. Promote a user to Hotel Owner

First find the user (admin-only, paged & filterable):
```http
GET /api/users?keyword=new-owner&role=Customer&isActive=true&pageNumber=1&pageSize=20
Authorization: Bearer <admin token>
```
Then promote by id:
```http
PUT /api/users/role
Authorization: Bearer <admin token>
Content-Type: application/json

{ "userId": "<their id>", "newRole": "HotelOwner" }
```
**204 No Content**. (The user must log in again to receive a token carrying the new role.)

> Guarded: an admin **cannot** remove their own Admin role (**403**). Deactivate/reactivate a user with
> `PUT /api/users/status { "userId": "…", "isActive": false }` — you also can't deactivate yourself (**403**).

### A5. Approve or reject a submitted hotel

Owners submit hotels as `Pending` (see Part B). List them:
```http
GET /api/hotels/pending?pageNumber=1&pageSize=20
Authorization: Bearer <admin token>
```
Or browse **every** hotel in any state (the admin grid): newest first, all filters optional:
```http
GET /api/hotels?keyword=paris&approvalStatus=Rejected&cityId=1&ownerId=<guid>&pageNumber=1&pageSize=20
Authorization: Bearer <admin token>
```
**200** → paged `HotelDto` (`name`, `starRating`, `ownerName`, `roomsCount`, `approvalStatus`, `cityName`,
`createdAt`, `modifiedAt`, …). `keyword` matches the name, address or city name; `pageSize` is at most 50.
Approve (makes it publicly searchable):
```http
POST /api/hotels/42/approve
Authorization: Bearer <admin token>
```
**204** (approving an already-approved hotel → **409**); the owner gets a "your hotel is live" email. Or reject with a reason (the owner can edit & resubmit):
```http
POST /api/hotels/42/reject
Authorization: Bearer <admin token>
Content-Type: application/json

{ "reason": "Please add exterior photos and a valid address." }
```
**204**, and the owner gets an email with the reason.

> An admin can also create a hotel directly and assign it to an existing owner — it is **auto-approved**
> (see the `ownerId` field in step B3).

Move a hotel to a different owner (e.g. the property was sold):
```http
PUT /api/hotels/42/owner
Authorization: Bearer <admin token>
Content-Type: application/json

{ "newOwnerId": "<guid of an active HotelOwner>" }
```
**204**. The previous owner immediately loses access to it (403). The target must be an **active HotelOwner**
(**400** otherwise). Reassigning to the current owner is a no-op. `PUT /api/hotels/{id}` ignores `ownerId`;
this is the only way to change it.

---

## Part B — Hotel Owner

The owner builds a listing: create the hotel, add rooms, attach amenities & images, run discounts,
and later check guests in/out.

### B1. Log in as Owner

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "owner@tabp.dev", "password": "Password123!" }
```
**200** → `role: "HotelOwner"`, `accessToken`. Use it below.

### B2. See your hotels

```http
GET /api/hotels/mine?pageNumber=1&pageSize=20
Authorization: Bearer <owner token>
```
**200** → paged `HotelDto` list (id, name, approvalStatus, roomsCount, …).

### B3. Create a hotel (starts `Pending`)

```http
POST /api/hotels
Authorization: Bearer <owner token>
Content-Type: application/json

{
  "name": "Barcelona Beach Resort",
  "starRating": 4,
  "description": "Seafront rooms a short walk from the marina.",
  "address": "Passeig Marítim 12, Barcelona",
  "latitude": 41.3784,
  "longitude": 2.1925,
  "cityId": 1003,
  "ownerId": null
}
```
**201 Created** → `HotelDto` `{ "id": 42, "approvalStatus": "Pending", "ownerId": "<you>", … }`

> `ownerId` is `null` for owner self-creation (you become the owner). When an **Admin** posts this and
> supplies a real `ownerId`, the hotel is created **Approved** and assigned to that owner.

### B4. Add rooms

```http
POST /api/rooms
Authorization: Bearer <owner token>
Content-Type: application/json

{
  "hotelId": 42,
  "number": "201",
  "roomType": "Deluxe",
  "adultCapacity": 2,
  "childCapacity": 1,
  "basePrice": 180.00,
  "currency": "USD"
}
```
**201 Created** → `RoomDto` `{ "id": 500, "number": "201", "roomType": "Deluxe", "isActive": true, … }`

> - List a hotel's rooms: `GET /api/rooms/by-hotel/42`. Read one: `GET /api/rooms/500`.
> - Update capacity only (the room number is immutable): `PUT /api/rooms/500 { "adultCapacity": 3, "childCapacity": 1 }`.
> - Block a maintenance hold: `POST /api/rooms/500/availability/block { "startDate": "2027-06-01", "endDate": "2027-06-05" }`
>   → **201** `{ "availabilityId": 12 }` (**409** if it overlaps a booking or another block); remove it with
>   `DELETE /api/rooms/500/availability/12` (**409** if that id is a booking's hold, not a manual block).
> - Delete a room: `DELETE /api/rooms/500` — **hard-deletes** if never booked, otherwise **soft-deletes**
>   (keeps history, frees the room number for reuse). Deleting an already soft-deleted room → **409**.

### B5. Attach amenities and images

```http
PUT /api/hotels/42/amenities
Authorization: Bearer <owner token>
Content-Type: application/json

{ "amenityIds": [1, 4, 1007] }
```
**204** (replaces the hotel's amenity set).

```http
POST /api/hotels/42/images
Authorization: Bearer <owner token>
Content-Type: application/json

{ "url": "https://example.com/hotels/42/exterior.jpg" }
```
**201** → `{ "imageId": 88 }`; remove with `DELETE /api/hotels/42/images/88` → **204** (**404** if that image
isn't on this hotel). List the gallery with ids (any approval state): `GET /api/hotels/42/images` →
`[{ "id": 88, "url": "…", "displayOrder": 0 }]`.

Rooms have galleries too, with the same shape:
```http
POST /api/rooms/500/images
Authorization: Bearer <owner token>
Content-Type: application/json

{ "url": "https://example.com/hotels/42/room-201.jpg" }
```
**201** → `{ "imageId": 91 }`. List: `GET /api/rooms/500/images`. Remove: `DELETE /api/rooms/500/images/91` → **204**.

### B6. Run a discount on a room (Owner only)

```http
POST /api/discounts
Authorization: Bearer <owner token>
Content-Type: application/json

{
  "roomId": 500,
  "name": "Summer Special",
  "type": "Percentage",
  "value": 15,
  "startDate": "2027-06-01",
  "endDate": "2027-06-30"
}
```
**201 Created** → `DiscountDto` `{ "id": 77, "type": "Percentage", "value": 15, "isActive": true, … }`

> Overlapping active discounts on the same room are rejected (**400**). Also: `GET /api/discounts/by-room/500`,
> `PUT /api/discounts/77`, `POST /api/discounts/77/deactivate`, `DELETE /api/discounts/77`.

### B7. Wait for admin approval

Your hotel is `Pending` until an admin approves it (step A5). Once **Approved**, it appears in public
search and detail. If **Rejected**, edit it (`PUT /api/hotels/42`) — an owner edit of a rejected hotel
**automatically resubmits** it to `Pending`.

### B8. Check guests in and out

After a customer confirms a booking at your hotel (Part C), find it in your hotel's booking list (ordered by
check-in date; all filters optional):
```http
GET /api/hotels/42/bookings?status=Confirmed&checkInFrom=2027-06-10&checkInTo=2027-06-10&keyword=ada&pageNumber=1&pageSize=20
Authorization: Bearer <owner token>
```
**200** → paged rows `{ id, confirmationNumber, guestName, guestEmail, roomNumber, checkIn, checkOut, adults,
children, status, totalPrice, currency, specialRequests, … }`. `status=Confirmed` + a check-in date gives the
day's arrivals; `status=CheckedIn` gives the guests in house. `keyword` matches the confirmation number or the
guest's name/email. Another owner's hotel → **403**.

Then move a booking (its `id`) through its stay:
```http
POST /api/bookings/<bookingId>/check-in
Authorization: Bearer <owner token>
```
```http
POST /api/bookings/<bookingId>/check-out
Authorization: Bearer <owner token>
```
Both **204**. (An Admin can do this for any hotel; an owner only for their own.) Out of order — checking in
a booking that isn't `Confirmed`, or checking out one that isn't `CheckedIn` — → **409** with the reason in `detail`.

---

## Part C — Customer (search → book → pay → review)

### C1. Register (or log in)

```http
POST /api/auth/register
Content-Type: application/json

{ "email": "ada@example.com", "password": "Passw0rd!23", "firstName": "Ada", "lastName": "Lovelace" }
```
**200** → `role: "Customer"`, `accessToken` (+ refresh cookie). Public self-registration is always a Customer.

### C2. Search for hotels (public, no token)

```http
GET /api/hotels/search?cityId=1003&checkIn=2027-06-10&checkOut=2027-06-13&adults=2&children=1&minStarRating=3&amenityIds=1&amenityIds=4&roomType=Deluxe&pageNumber=1&pageSize=20
```
**200** → paged `HotelSearchResultDto`:
```json
{
  "items": [
    { "hotelId": 42, "name": "Barcelona Beach Resort", "cityName": "Barcelona",
      "starRating": 4, "thumbnailUrl": "…", "pricePerNight": 153.00, "currency": "USD",
      "shortDescription": "Seafront rooms a short walk from the marina." }
  ],
  "totalCount": 1, "pageNumber": 1, "pageSize": 20
}
```

> `pricePerNight` reflects the lowest active discount at check-in. `amenityIds` may repeat and are ANDed
> (the hotel must have all of them). Also public: `GET /api/hotels/featured-deals?count=5`.

### C3. Open the hotel detail page (public)

```http
GET /api/hotels/42?checkIn=2027-06-10&checkOut=2027-06-13
```
**200** → `HotelDetailDto` with `amenities`, `imageUrls`, `averageRating`, `reviewCount`, and a `rooms`
array (each with `roomId`, `pricePerNight`, `isAvailable` for the given dates).

> Record the visit (feeds "recently visited" / "trending cities"); works logged-in or anonymous:
> `POST /api/hotel-visits/42` → **204**.
> Public discovery: `GET /api/hotel-visits/trending-cities?count=5` → `[{ cityId, cityName, thumbnailUrl, visitCount }]`.
> Logged-in: `GET /api/hotel-visits/recently-visited?count=5`.

### C4. Read reviews (public)

```http
GET /api/reviews/by-hotel/42?pageNumber=1&pageSize=20
```
**200** → paged `ReviewDto` (`reviewerName`, `rating`, `comment`, …).

### C5. Create a booking (starts `Pending`)

```http
POST /api/bookings
Authorization: Bearer <customer token>
Content-Type: application/json

{
  "roomId": 500,
  "checkIn": "2027-06-10",
  "checkOut": "2027-06-13",
  "adults": 2,
  "children": 1,
  "specialRequests": "High floor, late check-in"
}
```
**201 Created** → `CreateBookingResponse`:
```json
{ "bookingId": "9d9f…", "confirmationNumber": "HB-…", "totalPrice": 459.00, "currency": "USD", "status": "Pending" }
```
(The room is now reserved for those dates; the total is computed with any active discount applied.)
If the room is already taken for any of those nights → **409** `"Room 500 is not available for the range …"`.

### C6. Confirm & pay (mock gateway → PDF + email)

```http
POST /api/bookings/9d9f…/confirm
Authorization: Bearer <customer token>
Content-Type: application/json

{ "cardToken": "tok_visa_demo" }
```
**200** → `BookingDto` `{ "status": "Confirmed", "confirmationNumber": "HB-…", "totalPrice": 459.00, … }`

> The payment is mocked: a **non-empty** `cardToken` succeeds; an empty one is rejected (**400**), and the
> booking stays `Pending` (retryable). On success a confirmation PDF is generated and emailed to you with the PDF
> attached. Open **MailHog at `http://localhost:8025`** to see it. (Email is best-effort: if the mail server is down,
> the confirm still succeeds.) Confirming a booking that is no longer `Pending` (e.g. already confirmed) → **409**.

### C7. View your bookings and download the PDF

```http
GET /api/bookings/mine?pageNumber=1&pageSize=20
Authorization: Bearer <customer token>
```
```http
GET /api/bookings/9d9f…
Authorization: Bearer <customer token>
```
**200** → `BookingDetailDto` (hotel, room, nights, total, status, …).

```http
GET /api/bookings/9d9f…/confirmation-pdf
Authorization: Bearer <customer token>
```
**200** → `application/pdf` (a downloadable booking confirmation).

### C8. Stay happens, then review (after check-out)

Once the owner/admin has checked you **out** (steps B8), you may review the hotel — the API enforces a
**verified completed stay**:
```http
POST /api/reviews
Authorization: Bearer <customer token>
Content-Type: application/json

{ "hotelId": 42, "rating": 5, "comment": "Fantastic seafront location and spotless rooms." }
```
**201 Created** → `ReviewDto`.

> - Without a completed stay → **400** (verified-stay gate). One review per hotel per user → **400** on a second
>   (both are validator rules; the handler's 403/409 re-checks only fire on a concurrent request).
> - Edit your own review: `PUT /api/reviews/<id>` (author only). Delete: `DELETE /api/reviews/<id>` (author **or** Admin).

### C9. Refresh the session / log out

```http
POST /api/auth/refresh          # no body — reads the HttpOnly cookie; rotates it
```
**200** → new `accessToken`.
```http
POST /api/auth/logout           # revokes the refresh token and clears the cookie
```
**204**.

---

## Appendix — the full booking lifecycle at a glance

```
Customer: POST /bookings                → Pending    (room reserved, total computed)
Customer: POST /bookings/{id}/confirm   → Confirmed  (mock payment ok → PDF + email)
Owner/Admin: POST /bookings/{id}/check-in   → CheckedIn
Owner/Admin: POST /bookings/{id}/check-out  → CheckedOut
Customer: POST /reviews (verified stay) → review created
```

Hotel approval:
```
Owner: POST /hotels                → Pending
Admin: POST /hotels/{id}/approve   → Approved   (now public in search/detail)
Admin: POST /hotels/{id}/reject    → Rejected   → Owner PUT /hotels/{id} → back to Pending
```

Errors are RFC 7807 `ProblemDetails` with a user-safe `detail`: 400 validation / business rule, 401, 403,
404, 402 payment failed, 409 conflict or wrong-state transition, 429 rate limited. A 500 means a real bug.
Swagger lists the exact codes for each endpoint.

Authorization is enforced both at the HTTP edge (`[Authorize]` on the controller → 401/403) and in the
MediatR pipeline (the command's own `[Authorize]`), with ownership checks inside the handlers.
