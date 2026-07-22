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

        // Partial (filtered) unique index - see UserConfiguration for why: without the filter, a
        // soft-deleted category would permanently block a new category from reusing its name.
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("is_deleted = false");
    }
}
