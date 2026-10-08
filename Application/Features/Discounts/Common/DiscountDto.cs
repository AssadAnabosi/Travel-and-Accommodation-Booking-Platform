namespace Application.Features.Discounts.Common;

public record DiscountDto(
    int Id,
    int RoomId,
    string Name,
    string Type,
    decimal Value,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ModifiedAt);