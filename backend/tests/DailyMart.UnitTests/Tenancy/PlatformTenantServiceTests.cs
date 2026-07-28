using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Tenancy;
using DailyMart.Application.UsageAnalytics;
using DailyMart.Domain.Tenancy;
using Moq;

namespace DailyMart.UnitTests.Tenancy;

public class PlatformTenantServiceTests
{
    private readonly Mock<IRepository<Tenant>> _tenantRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly Mock<IUsageAnalyticsService> _usageAnalyticsService = new();
    private readonly Mock<ISupportChatService> _supportChatService = new();
    private readonly PlatformTenantService _sut;

    public PlatformTenantServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Tenant>()).Returns(_tenantRepository.Object);
        _subscriptionService
            .Setup(s => s.GetSummariesByTenantIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, TenantSubscriptionDto>());
        _usageAnalyticsService
            .Setup(s => s.GetSnapshotsByTenantIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, TenantUsageSnapshotDto>());
        _supportChatService
            .Setup(s => s.GetUnreadCountsForPlatformAdminAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, int>());
        _sut = new PlatformTenantService(
            _unitOfWork.Object, _subscriptionService.Object, _usageAnalyticsService.Object, _supportChatService.Object);
    }

    private void SetTenants(params Tenant[] tenants) =>
        _tenantRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tenants.ToList());

    [Fact]
    public async Task GetPagedAsync_maps_tenants_to_summary_dtos()
    {
        SetTenants(new Tenant { Id = 1, Name = "Acme Corp", IsActive = true });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Acme Corp", dto.Name);
        Assert.True(dto.IsActive);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_search_term_case_insensitively()
    {
        SetTenants(
            new Tenant { Id = 1, Name = "Acme Corp", IsActive = true },
            new Tenant { Id = 2, Name = "Beta Shop", IsActive = true });

        var result = await _sut.GetPagedAsync(new PagedRequest { SearchTerm = "acme" });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Acme Corp", dto.Name);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_status()
    {
        SetTenants(
            new Tenant { Id = 1, Name = "Active Co", IsActive = true },
            new Tenant { Id = 2, Name = "Suspended Co", IsActive = false });

        var result = await _sut.GetPagedAsync(new PagedRequest(), status: "suspended");

        var dto = Assert.Single(result.Items);
        Assert.Equal("Suspended Co", dto.Name);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_billing_status_overdue()
    {
        var tenants = new[]
        {
            new Tenant { Id = 1, Name = "Overdue Co", IsActive = true },
            new Tenant { Id = 2, Name = "Paid Co", IsActive = true },
            new Tenant { Id = 3, Name = "Free Co", IsActive = true }
        };
        SetTenants(tenants);

        _usageAnalyticsService
            .Setup(s => s.GetSnapshotsByTenantIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, TenantUsageSnapshotDto>());
        _subscriptionService
            .Setup(s => s.GetSummariesByTenantIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, TenantSubscriptionDto>
            {
                [1] = new TenantSubscriptionDto { TenantId = 1, IsFree = false, IsOverdue = true },
                [2] = new TenantSubscriptionDto { TenantId = 2, IsFree = false, IsOverdue = false },
                [3] = new TenantSubscriptionDto { TenantId = 3, IsFree = true, IsOverdue = false }
            });

        var result = await _sut.GetPagedAsync(new PagedRequest(), billingStatus: "overdue");

        var dto = Assert.Single(result.Items);
        Assert.Equal("Overdue Co", dto.Name);
    }

    [Fact]
    public async Task GetPagedAsync_sorts_by_name_descending()
    {
        SetTenants(
            new Tenant { Id = 1, Name = "Alpha", IsActive = true },
            new Tenant { Id = 2, Name = "Zeta", IsActive = true });

        var result = await _sut.GetPagedAsync(new PagedRequest { SortBy = "name", SortDescending = true });

        Assert.Equal(["Zeta", "Alpha"], result.Items.Select(d => d.Name));
    }

    [Fact]
    public async Task GetPagedAsync_paginates_the_sorted_and_filtered_result()
    {
        SetTenants(
            new Tenant { Id = 1, Name = "Alpha", IsActive = true },
            new Tenant { Id = 2, Name = "Beta", IsActive = true },
            new Tenant { Id = 3, Name = "Gamma", IsActive = true });

        var result = await _sut.GetPagedAsync(new PagedRequest { SortBy = "name", PageNumber = 2, PageSize = 1 });

        Assert.Equal(["Beta"], result.Items.Select(d => d.Name));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _tenantRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(404));
    }

    [Fact]
    public async Task SetActiveAsync_suspends_a_tenant()
    {
        var tenant = new Tenant { Id = 1, Name = "Acme Corp", IsActive = true };
        _tenantRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _sut.SetActiveAsync(1, isActive: false);

        Assert.False(result.IsActive);
        Assert.False(tenant.IsActive);
        _tenantRepository.Verify(r => r.Update(tenant), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetActiveAsync_reactivates_a_suspended_tenant()
    {
        var tenant = new Tenant { Id = 1, Name = "Acme Corp", IsActive = false };
        _tenantRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _sut.SetActiveAsync(1, isActive: true);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_merges_the_usage_snapshot_into_the_summary_dto()
    {
        var tenant = new Tenant { Id = 1, Name = "Acme Corp", IsActive = true };
        _tenantRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var lastLogin = DateTimeOffset.UtcNow.AddDays(-1);
        _usageAnalyticsService
            .Setup(s => s.GetSnapshotsByTenantIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, TenantUsageSnapshotDto>
            {
                [1] = new TenantUsageSnapshotDto { TenantId = 1, TotalUsers = 3, ActiveUsers = 2, LastLoginAt = lastLogin }
            });

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(3, result.TotalUsers);
        Assert.Equal(2, result.ActiveUsers);
        Assert.Equal(lastLogin, result.LastLoginAt);
        Assert.Equal(lastLogin, result.LastActiveAt);
    }
}
