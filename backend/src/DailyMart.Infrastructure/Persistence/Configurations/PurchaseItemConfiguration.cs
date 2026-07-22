using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");

        builder.HasIndex(i => i.PurchaseId);

        // Cascade - the one deliberate exception to this project's usual Restrict: a line item has no
        // life apart from its purchase (see Module 7 Step 1).
        builder.HasOne<Purchase>().WithMany().HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
