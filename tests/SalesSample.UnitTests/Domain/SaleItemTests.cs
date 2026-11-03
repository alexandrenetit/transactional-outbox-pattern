using SalesSample.Domain.Entities;
using SalesSample.Domain.ValueObjects;
using Shouldly;

namespace SalesSample.UnitTests.Domain;

public sealed class SaleItemTests
{
    [Theory]
    [InlineData(2, 100, 0, 200)]      // sem desconto
    [InlineData(5, 100, 0.1, 450)]    // 10%
    [InlineData(10, 100, 0.2, 800)]   // 20%
    public void TotalAmount_DeveAplicarDesconto(int quantity, decimal price, double discount, decimal expected)
    {
        var item = new SaleItem(new Product("Produto", new Money(price, "BRL")), quantity);

        item.Discount.ShouldBe((decimal)discount);
        item.TotalAmount.Amount.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DeveLancarQuandoQuantidadeNaoPositiva(int quantity)
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new SaleItem(new Product("Produto", new Money(100m, "BRL")), quantity);
        });
    }
}
