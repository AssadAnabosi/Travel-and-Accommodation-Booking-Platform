using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Commands.ChangePassword;

[Authorize]
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;