using System.Diagnostics;
using Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUserService.UserId?.ToString() ?? "anonymous";
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        logger.LogInformation("Handled {RequestName} for user {UserId} in {ElapsedMs}ms",
            requestName, userId, stopwatch.ElapsedMilliseconds);

        return response;
    }
}