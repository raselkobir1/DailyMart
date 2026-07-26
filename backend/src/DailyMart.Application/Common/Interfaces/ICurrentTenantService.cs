namespace DailyMart.Application.Common.Interfaces;

/// <summary>
/// Resolves which tenant (company/shop) the current request belongs to, for the DbContext-level
/// tenant query filter and for stamping TenantId on newly-created entities. Mirrors
/// ICurrentUserService exactly - implemented in Infrastructure against HttpContext, reading the
/// tenant_id JWT claim. Null when there is no tenant context (a platform-admin token, which never
/// carries a tenant claim, or seed-time/background code with no HttpContext) - see
/// TenancyModelExtensions for why null is a safe, fail-closed value rather than "no filtering."
/// </summary>
public interface ICurrentTenantService
{
    /// <summary>Single source of truth for the JWT claim type name, so JwtTokenGenerator (writer)
    /// and CurrentTenantService (reader) can't drift apart.</summary>
    const string ClaimType = "tenant_id";

    long? TenantId { get; }
}
