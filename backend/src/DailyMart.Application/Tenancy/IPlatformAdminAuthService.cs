namespace DailyMart.Application.Tenancy;

/// <summary>
/// Login only - no refresh/logout/change-password flow, unlike IAuthService. This is a "basic"
/// internal ops panel for the SaaS vendor's own staff, not a customer-facing feature, so the token
/// is longer-lived (see IJwtTokenGenerator.PlatformAdminAccessTokenLifetime) and re-authenticating
/// after it expires is an acceptable tradeoff against building a whole parallel refresh-token
/// mechanism scoped to PlatformAdmin instead of User.
/// </summary>
public interface IPlatformAdminAuthService
{
    Task<PlatformAdminAuthResponseDto> LoginAsync(
        PlatformAdminLoginRequestDto request, CancellationToken cancellationToken = default);
}
