using Application.Common.Security;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands.PromoteUserRole;

[Authorize(Roles = "Admin")]
public record PromoteUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest;