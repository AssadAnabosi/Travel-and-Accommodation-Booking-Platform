using Application.Common.Models;
using Application.Features.Hotels.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Hotels.Queries.SearchHotels;

public record SearchHotelsQuery(
    string? Keyword,
    int? CityId,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int Adults = 2,
    int Children = 0,
    int Rooms = 1,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinStarRating = null,
    IReadOnlyList<int>? AmenityIds = null,
    RoomType? RoomType = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<HotelSearchResultDto>>;