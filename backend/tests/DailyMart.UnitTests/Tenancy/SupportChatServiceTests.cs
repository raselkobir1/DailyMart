using System.Linq.Expressions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Tenancy;
using Moq;

namespace DailyMart.UnitTests.Tenancy;

public class SupportChatServiceTests
{
    private readonly Mock<IRepository<SupportMessage>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupportChatRealtimeNotifier> _realtimeNotifier = new();

    public SupportChatServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<SupportMessage>()).Returns(_repository.Object);
    }

    private SupportChatService CreateSut() => new(_unitOfWork.Object, _realtimeNotifier.Object);

    [Fact]
    public async Task SendFromTenantAsync_marks_the_tenant_side_read_and_the_platform_side_unread()
    {
        SupportMessage? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<SupportMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SupportMessage, CancellationToken>((m, _) => added = m)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.SendFromTenantAsync(5, "Hello, I need help.");

        Assert.NotNull(added);
        Assert.Equal(5, added!.TenantId);
        Assert.False(added.FromPlatformAdmin);
        Assert.True(added.IsReadByTenant);
        Assert.False(added.IsReadByPlatformAdmin);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendFromPlatformAdminAsync_marks_the_platform_side_read_and_the_tenant_side_unread()
    {
        SupportMessage? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<SupportMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SupportMessage, CancellationToken>((m, _) => added = m)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.SendFromPlatformAdminAsync(5, "How can we help?");

        Assert.NotNull(added);
        Assert.True(added!.FromPlatformAdmin);
        Assert.False(added.IsReadByTenant);
        Assert.True(added.IsReadByPlatformAdmin);
    }

    [Fact]
    public async Task Send_pushes_the_new_message_via_the_realtime_notifier()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<SupportMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SupportMessage, CancellationToken>((m, _) => m.Id = 42)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SendFromTenantAsync(5, "Hello");

        Assert.Equal(42, result.Id);
        _realtimeNotifier.Verify(
            n => n.NotifyNewMessageAsync(5, It.Is<SupportMessageDto>(d => d.Id == 42 && d.Message == "Hello"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConversationAsync_returns_messages_in_chronological_order()
    {
        // GetPagedAsync's unsorted default returns most-recent-first; the service must reverse it.
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SupportMessage>
            {
                Items =
                [
                    new SupportMessage { Id = 3, TenantId = 5, Message = "third", CreatedBy = "admin" },
                    new SupportMessage { Id = 2, TenantId = 5, Message = "second", CreatedBy = "admin" },
                    new SupportMessage { Id = 1, TenantId = 5, Message = "first", CreatedBy = "admin" }
                ],
                TotalCount = 3,
                PageNumber = 1,
                PageSize = 50
            });

        var sut = CreateSut();
        var result = await sut.GetConversationAsync(5, 50);

        Assert.Equal(["first", "second", "third"], result.Select(m => m.Message));
    }

    [Fact]
    public async Task GetUnreadCountForTenantAsync_counts_only_unread_platform_admin_messages()
    {
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SupportMessage { Id = 1 }, new SupportMessage { Id = 2 }]);

        var sut = CreateSut();
        var count = await sut.GetUnreadCountForTenantAsync(5);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetUnreadCountsForPlatformAdminAsync_groups_by_tenant()
    {
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SupportMessage { Id = 1, TenantId = 5 },
                new SupportMessage { Id = 2, TenantId = 5 },
                new SupportMessage { Id = 3, TenantId = 7 }
            ]);

        var sut = CreateSut();
        var counts = await sut.GetUnreadCountsForPlatformAdminAsync([5, 7]);

        Assert.Equal(2, counts[5]);
        Assert.Equal(1, counts[7]);
    }

    [Fact]
    public async Task GetUnreadCountsForPlatformAdminAsync_returns_empty_for_no_tenant_ids()
    {
        var sut = CreateSut();
        var counts = await sut.GetUnreadCountsForPlatformAdminAsync([]);

        Assert.Empty(counts);
        _repository.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkReadByTenantAsync_marks_every_unread_platform_admin_message_read_and_saves()
    {
        var messages = new List<SupportMessage>
        {
            new() { Id = 1, IsReadByTenant = false },
            new() { Id = 2, IsReadByTenant = false }
        };
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var sut = CreateSut();
        await sut.MarkReadByTenantAsync(5);

        Assert.All(messages, m => Assert.True(m.IsReadByTenant));
        _repository.Verify(r => r.Update(It.IsAny<SupportMessage>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkReadByTenantAsync_is_a_noop_when_nothing_is_unread()
    {
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        await sut.MarkReadByTenantAsync(5);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkReadByPlatformAdminAsync_marks_every_unread_tenant_message_read_and_saves()
    {
        var messages = new List<SupportMessage> { new() { Id = 1, IsReadByPlatformAdmin = false } };
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var sut = CreateSut();
        await sut.MarkReadByPlatformAdminAsync(5);

        Assert.True(messages[0].IsReadByPlatformAdmin);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SentMessage_maps_CreatedBy_as_SenderName()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<SupportMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SupportMessage, CancellationToken>((m, _) =>
            {
                m.Id = 1;
                // Simulates AuditingSaveChangesInterceptor stamping CreatedBy on save.
                m.CreatedBy = "shopowner1";
            })
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SendFromTenantAsync(5, "Hi");

        Assert.Equal("shopowner1", result.SenderName);
    }
}
