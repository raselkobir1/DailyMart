using DailyMart.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasColumnType("numeric(12,2)");
        builder.Property(p => p.BillingCycle).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.Name).IsUnique().HasFilter("is_deleted = false");
    }
}
