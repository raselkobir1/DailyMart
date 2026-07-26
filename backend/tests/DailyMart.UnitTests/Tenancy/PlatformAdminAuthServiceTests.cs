using DailyMart.Application.Auth;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DailyMart.UnitTests.Tenancy;

public class PlatformAdminAuthServiceTests
{
    private readonly Mock<IRepository<PlatformAdmin>> _platformAdminRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IPasswordHasher<PlatformAdmin>> _passwordHasher = new();
    private readonly PlatformAdminAuthService _sut;

    public PlatformAdminAuthServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<PlatformAdmin>()).Returns(_platformAdminRepository.Object);
        _jwtTokenGenerator.Setup(g => g.PlatformAdminAccessTokenLifetime).Returns(TimeSpan.FromHours(8));
        _jwtTokenGenerator.Setup(g => g.GeneratePlatformAdminAccessToken(It.IsAny<PlatformAdmin>())).Returns("fake-platform-jwt");

        _sut = new PlatformAdminAuthService(_unitOfWork.Object, _jwtTokenGenerator.Object, _passwordHasher.Object);
    }

    private static PlatformAdmin ActiveAdmin() => new()
    {
        Id = 1,
        Username = "platform",
        PasswordHash = "hashed",
        FullName = "Platform Administrator",
        IsActive = true
    };

    [Fact]
    public async Task LoginAsync_with_valid_credentials_issues_a_platform_admin_token()
    {
        var admin = ActiveAdmin();
        _platformAdminRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlatformAdmin, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([admin]);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(admin, admin.PasswordHash, "correct-password"))
            .Returns(PasswordVerificationResult.Success);

        var result = await _sut.LoginAsync(new PlatformAdminLoginRequestDto { Username = "platform", Password = "correct-password" });

        Assert.Equal("fake-platform-jwt", result.AccessToken);
        Assert.Equal("platform", result.Username);
    }

    [Fact]
    public async Task LoginAsync_with_unknown_username_throws_AuthenticationFailedException()
    {
        _platformAdminRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlatformAdmin, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new PlatformAdminLoginRequestDto { Username = "ghost", Password = "whatever" }));
    }

    [Fact]
    public async Task LoginAsync_for_an_inactive_platform_admin_throws_AuthenticationFailedException()
    {
        var admin = ActiveAdmin();
        admin.IsActive = false;
        _platformAdminRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlatformAdmin, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([admin]);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new PlatformAdminLoginRequestDto { Username = "platform", Password = "whatever" }));
    }

    [Fact]
    public async Task LoginAsync_with_wrong_password_throws_AuthenticationFailedException()
    {
        var admin = ActiveAdmin();
        _platformAdminRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlatformAdmin, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([admin]);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(admin, admin.PasswordHash, "wrong-password"))
            .Returns(PasswordVerificationResult.Failed);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new PlatformAdminLoginRequestDto { Username = "platform", Password = "wrong-password" }));
    }
}
