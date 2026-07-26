using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Platform-operator login - entirely separate from the tenant-scoped api/auth surface, see
/// IPlatformAdminAuthService's doc comment.</summary>
[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAdminAuthService _platformAdminAuthService;

    public PlatformAuthController(IPlatformAdminAuthService platformAdminAuthService)
    {
        _platformAdminAuthService = platformAdminAuthService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<PlatformAdminAuthResponseDto>> Login(
        PlatformAdminLoginRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _platformAdminAuthService.LoginAsync(request, cancellationToken));
    }
}
