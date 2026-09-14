using System.Reflection;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Services;
using Application.Common.Security;
using MediatR;

namespace Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var attributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>().ToList();

        if (attributes.Count != 0)
        {
            if (currentUserService.UserId is null)
                throw new UnauthorizedException("Authentication is required for this action.");

            var roleRestricted = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles)).ToList();
            if (roleRestricted.Count != 0)
            {
                var authorized = roleRestricted
                    .SelectMany(a => a.Roles!.Split(','))
                    .Any(role => currentUserService.IsInRole(role.Trim()));

                if (!authorized)
                    throw new ForbiddenAccessException();
            }
        }

        return await next();
    }
}