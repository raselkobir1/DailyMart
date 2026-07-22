using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Settings;
using DailyMart.Domain.Settings;
using Moq;

namespace DailyMart.UnitTests.Settings;

public class ShopSettingsServiceTests
{
    private readonly Mock<IShopSettingsRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly ShopSettingsService _sut;

    public ShopSettingsServiceTests()
    {
        _sut = new ShopSettingsService(_repository.Object, _unitOfWork.Object, _fileStorageService.Object);
    }

    private static ShopSettings ExistingSettings() => new()
    {
        Id = 1,
        ShopName = "DailyMart",
        InvoicePrefix = "INV-",
        CurrencyCode = "BDT",
        CurrencySymbol = "৳",
        BackupFrequency = BackupFrequency.Daily,
        DateFormat = "dd/MM/yyyy",
        TimeZone = "UTC"
    };

    [Fact]
    public async Task GetAsync_returns_the_singleton_mapped_to_a_dto()
    {
        var settings = ExistingSettings();
        _repository.Setup(r => r.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var result = await _sut.GetAsync();

        Assert.Equal("DailyMart", result.ShopName);
        Assert.Equal("Daily", result.BackupFrequency);
    }

    [Fact]
    public async Task GetAsync_throws_when_no_settings_row_exists()
    {
        _repository.Setup(r => r.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ShopSettings?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAsync());
    }

    [Fact]
    public async Task UpdateAsync_applies_every_field_except_the_logo_url_and_saves()
    {
        var settings = ExistingSettings();
        settings.ShopLogoUrl = "/uploads/logos/existing.png";
        _repository.Setup(r => r.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var request = new UpdateShopSettingsRequestDto
        {
            ShopName = "Renamed Shop",
            InvoicePrefix = "RCPT-",
            CurrencyCode = "USD",
            CurrencySymbol = "$",
            DefaultVatPercentage = 15,
            DefaultDiscountPercentage = 5,
            BackupEnabled = true,
            BackupFrequency = "Weekly",
            DateFormat = "yyyy-MM-dd",
            TimeZone = "UTC"
        };

        var result = await _sut.UpdateAsync(request);

        Assert.Equal("Renamed Shop", result.ShopName);
        Assert.Equal("RCPT-", result.InvoicePrefix);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal(15, result.DefaultVatPercentage);
        Assert.Equal("Weekly", result.BackupFrequency);
        // The logo URL survives an unrelated settings update untouched.
        Assert.Equal("/uploads/logos/existing.png", result.ShopLogoUrl);

        _repository.Verify(r => r.Update(settings), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLogoAsync_rejects_an_unsupported_extension_without_touching_storage()
    {
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UploadLogoAsync(content, "logo.gif"));

        _fileStorageService.Verify(
            f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_rejects_a_file_over_the_size_cap_without_touching_storage()
    {
        using var content = new MemoryStream(new byte[2 * 1024 * 1024 + 1]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UploadLogoAsync(content, "logo.png"));

        _fileStorageService.Verify(
            f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_with_a_valid_file_saves_it_and_updates_the_logo_url()
    {
        var settings = ExistingSettings();
        _repository.Setup(r => r.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        _fileStorageService
            .Setup(f => f.SaveAsync(It.IsAny<Stream>(), "logo.png", "logos", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/logos/new-guid.png");

        using var content = new MemoryStream([1, 2, 3]);

        var result = await _sut.UploadLogoAsync(content, "logo.png");

        Assert.Equal("/uploads/logos/new-guid.png", result.ShopLogoUrl);
        Assert.Equal("/uploads/logos/new-guid.png", settings.ShopLogoUrl);
        _repository.Verify(r => r.Update(settings), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
