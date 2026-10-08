using Application.Common.Models;

namespace Application.Common.Interfaces.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email, best-effort: implementations log delivery failures instead of throwing, so a
    /// notification can never fail the operation that triggered it. Callers must HTML-encode any
    /// user-supplied text they put into <see cref="EmailMessage.HtmlBody"/>.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
