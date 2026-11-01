namespace SalesSample.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj) =>
        obj is BaseEntity other && other.Id != Guid.Empty && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}