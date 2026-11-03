using MediatR;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Repositories;

namespace SalesSample.Application.Sales.Queries.GetSale;

public sealed class GetSaleHandler : IRequestHandler<GetSaleQuery, GetSaleResult>
{
    private readonly ISaleRepository _saleRepository;

    public GetSaleHandler(ISaleRepository saleRepository) => _saleRepository = saleRepository;

    public async Task<GetSaleResult> Handle(GetSaleQuery query, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(query.SaleId, cancellationToken)
                   ?? throw new NotFoundException($"Sale {query.SaleId} not found.");

        return new GetSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status.ToString(),
            sale.Customer.Name,
            sale.Branch.Name,
            sale.TotalAmount.Amount,
            sale.Items.Select(i => new GetSaleItemResult(
                i.Product.Id,
                i.Product.Name,
                i.Quantity,
                i.UnitPrice.Amount,
                i.Discount,
                i.TotalAmount.Amount)).ToList());
    }
}