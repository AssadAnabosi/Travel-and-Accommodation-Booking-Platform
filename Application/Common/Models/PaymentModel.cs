namespace Application.Common.Models;

public record PaymentRequest(Guid BookingId, decimal Amount, string Currency, string CardToken);

public record PaymentResult(bool Success, string? TransactionId, string? FailureReason);