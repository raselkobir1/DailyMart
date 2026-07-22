using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Purchases;

/// <summary>Create + read only - no Update/Delete. A return is itself a correction mechanism; re-editing
/// one isn't a BRD requirement, and once posted its stock/due effects are as final as a payment's.</summary>
public interface IPurchaseReturnService
{
    Task<PagedResult<PurchaseReturnDto>> GetPagedAsync(
        long purchaseId, PagedRequest request, CancellationToken cancellationToken = default);

    Task<PurchaseReturnDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Each item's UnitPrice/LineTotal are computed from the original purchase line, not trusted
    /// from the caller. Quantity is capped at that line's quantity minus whatever's already been returned
    /// against it. Posts one negative InventoryTransaction per item and one PurchaseReturn ledger entry
    /// (decreasing what's owed the supplier) - all committed together in one atomic operation.</summary>
    Task<PurchaseReturnDto> CreateAsync(
        long purchaseId, PurchaseReturnRequestDto request, CancellationToken cancellationToken = default);
}
