## Reasoning
* Ids: Why use `int` for some and `guid` for others?
  * int is used internally for performance reasons, while guid is used to give up some of that performance in exchange for some security (avoiding exposing analytics for bad actors).
* Room Blocking
  * (Deliberately: "not found" is an Application-layer concern (NotFoundException), so Unblock takes an already-resolved RoomAvailability rather than an id — the handler fetches it via FindAvailability first.)

## File Struct
```
HotelBooking.Domain/
├── Common/
│ ├── BaseEntity.cs
│ ├── AuditableEntity.cs
│ ├── ValueObject.cs
│ └── Guard.cs
├── Enums/
│ ├── UserRole.cs
│ ├── RoomType.cs
│ ├── BookingStatus.cs
│ ├── DiscountType.cs
│ └── AvailabilityStatus.cs
├── ValueObjects/
│ ├── Money.cs
│ └── DateRange.cs
├── Exceptions/
│ ├── DomainException.cs
│ ├── InvalidDateRangeException.cs
│ ├── RoomNotAvailableException.cs
│ └── InvalidDiscountException.cs
└── Entities/
├── User.cs
├── City.cs
├── Hotel.cs
├── Room.cs
├── RoomAvailability.cs
├── Booking.cs
├── Amenity.cs
├── HotelAmenity.cs
├── Review.cs
├── Discount.cs
└── HotelVisit.cs
```