using SalesSample.Application.Sales.Commands.CreateSale;
using Shouldly;
using Xunit;

namespace SalesSample.UnitTests.Application;

public sealed class CreateSaleValidatorTests
{
    private readonly CreateSaleValidator _validator = new();

    private static CreateSaleCommand ComandoValido() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new[] { new CreateSaleItem(Guid.NewGuid(), 4) });

    [Fact]
    public void Validate_DeveSerValido_QuandoComandoValido()
    {
        // Act
        var result = _validator.Validate(ComandoValido());

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DeveFalhar_QuandoCustomerIdVazio()
    {
        // Arrange
        var command = ComandoValido() with { CustomerId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Validate_DeveFalhar_QuandoBranchIdVazio()
    {
        var command = ComandoValido() with { BranchId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "BranchId");
    }

    [Fact]
    public void Validate_DeveFalhar_QuandoItemsVazio()
    {
        var command = ComandoValido() with { Items = Array.Empty<CreateSaleItem>() };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_DeveFalhar_QuandoProductIdVazio()
    {
        var command = ComandoValido() with
        {
            Items = new[] { new CreateSaleItem(Guid.Empty, 4) }
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("ProductId"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_DeveFalhar_QuandoQuantidadeNaoPositiva(int quantity)
    {
        var command = ComandoValido() with
        {
            Items = new[] { new CreateSaleItem(Guid.NewGuid(), quantity) }
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("Quantity"));
    }
}