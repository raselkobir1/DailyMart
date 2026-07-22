using DailyMart.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SupplierLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierLedgerEntry> builder)
    {
        builder.ToTable("supplier_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasColumnType("numeric(12,2)");
        builder.Property(e => e.BalanceAfter).HasColumnType("numeric(12,2)");
        builder.Property(e => e.TransactionDate).IsRequired();

        builder.HasIndex(e => e.SupplierId);

        // Restrict, not Cascade - same reasoning as Product's FKs (Module 4): normal deletes go through
        // Module 0's soft-delete interceptor (an UPDATE, never a real DELETE) anyway.
        builder.HasOne<Supplier>().WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
