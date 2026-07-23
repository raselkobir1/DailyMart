using DailyMart.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.CurrentDue).HasColumnType("numeric(12,2)").HasDefaultValue(0m);

        // Filtered unique index on Phone, not Name - see Module 6 Step 1's scope decision. Multiple NULLs
        // (customers with no phone on file) never collide in a Postgres unique index by default.
        builder.HasIndex(c => c.Phone).IsUnique().HasFilter("is_deleted = false");
    }
}
