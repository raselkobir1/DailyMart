using DailyMart.Application.Rbac;

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

    /// <summary>The CanView=true subset of this user's role's permission matrix, ordered by SortOrder -
    /// drives the frontend's dynamic sidebar and per-route canView(menuKey) guard. Deliberately NOT baked
    /// into the JWT (see JwtTokenGenerator's doc comment) - called at app bootstrap and right after login,
    /// so a permission change takes effect without needing to re-issue anyone's token.</summary>
    Task<IReadOnlyList<MenuPermissionDto>> GetMyPermissionsAsync(long userId, CancellationToken cancellationToken = default);
}
