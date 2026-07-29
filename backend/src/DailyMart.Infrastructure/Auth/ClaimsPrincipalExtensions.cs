using System.Security.Claims;
using DailyMart.Application.Common.Interfaces;

namespace DailyMart.Infrastructure.Auth;

/// <summary>
/// A tenant's own Role.Name is arbitrary (RoleService only enforces per-tenant uniqueness, not a reserved
/// word list), so a tenant's own Admin can create a role literally named "PlatformAdmin" and assign it to
/// a user - that user's JWT then carries ClaimTypes.Role = "PlatformAdmin" too. A bare
/// [Authorize(Roles = "PlatformAdmin")] check can't tell that apart from a real platform-admin token.
/// GeneratePlatformAdminAccessToken never adds a tenant_id claim (see its doc comment) while every
/// tenant-user token always does (GenerateAccessToken), so combining both checks closes the spoof
/// regardless of what a tenant names its own roles.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsGenuinePlatformAdmin(this ClaimsPrincipal? user) =>
        user is not null &&
        user.IsInRole("PlatformAdmin") &&
        !user.HasClaim(c => c.Type == ICurrentTenantService.ClaimType);
}
