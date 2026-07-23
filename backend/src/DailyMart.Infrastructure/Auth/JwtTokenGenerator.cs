using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DailyMart.Application.Auth;
using DailyMart.Application.Common.Options;
using DailyMart.Domain.Auth;
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
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
