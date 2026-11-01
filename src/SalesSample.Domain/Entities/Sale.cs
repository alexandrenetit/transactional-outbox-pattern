using SalesSample.Domain.Common;
using SalesSample.Domain.Enums;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.ValueObjects;
using SalesSample.Domain.Events;

namespace SalesSample.Domain.Entities;

public class Sale : AggregateRoot
{
    private const int MaxIdenticalItems = 20;
    private readonly List<SaleItem> _items = new();

    protected Sale() { } // EF Core

    public Sale(Customer customer, Branch branch, string saleNumber)
    {
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        SaleNumber = string.IsNullOrWhiteSpace(saleNumber)
            ? throw new ArgumentException("Sale number is required.", nameof(saleNumber))
            : saleNumber;
        SaleDate = DateTime.UtcNow;
        Status = SaleStatus.Created;

        AddDomainEvent(new SaleCreatedEvent(Id, saleNumber, Customer.Id, Branch.Id));
    }

    public string SaleNumber { get; private set; } = null!;
    public DateTime SaleDate { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public SaleStatus Status { get; private set; }
    public IReadOnlyCollection<SaleItem> ITems => _items.AsReadOnly();

    public Money TotalAmount
    {
        get
        {
            if (_items.Count == 0)
                return Money.Zero("USD");

            var currency = _items[0].TotalAmount.Currency;
            if (_items.Any(i => i.TotalAmount.Currency != currency))
                throw new AppDomainUnloadedException("Cannot sum items with different currencies.");

            return new Money(_items.Sum(i => i.TotalAmount.Amount), currency);
        }
    }

    public void AddItem(Product product, int quantity)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Cannot add item to a cancelled sale.");

        var totalForProduct = _items
        .Where(i => i.Product.Id == product.Id)
        .Sum(i => i.Quantity);

        if (totalForProduct + quantity > MaxIdenticalItems)
            throw new DomainException($"Cannot sell more than {MaxIdenticalItems} identical items.");

        _items.Add(new SaleItem(product, quantity));
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Sale is already cancelled.");

        Status = SaleStatus.Cancelled;
        AddDomainEvent(new SaleCancelledEvent(Id, SaleNumber));
    }
}