using DailyMart.Domain.Rbac;
using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class TenantFeatureGrantConfiguration : IEntityTypeConfiguration<TenantFeatureGrant>
{
    public void Configure(EntityTypeBuilder<TenantFeatureGrant> builder)
    {
        builder.ToTable("tenant_feature_grants");

        builder.HasKey(g => g.Id);

        // One (active) grant per tenant+menu - re-granting an already-granted menu is a no-op, not a
        // duplicate row.
        builder.HasIndex(g => new { g.TenantId, g.MenuId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Menu>().WithMany().HasForeignKey(g => g.MenuId).OnDelete(DeleteBehavior.Restrict);
    }
}
