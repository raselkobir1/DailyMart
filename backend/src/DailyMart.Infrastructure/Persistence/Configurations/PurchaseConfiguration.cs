using DailyMart.Domain.Purchases;
using DailyMart.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.SubtotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(p => p.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.VatAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.TotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(p => p.PaidAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.DueAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.PurchaseDate).IsRequired();

        builder.HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
