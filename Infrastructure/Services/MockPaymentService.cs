using System.Collections.Concurrent;
using Application.Common.Interfaces.Services;
using Application.Common.Models;

namespace Infrastructure.Services;

/// <summary>
/// Deliberate stand-in for a real payment provider (out of scope per the brief). It approves any
/// well-formed request and issues a fake transaction id; missing card token or non-positive amount
/// are rejected so the failure path stays exercisable end to end. Charges are idempotent on the
/// booking id (the idempotency key): a repeat charge for the same booking returns the original
/// result instead of charging again, so a double-submit or retry can never take a second payment.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    // Keyed on the idempotency key (the booking id). Process-wide so retries across requests dedupe.
    private static readonly ConcurrentDictionary<Guid, PaymentResult> Charges = new();

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CardToken))
            return Task.FromResult(new PaymentResult(false, null, "Missing card token."));

        if (request.Amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Amount must be greater than zero."));

        // Idempotency: the first successful charge for a booking wins; repeats return that same result.
        var result = Charges.GetOrAdd(
            request.IdempotencyKey,
            _ => new PaymentResult(true, $"MOCK-{Guid.NewGuid():N}", null));
        return Task.FromResult(result);
    }
}
