using System.Linq.Expressions;
using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Billing;
using DailyMart.Domain.Tenancy;
using Moq;

namespace DailyMart.UnitTests.Billing;

public class SubscriptionServiceTests
{
    private readonly Mock<IRepository<TenantSubscription>> _subscriptionRepository = new();
    private readonly Mock<IRepository<Plan>> _planRepository = new();
    private readonly Mock<IRepository<SubscriptionPayment>> _paymentRepository = new();
    private readonly Mock<IRepository<Tenant>> _tenantRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SubscriptionService _sut;

    public SubscriptionServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<TenantSubscription>()).Returns(_subscriptionRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Plan>()).Returns(_planRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SubscriptionPayment>()).Returns(_paymentRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Tenant>()).Returns(_tenantRepository.Object);
        _sut = new SubscriptionService(_unitOfWork.Object);

        _tenantRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = 1, Name = "Acme" });
    }

    private void SetSubscription(TenantSubscription subscription) =>
        _subscriptionRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TenantSubscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);

    [Fact]
    public async Task ChangePlanAsync_switching_to_a_free_plan_clears_CurrentPeriodEnd()
    {
        var subscription = new TenantSubscription
        {
            TenantId = 1, PlanId = 2, CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(10)
        };
        SetSubscription(subscription);

        var freePlan = new Plan { Id = 9, Name = "Free", IsFree = true, IsActive = true };
        _planRepository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(freePlan);

        var result = await _sut.ChangePlanAsync(1, 9);

        Assert.Null(result.CurrentPeriodEnd);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task ChangePlanAsync_switching_from_free_to_paid_leaves_CurrentPeriodEnd_null_and_is_overdue()
    {
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 9, CurrentPeriodEnd = null };
        SetSubscription(subscription);

        var paidPlan = new Plan { Id = 2, Name = "Basic", IsFree = false, IsActive = true, Price = 500m };
        _planRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(paidPlan);

        var result = await _sut.ChangePlanAsync(1, 2);

        Assert.Null(result.CurrentPeriodEnd);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task ChangePlanAsync_between_two_paid_plans_keeps_the_existing_CurrentPeriodEnd()
    {
        var paidUntil = DateTimeOffset.UtcNow.AddDays(15);
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 2, CurrentPeriodEnd = paidUntil };
        SetSubscription(subscription);

        var proPlan = new Plan { Id = 3, Name = "Pro", IsFree = false, IsActive = true, Price = 1500m };
        _planRepository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(proPlan);

        var result = await _sut.ChangePlanAsync(1, 3);

        Assert.Equal(paidUntil, result.CurrentPeriodEnd);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task ChangePlanAsync_throws_when_the_target_plan_is_retired()
    {
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 9 };
        SetSubscription(subscription);

        var retiredPlan = new Plan { Id = 4, Name = "Legacy", IsActive = false };
        _planRepository.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(retiredPlan);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ChangePlanAsync(1, 4));
    }

    [Fact]
    public async Task RecordPaymentAsync_throws_when_the_current_plan_is_free()
    {
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 9 };
        SetSubscription(subscription);

        var freePlan = new Plan { Id = 9, Name = "Free", IsFree = true };
        _planRepository.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(freePlan);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.RecordPaymentAsync(1, new RecordPaymentRequestDto { Amount = 500m, PaidUntil = DateTimeOffset.UtcNow.AddMonths(1), Method = "Cash" }));

        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordPaymentAsync_continues_from_the_existing_paid_through_date_when_still_valid()
    {
        var existingPeriodEnd = DateTimeOffset.UtcNow.AddDays(5);
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 2, CurrentPeriodEnd = existingPeriodEnd };
        SetSubscription(subscription);

        var plan = new Plan { Id = 2, Name = "Basic", IsFree = false };
        _planRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var paidUntil = existingPeriodEnd.AddMonths(1);
        SubscriptionPayment? captured = null;
        _paymentRepository
            .Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.RecordPaymentAsync(1, new RecordPaymentRequestDto
        {
            Amount = 500m, PaidUntil = paidUntil, Method = "Bank Transfer"
        });

        Assert.NotNull(captured);
        Assert.Equal(existingPeriodEnd, captured!.PeriodStart);
        Assert.Equal(paidUntil, captured.PeriodEnd);
        Assert.Equal(paidUntil, result.PeriodEnd);

        // Reconciliation invariant: the subscription's CurrentPeriodEnd always matches the latest payment.
        Assert.Equal(paidUntil, subscription.CurrentPeriodEnd);
    }

    [Fact]
    public async Task RecordPaymentAsync_starts_the_new_period_from_now_when_the_subscription_has_lapsed()
    {
        var lapsedPeriodEnd = DateTimeOffset.UtcNow.AddDays(-10);
        var subscription = new TenantSubscription { TenantId = 1, PlanId = 2, CurrentPeriodEnd = lapsedPeriodEnd };
        SetSubscription(subscription);

        var plan = new Plan { Id = 2, Name = "Basic", IsFree = false };
        _planRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        SubscriptionPayment? captured = null;
        _paymentRepository
            .Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var beforeCall = DateTimeOffset.UtcNow;
        await _sut.RecordPaymentAsync(1, new RecordPaymentRequestDto
        {
            Amount = 500m, PaidUntil = DateTimeOffset.UtcNow.AddMonths(1), Method = "Cash"
        });

        Assert.NotNull(captured);
        Assert.True(captured!.PeriodStart >= beforeCall);
    }
}
