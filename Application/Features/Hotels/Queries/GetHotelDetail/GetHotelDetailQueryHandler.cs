using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelDetail;

public class GetHotelDetailQueryHandler(IHotelRepository hotelRepository, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetHotelDetailQuery, HotelDetailDto>
{
    public async Task<HotelDetailDto> Handle(GetHotelDetailQuery request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdWithDetailsAsync(request.HotelId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        // Not-yet-approved hotels are treated as not found on the public endpoint —
        // never reveal a Pending/Rejected hotel's existence here. Owner/Admin have GetHotelByIdQuery for that.
        if (!hotel.IsPubliclyVisible)
            throw new NotFoundException(nameof(Hotel), request.HotelId);

        var priceDate = request.CheckIn ?? dateTimeProvider.Today;
        DateRange? range = request.CheckIn.HasValue && request.CheckOut.HasValue
            ? DateRange.Of(request.CheckIn.Value, request.CheckOut.Value)
            : null;

        var rooms = hotel.Rooms.Where(r => r.IsActive).Select(r =>
        {
            var isAvailable = range is null || r.IsAvailableFor(range);
            var price = r.GetActivePrice(priceDate);

            return new RoomSummaryDto(r.Id, r.RoomType.ToString(), r.AdultCapacity, r.ChildCapacity,
                price.Amount, price.Currency, isAvailable,
                r.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList());
        }).ToList();

        return new HotelDetailDto(
            hotel.Id, hotel.Name, hotel.StarRating, hotel.Description, hotel.Address, hotel.Latitude, hotel.Longitude,
            hotel.City.Name, hotel.AverageRating(), hotel.Reviews.Count,
            hotel.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList(),
            hotel.HotelAmenities.Select(ha => ha.Amenity.Name).ToList(),
            rooms);
    }
}