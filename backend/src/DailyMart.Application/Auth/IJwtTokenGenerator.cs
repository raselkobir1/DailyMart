using DailyMart.Domain.Auth;

namespace DailyMart.Application.Auth;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);

    TimeSpan AccessTokenLifetime { get; }
}
