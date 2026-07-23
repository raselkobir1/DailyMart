using DailyMart.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("sale_returns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.ReturnDate).IsRequired();

        // Restrict - the original sale is an independent prior document, not owned by this return.
        builder.HasOne<Sale>().WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
    }
}
