using DailyMart.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.OpeningBalance).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
        builder.Property(s => s.CurrentDue).HasColumnType("numeric(12,2)").HasDefaultValue(0m);

        // Partial (filtered) unique index - see UserConfiguration (Module 1) for why.
        builder.HasIndex(s => s.Name).IsUnique().HasFilter("is_deleted = false");
    }
}
