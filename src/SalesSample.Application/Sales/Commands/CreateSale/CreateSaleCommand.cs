using MediatR;

namespace SalesSample.Application.Sales.Commands.CreateSale;

public sealed record CreateSaleCommand(
    Guid CustomerId,
    Guid BranchId,
    string? SaleNumber,
    IReadOnlyCollection<CreateSaleItem> Items) : IRequest<CreateSaleResult>;