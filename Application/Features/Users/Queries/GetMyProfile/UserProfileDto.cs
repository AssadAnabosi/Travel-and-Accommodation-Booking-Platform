namespace Application.Features.Users.Queries.GetMyProfile;

public record UserProfileDto(Guid Id, string Email, string FirstName, string LastName, string Role);