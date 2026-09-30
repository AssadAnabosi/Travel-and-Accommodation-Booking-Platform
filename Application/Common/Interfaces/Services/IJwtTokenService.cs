using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(); // opaque random string, not a JWT
    string HashRefreshToken(string token);

    DateTime GetRefreshTokenExpiry();
}