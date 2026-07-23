using DailyMart.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
    {
        builder.ToTable("customer_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasColumnType("numeric(12,2)");
        builder.Property(e => e.BalanceAfter).HasColumnType("numeric(12,2)");
        builder.Property(e => e.TransactionDate).IsRequired();

        builder.HasIndex(e => e.CustomerId);

        // Restrict, not Cascade - same reasoning as SupplierLedgerEntry: normal deletes go through Module
        // 0's soft-delete interceptor (an UPDATE, never a real DELETE) anyway.
        builder.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
