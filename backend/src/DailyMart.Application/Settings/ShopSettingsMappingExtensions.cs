using DailyMart.Domain.Settings;

namespace DailyMart.Application.Settings;

internal static class ShopSettingsMappingExtensions
{
    public static ShopSettingsDto ToDto(this ShopSettings settings) => new()
    {
        Id = settings.Id,
        ShopName = settings.ShopName,
        ShopAddress = settings.ShopAddress,
        ShopPhone = settings.ShopPhone,
        ShopEmail = settings.ShopEmail,
        ShopLogoUrl = settings.ShopLogoUrl,
        InvoicePrefix = settings.InvoicePrefix,
        InvoiceFooterText = settings.InvoiceFooterText,
        CurrencyCode = settings.CurrencyCode,
        CurrencySymbol = settings.CurrencySymbol,
        DefaultVatPercentage = settings.DefaultVatPercentage,
        DefaultDiscountPercentage = settings.DefaultDiscountPercentage,
        BackupEnabled = settings.BackupEnabled,
        BackupFrequency = settings.BackupFrequency.ToString(),
        DateFormat = settings.DateFormat,
        TimeZone = settings.TimeZone
    };

    /// <summary>Assumes BackupFrequency has already passed FluentValidation - see
    /// UpdateShopSettingsRequestValidator - so Enum.Parse here is never reached with an invalid value.</summary>
    public static void ApplyTo(this UpdateShopSettingsRequestDto request, ShopSettings settings)
    {
        settings.ShopName = request.ShopName;
        settings.ShopAddress = request.ShopAddress;
        settings.ShopPhone = request.ShopPhone;
        settings.ShopEmail = request.ShopEmail;
        settings.InvoicePrefix = request.InvoicePrefix;
        settings.InvoiceFooterText = request.InvoiceFooterText;
        settings.CurrencyCode = request.CurrencyCode;
        settings.CurrencySymbol = request.CurrencySymbol;
        settings.DefaultVatPercentage = request.DefaultVatPercentage;
        settings.DefaultDiscountPercentage = request.DefaultDiscountPercentage;
        settings.BackupEnabled = request.BackupEnabled;
        settings.BackupFrequency = Enum.Parse<BackupFrequency>(request.BackupFrequency, ignoreCase: true);
        settings.DateFormat = request.DateFormat;
        settings.TimeZone = request.TimeZone;
    }
}
