using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Services;

namespace Infrastructure.UnitTests.Services;

public class MockPaymentGatewayTests
{
    private readonly MockPaymentGateway _gateway = new();

    private static PaymentRequest Request(decimal amount = 100m, string cardToken = "tok_visa") =>
        new(Guid.NewGuid(), amount, "USD", cardToken);

    [Fact]
    public async Task ChargeAsync_ValidRequest_SucceedsWithTransactionId()
    {
        var result = await _gateway.ChargeAsync(Request());

        result.Success.Should().BeTrue();
        result.TransactionId.Should().StartWith("MOCK-");
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ChargeAsync_MissingCardToken_Fails()
    {
        var result = await _gateway.ChargeAsync(Request(cardToken: " "));

        result.Success.Should().BeFalse();
        result.TransactionId.Should().BeNull();
        result.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ChargeAsync_NonPositiveAmount_Fails()
    {
        var result = await _gateway.ChargeAsync(Request(amount: 0m));

        result.Success.Should().BeFalse();
    }
}