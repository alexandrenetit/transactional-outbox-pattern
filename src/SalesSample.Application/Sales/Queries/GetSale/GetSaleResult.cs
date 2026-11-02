namespace SalesSample.Application.Sales.Queries.GetSale;

public sealed record GetSaleResult(
    Guid SaleId,
    string SaleNumber,
    string Status,
    string CustomerName,
    string BranchName,
    decimal TotalAmount,
    IReadOnlyCollection<GetSaleItemResult> Items);

public sealed record GetSaleItemResult(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal TotalAmount);