namespace Application.Features.Hotels.Common;

public record HotelDto(
    int Id,
    string Name,
    int StarRating,
    string Description,
    string Address,
    double Latitude,
    double Longitude,
    int CityId,
    string CityName,
    Guid OwnerId,
    string OwnerName,
    string ApprovalStatus,
    string? RejectionReason,
    int RoomsCount,
    DateTime CreatedAt,
    DateTime? ModifiedAt);