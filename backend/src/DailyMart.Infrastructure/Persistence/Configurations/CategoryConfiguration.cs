using DailyMart.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        // Partial (filtered) unique index, composite with TenantId - two different companies can
        // both have a category named "Grocery"; without the filter, a soft-deleted category would
        // also permanently block a new one from reusing its name within the same tenant.
        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique().HasFilter("is_deleted = false");
    }
}
