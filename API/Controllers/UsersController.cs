using Application.Common.Models;
using Application.Features.Users.Commands.ChangePassword;
using Application.Features.Users.Commands.PromoteUserRole;
using Application.Features.Users.Commands.SetUserActiveStatus;
using Application.Features.Users.Commands.UpdateProfile;
using Application.Features.Users.Queries.GetMyProfile;
using Application.Features.Users.Queries.GetUserById;
using Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PaginatedList<UserListItemDto>>> GetUsers(
        [FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        UpdateProfileCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("role")]
    public async Task<IActionResult> PromoteRole(
        PromoteUserRoleCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("status")]
    public async Task<IActionResult> SetActiveStatus(
        SetUserActiveStatusCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
