namespace DailyMart.Application.Common.Exceptions;

/// <summary>
/// Thrown for any credential/token failure (bad password, expired/revoked refresh token, wrong current
/// password on change). The API layer maps this to 401, not the generic 500 catch-all.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
