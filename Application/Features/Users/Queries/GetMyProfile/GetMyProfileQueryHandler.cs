using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries.GetMyProfile;

public class GetMyProfileQueryHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId =
            currentUserService.UserId!.Value; // guaranteed non-null — AuthorizationBehavior already enforced it
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException(nameof(User), userId);

        return new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
    }
}