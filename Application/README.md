## Auth Attr. Usage Eg.

* `[Authorize]`
* `[Authorize(Roles = "Admin")]`
* `[Authorize(Roles = "Admin,Manager")]`
* Because AllowMultiple = true, you can also apply the attribute more than once
  ```csharp
  [Authorize(Roles = "Admin")]
  [Authorize(Roles = "HotelManager")]
  ```

## Reasoning

* Paginated List
    * (Repositories in Infrastructure build this after materializing the query — Application never touches IQueryable/EF
      Core directly, keeping the layer boundary clean.)
* Room.Reserve
    * Re-enforced inside the aggregate — closes the gap between the check above and this write.
* Failed Login Response:
    * Deliberately identical error for "no such user" and "wrong password" — never reveal which one it was, to avoid
      leaking whether an email is registered (user enumeration).
* Logout with invalid token
    * Silently no-op if the token doesn't exist or is already inactive — logout is idempotent, never leak whether a
      token was valid to a client calling this endpoint.
* Why isn't the refresh token a JWT?
  ```text
  Deliberate, not an oversight — a few reasons:

  JWTs are stateless by design — that's the whole point of using them for access tokens (no DB hit to validate, just verify the signature). But a refresh token needs to be revocable (logout, token theft, admin deactivation) — which means checking it against something stored server-side anyway. If you make the refresh token a JWT too, you get the worst of both: the complexity/size of a JWT and still a mandatory DB lookup to check revocation — so the "statelessness" is a fiction that buys you nothing.
  Opaque random tokens are the industry-standard pattern for refresh tokens (OAuth2 spec treats them as opaque by design). A long, high-entropy random string (e.g. 256-bit, base64) that you store hashed in the DB — exactly what RefreshToken.Token is doing — is simpler, smaller, and carries no decodable claims for an attacker to inspect if intercepted.
  Rotation & reuse-detection (the ReplacedByToken chain we built) work naturally on an opaque token tied 1:1 to a DB row. Doing that with self-contained JWTs would mean re-deriving the same server-side revocation list anyway.

  So: access token = JWT (short-lived, stateless, verified by signature). Refresh token = opaque random string (long-lived, always checked against the DB, revocable). This is the standard pairing. If you specifically want the refresh token to also be a JWT for some reason (e.g. embedding claims in it for a specific client need), let me know and I'll switch it — but I'd recommend keeping it opaque.
  ```
* Hotel Creation
    * Owners can create hotels, but it will be placed under pending
    * Admins can create hotels and assign an owner
    * When hotel request is rejected, then updated it will automatically re-submitted.
* GetHotelByIdQuery here is deliberately the internal CRUD-form query
    * (Admin/Owner only, full HotelDto including approval status/owner). The public-facing hotel detail page (gallery,
      reviews, nearby attractions, room listings) is a separate, much richer query — coming with Search/Featured Deals.
* Room number
    * Can't be updated on purpose
* Deleting a review
    * Deliberately no Roles restriction here — eligibility is "you wrote it" OR "you're an Admin," which
      AuthorizationBehavior's role check can't express. Enforced by identity in the handler instead.
* Payment
    * IPaymentGateway is backed by a mock implementation in Infrastructure for now
      the Application layer doesn't know or care that it isn't a real processor.
* Checkout
  * `CheckOutBooking/CheckOutBookingCommand.cs` / handler — identical shape, calling `booking.CheckOut()`:

## Folder Struct

`Probably Out of Sync`

