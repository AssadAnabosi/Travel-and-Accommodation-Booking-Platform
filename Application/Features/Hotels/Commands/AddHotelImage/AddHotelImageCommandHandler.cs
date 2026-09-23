using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Commands.AddHotelImage;

public class AddHotelImageCommandHandler(
    IHotelRepository hotelRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddHotelImageCommand, int>
{
    public async Task<int> Handle(AddHotelImageCommand request, CancellationToken cancellationToken)
    {
        // Tracked load: change tracking inserts the new image; no Update() of the whole graph needed.
        var hotel = await hotelRepository.GetByIdWithImagesTrackedAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage images for your own hotel.");

        var image = hotel.AddImage(request.Url);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return image.Id;
    }
}