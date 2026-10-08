using Application.Common.Security;
using Application.Features.Users.Queries.GetMyProfile;
using MediatR;

namespace Application.Features.Users.Commands.UpdateProfile;

[Authorize]
public record UpdateProfileCommand(string FirstName, string LastName) : IRequest<UserProfileDto>;