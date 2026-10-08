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
        var presentedTokenHash = jwtTokenService.HashRefreshToken(request.Token);

        var user = await userRepository.GetByRefreshTokenAsync(presentedTokenHash, cancellationToken)
                   ?? throw new UnauthorizedException("Invalid refresh token.");

        var existingToken = user.FindActiveRefreshToken(presentedTokenHash)
                            ?? throw new UnauthorizedException("Refresh token is expired or has been revoked.");

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        // Rotation: revoke the used token and issue a fresh one (only its hash is persisted).
        var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = user.IssueRefreshToken(jwtTokenService.HashRefreshToken(newRefreshTokenValue),
            jwtTokenService.GetRefreshTokenExpiry());
        existingToken.Revoke();

        var accessToken = jwtTokenService.GenerateAccessToken(user);

        // The used token was loaded tracked, so its Revoke() is persisted on save; only the new
        // token needs to be added explicitly.
        userRepository.AddRefreshToken(newRefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            accessToken, newRefreshTokenValue, newRefreshToken.ExpiresAt);
    }
}