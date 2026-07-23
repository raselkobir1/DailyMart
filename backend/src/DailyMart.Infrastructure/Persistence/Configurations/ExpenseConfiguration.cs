using DailyMart.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Amount).HasColumnType("numeric(12,2)");
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ExpenseDate).IsRequired();

        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.Category);
    }
}
