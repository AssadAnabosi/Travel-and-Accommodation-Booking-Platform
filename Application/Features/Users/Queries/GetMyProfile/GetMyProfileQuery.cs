using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Queries.GetMyProfile;

[Authorize]
public record GetMyProfileQuery : IRequest<UserProfileDto>;