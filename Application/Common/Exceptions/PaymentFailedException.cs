namespace Application.Common.Exceptions;

public class PaymentFailedException(string reason) : Exception($"Payment failed: {reason}");