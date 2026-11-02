namespace SalesSample.Application.Sales.Commands.CreateSale;

public sealed record CreateSaleResult(Guid SaleId, string SaleNumber, decimal TotalAmount);