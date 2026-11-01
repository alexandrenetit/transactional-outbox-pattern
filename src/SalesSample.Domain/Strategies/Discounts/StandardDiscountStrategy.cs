namespace SalesSample.Domain.Strategies.Discounts;

public sealed class StandardDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(int quantity) =>
        quantity switch
        {
            >= 4 and < 10 => 0.1m, // 10% para 4-9 itens
            >= 10 and <= 20 => 0.2m, // 20% para 10-20 itens
            _               => 0m // Sem desconto para menos de 4 itens ou mais de 20 itens
        };
}