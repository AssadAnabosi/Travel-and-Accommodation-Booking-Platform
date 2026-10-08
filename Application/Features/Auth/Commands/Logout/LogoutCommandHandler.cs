using Application.Common.Interfaces.Persistence;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshTokenAsync(request.Token, cancellationToken);
        var token = user?.FindActiveRefreshToken(request.Token);

        if (token is null) return;

        token.Revoke();
        userRepository.Update(user!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}