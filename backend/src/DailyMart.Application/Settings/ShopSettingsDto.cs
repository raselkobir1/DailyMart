namespace DailyMart.Application.Settings;

public class ShopSettingsDto
{
    public long Id { get; init; }

    public string ShopName { get; init; } = string.Empty;

    public string? ShopAddress { get; init; }

    public string? ShopPhone { get; init; }

    public string? ShopEmail { get; init; }

    public string? ShopLogoUrl { get; init; }

    public string InvoicePrefix { get; init; } = string.Empty;

    public string? InvoiceFooterText { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public string CurrencySymbol { get; init; } = string.Empty;

    public decimal DefaultVatPercentage { get; init; }

    public decimal DefaultDiscountPercentage { get; init; }

    public bool BackupEnabled { get; init; }

    public string BackupFrequency { get; init; } = string.Empty;

    public string DateFormat { get; init; } = string.Empty;

    public string TimeZone { get; init; } = string.Empty;
}
