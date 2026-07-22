using DailyMart.Application.Settings;

namespace DailyMart.UnitTests.Settings;

public class UpdateShopSettingsRequestValidatorTests
{
    private readonly UpdateShopSettingsRequestValidator _validator = new();

    private static UpdateShopSettingsRequestDto ValidRequest(
        string shopName = "DailyMart",
        string? shopEmail = null,
        string invoicePrefix = "INV-",
        string currencyCode = "BDT",
        string currencySymbol = "৳",
        decimal defaultVatPercentage = 10,
        decimal defaultDiscountPercentage = 5,
        string backupFrequency = "Daily",
        string dateFormat = "dd/MM/yyyy",
        string timeZone = "UTC") => new()
    {
        ShopName = shopName,
        ShopEmail = shopEmail,
        InvoicePrefix = invoicePrefix,
        CurrencyCode = currencyCode,
        CurrencySymbol = currencySymbol,
        DefaultVatPercentage = defaultVatPercentage,
        DefaultDiscountPercentage = defaultDiscountPercentage,
        BackupFrequency = backupFrequency,
        DateFormat = dateFormat,
        TimeZone = timeZone
    };

    [Fact]
    public void A_fully_valid_request_passes()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_ShopName_is_invalid()
    {
        var result = _validator.Validate(ValidRequest(shopName: ""));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void DefaultVatPercentage_outside_0_to_100_is_invalid(decimal vat)
    {
        var result = _validator.Validate(ValidRequest(defaultVatPercentage: vat));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateShopSettingsRequestDto.DefaultVatPercentage));
    }

    [Fact]
    public void An_unrecognized_BackupFrequency_is_invalid()
    {
        var result = _validator.Validate(ValidRequest(backupFrequency: "Hourly"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateShopSettingsRequestDto.BackupFrequency));
    }

    [Fact]
    public void An_unrecognized_TimeZone_is_invalid()
    {
        var result = _validator.Validate(ValidRequest(timeZone: "Not/ARealZone"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateShopSettingsRequestDto.TimeZone));
    }

    [Fact]
    public void A_malformed_ShopEmail_is_invalid_when_provided()
    {
        var result = _validator.Validate(ValidRequest(shopEmail: "not-an-email"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateShopSettingsRequestDto.ShopEmail));
    }

    [Fact]
    public void A_missing_ShopEmail_is_valid_since_its_optional()
    {
        var result = _validator.Validate(ValidRequest(shopEmail: null));

        Assert.True(result.IsValid);
    }
}
