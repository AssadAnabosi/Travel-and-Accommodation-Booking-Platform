namespace API.Contracts;

// API-facing auth payload: the same data as the Application's AuthResponse, minus the refresh
// token — that now travels in an HttpOnly cookie instead of the response body.
// This would keep it safe from being accessed by JS.
public record AuthResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string AccessToken);
