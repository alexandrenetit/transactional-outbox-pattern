using SalesSample.Domain.Entities;

namespace SalesSample.Domain.Services;

public interface ISaleService
{
    Sale CreateSale(Customer customer, Branch branch, string? saleNumber,
        IEnumerable<(Product product, int quantity)> items);

    void CancelSale(Sale sale);
}