using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Settings;

namespace DailyMart.Application.Settings;

public class ShopSettingsService : IShopSettingsService
{
    private static readonly string[] AllowedLogoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024;

    private readonly IShopSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public ShopSettingsService(
        IShopSettingsRepository repository, IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(cancellationToken)).ToDto();

    public async Task<ShopSettingsDto> UpdateAsync(
        UpdateShopSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        var settings = await GetEntityAsync(cancellationToken);

        request.ApplyTo(settings);

        _repository.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }

    public async Task<ShopSettingsDto> UploadLogoAsync(
        Stream fileContent, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(extension))
        {
            throw new BusinessRuleException(
                $"Unsupported logo file type '{extension}'. Allowed types: {string.Join(", ", AllowedLogoExtensions)}.");
        }

        if (fileContent.Length > MaxLogoSizeBytes)
        {
            throw new BusinessRuleException(
                $"Logo file exceeds the maximum size of {MaxLogoSizeBytes / (1024 * 1024)} MB.");
        }

        var settings = await GetEntityAsync(cancellationToken);

        var url = await _fileStorageService.SaveAsync(fileContent, fileName, "logos", cancellationToken);

        settings.ShopLogoUrl = url;
        _repository.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }

    private async Task<ShopSettings> GetEntityAsync(CancellationToken cancellationToken) =>
        await _repository.GetSingletonAsync(cancellationToken)
        ?? throw new InvalidOperationException("Shop settings have not been initialized.");
}
