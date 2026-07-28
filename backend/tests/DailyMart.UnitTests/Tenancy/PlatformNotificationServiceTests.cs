using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyMart.UnitTests.Tenancy;

public class PlatformNotificationServiceTests
{
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IPlatformRealtimeNotifier> _platformRealtimeNotifier = new();
    private readonly Mock<IPlatformNotificationStore> _platformNotificationStore = new();
    private readonly Mock<ILogger<PlatformNotificationService>> _logger = new();
    private readonly EmailOptions _emailOptions = new() { Enabled = true, PlatformNotificationAddress = "owner@platform.test" };

    private static readonly DateTimeOffset RecordedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public PlatformNotificationServiceTests()
    {
        _platformNotificationStore
            .Setup(s => s.RecordNewTenantSignupAsync(5, "Acme Corp", "newadmin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformNotificationDto
            {
                Id = 42,
                Type = "NewTenantSignup",
                TenantId = 5,
                TenantName = "Acme Corp",
                AdminUsername = "newadmin",
                CreatedAt = RecordedAt
            });
    }

    private PlatformNotificationService CreateSut() => new(
        _emailSender.Object, _platformRealtimeNotifier.Object, _platformNotificationStore.Object,
        Options.Create(_emailOptions), _logger.Object);

    [Fact]
    public async Task NotifyNewTenantSignupAsync_sends_when_configured()
    {
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _emailSender.Verify(e => e.SendAsync(
            "owner@platform.test",
            "Platform Owner",
            It.Is<string>(subject => subject.Contains("Acme Corp")),
            It.Is<string>(body => body.Contains("Acme Corp") && body.Contains("newadmin")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_does_not_throw_and_does_not_send_email_when_email_is_disabled()
    {
        _emailOptions.Enabled = false;
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_does_not_throw_and_does_not_send_email_when_no_address_is_configured()
    {
        _emailOptions.PlatformNotificationAddress = null;
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_swallows_an_email_send_failure_rather_than_throwing()
    {
        _emailSender
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP is unreachable"));
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_always_pushes_the_realtime_notification_regardless_of_email_config()
    {
        _emailOptions.Enabled = false;
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _platformRealtimeNotifier.Verify(
            n => n.NotifyNewTenantSignupAsync(42, 5, "Acme Corp", "newadmin", RecordedAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_still_pushes_realtime_even_when_the_email_send_throws()
    {
        _emailSender
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP is unreachable"));
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _platformRealtimeNotifier.Verify(
            n => n.NotifyNewTenantSignupAsync(42, 5, "Acme Corp", "newadmin", RecordedAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_always_records_the_notification_regardless_of_email_config()
    {
        _emailOptions.Enabled = false;
        var sut = CreateSut();

        await sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin");

        _platformNotificationStore.Verify(
            s => s.RecordNewTenantSignupAsync(5, "Acme Corp", "newadmin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyNewTenantSignupAsync_still_pushes_realtime_and_sends_email_even_when_persisting_fails()
    {
        _platformNotificationStore
            .Setup(s => s.RecordNewTenantSignupAsync(5, "Acme Corp", "newadmin", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database is unreachable"));
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.NotifyNewTenantSignupAsync(5, "Acme Corp", "newadmin"));

        Assert.Null(exception);
        _emailSender.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _platformRealtimeNotifier.Verify(
            n => n.NotifyNewTenantSignupAsync(0, 5, "Acme Corp", "newadmin", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
