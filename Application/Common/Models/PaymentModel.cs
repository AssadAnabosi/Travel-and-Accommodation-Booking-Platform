namespace Application.Common.Models;

// BookingId doubles as the payment idempotency key: a booking can only ever be charged once.
public record PaymentRequest(Guid BookingId, decimal Amount, string Currency, string CardToken)
{
    public Guid IdempotencyKey => BookingId;
}

public record PaymentResult(bool Success, string? TransactionId, string? FailureReason);