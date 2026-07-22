using DailyMart.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.ToTable("purchase_return_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");

        builder.HasIndex(i => i.PurchaseReturnId);

        // Cascade - same reasoning as PurchaseItem: no life apart from its parent return.
        builder.HasOne<PurchaseReturn>()
            .WithMany()
            .HasForeignKey(i => i.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict - references the original purchase line, an independent prior record.
        builder.HasOne<PurchaseItem>()
            .WithMany()
            .HasForeignKey(i => i.PurchaseItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
