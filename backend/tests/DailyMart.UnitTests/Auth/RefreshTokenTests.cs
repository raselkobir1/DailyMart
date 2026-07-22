using DailyMart.Domain.Auth;

namespace DailyMart.UnitTests.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_is_true_when_not_revoked_and_not_expired()
    {
        var token = new RefreshToken { ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };

        Assert.True(token.IsActive);
    }

    [Fact]
    public void IsActive_is_false_once_revoked_even_if_not_yet_expired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            RevokedAt = DateTimeOffset.UtcNow
        };

        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_is_false_once_expired_even_if_never_revoked()
    {
        var token = new RefreshToken { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) };

        Assert.False(token.IsActive);
    }
}
