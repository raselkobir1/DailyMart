using DailyMart.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("purchase_returns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TotalAmount).HasColumnType("numeric(12,2)");
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.ReturnDate).IsRequired();

        // Restrict - the original purchase is an independent prior document, not owned by this return.
        builder.HasOne<Purchase>().WithMany().HasForeignKey(r => r.PurchaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
