namespace Application.Features.HotelVisits.Common;

public record RecentlyVisitedDto(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    string? ThumbnailUrl,
    decimal PricePerNight,
    string Currency);