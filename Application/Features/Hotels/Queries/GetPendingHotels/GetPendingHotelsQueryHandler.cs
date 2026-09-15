using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetPendingHotels;

public class GetPendingHotelsQueryHandler(IHotelRepository hotelRepository)
    : IRequestHandler<GetPendingHotelsQuery, PaginatedList<HotelDto>>
{
    public async Task<PaginatedList<HotelDto>> Handle(GetPendingHotelsQuery request,
        CancellationToken cancellationToken)
    {
        var result =
            await hotelRepository.GetPendingApprovalAsync(request.PageNumber, request.PageSize, cancellationToken);

        var items = result.Items.Select(h => new HotelDto(
            h.Id, h.Name, h.StarRating, h.Description, h.Address, h.Latitude, h.Longitude, h.CityId, h.City.Name,
            h.OwnerId, $"{h.Owner.FirstName} {h.Owner.LastName}",
            h.ApprovalStatus.ToString(), h.RejectionReason, h.Rooms.Count, h.CreatedAt, h.ModifiedAt)).ToList();

        return new PaginatedList<HotelDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}