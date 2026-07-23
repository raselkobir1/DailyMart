using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Sales;

/// <summary>Create + read only - same reasoning as IPurchaseReturnService: a return is itself a correction
/// mechanism, and once posted its stock/due effects are as final as a payment's.</summary>
public interface ISaleReturnService
{
    Task<PagedResult<SaleReturnDto>> GetPagedAsync(
        long saleId, PagedRequest request, CancellationToken cancellationToken = default);

    Task<SaleReturnDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Each item's UnitPrice/LineTotal are computed from the original sale line, not trusted from
    /// the caller. Quantity is capped at that line's quantity minus whatever's already been returned
    /// against it. Posts one positive InventoryTransaction per item (stock returns to shelf) and, only when
    /// the original sale had a customer attached, one CustomerLedgerEntry reducing what they owe - a
    /// walk-in Cash sale's return only reverses stock, since there's no due to reduce and any cash refund is
    /// out of this module's scope. All committed together in one atomic operation.</summary>
    Task<SaleReturnDto> CreateAsync(
        long saleId, SaleReturnRequestDto request, CancellationToken cancellationToken = default);
}
