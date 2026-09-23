using System.Net;
using System.Net.Sockets;
using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Services;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

// AI GENERATED BTW... basiclly copied from somewhere on the internet

namespace Infrastructure.UnitTests.Services;

public class SmtpEmailServiceTests
{
    private static readonly SmtpSettings Settings = new()
    {
        Host = "127.0.0.1",
        Port = 1025,
        Security = SecureSocketOptions.None,
        FromAddress = "no-reply@tabp.dev",
        FromName = "TABP Hotels",
        TimeoutMs = 2_000
    };

    [Fact]
    public void BuildMimeMessage_MapsSenderRecipientSubjectAndHtmlBody()
    {
        var message = new EmailMessage("guest@example.com", "Booking confirmed", "<p>Thanks</p>");

        var mime = SmtpEmailService.BuildMimeMessage(message, Settings);

        mime.From.Mailboxes.Single().Address.Should().Be("no-reply@tabp.dev");
        mime.From.Mailboxes.Single().Name.Should().Be("TABP Hotels");
        mime.To.Mailboxes.Single().Address.Should().Be("guest@example.com");
        mime.Subject.Should().Be("Booking confirmed");
        mime.HtmlBody.Should().Be("<p>Thanks</p>");
        mime.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void BuildMimeMessage_WithAttachment_AttachesItUnderItsFileName()
    {
        var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        var message = new EmailMessage("guest@example.com", "Booking confirmed", "<p>Thanks</p>", pdf,
            "booking-confirmation.pdf");

        var mime = SmtpEmailService.BuildMimeMessage(message, Settings);

        var attachment = mime.Attachments.OfType<MimePart>().Single();
        attachment.FileName.Should().Be("booking-confirmation.pdf");
        attachment.ContentType.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task SendAsync_WhenSmtpServerIsUnreachable_LogsAndDoesNotThrow()
    {
        var service = new SmtpEmailService(new SmtpSettings
        {
            Host = "127.0.0.1",
            Port = GetUnusedPort(),
            Security = SecureSocketOptions.None,
            TimeoutMs = 2_000
        }, NullLogger<SmtpEmailService>.Instance);

        var act = () => service.SendAsync(new EmailMessage("guest@example.com", "Subject", "<p>Body</p>"));

        await act.Should().NotThrowAsync();
    }

    private static int GetUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}