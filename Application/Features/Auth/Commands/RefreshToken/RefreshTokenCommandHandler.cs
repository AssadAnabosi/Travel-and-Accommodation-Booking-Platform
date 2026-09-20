using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.Common;
using MediatR;

namespace Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshTokenAsync(request.Token, cancellationToken)
                   ?? throw new UnauthorizedException("Invalid refresh token.");

        var existingToken = user.FindActiveRefreshToken(request.Token)
                            ?? throw new UnauthorizedException("Refresh token is expired or has been revoked.");

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        // Rotation: revoke the used token and issue a fresh one.
        var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = user.IssueRefreshToken(newRefreshTokenValue, jwtTokenService.GetRefreshTokenExpiry());
        existingToken.Revoke();

        var accessToken = jwtTokenService.GenerateAccessToken(user);

        // The used token was loaded tracked, so its Revoke() is persisted on save; only the new
        // token needs to be added explicitly.
        userRepository.AddRefreshToken(newRefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            accessToken, newRefreshToken.Token, newRefreshToken.ExpiresAt);
    }
}