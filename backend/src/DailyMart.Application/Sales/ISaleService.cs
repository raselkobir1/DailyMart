using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Sales;

/// <summary>
/// Create + read only - no Update/Delete, unlike Purchase (Module 7). The BRD's Module 9 scope lists
/// "sales return" as the correction mechanism for a posted sale, not edit/delete (unlike Purchase's
/// "entry/update/return"); a finalized POS sale is corrected via ISaleReturnService, matching how a real
/// register handles a completed transaction.
/// </summary>
public interface ISaleService
{
    Task<PagedResult<SaleDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<SaleDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Computes each item's LineTotal/UnitCost and the header's Subtotal/Total/Paid/Due/TotalCost/
    /// ProfitAmount, posts one negative InventoryTransaction per item (stock deduction - never below zero),
    /// and - only when a customer is attached and a due was created - one CustomerLedgerEntry via
    /// ICustomerService.AdjustDueAsync. All committed together in one atomic operation. Throws
    /// BusinessRuleException when PaymentType is Credit/Partial and no CustomerId is supplied - CLAUDE.md
    /// §8's "credit sale creates a customer due" requires someone to owe it.</summary>
    Task<SaleDto> CreateAsync(SaleRequestDto request, CancellationToken cancellationToken = default);
}
