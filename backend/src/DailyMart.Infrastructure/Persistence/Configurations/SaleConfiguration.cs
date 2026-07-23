using DailyMart.Domain.Customers;
using DailyMart.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PaymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.SubtotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.VatAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.TotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(s => s.PaidAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.DueAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.TotalCost).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.ProfitAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.SaleDate).IsRequired();

        // Nullable/Restrict - a walk-in Cash sale has no customer at all (see Sale's doc comment).
        builder.HasOne<Customer>().WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
