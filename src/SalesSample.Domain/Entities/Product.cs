using SalesSample.Domain.Common;
using SalesSample.Domain.ValueObjects;

namespace SalesSample.Domain.Entities;

public class Product : BaseEntity
{
    protected Product() { } // EF Core

    public Product(string name, Money unitPrice)
    {
        Name = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Name is required.", nameof(name))
        : name;
        UnitPrice = unitPrice ?? throw new ArgumentException(nameof(unitPrice));
    }

    public string Name { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;
}