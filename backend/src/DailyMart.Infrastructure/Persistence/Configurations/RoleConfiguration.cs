using DailyMart.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        // Partial (filtered) unique index, composite with TenantId - every tenant needs its own
        // "Admin" role (see TenantProvisioningService), so Name can't be globally unique anymore.
        builder.HasIndex(r => new { r.TenantId, r.Name }).IsUnique().HasFilter("is_deleted = false");
    }
}
