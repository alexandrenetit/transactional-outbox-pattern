using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSample.Infrastructure.Persistence.Inbox;

namespace SalesSample.Infrastructure.Persistence.Mapping;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(m => m.EventId);        // PK = EventId → não aceita duplicado
        builder.Property(m => m.ProcessedOn).IsRequired();
    }
}