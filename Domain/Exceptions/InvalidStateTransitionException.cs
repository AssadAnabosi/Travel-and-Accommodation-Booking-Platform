namespace Domain.Exceptions;

/// <summary>
/// An operation isn't allowed in the entity's current state (e.g. checking in a booking that
/// isn't confirmed, approving an already-approved hotel). Surfaces as 409 Conflict.
/// </summary>
public sealed class InvalidStateTransitionException(string message) : DomainException(message);
