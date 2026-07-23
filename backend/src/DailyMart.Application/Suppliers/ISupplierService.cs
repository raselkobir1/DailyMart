using DailyMart.Application.Common.Models;
using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Creates the supplier and, if OpeningBalance != 0, the one matching OpeningBalance ledger
    /// entry - see Module 5 Step 1's scope decision.</summary>
    Task<SupplierDto> CreateAsync(CreateSupplierRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Never touches OpeningBalance/CurrentDue - see Module 5 Step 1's scope decision.</summary>
    Task<SupplierDto> UpdateAsync(long id, SupplierRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Defaults to oldest-first (TransactionDate ascending) unless the caller specifies a
    /// different sort - a ledger reads top-to-bottom like a bank statement, unlike every other module's
    /// newest-first default.</summary>
    Task<PagedResult<SupplierLedgerEntryDto>> GetLedgerAsync(
        long supplierId, PagedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a ledger entry and updates CurrentDue together - the same "one place keeps these two
    /// in lockstep" logic CreateAsync's opening-balance handling already has. Stage-only: does not call
    /// SaveChangesAsync, so callers (e.g. PurchaseService) can compose this into one larger atomic
    /// commit alongside other staged changes.</summary>
    Task AdjustDueAsync(
        long supplierId,
        decimal amount,
        SupplierLedgerEntryType entryType,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Module 11's payment endpoint - the shop owner paying down what's owed to a supplier, built
    /// on the same AdjustDueAsync used by Purchase. Deliberately does NOT clamp at zero the way
    /// CustomerService.CollectPaymentAsync does: overpaying a supplier is a valid real state (a credit/
    /// advance balance to apply against a future purchase), and CLAUDE.md's "due cannot go negative" rule
    /// is scoped to customer due only. Commits itself (SaveChangesAsync), since a payment is a standalone
    /// action, not staged alongside a larger unit of work the way a purchase is.</summary>
    Task<SupplierDto> PaySupplierAsync(
        long supplierId,
        PaySupplierRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Suppliers with CurrentDue &gt; 0, sorted highest-due-first - the "who do we owe money to"
    /// report Module 11's BRD text calls for.</summary>
    Task<PagedResult<SupplierDto>> GetDueReportAsync(
        PagedRequest request, CancellationToken cancellationToken = default);
}
