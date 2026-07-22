namespace DailyMart.Application.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>userId comes from the authenticated caller's JWT claims, never from the request body -
    /// a user can only change their own password.</summary>
    Task ChangePasswordAsync(
        long userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
}
