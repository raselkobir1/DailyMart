using DailyMart.Domain.Common;

namespace DailyMart.Domain.Settings;

/// <summary>
/// Singleton shop-wide configuration - exactly one row per tenant (seeded once at tenant provisioning,
/// see TenantProvisioningService/AdminSeeder), enforced by a unique filtered index on TenantId
/// (ShopSettingsConfiguration) so ShopSettingsRepository.GetSingletonAsync's unordered lookup can never
/// return a non-deterministic result. Other modules read this for defaults (currency, VAT %, discount %)
/// rather than each keeping its own copy. Named ShopSettings rather than Settings to avoid a type
/// colliding with its own namespace.
/// </summary>
public class ShopSettings : TenantOwnedEntity
{
    public string ShopName { get; set; } = string.Empty;

    public string? ShopAddress { get; set; }

    public string? ShopPhone { get; set; }

    public string? ShopEmail { get; set; }

    /// <summary>Relative URL from IFileStorageService - the logo image itself lives on disk, not here.</summary>
    public string? ShopLogoUrl { get; set; }

    public string InvoicePrefix { get; set; } = "INV-";

    public string? InvoiceFooterText { get; set; }

    public string CurrencyCode { get; set; } = "BDT";

    public string CurrencySymbol { get; set; } = "৳";

    public decimal DefaultVatPercentage { get; set; }

    public decimal DefaultDiscountPercentage { get; set; }

    public bool BackupEnabled { get; set; }

    public BackupFrequency BackupFrequency { get; set; } = BackupFrequency.Daily;

    public string DateFormat { get; set; } = "dd/MM/yyyy";

    public string TimeZone { get; set; } = "UTC";
}
