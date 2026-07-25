using DailyMart.Application.Auth;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Rbac;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DailyMart.UnitTests.Auth;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Role>> _roleRepository = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Role>()).Returns(_roleRepository.Object);
        _roleRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new UserService(
            _userRepository.Object, _refreshTokenRepository.Object, _unitOfWork.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task UpdateAsync_updates_a_different_users_role_without_issue()
    {
        var user = new User { Id = 2, FullName = "Cashier One", Role = "Cashier", IsActive = true };
        _userRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.UpdateAsync(
            2, new UpdateUserRequestDto { FullName = "Cashier One", Role = "Manager", IsActive = true }, currentUserId: 1);

        Assert.Equal("Manager", result.Role);
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_a_user_tries_to_change_their_own_role()
    {
        var self = new User { Id = 1, FullName = "The Admin", Role = "Admin", IsActive = true };
        _userRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(
            1, new UpdateUserRequestDto { FullName = "The Admin", Role = "Cashier", IsActive = true }, currentUserId: 1));

        _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_a_user_tries_to_deactivate_themselves()
    {
        var self = new User { Id = 1, FullName = "The Admin", Role = "Admin", IsActive = true };
        _userRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(
            1, new UpdateUserRequestDto { FullName = "The Admin", Role = "Admin", IsActive = false }, currentUserId: 1));

        _userRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_allows_a_user_to_edit_their_own_name_without_touching_role_or_active_state()
    {
        var self = new User { Id = 1, FullName = "The Admin", Role = "Admin", IsActive = true };
        _userRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(self);

        var result = await _sut.UpdateAsync(
            1, new UpdateUserRequestDto { FullName = "The Administrator", Role = "Admin", IsActive = true }, currentUserId: 1);

        Assert.Equal("The Administrator", result.FullName);
        _userRepository.Verify(r => r.Update(self), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_when_deleting_your_own_account()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1, currentUserId: 1));

        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_hashes_the_new_password_and_revokes_active_sessions()
    {
        var user = new User { Id = 2, FullName = "Cashier One", Role = "Cashier", IsActive = true, PasswordHash = "old-hash" };
        _userRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.HashPassword(user, "NewPass1")).Returns("new-hash");

        await _sut.ResetPasswordAsync(2, new ResetPasswordRequestDto { NewPassword = "NewPass1" });

        Assert.Equal("new-hash", user.PasswordHash);
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAllActiveForUserAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_throws_when_the_user_does_not_exist()
    {
        _userRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ResetPasswordAsync(99, new ResetPasswordRequestDto { NewPassword = "NewPass1" }));

        _refreshTokenRepository.Verify(
            r => r.RevokeAllActiveForUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
