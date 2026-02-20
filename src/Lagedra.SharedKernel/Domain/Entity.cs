namespace Lagedra.SharedKernel.Domain;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    // Required by EF Core (parameterless ctor)
#pragma warning disable CS8618
    protected Entity() { }
#pragma warning restore CS8618

    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
