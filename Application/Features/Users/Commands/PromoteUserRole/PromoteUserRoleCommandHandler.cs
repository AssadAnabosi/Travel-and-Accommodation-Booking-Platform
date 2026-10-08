using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands.PromoteUserRole;

public class PromoteUserRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<PromoteUserRoleCommand>
{
    public async Task Handle(PromoteUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(User), request.UserId);

        // Guard on the CURRENT role, before mutating it — an Admin cannot demote themselves.
        if (currentUserService.UserId == request.UserId
            && user.Role == UserRole.Admin
            && request.NewRole != UserRole.Admin)
        {
            throw new ForbiddenAccessException("You cannot remove your own Admin role.");
        }

        user.PromoteToRole(request.NewRole);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}