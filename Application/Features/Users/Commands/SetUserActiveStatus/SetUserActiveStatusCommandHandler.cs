using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands.SetUserActiveStatus;

public class SetUserActiveStatusCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<SetUserActiveStatusCommand>
{
    public async Task Handle(SetUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsActive && currentUserService.UserId == request.UserId)
            throw new ForbiddenAccessException("You cannot deactivate your own account.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(User), request.UserId);

        if (request.IsActive) user.Activate();
        else user.Deactivate();

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}