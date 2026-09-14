using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}