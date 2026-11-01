using SalesSample.Domain.Common;

namespace SalesSample.Domain.Entities;

public class Customer : BaseEntity
{
    protected Customer() { } // EF Core

    public Customer(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Name is reuired.", nameof(name))
        : name;
    }
    public string Name { get; private set; } = null!;
}