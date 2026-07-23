using DailyMart.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Key).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Label).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Route).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Icon).HasMaxLength(20).IsRequired();

        builder.HasIndex(m => m.Key).IsUnique().HasFilter("is_deleted = false");

        // Restrict, not Cascade - a parent menu with children can't be deleted at all (MenuService
        // enforces this explicitly), so cascading would never actually trigger, but Restrict is this
        // project's default for every other self-referencing/cross-entity FK.
        builder.HasOne<Menu>().WithMany().HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
