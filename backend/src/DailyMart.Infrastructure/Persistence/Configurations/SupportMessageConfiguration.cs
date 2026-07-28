using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("support_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Message).HasMaxLength(2000).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);

        // Speeds up "this tenant's conversation, most recent first" and the two unread-count queries.
        builder.HasIndex(m => new { m.TenantId, m.CreatedAt });
        builder.HasIndex(m => new { m.TenantId, m.IsReadByPlatformAdmin });
        builder.HasIndex(m => m.IsReadByTenant);
    }
}
