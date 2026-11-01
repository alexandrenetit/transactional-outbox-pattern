using SalesSample.Domain.Common;

namespace SalesSample.Domain.Entities;

public class Branch : BaseEntity
{
    protected Branch() { }  // EF Core

    public Branch(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name;
    }

    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public void Deactive() => IsActive = false;
    public void Active() => IsActive = true;
}