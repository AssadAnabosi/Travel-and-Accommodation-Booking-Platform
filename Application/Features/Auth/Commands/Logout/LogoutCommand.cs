using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string Token) : IRequest;