using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Hotels.Commands.UpdateHotel;

public class UpdateHotelCommandValidator : AbstractValidator<UpdateHotelCommand>
{
    private readonly ICityRepository _cityRepository;

    public UpdateHotelCommandValidator(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;

        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StarRating).InclusiveBetween(1, 5);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.CityId).MustAsync(CityExists).WithMessage("The specified city does not exist.");
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }

    private async Task<bool> CityExists(int cityId, CancellationToken cancellationToken)
    {
        var city = await _cityRepository.GetByIdAsync(cityId, cancellationToken);
        return city is not null;
    }
}