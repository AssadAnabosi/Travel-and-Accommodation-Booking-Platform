using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.Common;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiry = jwtTokenService.GetRefreshTokenExpiry();
        var refreshToken = user.IssueRefreshToken(jwtTokenService.HashRefreshToken(refreshTokenValue),
            refreshTokenExpiry);

        userRepository.AddRefreshToken(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            accessToken, refreshTokenValue, refreshToken.ExpiresAt);
    }
}