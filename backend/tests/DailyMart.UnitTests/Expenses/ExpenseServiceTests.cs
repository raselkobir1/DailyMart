using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Expenses;
using DailyMart.Domain.Expenses;
using Moq;

namespace DailyMart.UnitTests.Expenses;

public class ExpenseServiceTests
{
    private readonly Mock<IRepository<Expense>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ExpenseService _sut;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    public ExpenseServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Expense>()).Returns(_repository.Object);
        _sut = new ExpenseService(_unitOfWork.Object);
    }

    private static ExpenseRequestDto ValidRequest(
        ExpenseCategory category = ExpenseCategory.Rent, decimal amount = 5000) => new()
    {
        Category = category,
        Amount = amount,
        Description = "Monthly rent",
        ExpenseDate = Now
    };

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_adds_and_saves_and_returns_the_mapped_dto()
    {
        var result = await _sut.CreateAsync(ValidRequest(ExpenseCategory.Salary, 20000));

        Assert.Equal("Salary", result.Category);
        Assert.Equal(20000, result.Amount);
        _repository.Verify(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, ValidRequest()));
    }

    [Fact]
    public async Task UpdateAsync_applies_every_field_from_the_request()
    {
        var existing = new Expense
        {
            Id = 5,
            Category = ExpenseCategory.Rent,
            Amount = 5000,
            Description = "Old",
            ExpenseDate = Now.AddDays(-10)
        };
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(5, ValidRequest(ExpenseCategory.Electricity, 750));

        Assert.Equal("Electricity", result.Category);
        Assert.Equal(750, result.Amount);
        Assert.Equal("Monthly rent", result.Description);
        _repository.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_expense_exists()
    {
        var existing = new Expense { Id = 7, Category = ExpenseCategory.Internet, Amount = 1200 };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _repository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_category_when_provided()
    {
        Expression<Func<Expense, bool>>? capturedPredicate = null;
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Expense, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<Expense, bool>>?, CancellationToken>((_, predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new PagedResult<Expense> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetPagedAsync(new PagedRequest(), category: ExpenseCategory.Rent);

        var isIncluded = capturedPredicate!.Compile();
        Assert.True(isIncluded(new Expense { Category = ExpenseCategory.Rent, ExpenseDate = Now }));
        Assert.False(isIncluded(new Expense { Category = ExpenseCategory.Salary, ExpenseDate = Now }));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_date_range_when_provided()
    {
        Expression<Func<Expense, bool>>? capturedPredicate = null;
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Expense, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<Expense, bool>>?, CancellationToken>((_, predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new PagedResult<Expense> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetPagedAsync(new PagedRequest(), fromDate: Now.AddDays(-7), toDate: Now);

        var isIncluded = capturedPredicate!.Compile();
        Assert.True(isIncluded(new Expense { Category = ExpenseCategory.Rent, ExpenseDate = Now.AddDays(-3) }));
        Assert.False(isIncluded(new Expense { Category = ExpenseCategory.Rent, ExpenseDate = Now.AddDays(-30) }));
    }

    [Fact]
    public async Task GetPagedAsync_defaults_to_sorting_by_ExpenseDate_descending()
    {
        PagedRequest? capturedRequest = null;
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Expense, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<Expense, bool>>?, CancellationToken>((request, _, _) => capturedRequest = request)
            .ReturnsAsync(new PagedResult<Expense> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetPagedAsync(new PagedRequest());

        Assert.Equal(nameof(Expense.ExpenseDate), capturedRequest!.SortBy);
        Assert.True(capturedRequest.SortDescending);
    }
}
