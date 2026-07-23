using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Rbac;
using DailyMart.Domain.Rbac;
using Moq;

namespace DailyMart.UnitTests.Rbac;

public class MenuServiceTests
{
    private readonly Mock<IRepository<Menu>> _menuRepository = new();
    private readonly Mock<IRepository<Role>> _roleRepository = new();
    private readonly Mock<IRepository<RoleMenuPermission>> _permissionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly MenuService _sut;

    public MenuServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Menu>()).Returns(_menuRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Role>()).Returns(_roleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<RoleMenuPermission>()).Returns(_permissionRepository.Object);

        _menuRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _menuRepository
            .Setup(r => r.AddAsync(It.IsAny<Menu>(), It.IsAny<CancellationToken>()))
            .Callback<Menu, CancellationToken>((m, _) => m.Id = 10)
            .Returns(Task.CompletedTask);
        _roleRepository
            .Setup(r => r.FindAsync(It.Is<Expression<Func<Role, bool>>>(e => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Role { Id = 1, Name = "Admin" }]);

        _sut = new MenuService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_key()
    {
        _menuRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(
            new CreateMenuRequestDto { Key = "products", Label = "Products", Route = "/products", Icon = "🛍️" }));
    }

    [Fact]
    public async Task CreateAsync_grants_the_Admin_role_full_CRUD_on_the_new_menu()
    {
        await _sut.CreateAsync(new CreateMenuRequestDto { Key = "reports", Label = "Reports", Route = "/reports", Icon = "📊" });

        _permissionRepository.Verify(r => r.AddAsync(
            It.Is<RoleMenuPermission>(p => p.RoleId == 1 && p.MenuId == 10 && p.CanView && p.CanCreate && p.CanEdit && p.CanDelete),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_BusinessRuleException_when_the_new_parent_is_the_menus_own_descendant()
    {
        // menu 1 (root) -> menu 2 (child) -> attempting to set menu 1's parent to menu 2 is a cycle.
        var root = new Menu { Id = 1, Key = "root", Label = "Root", Route = "/root", Icon = "📁" };
        var child = new Menu { Id = 2, Key = "child", Label = "Child", Route = "/child", Icon = "📄", ParentId = 1 };

        _menuRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(root);
        _menuRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _menuRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // parent (child, id=2) exists

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(
            1, new MenuRequestDto { Label = "Root", Route = "/root", Icon = "📁", ParentId = 2 }));
    }

    [Fact]
    public async Task DeleteAsync_throws_BusinessRuleException_when_the_menu_has_children()
    {
        var menu = new Menu { Id = 1, Key = "root", Label = "Root", Route = "/root", Icon = "📁" };
        _menuRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(menu);
        _menuRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // has children

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1));

        _menuRepository.Verify(r => r.Remove(It.IsAny<Menu>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_succeeds_when_the_menu_has_no_children()
    {
        var menu = new Menu { Id = 1, Key = "leaf", Label = "Leaf", Route = "/leaf", Icon = "📄" };
        _menuRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(menu);
        _menuRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Menu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // no children

        await _sut.DeleteAsync(1);

        _menuRepository.Verify(r => r.Remove(menu), Times.Once);
    }
}
