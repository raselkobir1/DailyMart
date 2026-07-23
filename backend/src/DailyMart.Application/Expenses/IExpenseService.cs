using DailyMart.Application.Common.Models;
using DailyMart.Domain.Expenses;

namespace DailyMart.Application.Expenses;

public interface IExpenseService
{
    /// <summary>Newest-first by default (like most lists, unlike a ledger). category/fromDate/toDate are
    /// optional filters, mirroring IInventoryService.GetTransactionHistoryAsync's optional-productId
    /// pattern rather than growing PagedRequest with module-specific fields.</summary>
    Task<PagedResult<ExpenseDto>> GetPagedAsync(
        PagedRequest request,
        ExpenseCategory? category = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ExpenseDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<ExpenseDto> CreateAsync(ExpenseRequestDto request, CancellationToken cancellationToken = default);

    Task<ExpenseDto> UpdateAsync(long id, ExpenseRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
