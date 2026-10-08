using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        // Validation and Domain exceptions are expected control flow — the Api middleware
        // maps them to proper 4xx responses. Only truly unexpected failures get logged as errors here.
        catch (Exception ex) when (ex is not Exceptions.ValidationException && ex is not DomainException)
        {
            logger.LogError(ex, "Unhandled exception for request {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}