namespace Application.Features.Hotels.Common;

public record HotelSearchResultDto(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    string? ThumbnailUrl,
    decimal PricePerNight,
    string Currency,
    string ShortDescription);