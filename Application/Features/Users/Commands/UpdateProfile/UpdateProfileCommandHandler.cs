using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Queries.GetMyProfile;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException(nameof(User), userId);

        user.UpdateProfile(request.FirstName, request.LastName);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
    }
}