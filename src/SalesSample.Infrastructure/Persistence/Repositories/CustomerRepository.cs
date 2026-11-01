using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Repositories;

namespace SalesSample.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly SalesDbContext _context;

    public CustomerRepository(SalesDbContext context) => _context = context;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}