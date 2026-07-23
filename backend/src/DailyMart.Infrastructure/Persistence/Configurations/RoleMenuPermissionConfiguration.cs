using DailyMart.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class RoleMenuPermissionConfiguration : IEntityTypeConfiguration<RoleMenuPermission>
{
    public void Configure(EntityTypeBuilder<RoleMenuPermission> builder)
    {
        builder.ToTable("role_menu_permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CanView).HasDefaultValue(false);
        builder.Property(p => p.CanCreate).HasDefaultValue(false);
        builder.Property(p => p.CanEdit).HasDefaultValue(false);
        builder.Property(p => p.CanDelete).HasDefaultValue(false);

        // One permission row per (role, menu) pair - RbacService gets-or-creates against this rather than
        // ever inserting a duplicate.
        builder.HasIndex(p => new { p.RoleId, p.MenuId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne<Role>().WithMany().HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Menu>().WithMany().HasForeignKey(p => p.MenuId).OnDelete(DeleteBehavior.Restrict);
    }
}
