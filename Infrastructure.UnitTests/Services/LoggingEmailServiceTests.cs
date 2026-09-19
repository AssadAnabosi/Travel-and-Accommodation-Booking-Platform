using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Infrastructure.UnitTests.Services;

public class LoggingEmailServiceTests
{
    [Fact]
    public async Task SendAsync_CompletesWithoutThrowing()
    {
        var service = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var message = new EmailMessage("guest@example.com", "Booking confirmed", "<p>Thanks</p>");

        var act = () => service.SendAsync(message);

        await act.Should().NotThrowAsync();
    }
}