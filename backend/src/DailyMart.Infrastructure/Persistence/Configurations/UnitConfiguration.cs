using DailyMart.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Symbol).HasMaxLength(10).IsRequired();

        // Partial (filtered) unique index - see UserConfiguration for why: without the filter, a
        // soft-deleted unit would permanently block a new unit from reusing its name.
        builder.HasIndex(u => u.Name).IsUnique().HasFilter("is_deleted = false");
    }
}
