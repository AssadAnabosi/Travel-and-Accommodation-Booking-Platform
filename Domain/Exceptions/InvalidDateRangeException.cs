namespace Domain.Exceptions;

public sealed class InvalidDateRangeException(string message) : DomainException(message);