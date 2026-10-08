using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotelImages;

public class GetHotelImagesQueryHandler(IHotelRepository hotelRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetHotelImagesQuery, IReadOnlyList<ImageDto>>
{
    public async Task<IReadOnlyList<ImageDto>> Handle(GetHotelImagesQuery request, CancellationToken cancellationToken)
    {
        // Any approval state (unlike the public detail page), so owners can manage a pending listing's gallery.
        var hotel = await hotelRepository.GetByIdWithImagesTrackedAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view images for your own hotel.");

        return hotel.Images
            .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, i.Url, i.DisplayOrder))
            .ToList();
    }
}
