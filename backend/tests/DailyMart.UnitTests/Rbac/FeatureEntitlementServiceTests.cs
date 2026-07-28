using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Rbac;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Rbac;
using Moq;

namespace DailyMart.UnitTests.Rbac;

public class FeatureEntitlementServiceTests
{
    private readonly Mock<IRepository<Menu>> _menuRepository = new();
    private readonly Mock<IRepository<TenantFeatureGrant>> _grantRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantProvisioningService> _tenantProvisioningService = new();
    private readonly FeatureEntitlementService _sut;

    private static readonly Menu GenerallyAvailableMenu = new()
    {
        Id = 1, Key = "products", Label = "Products", IsGenerallyAvailable = true
    };

    private static readonly Menu RestrictedMenu = new()
    {
        Id = 2, Key = "beta-analytics", Label = "Beta Analytics", IsGenerallyAvailable = false
    };

    public FeatureEntitlementServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Menu>()).Returns(_menuRepository.Object);
        _unitOfWork.Setup(u => u.Repository<TenantFeatureGrant>()).Returns(_grantRepository.Object);

        _sut = new FeatureEntitlementService(_unitOfWork.Object, _tenantProvisioningService.Object);
    }

    [Fact]
    public async Task GetAvailableMenuIdsAsync_includes_every_generally_available_menu_regardless_of_grants()
    {
        _menuRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([GenerallyAvailableMenu, RestrictedMenu]);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetAvailableMenuIdsAsync(tenantId: 10);

        Assert.Contains(1L, result);
        Assert.DoesNotContain(2L, result);
    }

    [Fact]
    public async Task GetAvailableMenuIdsAsync_includes_a_restricted_menu_the_tenant_holds_a_grant_for()
    {
        _menuRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([GenerallyAvailableMenu, RestrictedMenu]);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TenantFeatureGrant { TenantId = 10, MenuId = 2 }]);

        var result = await _sut.GetAvailableMenuIdsAsync(tenantId: 10);

        Assert.Contains(2L, result);
    }

    [Fact]
    public async Task IsMenuAvailableAsync_returns_true_for_a_generally_available_menu_with_no_grant()
    {
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([GenerallyAvailableMenu]);

        Assert.True(await _sut.IsMenuAvailableAsync(10, "products"));

        // Restricted-only lookup (grants) should never even be consulted for a generally-available menu.
        _grantRepository.Verify(
            r => r.ExistsAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IsMenuAvailableAsync_returns_false_for_a_restricted_menu_with_no_grant()
    {
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([RestrictedMenu]);
        _grantRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.False(await _sut.IsMenuAvailableAsync(10, "beta-analytics"));
    }

    [Fact]
    public async Task IsMenuAvailableAsync_returns_true_for_a_restricted_menu_the_tenant_is_granted()
    {
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([RestrictedMenu]);
        _grantRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Assert.True(await _sut.IsMenuAvailableAsync(10, "beta-analytics"));
    }

    [Fact]
    public async Task IsMenuAvailableAsync_returns_false_for_an_unknown_menu_key()
    {
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.False(await _sut.IsMenuAvailableAsync(10, "does-not-exist"));
    }

    [Fact]
    public async Task GrantAsync_throws_BusinessRuleException_when_the_menu_is_already_generally_available()
    {
        _menuRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(GenerallyAvailableMenu);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GrantAsync(10, 1));

        _tenantProvisioningService.Verify(
            s => s.EnsureAdminRoleHasFullMenuAccessAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GrantAsync_creates_a_grant_row_and_resyncs_the_admin_role_for_a_restricted_menu()
    {
        _menuRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(RestrictedMenu);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.GrantAsync(10, 2);

        _grantRepository.Verify(
            r => r.AddAsync(It.Is<TenantFeatureGrant>(g => g.TenantId == 10 && g.MenuId == 2), It.IsAny<CancellationToken>()),
            Times.Once);
        _tenantProvisioningService.Verify(s => s.EnsureAdminRoleHasFullMenuAccessAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_is_idempotent_when_the_tenant_already_holds_the_grant()
    {
        _menuRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(RestrictedMenu);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TenantFeatureGrant { Id = 5, TenantId = 10, MenuId = 2 }]);

        await _sut.GrantAsync(10, 2);

        _grantRepository.Verify(r => r.AddAsync(It.IsAny<TenantFeatureGrant>(), It.IsAny<CancellationToken>()), Times.Never);
        // Still re-syncs - covers the case where a permission row was manually cleared without a revoke.
        _tenantProvisioningService.Verify(s => s.EnsureAdminRoleHasFullMenuAccessAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_throws_BusinessRuleException_when_the_menu_is_generally_available()
    {
        _menuRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(GenerallyAvailableMenu);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RevokeAsync(10, 1));

        _tenantProvisioningService.Verify(
            s => s.RevokeMenuAccessAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAsync_removes_the_grant_and_strips_every_roles_access_to_the_menu()
    {
        var grant = new TenantFeatureGrant { Id = 5, TenantId = 10, MenuId = 2 };
        _menuRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(RestrictedMenu);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([grant]);

        await _sut.RevokeAsync(10, 2);

        _grantRepository.Verify(r => r.Remove(grant), Times.Once);
        _tenantProvisioningService.Verify(s => s.RevokeMenuAccessAsync(10, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_is_a_noop_when_the_tenant_holds_no_grant()
    {
        _menuRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(RestrictedMenu);
        _grantRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantFeatureGrant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.RevokeAsync(10, 2);

        _grantRepository.Verify(r => r.Remove(It.IsAny<TenantFeatureGrant>()), Times.Never);
        _tenantProvisioningService.Verify(
            s => s.RevokeMenuAccessAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
