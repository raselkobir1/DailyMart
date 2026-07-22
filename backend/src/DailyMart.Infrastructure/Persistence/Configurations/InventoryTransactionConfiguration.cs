using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.QuantityChange).HasColumnType("numeric(18,3)");
        builder.Property(t => t.BalanceAfter).HasColumnType("numeric(18,3)");
        builder.Property(t => t.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.TransactionDate).IsRequired();

        builder.HasIndex(t => t.ProductId);
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });

        // Restrict - a product's transaction history must outlive normal deletes (which are soft
        // deletes anyway, per Module 0's interceptor).
        builder.HasOne<Product>().WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
