# Hotel Booking Platform — System Overview

---

## 1. What This Is

A backend API system for an online hotel booking platform. It covers Login, Home/Search,
Hotel Details, Secure Checkout, and Admin Management, exposed as a set of RESTful APIs.
No frontend is implemented as part of this phase — this is a backend-only system.

Three user roles exist:

- **Customer** — browses, searches, books rooms, manages their own profile and reviews.
- **HotelOwner** — manages their own hotel(s): listings, rooms, amenities, discounts.
- **Admin** — full oversight: manages cities, approves/rejects hotels, manages any
  hotel/room, manages users and the shared amenity list.

### Getting started

```bash
docker compose up --build -d     # SQL Server + Redis + MailHog + API; migrates and seeds in Development
docker compose down -v           # stop and reset the database to a fresh seed
dotnet test                      # unit tests (Domain, Application, Infrastructure)
```

| What | Where |
|---|---|
| Swagger UI (Development only) | `http://localhost:8080/swagger` (raw spec: `/swagger/v1/swagger.json`) |
| Health | `GET http://localhost:8080/api/health` |
| Sent emails (MailHog) | `http://localhost:8025` |
| Seeded logins (password `Password123!`) | `admin@tabp.dev` (Admin), `owner@tabp.dev` (HotelOwner), `customer@tabp.dev` (Customer) |

To run the API without Docker: `dotnet run --project API` against SQL Server on `localhost,1433` and Redis on
`localhost:6379` (see `API/appsettings.json`). A step-by-step tour of every persona is in **`API-WALKTHROUGH.md`**.

---

## 2. Tech Stack

| Concern | Choice |
|---|---|
| Runtime / Framework | .NET 10, ASP.NET Core **Web API (controllers)** |
| ORM | Entity Framework Core |
| Database | SQL Server |
| CQRS / Mediator | MediatR |
| Input Validation | FluentValidation |
| Query composition | A small custom **Specification pattern** (`ISpecification<T>`/`BaseSpecification<T>` in Application, `SpecificationEvaluator<T>` in Infrastructure) for search/filter queries |
| Authentication | JWT access tokens (HS256, System.IdentityModel.Tokens.Jwt) + opaque refresh tokens (rotation) |
| Authorization | Role-Based Access Control (RBAC), enforced in the Application layer |
| Payment | Mocked payment gateway (real Stripe/PayPal integration deferred) |
| Password hashing | BCrypt (BCrypt.Net-Next) |
| PDF generation | PdfSharpCore (booking-confirmation PDF) |
| Email | MailKit over SMTP; **MailHog** container catches all mail locally (UI `http://localhost:8025`) |
| Rate limiting | ASP.NET rate limiter with **Redis**-backed partitions (fails open) |
| Containerization | Docker + Docker Compose (API, SQL Server, Redis, MailHog) |
| Testing | xUnit + FluentAssertions + Moq (unit tests; integration tests deprioritized) |
| API Docs | Swagger / OpenAPI (Swashbuckle) with JWT bearer auth in the UI |

---

## 3. Architecture — Clean Architecture

The solution is split into layers with dependencies pointing strictly inward:

```
Api  →  Infrastructure  →  Application  →  Domain
 └────────────────────────────↗
```

### Domain
Pure C#, **zero external dependencies** (no EF Core, no ASP.NET, no third-party packages).
Entities are rich (private setters, static factory methods, internal guard clauses) so that
business invariants hold no matter which layer or caller constructs/mutates them. This is
the only layer that enforces invariants *unconditionally*.

### Application
Depends only on Domain. Organized **feature-first**:

```
Features/<FeatureName>/
├── Commands/<UseCase>/   (Command + Validator + Handler)
└── Queries/<UseCase>/    (Query + Handler)
```

Key patterns used throughout:

- **CQRS via MediatR** — every use case is a single Command or Query with exactly one
  Handler. No generic "service" classes with grab-bag methods.
- **Repository-per-aggregate** — dedicated interfaces (`IHotelRepository`, `IRoomRepository`,
  `IUserRepository`, etc.), each shaped around what that aggregate actually needs to query.
  Not a single generic repository, and not direct `DbContext` exposure to the Application layer.
