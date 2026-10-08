using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(nameof(request.Email),
                    "An account with this email already exists.")
            });

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(normalizedEmail, passwordHash, request.FirstName, request.LastName);

        await userRepository.AddAsync(user, cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiry = jwtTokenService.GetRefreshTokenExpiry();
        var refreshToken = user.IssueRefreshToken(refreshTokenValue, refreshTokenExpiry);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }
}