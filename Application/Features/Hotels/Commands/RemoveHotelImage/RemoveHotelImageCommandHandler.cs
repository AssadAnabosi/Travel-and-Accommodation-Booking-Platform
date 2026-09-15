using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.RemoveHotelImage;

public class RemoveHotelImageCommandHandler(
    IHotelRepository hotelRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<RemoveHotelImageCommand>
{
    public async Task Handle(RemoveHotelImageCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdWithDetailsAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage images for your own hotel.");

        hotel.RemoveImage(request.ImageId);

        hotelRepository.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}