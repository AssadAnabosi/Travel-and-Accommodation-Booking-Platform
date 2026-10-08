using Application.Features.Auth.Commands.Common;
using MediatR;

namespace Application.Features.Auth.Commands.Register;

// No "Role" field here on purpose — public self-registration always creates a Customer.
// HotelOwner assignment is an Admin-only action (a separate Users feature, not this endpoint).
public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<AuthResponse>;