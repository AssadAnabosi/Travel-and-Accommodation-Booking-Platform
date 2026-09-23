using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Cities.Commands.UpdateCity;

public class UpdateCityCommandHandler(ICityRepository cityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCityCommand, CityDto>
{
    public async Task<CityDto> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetByIdAsync(request.CityId, cancellationToken)
                   ?? throw new NotFoundException(nameof(City), request.CityId);

        city.Update(request.Name, request.Country, request.PostOffice, request.ThumbnailUrl);

        cityRepository.Update(city);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CityDto(city.Id, city.Name, city.Country, city.PostOffice, city.ThumbnailUrl, city.Hotels.Count,
            city.CreatedAt,
            city.ModifiedAt);
    }
}