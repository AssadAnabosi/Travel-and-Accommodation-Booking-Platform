using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Hotels.Commands.UpdateHotelAmenities;

public class UpdateHotelAmenitiesCommandValidator : AbstractValidator<UpdateHotelAmenitiesCommand>
{
    private readonly IAmenityRepository _amenityRepository;

    public UpdateHotelAmenitiesCommandValidator(IAmenityRepository amenityRepository)
    {
        _amenityRepository = amenityRepository;

        RuleFor(x => x.HotelId).GreaterThan(0);

        RuleFor(x => x.AmenityIds)
            .NotNull()
            .MustAsync(AllExist).WithMessage("One or more amenity IDs do not exist.");
    }

    private async Task<bool> AllExist(IReadOnlyList<int> amenityIds, CancellationToken cancellationToken) =>
        amenityIds.Count == 0 || await _amenityRepository.AllExistAsync(amenityIds, cancellationToken);
}