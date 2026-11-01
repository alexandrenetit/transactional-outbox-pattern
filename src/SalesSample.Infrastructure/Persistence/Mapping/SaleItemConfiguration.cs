using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSample.Domain.Entities;

namespace SalesSample.Infrastructure.Persistence.Mapping;

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Quantity).IsRequired();
        builder.Property(si => si.Discount).HasColumnType("decimal(5,4)").IsRequired();

        builder.HasOne(si => si.Product)
            .WithMany()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(si => si.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
        });

        builder.Ignore(si => si.TotalAmount);   // calculado
    }
}