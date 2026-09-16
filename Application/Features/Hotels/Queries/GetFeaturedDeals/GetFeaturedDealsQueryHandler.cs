using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetFeaturedDeals;

public class GetFeaturedDealsQueryHandler(IHotelRepository hotelRepository, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetFeaturedDealsQuery, IReadOnlyList<FeaturedDealDto>>
{
    public async Task<IReadOnlyList<FeaturedDealDto>> Handle(GetFeaturedDealsQuery request,
        CancellationToken cancellationToken)
    {
        var hotels = await hotelRepository.GetFeaturedDealsAsync(request.Count, cancellationToken);
        var today = dateTimeProvider.Today;
        var deals = new List<FeaturedDealDto>();

        foreach (var hotel in hotels)
        {
            // Pick the room with the single best (largest) currently active discount.
            var best = hotel.Rooms
                .Where(r => r.IsActive)
                .Select(r => new { Room = r, Discounted = r.GetActivePrice(today) })
                .Where(x => x.Discounted.Amount < x.Room.BasePrice.Amount)
                .OrderBy(x => x.Discounted.Amount)
                .FirstOrDefault();

            if (best is null) continue; // shouldn't happen given the repository's filter, but stay defensive

            var thumbnail = hotel.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url;

            deals.Add(new FeaturedDealDto(hotel.Id, hotel.Name, hotel.City.Name, thumbnail, hotel.StarRating,
                best.Room.BasePrice.Amount, best.Discounted.Amount, best.Room.BasePrice.Currency));
        }

        return deals;
    }
}