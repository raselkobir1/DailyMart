using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Rbac;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Rbac;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DailyMart.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            _userRepository.Update(user);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        // Rotate: the presented refresh token is single-use, so a captured/replayed token stops working
        // the moment the legitimate client redeems it.
        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        _refreshTokenRepository.Update(existingToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Unknown or already-revoked tokens are a silent no-op - logout is idempotent and shouldn't leak
        // whether a given token ever existed.
        if (existingToken is { IsActive: true })
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            _refreshTokenRepository.Update(existingToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ChangePasswordAsync(
        long userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthenticationFailedException("User not found.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new AuthenticationFailedException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        _userRepository.Update(user);

        // A deliberate password change shouldn't leave any other existing session valid.
        await _refreshTokenRepository.RevokeAllActiveForUserAsync(userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuPermissionDto>> GetMyPermissionsAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthenticationFailedException("User not found.");

        var role = (await _unitOfWork.Repository<Role>().FindAsync(r => r.Name == user.Role, cancellationToken))
            .FirstOrDefault();
        if (role is null)
        {
            // The role name on this user doesn't match any existing Role row (e.g. the role was deleted
            // out from under them, which RoleService.DeleteAsync should already prevent - this is a
            // defensive fallback, not the expected path) - no permitted menus rather than an error, same
            // "no access" outcome the reference app's "no admin access" logout produces.
            return [];
        }

        var menus = await _unitOfWork.Repository<Menu>().GetAllAsync(cancellationToken);
        var permissions = await _unitOfWork.Repository<RoleMenuPermission>()
            .FindAsync(p => p.RoleId == role.Id && p.CanView, cancellationToken);
        var permissionsByMenu = permissions.ToDictionary(p => p.MenuId);

        return menus
            .Where(m => permissionsByMenu.ContainsKey(m.Id))
            .OrderBy(m => m.SortOrder)
            .Select(m =>
            {
                var permission = permissionsByMenu[m.Id];
                return new MenuPermissionDto
                {
                    MenuId = m.Id,
                    MenuKey = m.Key,
                    Label = m.Label,
                    Route = m.Route,
                    Icon = m.Icon,
                    SortOrder = m.SortOrder,
                    ParentId = m.ParentId,
                    CanView = permission.CanView,
                    CanCreate = permission.CanCreate,
                    CanEdit = permission.CanEdit,
                    CanDelete = permission.CanDelete
                };
            })
            .ToList();
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var accessTokenExpiresAt = DateTimeOffset.UtcNow.Add(_jwtTokenGenerator.AccessTokenLifetime);

        var refreshTokenPlainText = RefreshTokenHasher.GenerateToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenHasher.Hash(refreshTokenPlainText),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlainText,
            ExpiresAtUtc = accessTokenExpiresAt,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