```text
Application/
├── DependencyInjection.cs
├── Common/
│   ├── Interfaces/
│   │   ├── Persistence/
│   │   │   ├── IRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   ├── IUserRepository.cs
│   │   │   ├── ICityRepository.cs
│   │   │   ├── IHotelRepository.cs
│   │   │   ├── IRoomRepository.cs
│   │   │   ├── IBookingRepository.cs
│   │   │   ├── IAmenityRepository.cs
│   │   │   ├── IReviewRepository.cs
│   │   │   ├── IDiscountRepository.cs
│   │   │   └── IHotelVisitRepository.cs
│   │   └── Services/
│   │       ├── IJwtTokenService.cs
│   │       ├── IPasswordHasher.cs
│   │       ├── ICurrentUserService.cs
│   │       ├── IDateTimeProvider.cs
│   │       ├── IEmailService.cs
│   │       ├── IPdfGenerator.cs
│   │       └── IPaymentGateway.cs
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   ├── LoggingBehavior.cs
│   │   └── UnhandledExceptionBehavior.cs
│   ├── Exceptions/
│   │   ├── ValidationException.cs
│   │   ├── NotFoundException.cs
│   │   └── ForbiddenAccessException.cs
│   └── Models/
│       ├── PaginatedList.cs
│       ├── EmailMessage.cs
│       ├── HotelSearchFilter.cs
│       └── PaymentModels.cs
└── Features/
    ├── Auth/
    │   └── Commands/
    │       ├── Common/
    │       │   └── AuthResponse.cs
    │       ├── Login/
    │       ├── Logout/
    │       ├── RefreshToken/
    │       └── Register/
    ├── Users/
    │   ├── Queries/
    │   │   ├── GetUsers/
    │   │   ├── GetUserById/
    │   │   └── GetMyProfile/
    │   └── Commands/
    │       ├── PromoteUserRole/
    │       ├── SetUserActiveStatus/
    │       ├── UpdateProfile/
    │       └── ChangePassword/
    ├── Cities/
    │   ├── Common/
    │   │   └── CityDto.cs    
    │   ├── Queries/
    │   │   ├── GetCities/
    │   │   └── GetCityById/
    │   └── Commands/
    │       ├── CreateCity/
    │       ├── UpdateCity/
    │       └── DeleteCity/
    ├── Hotels/
    │   ├── Common/
    │   │   └── HotelSearchResultDto.cs
    │   │   └── FeaturedDealDto.cs
    │   │   └── HotelDetailDto.cs
    │   │   └── HotelDto.cs
    │   ├── Queries/
    │   │   ├── GetHotelById/
    │   │   ├── GetMyHotels/
    │   │   ├── GetPendingHotels/
    │   │   ├── SearchHotels/
    │   │   ├── GetFeaturedDeals/
    │   │   └── GetHotelDetail/
    │   └── Commands/
    │       ├── CreateHotel/
    │       ├── UpdateHotel/
    │       ├── DeleteHotel/
    │       ├── AddHotelImage/
    │       ├── RemoveHotelImage/
    │       ├── ApproveHotel/
    │       └── RejectHotel/
    ├── Rooms/
    │   ├── Common/
    │   │   └── RoomDto.cs
    │   ├── Queries/
    │   │   ├── GetRoomsByHotel/ 
    │   │   └── GetRoomById/ 
    │   └── Commands/
    │       ├── CreateRoom/
    │       ├── UpdateRoom/
    │       ├── DeleteRoom/
    │       ├── BlockRoomAvailability/
    │       └── UnblockRoomAvailability/
    ├── Bookings/
    │   ├── Common/
    │   │   ├── BookingDto.cs
    │   │   ├── BookingListItemDto.cs
    │   │   └── ReviewDto.cs
    │   ├── Queries/
    │   │   ├── GetMyBookings/
    │   │   ├── GetBookingById/
    │   │   └── GetBookingConfirmationPdf/
    │   └── Commands/
    │       ├── CreateBooking/
    │       ├── ConfirmBooking/
    │       ├── CheckInBooking/
    │       └── CheckOutBooking/
    ├── Reviews/
    │   ├── Common/
    │   │   └── ReviewDto.cs
    │   ├── Queries/
    │   │   └── GetReviewsByHotel/   
    │   └── Commands/
    │       ├── CreateReview/
    │       ├── UpdateReview/
    │       └── DeleteReview/
    ├── Discounts/
    │   ├── Common/
    │   │   └── DiscountDto.cs
    │   ├── Queries/
    │   │   └── GetDiscountsByRoom/   
    │   └── Commands/
    │       ├── CreateDiscount/
    │       ├── UpdateDiscount/
    │       ├── DeactivateDiscount/
    │       └── DeleteDiscount/
    ├── Amenities/
    │   ├── Common/
    │   │   └── AmenityDto.cs
    │   ├── Queries/
    │   │   └── GetAmenities/   
    │   └── Commands/
    │       ├── CreateAmenity/
    │       ├── UpdateAmenity/
    │       └── DeleteAmenity/
    └── HotelVisits/
        ├── Common/
        │   ├── RecentlyVisitedDto.cs
        │   └── TrendingCityDto.cs
        ├── Queries/
        │   ├── GetRecentlyVisited/ 
        │   └── GetTrendingCities/ 
        └── Commands/
            └── RecordHotelVisit/
```