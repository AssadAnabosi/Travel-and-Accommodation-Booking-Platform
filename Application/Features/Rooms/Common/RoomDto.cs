namespace Application.Features.Rooms.Common;

public record RoomDto(
    int Id,
    int HotelId,
    string Number,
    string RoomType,
    int AdultCapacity,
    int ChildCapacity,
    decimal BasePrice,
    string Currency,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ModifiedAt);