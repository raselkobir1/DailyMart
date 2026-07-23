using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Rbac;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Rbac;
using Moq;

namespace DailyMart.UnitTests.Rbac;

public class RoleServiceTests
{
    private readonly Mock<IRepository<Role>> _roleRepository = new();
    private readonly Mock<IRepository<User>> _userRepository = new();
    private readonly Mock<IRepository<Menu>> _menuRepository = new();
    private readonly Mock<IRepository<RoleMenuPermission>> _permissionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Role>()).Returns(_roleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<User>()).Returns(_userRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Menu>()).Returns(_menuRepository.Object);
        _unitOfWork.Setup(u => u.Repository<RoleMenuPermission>()).Returns(_permissionRepository.Object);

        _roleRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new RoleService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_name()
    {
        _roleRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new RoleRequestDto { Name = "Cashier" }));
    }

    [Fact]
    public async Task UpdateAsync_throws_BusinessRuleException_for_a_system_role()
    {
        var systemRole = new Role { Id = 1, Name = "Admin", IsSystem = true };
        _roleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(systemRole);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.UpdateAsync(1, new RoleRequestDto { Name = "Renamed" }));
    }

    [Fact]
    public async Task DeleteAsync_throws_BusinessRuleException_for_a_system_role()
    {
        var systemRole = new Role { Id = 1, Name = "Admin", IsSystem = true };
        _roleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(systemRole);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1));

        _roleRepository.Verify(r => r.Remove(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_throws_BusinessRuleException_when_a_user_still_has_this_role()
    {
        var role = new Role { Id = 2, Name = "Cashier", IsSystem = false };
        _roleRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _userRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(2));

        _roleRepository.Verify(r => r.Remove(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_succeeds_for_a_non_system_role_with_no_users()
    {
        var role = new Role { Id = 2, Name = "Cashier", IsSystem = false };
        _roleRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _userRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.DeleteAsync(2);

        _roleRepository.Verify(r => r.Remove(role), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsAsync_returns_every_menu_with_false_flags_when_no_permission_row_exists_yet()
    {
        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _menuRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Menu { Id = 1, Key = "products", Label = "Products", Route = "/products", Icon = "🛍️", SortOrder = 10 }]);
        _permissionRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleMenuPermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetPermissionsAsync(5);

        var item = Assert.Single(result);
        Assert.Equal("products", item.MenuKey);
        Assert.False(item.CanView);
        Assert.False(item.CanCreate);
    }

    [Fact]
    public async Task GetPermissionsAsync_throws_NotFoundException_when_the_role_does_not_exist()
    {
        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetPermissionsAsync(404));
    }

    [Fact]
    public async Task SetPermissionsAsync_creates_a_new_permission_row_when_none_exists_for_this_menu_yet()
    {
        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Menu { Id = 1 }]);
        _permissionRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleMenuPermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new SetPermissionsRequestDto
        {
            Permissions = [new MenuPermissionItemDto { MenuId = 1, CanView = true, CanCreate = false, CanEdit = false, CanDelete = false }]
        };

        await _sut.SetPermissionsAsync(5, request);

        _permissionRepository.Verify(r => r.AddAsync(
            It.Is<RoleMenuPermission>(p => p.RoleId == 5 && p.MenuId == 1 && p.CanView && !p.CanCreate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPermissionsAsync_updates_an_existing_permission_row_in_place_rather_than_recreating_it()
    {
        var existing = new RoleMenuPermission { Id = 9, RoleId = 5, MenuId = 1, CanView = true, CanCreate = false, CanEdit = false, CanDelete = false };

        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Menu { Id = 1 }]);
        _permissionRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleMenuPermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var request = new SetPermissionsRequestDto
        {
            Permissions = [new MenuPermissionItemDto { MenuId = 1, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true }]
        };

        await _sut.SetPermissionsAsync(5, request);

        Assert.True(existing.CanCreate);
        Assert.True(existing.CanEdit);
        Assert.True(existing.CanDelete);
        _permissionRepository.Verify(r => r.Update(existing), Times.Once);
        _permissionRepository.Verify(r => r.AddAsync(It.IsAny<RoleMenuPermission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetPermissionsAsync_throws_BusinessRuleException_when_a_menu_does_not_exist()
    {
        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _menuRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new SetPermissionsRequestDto
        {
            Permissions = [new MenuPermissionItemDto { MenuId = 999, CanView = true }]
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetPermissionsAsync(5, request));
    }
}
