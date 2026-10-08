using Application.Common.Interfaces.Persistence;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Hotels.Commands.ReassignHotelOwner;

public class ReassignHotelOwnerCommandValidator : AbstractValidator<ReassignHotelOwnerCommand>
{
    private readonly IUserRepository _userRepository;

    public ReassignHotelOwnerCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.NewOwnerId)
            .NotEmpty()
            .MustAsync(BeAnActiveHotelOwner)
            .WithMessage("The new owner must be an active user holding the HotelOwner role.");
    }

    private async Task<bool> BeAnActiveHotelOwner(Guid ownerId, CancellationToken cancellationToken)
    {
        var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
        return owner is { Role: UserRole.HotelOwner, IsActive: true };
    }
}
