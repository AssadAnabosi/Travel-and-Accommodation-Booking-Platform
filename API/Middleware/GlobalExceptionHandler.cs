using Application.Common.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = Map(exception);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Application exceptions carry safe, user-facing messages; unexpected ones do not.
            Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title) Map(Exception exception) => exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
        UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
        PaymentFailedException => (StatusCodes.Status402PaymentRequired, "Payment required"),
        // Domain rule violations carry safe, user-facing messages too.
        RoomNotAvailableException => (StatusCodes.Status409Conflict, "Conflict"),
        DomainException => (StatusCodes.Status400BadRequest, "Business rule violation"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
