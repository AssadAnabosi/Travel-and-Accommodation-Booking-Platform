using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.UpdateHotelAmenities;

public class UpdateHotelAmenitiesCommandHandler(
    IHotelRepository hotelRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateHotelAmenitiesCommand>
{
    public async Task Handle(UpdateHotelAmenitiesCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdWithAmenitiesTrackedAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage amenities for your own hotel.");

        // The hotel is tracked with its amenity links loaded, so SetAmenities()'s clear/add is
        // change-tracked; SaveChanges deletes the removed links and inserts the new ones.
        hotel.SetAmenities(request.AmenityIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}