- **Specification pattern** for search/filter queries — a concrete `BaseSpecification<T>`
  subclass (e.g. `HotelSearchSpecification`) builds up filter criteria, includes, ordering,
  and paging declaratively, instead of a repository method growing into one giant `if` chain.
  The interface/base class live in Application (pure C#, no EF dependency); the
  `SpecificationEvaluator<T>` that actually applies one to an `IQueryable` lives in
  Infrastructure, since `.Include()` is EF-specific.
- **FluentValidation** — validates input shape and simple existence/business lookups
  (e.g. "does this city exist?") *before* a handler runs. This coexists deliberately with
  Domain-level guard clauses — see decision table below.
- **MediatR pipeline behaviors**, applied in this order to every request:
  1. `UnhandledExceptionBehavior` — outer safety net, logs truly unexpected exceptions only.
  2. `AuthorizationBehavior` — reads a custom `[Authorize(Roles = "...")]` attribute placed
     directly on Commands/Queries and enforces it against the current user, independent of
     any ASP.NET Core-level attribute. This means authorization can't be bypassed by wiring
     a Command to a different or new endpoint.
  3. `ValidationBehavior` — runs all FluentValidation validators for the request.
  4. `LoggingBehavior` — logs handler name, user, and elapsed time.

  The behaviors are constrained `where TRequest : notnull`, **not** `IRequest<TResponse>`. A void command
  (`IRequest`) doesn't satisfy the latter, and DI then silently drops the behavior, so every void command
  would skip validation and authorization (this happened once). Keep `notnull`;
  `PipelineBehaviorRegistrationTests` guards it.

### Infrastructure — ✅ complete
Implements every interface defined in Application: the EF Core `DbContext` (which also serves
as the `IUnitOfWork`), all 14 entity type configurations, every repository, the
`SpecificationEvaluator<T>`, the dev-only seeder, and all services (JWT, BCrypt password
hashing, a mocked payment gateway, a MailKit SMTP email sender with a logging fallback, and a PdfSharpCore PDF generator) —
all registered in `AddInfrastructure`.

- **Migrations** (`Infrastructure/Migrations`, committed): `InitialCreate` and `AddCityThumbnailUrl`. Applied
  automatically on startup, but **only when running in the Development environment**. Production databases are
  never auto-migrated. Add new ones with
  `dotnet ef migrations add <Name> --project Infrastructure --startup-project API`.
- **Seed data**: sample data for local development lives in its own clearly-marked
  `Persistence/Seed/` directory, and is only ever invoked from the same
  Development-only code path as migrations — never reachable from a production run.

### Api — ✅ Feature-complete (controllers done)
Controller-based host. The composition root (`Program.cs`) is wired: it registers
`AddApplication()` + `AddInfrastructure(configuration)`, JWT bearer authentication + authorization,
Swagger/OpenAPI with a JWT bearer scheme in the UI, `ICurrentUserService` (via
`IHttpContextAccessor`), and a global exception handler that maps Application exceptions to
RFC 7807 `ProblemDetails`. The EF Core migrations and Docker Compose (API + SQL Server + Redis + MailHog) are
in place — `docker compose up` migrates, seeds, and serves Swagger at `http://localhost:8080/swagger`.
All ten feature controllers are verified end-to-end against the
containerized SQL Server (Auth: register/login/refresh/logout/current-user; Users: admin
list/get + role/status management and self-service profile/password; Cities: public read + admin
CRUD; Hotels: public search/featured/detail + owner/admin management with the approval workflow,
images and amenities; Rooms: owner/admin CRUD + availability block/unblock; Amenities: public read +
Admin/HotelOwner CRUD; Discounts: HotelOwner-only management (overlap-checked) + admin/owner read;
Reviews: public read + verified-stay-gated create, author-only edit, author/Admin delete;
HotelVisits: record a visit (anonymous-friendly), recently-visited (self), trending-cities (public);
Bookings: create → confirm (mock payment + PDF + email), check-in/out, my-bookings, get, and a
downloadable confirmation PDF — all with RBAC enforced). Admins also get a list of every hotel with
owner reassignment, and owners get a front-desk list of their hotel's bookings plus image management with ids.

**Hardening:** security headers on every response, config-driven CORS, Redis-backed rate limiting that fails
open, and a health endpoint that reports the database and Redis separately (see the decision table).

**Swagger is a complete contract.** Endpoints are grouped with `[Tags]` (public discovery vs
management), every action has a `///` summary (XML docs are generated and fed to Swashbuckle), and
every action declares its responses: the success code and type (200/201/204, the PDF as
`application/pdf` binary) plus the specific 402/404/409 it can return. Responses every endpoint can
return (400 `ValidationProblemDetails`, 429, 500) are registered once as global filters, and 401/403
are added only to endpoints that require auth (`API/Swagger/AuthorizationResponsesConvention.cs`), so
public endpoints don't advertise them. The spec at `/swagger/v1/swagger.json` is accurate enough to
generate a typed client from.

Each endpoint's job is only to accept a request, send the corresponding Command/Query through
MediatR, and translate the result or a thrown exception into the appropriate HTTP response.
No business logic lives here.

---

## 4. Key Architectural Decisions

| Decision | What we chose | Why |
|---|---|---|
| Namespaces | No `HotelBooking.` prefix — just `Domain.*`, `Application.*`, `Infrastructure.*`, etc. | Requested simplification |
| Entity IDs | `int` by default; `Guid` for `User`, `Booking`, `HotelVisit` | Guid avoids leaking sequential volume/identity for entities tied to auth or individual analytics |
| Command/Query pattern | MediatR + CQRS | Idiomatic for Clean Architecture; scales cleanly as features grow |
| Data access | Repository-per-aggregate | Clean, mockable seam; each repo shaped to its aggregate's real query needs |
| Search/filter queries | Small custom Specification pattern | Keeps repository methods (Hotels, Users, Cities search) from becoming a giant `if` chain, while staying framework-agnostic in Application |
| Validation strategy | FluentValidation (Application) *and* Domain guard clauses, both, deliberately | Defense-in-depth: FluentValidation gives fast, friendly 400s; Domain guarantees invariants hold regardless of caller |
| Room availability | Separate `RoomAvailability` entity (date-range rows) | A single boolean flag can't represent overlapping date-range bookings correctly |
| Shopping cart | None — a `Booking` is created directly as `Pending` at checkout | Scope reduction; no persisted cart entity |
| Discounts | Separate `Discount` entity on `Room` (type, value, date range); overlap-checked so pricing stays deterministic | Supports "Featured Deals" without hardcoding two price columns |
| Discount management | Hotel Owner only, no approval gate — live immediately | Accepted risk for project scope; Owner is trusted to manage their own pricing |
| Money currency | Kept as a per-row column (not hardcoded to a single currency) | Future-proofs multi-currency support even though no conversion logic exists yet |
| Auth tokens | JWT access token (short-lived, stateless) + opaque random refresh token (long-lived, DB-stored, rotated each use) | Refresh tokens must be revocable (logout, theft), which requires a DB check regardless — so making them JWTs too would add complexity for no benefit. Standard OAuth2-style pairing. |
| Hotel creation | Dual-path: Admin creates & assigns an *existing* HotelOwner (auto-approved); HotelOwner self-creates (starts Pending) | Balances self-service with admin oversight |
| HotelOwner promotion | Admin-only, direct — no self-service "apply to be an owner" flow | Keeps role escalation strictly gated |
| Hotel approval workflow | `Pending → Approved` or `Pending → Rejected`; a Rejected hotel can be edited and resubmitted (→ back to `Pending`) | Rejection isn't terminal; owners can fix and retry |
| User deletion | Soft-delete only (`IsActive` flag) — no hard delete | Preserves referential integrity with Bookings/Reviews |
| Self-lockout prevention | Admin cannot deactivate their own account or remove their own Admin role | Prevents accidental lockout with no other Admin to fix it |
| Room management | Either the hotel's Owner *or* any Admin | Flexible oversight without blocking owners |
| Room creation timing | Rooms can be added to a hotel regardless of its approval status | Owners can prep a full listing while awaiting approval |
| Room number | Immutable after creation via Update — but see the deletion rule below for how it can still change | Avoids uniqueness-on-rename complexity entirely |
| Room deletion | If the room has **no** booking history at all, it's hard-deleted. If it has **any** booking history (past or future), it's **soft-deleted** instead (`IsActive = false`) and its `Number` is mangled with a unique suffix, freeing the original number for reuse by a genuinely new room | Preserves all historical Booking/Discount/Review data while still letting owners "remove" a room from active listings and reuse its old room number |
| Amenity master list | Managed by both Admin and HotelOwner | Shared, low-risk reference data |
| Login/Register errors | Identical generic message for "no such user" and "wrong password" | Prevents user-enumeration attacks |
| HotelVisit storage | Kept in SQL Server (not a separate NoSQL store) | Read/write patterns are simple and well within what a relational DB handles well; a cache-aside layer (e.g. Redis) is the recommended future optimization, not a second source of truth |
| HotelVisit recording | A separate explicit `RecordHotelVisitCommand`, not a side effect of viewing detail | Gives the client control over when a "visit" actually counts |
| Anonymous visit tracking | Anonymous (non-logged-in) views ARE recorded — `HotelVisit.UserId` is nullable | "Trending Destinations" should reflect all traffic, not just logged-in traffic |
| Hotel location & gallery | Added `Address`/`Latitude`/`Longitude` to `Hotel`, plus new `HotelImage`/`RoomImage` entities | Needed for the interactive map and visual gallery requirements; images stored as ordered URLs (actual upload/storage is an Infrastructure concern, not yet built) |
| Reviews | Only users with a completed (`CheckedOut`) booking at a hotel may review it ("verified stay"); one review per user per hotel (editable); can be edited by its author only; can be deleted by its author or an Admin — **never by the HotelOwner** | Prevents fake reviews; keeps moderation power with the platform, not the party being reviewed |
| Payment | Mocked payment gateway for now | Real payment integration (Stripe/PayPal) explicitly deferred |
| Booking cancellation | **Not implemented at all yet**, for any role | Customers explicitly cannot cancel; no other role's cancellation flow has been requested yet either |
| Check-In / Check-Out | Admin or the hotel's Owner only, working from `GET /api/hotels/{id}/bookings` | Treated as the "front desk" role; there's no separate Staff role in the system. The hotel's booking list (by status and check-in date) is how the front desk finds a guest |
| EF Core migrations | Auto-applied on startup, **Development environment only** | Convenience for local dev/Docker Compose without risking an accidental production auto-migration |
| Seed data | Sample data for local dev only, isolated in its own `Persistence/Seed/` directory, invoked only from the same dev-gated path as migrations | Keeps sample/demo data completely out of any code path that could run in production |
| API style | Controller-based Web API (not Minimal API) | Requested; controllers group endpoints per feature and integrate cleanly with Swagger and attribute routing |
| Unit of work | The EF `AppDbContext` implements `IUnitOfWork` directly | It already tracks changes and exposes `SaveChangesAsync`; a separate wrapper class would add nothing |
| Search price/availability filtering | Inlined SQL-translatable rewrite in `HotelRepository.SearchAsync` — price at check-in (today if no dates), best (lowest) active discount, single-room capacity match | Domain methods `GetActivePrice`/`IsAvailableFor` don't translate to SQL; the rewrite keeps filtering in one DB round-trip. Trade-off: the pricing/availability logic is duplicated in SQL and must stay in sync with the domain |
| Password hashing | BCrypt (`BCrypt.Net-Next`) | Salt + work factor embedded per hash; industry standard |
| JWT implementation | `System.IdentityModel.Tokens.Jwt`, HS256, config-driven `JwtSettings` | Standard, well-supported; secret/issuer/audience/expiry come from configuration |
| Email service | **MailKit** SMTP (`SmtpEmailService`) behind `IEmailService`, pointed at a **MailHog** container locally (web UI `http://localhost:8025`); `LoggingEmailService` is the fallback when `Smtp:Host` is empty | Chosen over Resend (HTTP API): free, provider-agnostic, works offline, and every email is viewable locally with no account. Production just points `Smtp:*` at a real relay (SES/SendGrid/Mailgun SMTP) |
| Email failure handling | **Best-effort**: the sender logs delivery failures and never throws; a short SMTP timeout (`Smtp:TimeoutMs`, 10 s) | Emails are sent after the DB commit (e.g. after a booking is paid). A mail outage must not turn a successful, paid operation into a 500 that the client might retry. Trade-off: a failed email is lost (only logged); an outbox/queue with retries is the upgrade path if delivery must be guaranteed |
| Email content | Handlers HTML-encode (`WebUtility.HtmlEncode`) every user-supplied value put into an HTML body (hotel name, owner name, rejection reason) | Hotel names and reasons are user input, so they could inject markup or links into emails otherwise |
| PDF generation | PdfSharpCore | MIT-licensed; fonts come from a custom `FileFontResolver` (DejaVu in the container). Its transitive `SixLabors.ImageSharp` is **pinned to 2.1.13** (patched, Apache-2.0) to clear the 1.0.4 advisories. Don't bump it to 3.x (Split License, breaking API). PDFsharp 6 is the upgrade path |
| OpenAPI/Swagger package | Swashbuckle.AspNetCore, replacing the built-in `Microsoft.AspNetCore.OpenApi` | Gives a real Swagger UI with an Authorize button; also drops the built-in package that transitively pulled the vulnerable `Microsoft.OpenApi` 2.0.0 |
| `Microsoft.OpenApi` version | Pinned to 2.7.5 (matches Swashbuckle 10.2.3's dependency) | Clears the `Microsoft.OpenApi` 2.0.0 high-severity advisory while staying on the major version Swashbuckle is built against. A 3.x pin compiles cleanly but throws at runtime when Swashbuckle serializes operations (an empty doc succeeds; a doc with real endpoints returns 500), so 3.x is deliberately avoided until Swashbuckle supports it |
| JWT inbound claim mapping | Disabled (`MapInboundClaims = false`); `RoleClaimType = ClaimTypes.Role` | Keeps the raw claim names the token is issued with (`sub`, `email`) instead of ASP.NET's legacy URI remapping, so `ICurrentUserService` reads them directly; role claim type matches what `JwtTokenService` writes so `[Authorize(Roles = …)]` works |
| Token clock skew | `ClockSkew = TimeSpan.Zero` | Access tokens expire exactly at their stated time, not up to the default 5 minutes late |
| `ICurrentUserService` location | Implemented in the **Api** layer via `IHttpContextAccessor` | The current user is a property of the HTTP request; the interface stays in Application, and the HTTP-bound implementation is deliberately kept out of Infrastructure |
| API error responses | A single global `IExceptionHandler` writing RFC 7807 `ProblemDetails` | One place maps Application exceptions to status codes (400/401/403/404/409, and 402 for payment failures); validation errors surface under an `errors` member; unexpected 500s do not leak internal messages |
| Domain rule violations over HTTP | Specific `DomainException` subclasses, mapped in the same handler: `RoomNotAvailableException` and `InvalidStateTransitionException` → **409**, `ImageNotFoundException` → **404**, any other `DomainException` (invalid date range/discount) → **400**. Entity state guards (confirm/check-in/check-out/cancel a booking, approve/resubmit a hotel, soft-delete a room twice, unblock a booking-held range, revoke a revoked token) throw `InvalidStateTransitionException`, not `InvalidOperationException` | Domain messages are safe to show and describe a client mistake, so they must not surface as 500. `InvalidOperationException` is deliberately **not** mapped globally — EF Core and the framework throw it for genuine bugs, which should stay 500 with no detail. The one remaining domain `InvalidOperationException` (mixing `Money` currencies) is a programming invariant, not a client error |
| Swagger response documentation | Per-action `[ProducesResponseType]` + global filters for shared codes + an `IActionModelConvention` adding 401/403 only to `[Authorize]` actions | The spec lists exactly the codes each endpoint can return, so a frontend can generate a typed client from it; declaring 401/403 globally would wrongly advertise them on public endpoints |
| Refresh-token transport | Sent as an **HttpOnly cookie** (`SameSite=Strict`, `Path=/api/auth`, `Secure` when the request is HTTPS), not in the JSON body | Keeps the long-lived refresh token out of reach of page JavaScript (XSS mitigation); the short-lived access token still comes back in the body. All cookie handling lives in `AuthController` — the Application commands stay transport-agnostic and take a plain token string, which the controller supplies from the cookie on refresh/logout |
| Route casing | Lowercase route tokens via `RouteTokenTransformerConvention` (`/api/auth`, not `/api/Auth`) | Conventional lowercase REST URLs, and necessary so the case-sensitive refresh-token cookie `Path=/api/auth` matches the request path |
| JSON enums | Serialized/accepted as string names via `JsonStringEnumConverter` (`"HotelOwner"`, not `1`) | Readable, stable API contract; also fixes enum-typed command fields, which otherwise fail to bind from string values since System.Text.Json defaults to integers |
| Health check | ASP.NET health-checks middleware at `GET /api/health`, returning JSON `{ status, totalDurationMs, components: { database, redis } }`. DB down → `Unhealthy` 503; Redis down → `Degraded` 200. Exempt from rate limiting | The API can't serve without the database, but it keeps working without Redis (rate limiting fails open), so Redis only degrades |
| Security headers | A small middleware adds `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` | Cheap, safe defaults. No strict CSP (it breaks the Swagger UI) and no HSTS (a production, behind-HTTPS concern) |
| CORS | Named policy from `Cors:AllowedOrigins`, explicit origins + `AllowCredentials()` | A cross-origin SPA must be able to send the HttpOnly refresh cookie, which rules out the `*` wildcard. No configured origins → none allowed |
| Rate limiting | ASP.NET rate limiter with **Redis** partitions: 100/min sliding window per user (or IP), plus 10/min fixed window per IP on `/api/auth`. Rejections → 429 `application/problem+json` | Limits hold across instances. Wrapped to **fail open**: if Redis is down, requests are allowed (and logged) instead of the whole API failing |
| Admin hotel management | `GET /api/hotels` lists every hotel in any state (filters: keyword, city, status, owner); `PUT /api/hotels/{id}/owner` reassigns the owner (target must be an active HotelOwner) | The admin grid needs Pending and Rejected hotels too. Owner changes are a separate admin-only command, so the shared update command stays owner-safe |
| Front desk | `GET /api/hotels/{id}/bookings` for the owner/admin, filtered by status, check-in range and keyword, ordered by check-in date | Owners need to find today's arrivals and in-house guests to check them in and out |
| Image management | `GET /api/hotels/{id}/images` and `GET /api/rooms/{id}/images` return `{ id, url, displayOrder }`; add/remove endpoints for both | Removal needs the image id. Separate endpoints keep galleries out of the list DTOs, which would otherwise have to load them |
| City thumbnails | Optional `City.ThumbnailUrl`, admin-set, absolute http(s) only; returned by city endpoints and trending cities. `PUT /api/cities/{id}` is a full replace, so omitting it clears it | Predictable images for "Trending destinations". http(s)-only because the value goes straight into an `<img src>` on a public page |

---

## 5. Domain Model Summary

| Entity | Id type | Belongs to / Key relationships | Notes |
|---|---|---|---|
| **User** | Guid | — | Role: Customer / HotelOwner / Admin. Owns `RefreshToken`s, `Hotel`s (if HotelOwner), `Booking`s. |
| **City** | int | — | Name, Country, PostOffice, optional ThumbnailUrl (for "Trending destinations"). Has many Hotels. |
| **Hotel** | int | City, Owner (User) | `ApprovalStatus`: Pending / Approved / Rejected (+ `RejectionReason`). Has `Address`/`Latitude`/`Longitude`, many Rooms, Reviews, Amenity links, and `HotelImage`s. |
| **HotelImage** | int | Hotel | Ordered image URL for the gallery. |
| **Room** | int | Hotel | `RoomType` enum, capacities, `BasePrice` (Money). Has many RoomAvailability rows, Discounts, and `RoomImage`s. Can be soft-deleted (`IsActive = false`, `Number` mangled) if it has booking history, or hard-deleted if not. |
| **RoomImage** | int | Room | Ordered image URL for the room's gallery. |
| **RoomAvailability** | int | Room | A blocked date range — `Booked` (tied to a Booking, via a plain `BookingId` value, not a navigable FK) or `Blocked` (manual hold). No row for a date = available. |
| **Booking** | **Guid** | User, Room | DateRange stay, `BookingStatus` (Pending/Confirmed/CheckedIn/CheckedOut/Cancelled — Cancelled currently unreachable, no flow builds it yet), total price, confirmation number. Never hard-deleted; `Room`'s FK to it is `Restrict`. |
| **Amenity** | int | — | Shared master list (WiFi, Pool, etc.), many-to-many with Hotel via `HotelAmenity`. |
| **Discount** | int | Room | Type (Percentage/FixedAmount), plain `DateOnly` date range (not the `DateRange` value object), active flag. Overlap-checked against other active discounts on the same room. |
| **Review** | int | Hotel, User | Rating 1–5 + comment. Gated by a completed (`CheckedOut`) booking. DB-enforced unique per (Hotel, User). |
| **HotelVisit** | **Guid** | User (nullable), Hotel | Append-only analytics log entry — powers "Recently Visited" (logged-in only) / "Trending Destinations" (all visits, including anonymous). |

---

## 6. Roles & Permissions Summary

| Capability | Customer | HotelOwner | Admin |
|---|:---:|:---:|:---:|
| Register / Login / manage own profile | ✅ | ✅ | ✅ |
| Search & book hotels | ✅ | ✅ | ✅ |
| Leave a review (after a completed stay) | ✅ | ✅ (as a guest elsewhere) | ✅ (as a guest elsewhere) |
| Delete any review | own only | ⬜ (cannot delete reviews on own hotel either) | ✅ (any) |
| Create a hotel (starts Pending) | ⬜ | ✅ | ✅ (auto-approved, must assign an existing HotelOwner) |
| Manage own hotel's rooms/amenities/images | ⬜ | ✅ (own only) | ✅ (any) |
| Manage discounts | ⬜ | ✅ (own hotel's rooms only) | ⬜ |
| Approve / Reject a hotel | ⬜ | ⬜ | ✅ |
| Check guests in / out | ⬜ | ✅ (own hotel) | ✅ (any) |
| Cancel a booking | ⬜ (not built for anyone yet) | ⬜ | ⬜ |
| Manage Cities | ⬜ | ⬜ | ✅ |
| Manage Users (promote/activate/deactivate) | ⬜ | ⬜ | ✅ |
| Manage the Amenity master list | ⬜ | ✅ | ✅ |

---

## 7. Key Workflows

### Hotel Approval Workflow
```
HotelOwner creates Hotel  ─────────►  Pending
Admin creates Hotel       ─────────►  Approved (auto)

Pending  ──Admin approves──►  Approved   (now publicly visible in search / featured deals)
Pending  ──Admin rejects (reason)──►  Rejected
Rejected ──Owner edits & resubmits──►  Pending   (re-enters the review queue)
```
Approve and reject each email the owner (reject includes the reason).

### Auth / Token Lifecycle
```
Register or Login
  → Access Token issued  (JWT, short-lived)
  → Refresh Token issued (opaque string, long-lived, stored server-side)

Client calls "refresh" with the refresh token
  → old refresh token is revoked and chained to the new one (rotation)
  → new Access Token + new Refresh Token issued

Client calls "logout"
  → refresh token revoked (idempotent: no error if already invalid/unknown)
```

### Booking Lifecycle (implemented so far)
```
CreateBookingCommand   (Customer)
  → validate date range, check RoomAvailability, compute price with active Discount applied
  → Booking created (status = Pending), RoomAvailability row reserved

ConfirmBookingCommand  (the booking's own Customer)
  → charge via IPaymentGateway (mocked)
  → on success: Booking → Confirmed; PDF generated + confirmation email sent (PDF attached)
  → on failure: throws PaymentFailedException, booking stays Pending (retryable)

CheckInBookingCommand / CheckOutBookingCommand   (Admin or the hotel's Owner)
  → Confirmed → CheckedIn → CheckedOut

(No cancellation flow exists yet, for any role.)
```

### Room Deletion
```
DeleteRoomCommand
  → has this room EVER had a booking (past or future)?
      NO  → hard delete the Room row entirely
      YES → soft-delete: Room.IsActive = false, Number mangled with a unique suffix
             (original number becomes free for a genuinely new room to reuse;
              all historical Bookings/Discounts/RoomAvailability rows are preserved intact)
```

### Hotel Visit Tracking
```
Client explicitly calls RecordHotelVisitCommand(HotelId) after viewing a hotel's detail page
  → HotelVisit logged with UserId = current user, or null if anonymous
  → feeds GetRecentlyVisitedQuery (logged-in users only) and GetTrendingCitiesQuery (everyone)
```

---

## 8. Use Cases Implemented So Far

**Auth** — Register, Login, RefreshToken (rotation), Logout

**Users** — GetUsers (admin), GetUserById (admin), GetMyProfile, PromoteUserRole (admin, self-demotion guarded), SetUserActiveStatus (admin, self-deactivation guarded), UpdateProfile (self), ChangePassword (self)

**Cities** — GetCities, GetCityById, CreateCity, UpdateCity, DeleteCity (blocked if hotels exist)

**Hotels** — CreateHotel (dual-path), UpdateHotel (auto-resubmits if it was Rejected), DeleteHotel (Admin-only, blocked if rooms exist), ApproveHotel, GetHotels (Admin list of every hotel, filterable), ReassignHotelOwner (Admin), GetHotelImages (image ids for removal), RejectHotel, GetHotelById, GetMyHotels, GetPendingHotels, UpdateHotelAmenities, AddHotelImage, RemoveHotelImage

**Amenities** — GetAmenities (public), CreateAmenity, UpdateAmenity, DeleteAmenity (blocked if still in use)

**Rooms** — GetRoomsByHotel, GetRoomById, CreateRoom, UpdateRoom (capacity only — number is immutable via Update), DeleteRoom (soft-delete with history preserved, or hard-delete if never booked), BlockRoomAvailability, UnblockRoomAvailability, AddRoomImage, RemoveRoomImage, GetRoomImages

**Discounts** — GetDiscountsByRoom, CreateDiscount, UpdateDiscount, DeactivateDiscount, DeleteDiscount (all Owner-only to manage, overlap-checked)

**Reviews** — GetReviewsByHotel (public), CreateReview (verified-stay gated), UpdateReview (author only), DeleteReview (author or Admin only)

**Public Search / Discovery** — SearchHotels (filters: keyword, city, dates, capacity, price range, star rating, amenities, room type — approved hotels only), GetFeaturedDeals, GetHotelDetail (public, 404s on unapproved hotels)

**HotelVisits** — RecordHotelVisit (public, anonymous-friendly), GetRecentlyVisited (self, logged-in only), GetTrendingCities (public)

**Bookings** — CreateBooking, ConfirmBooking (mock payment + email + PDF), CheckInBooking, CheckOutBooking, GetMyBookings, GetBookingById, GetBookingConfirmationPdf, GetHotelBookings (a hotel's bookings for its owner/admin front desk, filterable)

---

## 9. Conventions and Gotchas

- Namespaces are just `Domain.*`, `Application.*`, `Infrastructure.*` — no prefix. IDs are `int` except `User`,
  `Booking`, `HotelVisit` (`Guid`).
- **Authorization is enforced twice:** ASP.NET `[Authorize]` on controllers (HTTP 401/403) *and* the custom
  `[Authorize]` on Commands/Queries via `AuthorizationBehavior` (the authoritative rule). Ownership checks live in
  the handlers. Keep both in sync when adding an endpoint.
- **EF loading rules** (the cause of most past bugs): repository reads use `AsNoTracking`; a DTO must only read
  navigations its repository method eager-loads; a mutation that changes a **collection** (images, amenities)
  must load the entity **tracked** (e.g. `GetByIdWithImagesTrackedAsync`) and rely on change tracking, not `Update()`.
- Domain state guards throw `InvalidStateTransitionException` (→ 409), **not** `InvalidOperationException`, which is
  deliberately left unmapped (→ 500) because EF and the framework throw it for real bugs.
- Room deletion is soft-delete-with-history (mangled `Number`) when any booking exists; hard delete only when never
  booked. A hotel can't be deleted while it has any rooms.
- Review create: the **validator** enforces the completed stay and one review per hotel (both → 400); the handler's
  403/409 only catch races.
- `IEmailService` is **best-effort**: an email failure must never fail the request, and user text in email bodies
  must be HTML-encoded.
- `PUT` endpoints are full replaces (e.g. omitting a city's `thumbnailUrl` clears it). `PUT /api/hotels/{id}`
  ignores `ownerId`; use `PUT /api/hotels/{id}/owner`.
- The payment mock is `MockPaymentGateway` (in the misnamed file `MockPaymentService.cs`); any non-empty card token
  succeeds.
- The dev database accumulates test data; `docker compose down -v` and `up` restores the seed.
