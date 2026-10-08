using Application.Common.Interfaces.Services;
using Application.Common.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Services;

/// <summary>
/// Sends email over SMTP with MailKit (locally: the MailHog container from compose.yaml).
/// Best-effort by contract (see <see cref="IEmailService"/>): a delivery failure is logged, never
/// thrown, so e.g. a paid booking confirmation doesn't turn into a 500 because the mail server is down.
/// </summary>
public class SmtpEmailService(SmtpSettings settings, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient { Timeout = settings.TimeoutMs };
            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
            if (!string.IsNullOrEmpty(settings.Username))
                await client.AuthenticateAsync(settings.Username, settings.Password ?? "", cancellationToken);

            await client.SendAsync(BuildMimeMessage(message, settings), cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Email sent -> To: {ToAddress}, Subject: {Subject}", message.ToAddress, message.Subject);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Email delivery failed -> To: {ToAddress}, Subject: {Subject}; continuing without it",
                message.ToAddress, message.Subject);
        }
    }

    public static MimeMessage BuildMimeMessage(EmailMessage message, SmtpSettings settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.ToAddress));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (message.Attachment is { Length: > 0 })
            body.Attachments.Add(message.AttachmentFileName ?? "attachment", message.Attachment);

        mime.Body = body.ToMessageBody();
        return mime;
    }
}
