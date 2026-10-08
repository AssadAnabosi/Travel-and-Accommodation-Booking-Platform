namespace Application.Features.Users.Queries.GetUserById;

public record UserDetailsDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    int OwnedHotelsCount,
    int BookingsCount);