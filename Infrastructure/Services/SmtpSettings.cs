using MailKit.Security;

namespace Infrastructure.Services;

/// <summary>
/// SMTP settings (config section <c>Smtp</c>). An empty <see cref="Host"/> means "not configured":
/// the logging email sink is used instead of <see cref="SmtpEmailService"/>.
/// </summary>
public class SmtpSettings
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 25;
    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.Auto;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromAddress { get; init; } = "no-reply@tabp.dev";
    public string FromName { get; init; } = "TABP Hotels";
    public int TimeoutMs { get; init; } = 10_000;
}
