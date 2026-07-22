using DailyMart.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyMart.Infrastructure.Persistence.Configurations;

public class ShopSettingsConfiguration : IEntityTypeConfiguration<ShopSettings>
{
    public void Configure(EntityTypeBuilder<ShopSettings> builder)
    {
        builder.ToTable("settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShopName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ShopAddress).HasMaxLength(500);
        builder.Property(s => s.ShopPhone).HasMaxLength(50);
        builder.Property(s => s.ShopEmail).HasMaxLength(200);
        builder.Property(s => s.ShopLogoUrl).HasMaxLength(500);

        builder.Property(s => s.InvoicePrefix).HasMaxLength(20).IsRequired().HasDefaultValue("INV-");
        builder.Property(s => s.InvoiceFooterText).HasMaxLength(500);

        builder.Property(s => s.CurrencyCode).HasMaxLength(10).IsRequired().HasDefaultValue("BDT");
        builder.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired().HasDefaultValue("৳");

        builder.Property(s => s.DefaultVatPercentage).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
        builder.Property(s => s.DefaultDiscountPercentage).HasColumnType("numeric(5,2)").HasDefaultValue(0m);

        builder.Property(s => s.BackupEnabled).HasDefaultValue(false);
        builder.Property(s => s.BackupFrequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(BackupFrequency.Daily);

        builder.Property(s => s.DateFormat).HasMaxLength(20).IsRequired().HasDefaultValue("dd/MM/yyyy");
        builder.Property(s => s.TimeZone).HasMaxLength(100).IsRequired().HasDefaultValue("UTC");
    }
}
