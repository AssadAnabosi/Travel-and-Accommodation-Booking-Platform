using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelById;

public class GetHotelByIdQueryHandler(IHotelRepository hotelRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetHotelByIdQuery, HotelDto>
{
    public async Task<HotelDto> Handle(GetHotelByIdQuery request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdWithDetailsAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view your own hotel.");

        return new HotelDto(hotel.Id, hotel.Name, hotel.StarRating, hotel.Description, hotel.Address, hotel.Latitude,
            hotel.Longitude, hotel.CityId, hotel.City.Name,
            hotel.OwnerId, $"{hotel.Owner.FirstName} {hotel.Owner.LastName}",
            hotel.ApprovalStatus.ToString(), hotel.RejectionReason, hotel.Rooms.Count, hotel.CreatedAt,
            hotel.ModifiedAt);
    }
}