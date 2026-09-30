using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.Token);

        var user = await userRepository.GetByRefreshTokenAsync(tokenHash, cancellationToken);
        var token = user?.FindActiveRefreshToken(tokenHash);

        if (token is null) return;

        token.Revoke();
        userRepository.Update(user!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}