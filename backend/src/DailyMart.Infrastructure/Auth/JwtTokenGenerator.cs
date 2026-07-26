using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DailyMart.Application.Auth;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Tenancy;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DailyMart.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_settings.AccessTokenMinutes);

    public TimeSpan PlatformAdminAccessTokenLifetime => TimeSpan.FromHours(8);

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            // Role claim only - deliberately not the user's permitted-menu list too. That's fetched
            // separately via GET /api/auth/me/permissions (IAuthService.GetMyPermissionsAsync) at app
            // bootstrap and right after login, so changing a role's permissions takes effect immediately
            // without needing to re-issue every affected user's token.
            new Claim(ClaimTypes.Role, user.Role),
            // Read by ICurrentTenantService to drive the DbContext-level tenant query filter on every
            // subsequent request - see TenancyModelExtensions. Platform-admin tokens never carry this.
            new Claim(ICurrentTenantService.ClaimType, user.TenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return WriteToken(claims, AccessTokenLifetime);
    }

    public string GeneratePlatformAdminAccessToken(PlatformAdmin admin)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, admin.Username),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, "PlatformAdmin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return WriteToken(claims, PlatformAdminAccessTokenLifetime);
    }

    private string WriteToken(Claim[] claims, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
