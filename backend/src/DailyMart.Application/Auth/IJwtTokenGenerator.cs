using DailyMart.Domain.Auth;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Auth;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);

    /// <summary>No tenant_id claim, unlike GenerateAccessToken(User) - a platform admin isn't scoped to
    /// any tenant, and the tenant query filter's fail-closed behavior (see TenancyModelExtensions)
    /// depends on this claim being absent. Longer-lived than a regular access token since platform-admin
    /// sessions have no refresh-token flow (see PlatformAdminAuthService's doc comment) - not worth
    /// building a whole parallel refresh mechanism for a "basic" internal ops panel.</summary>
    string GeneratePlatformAdminAccessToken(PlatformAdmin admin);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan PlatformAdminAccessTokenLifetime { get; }
}
