using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetMyHotels;

public class GetMyHotelsQueryHandler(IHotelRepository hotelRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyHotelsQuery, PaginatedList<HotelDto>>
{
    public async Task<PaginatedList<HotelDto>> Handle(GetMyHotelsQuery request, CancellationToken cancellationToken)
    {
        var ownerId = currentUserService.UserId!.Value;
        var result =
            await hotelRepository.GetByOwnerIdAsync(ownerId, request.PageNumber, request.PageSize, cancellationToken);

        var items = result.Items.Select(h => new HotelDto(
            h.Id, h.Name, h.StarRating, h.Description, h.Address, h.Latitude, h.Longitude, h.CityId,
            h.City.Name,
            h.OwnerId, $"{h.Owner.FirstName} {h.Owner.LastName}",
            h.ApprovalStatus.ToString(), h.RejectionReason, h.Rooms.Count, h.CreatedAt, h.ModifiedAt,
            h.HotelAmenities.Select(ha => ha.AmenityId).ToList())).ToList();

        return new PaginatedList<HotelDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}