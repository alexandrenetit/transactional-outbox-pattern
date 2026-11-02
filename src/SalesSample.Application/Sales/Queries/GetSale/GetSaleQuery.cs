using MediatR;

namespace SalesSample.Application.Sales.Queries.GetSale;

public sealed record GetSaleQuery(Guid SaleId) : IRequest<GetSaleResult>;