using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class PlatformNotificationConfiguration : IEntityTypeConfiguration<PlatformNotification>
{
    public void Configure(EntityTypeBuilder<PlatformNotification> builder)
    {
        builder.ToTable("platform_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.TenantName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.AdminUsername).HasMaxLength(100);

        // Restrict, not Cascade - a deleted/suspended tenant's notification history should stay readable,
        // same "keep the audit trail" reasoning as AuditLog not being deleted alongside the entity it
        // describes.
        builder.HasOne<Tenant>().WithMany().HasForeignKey(n => n.TenantId).OnDelete(DeleteBehavior.Restrict);

        // Speeds up the two real query shapes: "how many unread" and "most recent N."
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAt);
    }
}
