using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Repositories;

namespace SalesSample.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository : IBranchRepository
{
    private readonly SalesDbContext _context;

    public BranchRepository(SalesDbContext context) => _context = context;

    public async Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}