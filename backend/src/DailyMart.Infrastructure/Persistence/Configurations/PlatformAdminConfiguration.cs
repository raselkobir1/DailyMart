using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("platform_admins");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Username).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        // Global unique, same reasoning as User.Username - platform admins aren't tenant-scoped at all.
        builder.HasIndex(p => p.Username).IsUnique().HasFilter("is_deleted = false");
    }
}
