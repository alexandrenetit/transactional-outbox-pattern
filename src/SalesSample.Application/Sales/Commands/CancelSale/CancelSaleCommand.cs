using MediatR;

namespace SalesSample.Application.Sales.Commands.CancelSale;

public sealed record CancelSaleCommand(Guid SaleId) : IRequest;