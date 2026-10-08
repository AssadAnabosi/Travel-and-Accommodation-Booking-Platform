using API.Contracts;
using Application.Common.Exceptions;
using Application.Features.Auth.Commands.Common;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Logout;
using Application.Features.Auth.Commands.RefreshToken;
using Application.Features.Auth.Commands.Register;
using Application.Features.Users.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMyProfileQuery(), cancellationToken));

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterCommand command, CancellationToken cancellationToken)
        => Ok(IssueTokens(await sender.Send(command, cancellationToken)));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginCommand command, CancellationToken cancellationToken)
        => Ok(IssueTokens(await sender.Send(command, cancellationToken)));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> Refresh(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(token))
            throw new UnauthorizedException("No refresh token was provided.");

        return Ok(IssueTokens(await sender.Send(new RefreshTokenCommand(token), cancellationToken)));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(token))
            await sender.Send(new LogoutCommand(token), cancellationToken);

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    // Writes the refresh token to an HttpOnly cookie and returns the rest of the payload in the body.
    private AuthResult IssueTokens(AuthResponse auth)
    {
        SetRefreshTokenCookie(auth.RefreshToken, auth.RefreshTokenExpiresAt);
        return new AuthResult(
            auth.UserId, auth.Email, auth.FirstName, auth.LastName, auth.Role, auth.AccessToken);
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt) =>
        Response.Cookies.Append(RefreshTokenCookie, token, BuildCookieOptions(expiresAt));

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, BuildCookieOptions(null));

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps, // always Secure behind HTTPS; relaxed for local HTTP dev
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth", // only sent to the auth endpoints
        Expires = expires
    };
}
