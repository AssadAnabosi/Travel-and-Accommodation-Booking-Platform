namespace Domain.Exceptions;

public class InvalidDiscountException(string message) : DomainException(message);