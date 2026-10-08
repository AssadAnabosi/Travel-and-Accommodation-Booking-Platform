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

/// <summary>User administration (list, role, active status) and self-service profile/password.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ISender sender) : ControllerBase
{
    /// <summary>Lists users (paginated, filterable).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<UserListItemDto>>> GetUsers(
        [FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Gets a user's details.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));

    /// <summary>Updates the current user's profile.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        UpdateProfileCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    /// <summary>Changes the current user's password; 401 if the current password is wrong.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Changes a user's role (an admin cannot demote themselves).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteRole(
        PromoteUserRoleCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Activates or deactivates a user.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActiveStatus(
        SetUserActiveStatusCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
