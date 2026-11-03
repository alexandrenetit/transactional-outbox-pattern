using SalesSample.Domain.Strategies.Discounts;
using Shouldly;

namespace SalesSample.UnitTests.Domain;

public sealed class StandardDiscountStrategyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0.1)]   // borda inferior da faixa 10%
    [InlineData(9, 0.1)]
    [InlineData(10, 0.2)]  // borda inferior da faixa 20%
    [InlineData(20, 0.2)]  // borda superior permitida
    [InlineData(21, 0)]    // acima do limite -> 0%
    public void CalculateDiscount_DeveSeguirFaixas(int quantity, double expected)
    {
        var strategy = new StandardDiscountStrategy();

        strategy.CalculateDiscount(quantity).ShouldBe((decimal)expected);
    }
}
