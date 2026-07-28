using System.Security.Cryptography;
using System.Text;
using DailyMart.Application.Auth;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Rbac;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyMart.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IRepository<Tenant>> _tenantRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<ITenantProvisioningService> _tenantProvisioningService = new();
    private readonly Mock<IFeatureEntitlementService> _featureEntitlementService = new();
    private readonly Mock<IPlatformNotificationService> _platformNotificationService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _jwtTokenGenerator.Setup(g => g.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _jwtTokenGenerator.Setup(g => g.GenerateAccessToken(It.IsAny<User>())).Returns("fake-jwt");

        _unitOfWork.Setup(u => u.Repository<Tenant>()).Returns(_tenantRepository.Object);
        _tenantRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = 1, Name = "Acme Corp", IsActive = true });

        _sut = new AuthService(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            _jwtTokenGenerator.Object,
            _passwordHasher.Object,
            _tenantProvisioningService.Object,
            _featureEntitlementService.Object,
            _platformNotificationService.Object,
            Options.Create(new JwtSettings { RefreshTokenDays = 7 }));
    }

    private static User ActiveUser() => new()
    {
        Id = 1,
        TenantId = 1,
        Username = "admin",
        PasswordHash = "hashed",
        FullName = "Administrator",
        Role = "Admin",
        IsActive = true
    };

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    [Fact]
    public async Task LoginAsync_with_valid_credentials_issues_tokens_and_persists_the_refresh_token_hashed()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "correct-password"))
            .Returns(PasswordVerificationResult.Success);

        RefreshToken? capturedToken = null;
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => capturedToken = token)
            .Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "correct-password" });

        Assert.Equal("fake-jwt", result.AccessToken);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
        Assert.NotEmpty(result.RefreshToken);

        Assert.NotNull(capturedToken);
        Assert.Equal(user.Id, capturedToken!.UserId);
        // The persisted hash must match the plaintext token actually handed back to the client.
        Assert.Equal(Sha256Hex(result.RefreshToken), capturedToken.TokenHash);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_with_unknown_username_throws_AuthenticationFailedException()
    {
        _userRepository
            .Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "ghost", Password = "whatever" }));
    }

    [Fact]
    public async Task LoginAsync_for_an_inactive_user_throws_AuthenticationFailedException()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "whatever" }));
    }

    [Fact]
    public async Task LoginAsync_for_a_suspended_tenant_throws_AuthenticationFailedException()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "correct-password"))
            .Returns(PasswordVerificationResult.Success);
        _tenantRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = 1, Name = "Acme Corp", IsActive = false });

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "correct-password" }));
    }

    [Fact]
    public async Task LoginAsync_with_wrong_password_throws_AuthenticationFailedException()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "wrong-password"))
            .Returns(PasswordVerificationResult.Failed);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "wrong-password" }));

        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_rehashes_the_password_when_the_hasher_reports_SuccessRehashNeeded()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "correct-password"))
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);
        _passwordHasher.Setup(h => h.HashPassword(user, "correct-password")).Returns("new-hash");

        var result = await _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "correct-password" });

        Assert.NotNull(result);
        Assert.Equal("new-hash", user.PasswordHash);
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_with_an_active_token_rotates_it_and_issues_new_tokens()
    {
        var user = ActiveUser();
        var existingToken = new RefreshToken
        {
            Id = 10,
            UserId = user.Id,
            TokenHash = "irrelevant-for-this-test",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "presented-token" });

        Assert.NotNull(existingToken.RevokedAt);
        Assert.False(existingToken.IsActive);
        _refreshTokenRepository.Verify(r => r.Update(existingToken), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("fake-jwt", result.AccessToken);
    }

    [Fact]
    public async Task RefreshAsync_with_an_unknown_token_throws_AuthenticationFailedException()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "never-issued" }));
    }

    [Fact]
    public async Task RefreshAsync_with_an_already_revoked_token_throws_AuthenticationFailedException()
    {
        var revoked = new RefreshToken
        {
            UserId = 1,
            TokenHash = "x",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revoked);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "already-used" }));
    }

    [Fact]
    public async Task LogoutAsync_revokes_an_active_token_and_saves()
    {
        var active = new RefreshToken { UserId = 1, TokenHash = "x", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        await _sut.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = "some-token" });

        Assert.NotNull(active.RevokedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_with_an_unknown_token_is_a_silent_no_op()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await _sut.LogoutAsync(new RefreshTokenRequestDto { RefreshToken = "never-issued" });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_with_the_correct_current_password_updates_hash_and_revokes_other_sessions()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "old-password"))
            .Returns(PasswordVerificationResult.Success);
        _passwordHasher.Setup(h => h.HashPassword(user, "new-password")).Returns("new-hash");

        await _sut.ChangePasswordAsync(
            user.Id, new ChangePasswordRequestDto { CurrentPassword = "old-password", NewPassword = "new-password" });

        Assert.Equal("new-hash", user.PasswordHash);
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAllActiveForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_with_the_wrong_current_password_throws_and_changes_nothing()
    {
        var user = ActiveUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto { CurrentPassword = "wrong", NewPassword = "new-password" }));

        _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _refreshTokenRepository.Verify(
            r => r.RevokeAllActiveForUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_provisions_a_new_tenant_and_issues_tokens_for_its_admin()
    {
        _userRepository
            .Setup(r => r.ExistsByUsernameAsync("newadmin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var newAdmin = new User
        {
            Id = 2,
            TenantId = 5,
            Username = "newadmin",
            FullName = "New Admin",
            Role = "Admin",
            IsActive = true
        };
        _tenantProvisioningService
            .Setup(s => s.ProvisionNewTenantAsync(
                "Acme Corp", "newadmin", It.IsAny<string>(), "New Admin", "owner@acme.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newAdmin);
        _tenantRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = 5, Name = "Acme Corp", IsActive = true });

        var result = await _sut.RegisterAsync(new RegisterRequestDto
        {
            CompanyName = "Acme Corp",
            Username = "newadmin",
            Password = "correct-password",
            FullName = "New Admin",
            Email = "owner@acme.test"
        });

        Assert.Equal("fake-jwt", result.AccessToken);
        Assert.Equal("newadmin", result.Username);
        Assert.Equal(5, result.TenantId);
        Assert.Equal("Acme Corp", result.CompanyName);

        _platformNotificationService.Verify(
            s => s.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_throws_when_the_username_is_already_taken()
    {
        _userRepository
            .Setup(r => r.ExistsByUsernameAsync("taken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RegisterAsync(new RegisterRequestDto
        {
            CompanyName = "Acme Corp",
            Username = "taken",
            Password = "correct-password",
            FullName = "New Admin"
        }));

        _tenantProvisioningService.Verify(
            s => s.ProvisionNewTenantAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
