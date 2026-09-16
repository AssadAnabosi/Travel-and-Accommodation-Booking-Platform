namespace Application.Features.Hotels.Common;

public record FeaturedDealDto(
    int HotelId,
    string Name,
    string CityName,
    string? ThumbnailUrl,
    int StarRating,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    string Currency);