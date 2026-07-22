namespace DailyMart.Application.Settings;

public interface IShopSettingsService
{
    Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<ShopSettingsDto> UpdateAsync(UpdateShopSettingsRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Validates type/size, stores the file, and updates ShopLogoUrl. Returns the full updated
    /// settings (not just the new URL) so the controller doesn't need a second round-trip to refresh it.</summary>
    Task<ShopSettingsDto> UploadLogoAsync(
        Stream fileContent, string fileName, CancellationToken cancellationToken = default);
}
