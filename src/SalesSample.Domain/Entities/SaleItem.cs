using SalesSample.Domain.Common;
using SalesSample.Domain.Strategies.Discounts;
using SalesSample.Domain.Strategies.Discounts.Enums;
using SalesSample.Domain.ValueObjects;

namespace SalesSample.Domain.Entities;

public class SaleItem : BaseEntity
{
    protected SaleItem() { }  // EF Core

    public SaleItem(Product product, int quantity)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        UnitPrice = product.UnitPrice;
        SetQuantity(quantity);
    }

    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public decimal Discount { get; private set; }

    public Money TotalAmount =>
        new(
            Math.Round(UnitPrice.Amount * Quantity * (1 - Discount), 2, MidpointRounding.AwayFromZero),
            UnitPrice.Currency);

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        Quantity = quantity;
        Discount = DiscountStrategyFactory
        .CreateStrategy(DiscountStrategyType.Standard)
        .CalculateDiscount(quantity);
    }
}