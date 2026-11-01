using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Common;
using SalesSample.Domain.Entities;
using SalesSample.Infrastructure.Persistence.Outbox;
using SalesSample.Domain.Repositories;

namespace SalesSample.Infrastructure.Persistence;

public sealed class SalesDbContext : DbContext, IUnitOfWork
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddOutboxMessages();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AddOutboxMessages();
        return base.SaveChanges();
    }

    private void AddOutboxMessages()
    {
        var entries = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        foreach (var entry in entries)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage(
                    domainEvent.EventId,
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    domainEvent.OccurredOn));
            }

            entry.Entity.ClearDomainEvents();
        }
    }
}