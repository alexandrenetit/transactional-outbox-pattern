using SalesSample.Domain.Entities;

namespace SalesSample.Domain.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}