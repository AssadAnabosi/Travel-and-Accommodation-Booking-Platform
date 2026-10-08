namespace Application.Features.Hotels.Common;

public record HotelDetailDto(
    int Id,
    string Name,
    int StarRating,
    string Description,
    string Address,
    double Latitude,
    double Longitude,
    string CityName,
    double AverageRating,
    int ReviewCount,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<string> Amenities,
    IReadOnlyList<RoomSummaryDto> Rooms);

public record RoomSummaryDto(
    int RoomId,
    string RoomType,
    int AdultCapacity,
    int ChildCapacity,
    decimal PricePerNight,
    string Currency,
    bool IsAvailable,
    IReadOnlyList<string> ImageUrls);