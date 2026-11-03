using SalesSample.Domain.ValueObjects;
using Shouldly;

namespace SalesSample.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void DeveSerIgualPorValor()
    {
        var a = new Money(10m, "BRL");
        var b = new Money(10m, "BRL");

        a.ShouldBe(b);
    }

    [Fact]
    public void Soma_DeveSomarMesmaMoeda()
    {
        var result = new Money(10m, "BRL") + new Money(5m, "BRL");

        result.ShouldBe(new Money(15m, "BRL"));
    }

    [Fact]
    public void Soma_DeveLancarComMoedasDiferentes()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            _ = new Money(10m, "BRL") + new Money(5m, "USD");
        });
    }

    [Fact]
    public void Multiplicacao_DeveMultiplicarAmount()
    {
        var result = new Money(10m, "BRL") * 3;

        result.Amount.ShouldBe(30m);
    }

    [Fact]
    public void Zero_DeveRetornarZeroNaMoeda()
    {
        Money.Zero("BRL").ShouldBe(new Money(0m, "BRL"));
    }
}
