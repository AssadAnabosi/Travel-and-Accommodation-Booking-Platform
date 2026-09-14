using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Queries.GetUserById;

[Authorize(Roles = "Admin")]
public record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailsDto>;