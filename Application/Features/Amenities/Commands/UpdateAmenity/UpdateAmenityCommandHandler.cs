using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Amenities.Commands.UpdateAmenity;

public class UpdateAmenityCommandHandler(IAmenityRepository amenityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAmenityCommand, AmenityDto>
{
    public async Task<AmenityDto> Handle(UpdateAmenityCommand request, CancellationToken cancellationToken)
    {
        var amenity = await amenityRepository.GetByIdAsync(request.AmenityId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Amenity), request.AmenityId);

        amenity.Rename(request.Name);

        amenityRepository.Update(amenity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AmenityDto(amenity.Id, amenity.Name, amenity.CreatedAt, amenity.ModifiedAt);
    }
}