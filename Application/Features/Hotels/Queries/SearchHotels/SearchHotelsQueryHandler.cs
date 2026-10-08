using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.SearchHotels;

public class SearchHotelsQueryHandler(IHotelRepository hotelRepository, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<SearchHotelsQuery, PaginatedList<HotelSearchResultDto>>
{
    public async Task<PaginatedList<HotelSearchResultDto>> Handle(SearchHotelsQuery request,
        CancellationToken cancellationToken)
    {
        // Repository is responsible for restricting results to hotel.ApprovalStatus == Approved,
        // matching capacity (Adults/Children/Rooms) and availability when CheckIn/CheckOut are given.
        var filter = new HotelSearchFilter(request.Keyword, request.CityId, request.CheckIn, request.CheckOut,
            request.Adults, request.Children, request.Rooms, request.MinPrice, request.MaxPrice,
            request.MinStarRating, request.AmenityIds, request.RoomType);

        var result =
            await hotelRepository.SearchAsync(filter, request.PageNumber, request.PageSize, cancellationToken);
        var today = dateTimeProvider.Today;

        var items = result.Items.Select(h =>
        {
            var thumbnail = h.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url;
            var cheapest = h.Rooms.Where(r => r.IsActive).OrderBy(r => r.GetActivePrice(today).Amount).FirstOrDefault();
            var price = cheapest?.GetActivePrice(today);
            var shortDescription = h.Description.Length > 150 ? h.Description[..150] + "..." : h.Description;

            return new HotelSearchResultDto(h.Id, h.Name, h.City.Name, h.StarRating, thumbnail,
                price?.Amount ?? 0, price?.Currency ?? "USD", shortDescription);
        }).ToList();

        return new PaginatedList<HotelSearchResultDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}