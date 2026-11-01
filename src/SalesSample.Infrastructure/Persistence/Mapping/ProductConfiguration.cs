using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSample.Domain.Entities;

namespace SalesSample.Infrastructure.Persistence.Mapping;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

        builder.ComplexProperty(p => p.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });
    }
}