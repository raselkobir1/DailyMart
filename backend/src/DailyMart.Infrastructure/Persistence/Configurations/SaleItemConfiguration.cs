using DailyMart.Domain.Products;
using DailyMart.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
        builder.Property(i => i.UnitCost).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(i => i.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");

        builder.HasIndex(i => i.SaleId);

        // Cascade - the one deliberate exception to this project's usual Restrict: a line item has no life
        // apart from its sale (mirrors PurchaseItem, Module 7).
        builder.HasOne<Sale>().WithMany().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
