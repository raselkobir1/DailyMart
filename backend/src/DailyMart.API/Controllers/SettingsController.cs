using DailyMart.Application.Settings;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>No [AllowAnonymous] anywhere here - shop configuration requires the global [Authorize]
/// fallback policy like everything else added since Module 1.</summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IShopSettingsService _shopSettingsService;

    public SettingsController(IShopSettingsService shopSettingsService)
    {
        _shopSettingsService = shopSettingsService;
    }

    [HttpGet]
    public async Task<ActionResult<ShopSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await _shopSettingsService.GetAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<ShopSettingsDto>> Update(
        UpdateShopSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var settings = await _shopSettingsService.UpdateAsync(request, cancellationToken);
        return Ok(settings);
    }

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ShopSettingsDto>> UploadLogo(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var settings = await _shopSettingsService.UploadLogoAsync(stream, file.FileName, cancellationToken);

        return Ok(settings);
    }
}
