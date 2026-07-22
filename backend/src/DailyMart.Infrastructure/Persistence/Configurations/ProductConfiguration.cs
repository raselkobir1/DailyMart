using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        builder.Property(p => p.PurchasePrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.SellingPrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.WholesalePrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.DiscountPercentage).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
        builder.Property(p => p.TaxPercentage).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
        builder.Property(p => p.CurrentStock).HasColumnType("numeric(18,3)").HasDefaultValue(0m);
        builder.Property(p => p.MinimumStock).HasColumnType("numeric(18,3)").HasDefaultValue(0m);
        builder.Property(p => p.AllowPriceBelowCost).HasDefaultValue(false);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);

        // Partial (filtered) unique indexes - see UserConfiguration (Module 1) for why: without the
        // filter, a soft-deleted product would permanently block a new one from reusing its code/barcode.
        builder.HasIndex(p => p.Code).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.Barcode).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.Name);

        // Restrict, not Cascade: deleting a Category/Brand/Unit should never silently delete products.
        // In practice this FK behavior rarely even triggers - normal deletes go through Module 0's
        // soft-delete interceptor, which issues an UPDATE, not a DELETE.
        builder.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Brand>().WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(p => p.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
