using System.Linq.Expressions;
using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Billing;
using Moq;

namespace DailyMart.UnitTests.Billing;

public class PlanServiceTests
{
    private readonly Mock<IRepository<Plan>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PlanService _sut;

    public PlanServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Plan>()).Returns(_repository.Object);
        _sut = new PlanService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetPagedAsync_maps_the_page_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Plan, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Plan>
            {
                Items = [new Plan { Id = 1, Name = "Basic" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        Assert.Equal("Basic", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Plan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_case_insensitive_duplicate_name()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new PlanRequestDto { Name = "BASIC" }));

        _repository.Verify(r => r.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_unique_name_adds_and_saves()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new PlanRequestDto { Name = "Pro", Price = 999m });

        Assert.Equal("Pro", result.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_zeroes_the_price_for_a_free_plan_even_if_one_was_supplied()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new PlanRequestDto { Name = "Free", Price = 50m, IsFree = true });

        Assert.Equal(0m, result.Price);
    }

    [Fact]
    public async Task ActivateAsync_sets_IsActive_true_and_saves()
    {
        var plan = new Plan { Id = 5, Name = "Retired", IsActive = false };
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var result = await _sut.ActivateAsync(5);

        Assert.True(result.IsActive);
        _repository.Verify(r => r.Update(plan), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_sets_IsActive_false_and_saves()
    {
        var plan = new Plan { Id = 5, Name = "Pro", IsActive = true };
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var result = await _sut.DeactivateAsync(5);

        Assert.False(result.IsActive);
    }
}
