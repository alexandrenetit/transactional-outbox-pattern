using SalesSample.Domain.Entities;
using SalesSample.Domain.Enums;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.ValueObjects;
using Shouldly;

namespace SalesSample.UnitTests.Domain;

public sealed class SaleTests
{
    private static Sale CreateSale() =>
        new(new Customer("Fazenda Santa Fé"), new Branch("Filial São Paulo"), "SALE-TEST-001");

    private static Product Product(string name = "Fertilizante NPK", decimal price = 100m) =>
        new(name, new Money(price, "BRL"));

    [Fact]
    public void AddItem_DeveAdicionar()
    {
        var sale = CreateSale();

        sale.AddItem(Product(), 5);

        sale.Items.ShouldHaveSingleItem();
        sale.Items.First().Quantity.ShouldBe(5);
    }

    [Fact]
    public void AddItem_DeveLancarAcimaDe20Iguais()
    {
        var sale = CreateSale();
        var product = Product();

        sale.AddItem(product, 15);

        var act = () => sale.AddItem(product, 6);   // 15 + 6 = 21 > 20

        var ex = Should.Throw<DomainException>(act);
        ex.Message.ShouldContain("20");
    }

    [Fact]
    public void AddItem_DevePermitirExatamente20Iguais()
    {
        var sale = CreateSale();

        sale.AddItem(Product(), 20);

        sale.Items.Sum(i => i.Quantity).ShouldBe(20);
    }

    [Fact]
    public void AddItem_DeveLancarEmVendaCancelada()
    {
        var sale = CreateSale();
        sale.Cancel();

        var act = () => sale.AddItem(Product(), 1);

        var ex = Should.Throw<DomainException>(act);
        ex.Message.ShouldContain("cancelled");
    }

    [Fact]
    public void Cancel_DeveMudarStatus()
    {
        var sale = CreateSale();

        sale.Cancel();

        sale.Status.ShouldBe(SaleStatus.Cancelled);
    }

    [Fact]
    public void Cancel_DeveLancarQuandoJaCancelada()
    {
        var sale = CreateSale();
        sale.Cancel();

        var act = () => sale.Cancel();

        var ex = Should.Throw<DomainException>(act);
        ex.Message.ShouldContain("already cancelled");
    }

    [Fact]
    public void TotalAmount_DeveSomarComDesconto()
    {
        var sale = CreateSale();
        sale.AddItem(Product(price: 100m), 5);          // 5  -> 10% -> 450
        sale.AddItem(Product("Semente", 200m), 10);     // 10 -> 20% -> 1600

        sale.TotalAmount.Amount.ShouldBe(2050m);
    }
}
