using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await userRepository.GetByIdWithCountsAsync(request.UserId, cancellationToken)
                     ?? throw new NotFoundException(nameof(User), request.UserId);

        var user = result.User;
        return new UserDetailsDto(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), user.IsActive, user.CreatedAt, user.ModifiedAt,
            result.OwnedHotelsCount, result.BookingsCount);
    }
}