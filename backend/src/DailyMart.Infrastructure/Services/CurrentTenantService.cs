using DailyMart.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DailyMart.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Null when there's no HttpContext (seed-time/background code) or the current token
    /// carries no tenant_id claim (a platform-admin token) - see ICurrentTenantService's doc comment
    /// for why null is the correct, fail-closed value rather than "skip filtering."</summary>
    public long? TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ICurrentTenantService.ClaimType);
            return claim is not null && long.TryParse(claim.Value, out var tenantId) ? tenantId : null;
        }
    }
}
