using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Hotels.Common;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotels;

public class GetHotelsQueryHandler(IHotelRepository hotelRepository)
    : IRequestHandler<GetHotelsQuery, PaginatedList<HotelDto>>
{
    public async Task<PaginatedList<HotelDto>> Handle(GetHotelsQuery request, CancellationToken cancellationToken)
    {
        var filter = new HotelAdminFilter(request.Keyword, request.CityId, request.ApprovalStatus, request.OwnerId);
        var result = await hotelRepository.GetAllForAdminAsync(filter, request.PageNumber, request.PageSize,
            cancellationToken);

        var items = result.Items.Select(h => new HotelDto(
            h.Id, h.Name, h.StarRating, h.Description, h.Address, h.Latitude, h.Longitude, h.CityId, h.City.Name,
            h.OwnerId, $"{h.Owner.FirstName} {h.Owner.LastName}",
            h.ApprovalStatus.ToString(), h.RejectionReason, h.Rooms.Count, h.CreatedAt, h.ModifiedAt)).ToList();

        return new PaginatedList<HotelDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}
