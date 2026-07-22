using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DailyMart.Application.Common.Options;
using DailyMart.Domain.Auth;
using DailyMart.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace DailyMart.UnitTests.Auth;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(JwtSettings? settings = null)
    {
        settings ??= new JwtSettings
        {
            Secret = "unit-test-signing-secret-at-least-256-bits-long!!",
            Issuer = "DailyMart",
            Audience = "DailyMartClient",
            AccessTokenMinutes = 15
        };

        return new JwtTokenGenerator(Options.Create(settings));
    }

    [Fact]
    public void GenerateAccessToken_includes_sub_username_and_role_claims()
    {
        var generator = CreateGenerator();
        var user = new User { Id = 42, Username = "admin", FullName = "Administrator", Role = "Admin" };

        var jwt = generator.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        Assert.Equal("42", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin", token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Admin", token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_sets_issuer_audience_and_expiry_from_settings()
    {
        var settings = new JwtSettings
        {
            Secret = "unit-test-signing-secret-at-least-256-bits-long!!",
            Issuer = "DailyMart",
            Audience = "DailyMartClient",
            AccessTokenMinutes = 5
        };
        var generator = CreateGenerator(settings);
        var user = new User { Id = 1, Username = "admin", Role = "Admin" };

        var before = DateTime.UtcNow;
        var jwt = generator.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        Assert.Equal("DailyMart", token.Issuer);
        Assert.Equal("DailyMartClient", token.Audiences.Single());
        Assert.InRange(token.ValidTo, before.AddMinutes(5).AddSeconds(-5), before.AddMinutes(5).AddSeconds(5));
        Assert.Equal(TimeSpan.FromMinutes(5), generator.AccessTokenLifetime);
    }

    [Fact]
    public void Two_tokens_for_the_same_user_have_different_jti_claims()
    {
        var generator = CreateGenerator();
        var user = new User { Id = 1, Username = "admin", Role = "Admin" };
        var handler = new JwtSecurityTokenHandler();

        var first = handler.ReadJwtToken(generator.GenerateAccessToken(user));
        var second = handler.ReadJwtToken(generator.GenerateAccessToken(user));

        var firstJti = first.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = second.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(firstJti, secondJti);
    }
}
