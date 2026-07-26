using DailyMart.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired().HasDefaultValue("Admin");
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        // Partial unique index: without the filter, soft-deleting a user (Module 0's interceptor sets
        // IsDeleted = true instead of a real DELETE) would leave the username permanently "taken" and
        // block a legitimate new user from ever reusing it. Found while designing Module 3's unique
        // name constraints and applied here retroactively - this bug existed since Module 1.
        //
        // Deliberately kept GLOBAL, not composite with TenantId, unlike every other "Name must be
        // unique" entity in this file set: login (POST /api/auth/login) takes only a username and
        // password, with no company selector, so a per-tenant-unique Username would make the login
        // lookup ambiguous ("admin" at two different companies) without also redesigning login to
        // ask which company first. Simpler to keep the login flow untouched and require every user's
        // chosen username to be unique platform-wide - the same tradeoff most SaaS products make by
        // requiring email-as-login instead of a freely-chosen username.
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("is_deleted = false");
    }
}
