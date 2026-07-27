using DailyMart.Domain.Billing;
using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Amount).HasColumnType("numeric(12,2)");
        builder.Property(sp => sp.Method).HasMaxLength(100).IsRequired();
        builder.Property(sp => sp.Notes).HasMaxLength(500);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(sp => sp.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Plan>().WithMany().HasForeignKey(sp => sp.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
