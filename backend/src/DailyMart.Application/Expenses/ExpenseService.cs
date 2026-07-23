using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Expenses;

namespace DailyMart.Application.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Expense> Repository => _unitOfWork.Repository<Expense>();

    public async Task<PagedResult<ExpenseDto>> GetPagedAsync(
        PagedRequest request,
        ExpenseCategory? category = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = request.SearchTerm;
        Expression<Func<Expense, bool>> predicate = expense =>
            (string.IsNullOrWhiteSpace(searchTerm)
                || (expense.Description != null && expense.Description.Contains(searchTerm)))
            && (category == null || expense.Category == category)
            && (fromDate == null || expense.ExpenseDate >= fromDate)
            && (toDate == null || expense.ExpenseDate <= toDate);

        var effectiveRequest = string.IsNullOrWhiteSpace(request.SortBy)
            ? new PagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = nameof(Expense.ExpenseDate),
                SortDescending = true
            }
            : request;

        var result = await Repository.GetPagedAsync(effectiveRequest, predicate, cancellationToken);

        return new PagedResult<ExpenseDto>
        {
            Items = result.Items.Select(e => e.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<ExpenseDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<ExpenseDto> CreateAsync(ExpenseRequestDto request, CancellationToken cancellationToken = default)
    {
        var expense = request.ToEntity();
        await Repository.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expense.ToDto();
    }

    public async Task<ExpenseDto> UpdateAsync(
        long id, ExpenseRequestDto request, CancellationToken cancellationToken = default)
    {
        var expense = await GetEntityAsync(id, cancellationToken);

        request.ApplyTo(expense);

        Repository.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expense.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var expense = await GetEntityAsync(id, cancellationToken);

        Repository.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Expense> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Expense), id);
}
