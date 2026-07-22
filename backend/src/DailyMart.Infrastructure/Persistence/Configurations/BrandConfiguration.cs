using DailyMart.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500);

        // Partial (filtered) unique index - see UserConfiguration for why: without the filter, a
        // soft-deleted brand would permanently block a new brand from reusing its name.
        builder.HasIndex(b => b.Name).IsUnique().HasFilter("is_deleted = false");
    }
}
