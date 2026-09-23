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
        // Must be a tracked load: removing from the AsNoTracking GetByIdWithDetailsAsync graph and calling
        // Update() returned 204 but never deleted the row (decision #71).
        var hotel = await hotelRepository.GetByIdWithImagesTrackedAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage images for your own hotel.");

        hotel.RemoveImage(request.ImageId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}