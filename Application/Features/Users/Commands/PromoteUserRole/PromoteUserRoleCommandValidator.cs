using FluentValidation;

namespace Application.Features.Users.Commands.PromoteUserRole;

public class PromoteUserRoleCommandValidator : AbstractValidator<PromoteUserRoleCommand>
{
    public PromoteUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}