using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Hotels.Commands.CreateHotel;

public class CreateHotelCommandValidator : AbstractValidator<CreateHotelCommand>
{
    private readonly ICityRepository _cityRepository;
    private readonly IUserRepository _userRepository;

    public CreateHotelCommandValidator(ICityRepository cityRepository, IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _cityRepository = cityRepository;
        _userRepository = userRepository;

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StarRating).InclusiveBetween(1, 5);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.CityId).MustAsync(CityExists).WithMessage("The specified city does not exist.");

        When(_ => currentUserService.IsInRole("Admin"), () =>
        {
            RuleFor(x => x.OwnerId)
                .NotNull().WithMessage("Admin must specify an OwnerId.");

            RuleFor(x => x.OwnerId)
                .MustAsync(BeAnExistingHotelOwner!)
                .WithMessage("The assigned user must already hold the HotelOwner role.")
                .When(x => x.OwnerId.HasValue);
        }).Otherwise(() =>
        {
            RuleFor(x => x.OwnerId)
                .Must(id => id is null)
                .WithMessage("Only an Admin may assign a hotel to a different owner.");
        });
    }

    private async Task<bool> CityExists(int cityId, CancellationToken cancellationToken)
    {
        var city = await _cityRepository.GetByIdAsync(cityId, cancellationToken);
        return city is not null;
    }

    private async Task<bool> BeAnExistingHotelOwner(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (ownerId is null) return false;
        var owner = await _userRepository.GetByIdAsync(ownerId.Value, cancellationToken);
        return owner is not null && owner.Role == UserRole.HotelOwner;
    }
}