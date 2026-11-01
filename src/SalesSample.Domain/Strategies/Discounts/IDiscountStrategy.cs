namespace SalesSample.Domain.Strategies.Discounts;

public interface IDiscountStrategy
{
    decimal CalculateDiscount(int quantity);
}