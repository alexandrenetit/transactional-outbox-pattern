using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Repositories;

namespace SalesSample.Infrastructure.Persistence.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly SalesDbContext _context;

    public SaleRepository(SalesDbContext context) => _context = context;

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Sales
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
        await _context.Sales.AddAsync(sale, cancellationToken);

    public void Update(Sale sale) => _context.Update(sale);

    public void Remove(Sale sale) => _context.Sales.Remove(sale);
}