using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSample.Domain.Entities;

namespace SalesSample.Infrastructure.Persistence.Mapping;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SaleDate).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey("CustomerId")
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey("BranchId")
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey("SaleId")
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Ignore(s => s.TotalAmount);   // calculado, não persiste
        builder.Ignore(s => s.DomainEvents); 

        builder.HasIndex(s => s.SaleNumber).IsUnique();
    }
}