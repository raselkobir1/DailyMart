using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("inventory_adjustments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdjustmentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.QuantityChange).HasColumnType("numeric(18,3)");
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.AdjustmentDate).IsRequired();

        builder.HasIndex(a => a.ProductId);

        builder.HasOne<Product>().WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
