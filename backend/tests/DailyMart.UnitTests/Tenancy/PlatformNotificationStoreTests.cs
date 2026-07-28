using System.Linq.Expressions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Tenancy;
using Moq;

namespace DailyMart.UnitTests.Tenancy;

public class PlatformNotificationStoreTests
{
    private readonly Mock<IRepository<PlatformNotification>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public PlatformNotificationStoreTests()
    {
        _unitOfWork.Setup(u => u.Repository<PlatformNotification>()).Returns(_repository.Object);
    }

    private PlatformNotificationStore CreateSut() => new(_unitOfWork.Object);

    [Fact]
    public async Task RecordNewTenantSignupAsync_creates_an_unread_row_and_returns_it()
    {
        PlatformNotification? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<PlatformNotification>(), It.IsAny<CancellationToken>()))
            .Callback<PlatformNotification, CancellationToken>((n, _) =>
            {
                n.Id = 42;
                added = n;
            })
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RecordNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        Assert.NotNull(added);
        Assert.Equal("NewTenantSignup", added!.Type);
        Assert.Equal(5, added.TenantId);
        Assert.Equal("Acme Corp", added.TenantName);
        Assert.Equal("newadmin", added.AdminUsername);
        Assert.False(added.IsRead);

        Assert.Equal(42, result.Id);
        Assert.Equal("Acme Corp", result.TenantName);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_counts_only_unread_rows()
    {
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlatformNotification, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlatformNotification { Id = 1 }, new PlatformNotification { Id = 2 }]);

        var sut = CreateSut();
        var count = await sut.GetUnreadCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_marks_an_unread_notification_read_and_saves()
    {
        var notification = new PlatformNotification { Id = 7, IsRead = false };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var sut = CreateSut();
        await sut.MarkAsReadAsync(7);

        Assert.True(notification.IsRead);
        _repository.Verify(r => r.Update(notification), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_is_a_noop_when_already_read()
    {
        var notification = new PlatformNotification { Id = 7, IsRead = true };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var sut = CreateSut();
        await sut.MarkAsReadAsync(7);

        _repository.Verify(r => r.Update(It.IsAny<PlatformNotification>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_is_a_noop_when_the_id_does_not_exist()
    {
        _repository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((PlatformNotification?)null);

        var sut = CreateSut();
        var exception = await Record.ExceptionAsync(() => sut.MarkAsReadAsync(999));

        Assert.Null(exception);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecentAsync_maps_the_paged_result_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(
                It.Is<PagedRequest>(req => req.PageSize == 5),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PlatformNotification>
            {
                Items = [new PlatformNotification { Id = 1, Type = "NewTenantSignup", TenantName = "Acme Corp" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 5
            });

        var sut = CreateSut();
        var result = await sut.GetRecentAsync(5);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("Acme Corp", item.TenantName);
    }
}
