using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Commands.SetUserActiveStatus;

[Authorize(Roles = "Admin")]
public record SetUserActiveStatusCommand(Guid UserId, bool IsActive) : IRequest;