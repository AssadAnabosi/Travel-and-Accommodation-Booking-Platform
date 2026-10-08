namespace Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id is null ? 0 : Id.GetHashCode();

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right) => !(left == right);
}