using Domain.Enums;

namespace Application.Common.Models;

public record HotelSearchFilter(
    string? Keyword,
    int? CityId,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int Adults,
    int Children,
    int Rooms,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinStarRating,
    IReadOnlyList<int>? AmenityIds,
    RoomType? RoomType);