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
        _sut = new PlatformTenantService(_unitOfWork.Object, _subscriptionService.Object, _usageAnalyticsService.Object);
    }

    [Fact]
    public async Task GetPagedAsync_maps_tenants_to_summary_dtos()
    {
        var tenant = new Tenant { Id = 1, Name = "Acme Corp", IsActive = true };
        _tenantRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Tenant> { Items = [tenant], TotalCount = 1, PageNumber = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Acme Corp", dto.Name);
        Assert.True(dto.IsActive);
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
    }
}
