using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Amenities.Commands.DeleteAmenity;

public class DeleteAmenityCommandHandler(IAmenityRepository amenityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAmenityCommand>
{
    public async Task Handle(DeleteAmenityCommand request, CancellationToken cancellationToken)
    {
        var amenity = await amenityRepository.GetByIdAsync(request.AmenityId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Amenity), request.AmenityId);

        if (await amenityRepository.IsInUseAsync(amenity.Id, cancellationToken))
            throw new ConflictException("Cannot delete an amenity that is currently assigned to one or more hotels.");

        amenityRepository.Remove(amenity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}