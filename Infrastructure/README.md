# Reasoning

* Representative configurations — the tricky patterns

    ```text
    Money and DateRange are value objects with private constructors and no parameterless
    constructor — EF Core needs owned-type mapping with backing fields, or a value converter. 
    Owned types are the cleaner fit since both are always 1:1-owned by their parent (never 
    shared/nullable-reference), but their factory-method-only construction means EF needs to use 
    the private constructor via reflection — that already works fine with EF Core's owned-entity 
    conventions as long as we don't fight it with strict encapsulation on the value object's 
    properties (only the constructor is private, properties have public getters, which is enough).
  
  RoomConfiguration.cs — demonstrates Money owned-type mapping, the unique index, and the widened Number column:
    ```

(RoomAvailability/Booking cascade from Room deliberately — but remember, Room itself now only hard-deletes when
HasAnyBookingsAsync is false, so a real Booking row is never actually destroyed by this cascade in practice. It's there
for the "never-booked room" case where RoomAvailability might still exist from an admin Block.)
BookingConfiguration.cs — demonstrates DateRange owned-type mapping and the Guid key with no cascade back onto Room:

HotelConfiguration.cs — the approval-status + location fields + Restrict-vs-Cascade split by relationship:

The remaining configurations (User, RefreshToken, City, HotelImage, RoomImage, RoomAvailability, Amenity, Discount, Review, HotelVisit) follow the same three conventions consistently: string-backed enum conversions, HasMaxLength on every string column, and Restrict-vs-Cascade chosen to mirror whatever the corresponding Application handler already checks. I'll write these out in full in the next pass rather than padding this response with near-identical boilerplate — flag if you'd rather see all fourteen in full right now regardless.

(This will be called from the Api project's Program.cs, something like await DatabaseInitializer.InitializeAsync(dbContext, app.Environment.IsDevelopment()) — right after the app builds, before it starts handling requests. That wiring happens once the Api project exists.)

RoomAvailabilityConfiguration.cs — novel: owned DateRange, and BookingId is deliberately a plain column, not a navigable FK (nothing needs to join from here back to Booking, so no second cascade path is introduced):

ReviewConfiguration.cs — novel: composite unique index as the DB-level backstop for "one review per user per hotel":

HotelVisitConfiguration.cs — novel: nullable UserId for anonymous visits:\

## DevSeeder (Infrastructure/Persistence/Seed/DevSeeder.cs)

Idempotent (if (await context.Users.AnyAsync(...)) return;), dev-gated via DatabaseInitializer. Seeded in phases because the domain has no navigation adders and AddImage/SetAmenities/Discount.Create/Room.Create all need the parent's real identity Id — so parents are saved before children are attached:

Users — admin@tabp.dev, owner@tabp.dev, customer@tabp.dev (all password Password123!, hashed via BCrypt).
Cities (Paris, Tokyo, New York) + Amenities (WiFi, Pool, Parking, Gym, Spa, Restaurant).
Hotels — 3 approved (owned by the owner) + 1 pending (so the admin approval queue isn't empty); then amenities + images attached.
Rooms — varied RoomType/capacity/price across hotels; then room images.
Discounts (active today → drive Featured Deals), a blocked date range (so availability filtering has something to exclude), reviews, and hotel visits weighted so Paris tops Trending Destinations.

This exercises every non-trivial read path the repositories added: search + price/availability filters, featured deals, approval queue, reviews, trending cities.

# Folder Struct

`Probably Out of Sync`

```text
Infrastructure/
├── DependencyInjection.cs
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   ├── RefreshTokenConfiguration.cs
│   │   ├── CityConfiguration.cs
│   │   ├── HotelConfiguration.cs
│   │   ├── HotelImageConfiguration.cs
│   │   ├── RoomConfiguration.cs
│   │   ├── RoomImageConfiguration.cs
│   │   ├── RoomAvailabilityConfiguration.cs
│   │   ├── BookingConfiguration.cs
│   │   ├── AmenityConfiguration.cs
│   │   ├── HotelAmenityConfiguration.cs
│   │   ├── DiscountConfiguration.cs
│   │   ├── ReviewConfiguration.cs
│   │   └── HotelVisitConfiguration.cs
│   ├── Repositories/
│   │   ├── AmenityRepository.cs
│   │   ├── BookingRepository.cs
│   │   ├── CityRepository.cs
│   │   ├── DiscountRepository.cs
│   │   ├── HotelRepository.cs
│   │   ├── HotelVisitRepository.cs
│   │   ├── ReviewRepository.cs
│   │   ├── RoomRepository.cs
│   │   └── UserRepository.cs
│   ├── UnitOfWork.cs
│   ├── Migrations
│   └── Seed/
│       └── DevSeeder.cs
├── Services/
│   ├── DateTimeProvider.cs
│   ├── JwtSettings.cs
│   ├── JwtTokenService.cs
│   ├── LoggingEmailService.cs
│   ├── MockPaymentService.cs
│   ├── PasswordHasher.cs
│   └── PdfGenerator.cs
├── DatabaseInitializer.cs
└── DependencyInjection.cs
```
