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
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("is_deleted = false");
    }
}
