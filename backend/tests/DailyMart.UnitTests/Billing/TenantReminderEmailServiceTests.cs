using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Tenancy;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyMart.UnitTests.Billing;

public class TenantReminderEmailServiceTests
{
    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly Mock<ITenantContactLookupService> _tenantContactLookupService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly EmailOptions _emailOptions = new() { Enabled = true };

    private static TenantSubscriptionDto Overdue() => new()
    {
        TenantId = 1,
        TenantName = "Acme Corp",
        PlanId = 2,
        PlanName = "Pro",
        IsFree = false,
        Price = 999,
        CurrentPeriodStart = DateTimeOffset.UtcNow.AddDays(-60),
        CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(-5),
        IsOverdue = true
    };

    private static TenantSubscriptionDto Free() => new()
    {
        TenantId = 1,
        TenantName = "Acme Corp",
        PlanId = 1,
        PlanName = "Free",
        IsFree = true,
        Price = 0,
        CurrentPeriodStart = DateTimeOffset.UtcNow.AddDays(-60),
        CurrentPeriodEnd = null,
        IsOverdue = false
    };

    private static TenantSubscriptionDto PaidAndCurrent() => new()
    {
        TenantId = 1,
        TenantName = "Acme Corp",
        PlanId = 2,
        PlanName = "Pro",
        IsFree = false,
        Price = 999,
        CurrentPeriodStart = DateTimeOffset.UtcNow.AddDays(-10),
        CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(20),
        IsOverdue = false
    };

    public TenantReminderEmailServiceTests()
    {
        _tenantContactLookupService
            .Setup(s => s.GetShopEmailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync("owner@acme.test");
    }

    private TenantReminderEmailService CreateSut() => new(
        _subscriptionService.Object,
        _tenantContactLookupService.Object,
        _emailSender.Object,
        Options.Create(_emailOptions));

    [Fact]
    public async Task SendReminderAsync_throws_when_email_sending_is_disabled()
    {
        _emailOptions.Enabled = false;
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendReminderAsync(1));

        _subscriptionService.Verify(s => s.GetByTenantIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendReminderAsync_throws_when_the_tenant_is_neither_overdue_nor_free()
    {
        _subscriptionService.Setup(s => s.GetByTenantIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(PaidAndCurrent());
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendReminderAsync(1));

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendReminderAsync_throws_when_the_tenant_has_no_contact_email_on_file()
    {
        _subscriptionService.Setup(s => s.GetByTenantIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Overdue());
        _tenantContactLookupService.Setup(s => s.GetShopEmailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendReminderAsync(1));

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendReminderAsync_sends_the_overdue_reminder_when_the_subscription_is_overdue()
    {
        _subscriptionService.Setup(s => s.GetByTenantIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Overdue());
        var sut = CreateSut();

        var result = await sut.SendReminderAsync(1);

        Assert.Equal("Overdue", result.ReminderType);
        Assert.Equal("owner@acme.test", result.SentTo);
        _emailSender.Verify(e => e.SendAsync(
            "owner@acme.test",
            "Acme Corp",
            It.Is<string>(subject => subject.Contains("overdue", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body => body.Contains("Acme Corp") && body.Contains("Pro")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReminderAsync_sends_the_free_plan_nudge_when_the_tenant_is_on_the_free_plan()
    {
        _subscriptionService.Setup(s => s.GetByTenantIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Free());
        var sut = CreateSut();

        var result = await sut.SendReminderAsync(1);

        Assert.Equal("Free", result.ReminderType);
        Assert.Equal("owner@acme.test", result.SentTo);
        _emailSender.Verify(e => e.SendAsync(
            "owner@acme.test",
            "Acme Corp",
            It.Is<string>(subject => subject.Contains("Free", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(body => body.Contains("Acme Corp") && body.Contains("Free")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReminderAsync_prefers_the_overdue_reminder_when_a_paid_plan_is_both_overdue_and_somehow_flagged_free()
    {
        // Defensive: IsOverdue is already defined as never true when IsFree is true (SubscriptionService),
        // but this pins the service's own precedence in case that invariant is ever loosened elsewhere.
        var subscription = new TenantSubscriptionDto
        {
            TenantId = 1,
            TenantName = "Acme Corp",
            PlanId = 2,
            PlanName = "Pro",
            IsFree = true,
            Price = 999,
            CurrentPeriodStart = DateTimeOffset.UtcNow.AddDays(-60),
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(-5),
            IsOverdue = true
        };
        _subscriptionService.Setup(s => s.GetByTenantIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        var sut = CreateSut();

        var result = await sut.SendReminderAsync(1);

        Assert.Equal("Overdue", result.ReminderType);
    }
}
