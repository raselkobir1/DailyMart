using DailyMart.Domain.Billing;
using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("tenant_subscriptions");

        builder.HasKey(ts => ts.Id);

        // One subscription per tenant - see TenantSubscription's doc comment.
        builder.HasIndex(ts => ts.TenantId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(ts => ts.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Plan>().WithMany().HasForeignKey(ts => ts.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
