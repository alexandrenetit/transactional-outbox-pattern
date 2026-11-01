using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Repositories;

namespace SalesSample.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly SalesDbContext _context;

    public ProductRepository(SalesDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
}