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
        var hotel = await hotelRepository.GetByIdWithDetailsAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage amenities for your own hotel.");

        hotel.SetAmenities(request.AmenityIds);

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}