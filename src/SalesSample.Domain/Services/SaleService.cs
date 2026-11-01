using SalesSample.Domain.Entities;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Specifications;

namespace SalesSample.Domain.Services;

public sealed class SaleService : ISaleService
{
    private readonly ISpecification<Branch> _activeBranch;

    public SaleService(ISpecification<Branch> activeBranch)
    {
        _activeBranch = activeBranch;
    }

    public Sale CreateSale(Customer customer, Branch branch, string saleNumber,
        IEnumerable<(Product product, int quantity)> items)
    {
        if (!_activeBranch.IsSatisfiedBy(branch))
            throw new AppDomainUnloadedException("Branch is not active.");

        var sale = new Sale(customer, branch, saleNumber);

        foreach (var (product, quantity) in items)
            sale.AddItem(product, quantity);

        return sale;
    }

    public void CancelSale(Sale sale) => sale.Cancel();
}