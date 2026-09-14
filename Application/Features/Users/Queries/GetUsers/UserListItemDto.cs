namespace Application.Features.Users.Queries.GetUsers;

public record UserListItemDto(Guid Id, string Email, string FirstName, string LastName, string Role, bool IsActive, DateTime CreatedAt);