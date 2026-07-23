using DailyMart.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SaleReturnItemConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.ToTable("sale_return_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(12,2)");

        builder.HasIndex(i => i.SaleReturnId);

        // Cascade - same reasoning as SaleItem: no life apart from its parent return.
        builder.HasOne<SaleReturn>().WithMany().HasForeignKey(i => i.SaleReturnId).OnDelete(DeleteBehavior.Cascade);

        // Restrict - references the original sale line, an independent prior record.
        builder.HasOne<SaleItem>().WithMany().HasForeignKey(i => i.SaleItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
