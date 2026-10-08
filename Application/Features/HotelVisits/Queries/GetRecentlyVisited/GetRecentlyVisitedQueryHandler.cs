using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.HotelVisits.Common;
using MediatR;

namespace Application.Features.HotelVisits.Queries.GetRecentlyVisited;

public class
    GetRecentlyVisitedQueryHandler(
        IHotelVisitRepository hotelVisitRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetRecentlyVisitedQuery, IReadOnlyList<RecentlyVisitedDto>>
{
    public async Task<IReadOnlyList<RecentlyVisitedDto>> Handle(GetRecentlyVisitedQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var hotels = await hotelVisitRepository.GetRecentlyVisitedAsync(userId, request.Count, cancellationToken);
        var today = dateTimeProvider.Today;

        return hotels.Select(h =>
        {
            var thumbnail = h.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url;
            var cheapest = h.Rooms.Where(r => r.IsActive).OrderBy(r => r.GetActivePrice(today).Amount).FirstOrDefault();
            var price = cheapest?.GetActivePrice(today);

            return new RecentlyVisitedDto(h.Id, h.Name, h.City.Name, h.StarRating, thumbnail, price?.Amount ?? 0,
                price?.Currency ?? "USD");
        }).ToList();
    }
}