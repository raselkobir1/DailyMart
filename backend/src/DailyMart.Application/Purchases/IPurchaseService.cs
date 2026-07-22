using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Purchases;

public interface IPurchaseService
{
    Task<PagedResult<PurchaseDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<PurchaseDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Computes each item's LineTotal and the header's Subtotal/Total/Paid/Due amounts, posts one
    /// InventoryTransaction per item, and (if anything is owed) one SupplierLedgerEntry via
    /// ISupplierService.AdjustDueAsync - all committed together in one atomic operation.</summary>
    Task<PurchaseDto> CreateAsync(PurchaseRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Full reverse-and-reapply: undoes this purchase's original stock/due effects with new,
    /// visible correction rows, then re-applies the new request's effects exactly like CreateAsync would -
    /// see Module 7 Step 1's scope decision. Nothing is ever silently overwritten.</summary>
    Task<PurchaseDto> UpdateAsync(long id, PurchaseRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Reverses this purchase's stock/due effects the same way UpdateAsync does, then soft-deletes
    /// the purchase and its items - deleting a posted purchase must not leave stock/due permanently wrong.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
