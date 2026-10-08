using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Amenities.Commands.CreateAmenity;

public class CreateAmenityCommandHandler(IAmenityRepository amenityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAmenityCommand, AmenityDto>
{
    public async Task<AmenityDto> Handle(CreateAmenityCommand request, CancellationToken cancellationToken)
    {
        var amenity = Amenity.Create(request.Name);

        await amenityRepository.AddAsync(amenity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AmenityDto(amenity.Id, amenity.Name, amenity.CreatedAt, amenity.ModifiedAt);
    }
}