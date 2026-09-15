using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Cities.Commands.DeleteCity;

public class DeleteCityCommandHandler(ICityRepository cityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCityCommand>
{
    public async Task Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetByIdAsync(request.CityId, cancellationToken)
                   ?? throw new NotFoundException(nameof(City), request.CityId);

        if (await cityRepository.HasHotelsAsync(city.Id, cancellationToken))
            throw new ConflictException("Cannot delete a city that still has hotels assigned to it.");

        cityRepository.Remove(city);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}