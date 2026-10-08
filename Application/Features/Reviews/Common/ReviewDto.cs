namespace Application.Features.Reviews.Common;

public record ReviewDto(
    int Id,
    int HotelId,
    Guid UserId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime? ModifiedAt);