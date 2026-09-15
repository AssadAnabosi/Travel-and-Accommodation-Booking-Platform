using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Cities.Commands.CreateCity;

public class CreateCityCommandHandler(ICityRepository cityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCityCommand, CityDto>
{
    public async Task<CityDto> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        var city = City.Create(request.Name, request.Country, request.PostOffice);

        await cityRepository.AddAsync(city, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CityDto(city.Id, city.Name, city.Country, city.PostOffice, HotelsCount: 0, city.CreatedAt,
            city.ModifiedAt);
    }
}