using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Development/default email sink: logs the message instead of dispatching it.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email suppressed (logging sink) -> To: {ToAddress}, Subject: {Subject}, Attachment: {AttachmentBytes} bytes",
            message.ToAddress, message.Subject, message.Attachment?.Length ?? 0);

        return Task.CompletedTask;
    }
}