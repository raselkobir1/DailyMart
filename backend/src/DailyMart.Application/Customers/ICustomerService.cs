using DailyMart.Application.Common.Models;
using DailyMart.Domain.Customers;

namespace DailyMart.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CustomerRequestDto request, CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateAsync(long id, CustomerRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Defaults to oldest-first (TransactionDate ascending) unless the caller specifies a
    /// different sort - mirrors ISupplierService.GetLedgerAsync.</summary>
    Task<PagedResult<CustomerLedgerEntryDto>> GetLedgerAsync(
        long customerId, PagedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a ledger entry and updates CurrentDue together - mirrors ISupplierService.AdjustDueAsync,
    /// with one addition: CLAUDE.md's business rule ("customer due cannot go negative") is enforced here by
    /// clamping the applied amount so CurrentDue never drops below zero - the ledger entry's Amount reflects
    /// what was actually applied, not the raw requested amount, so it always reconciles to CurrentDue.
    /// Stage-only: does not call SaveChangesAsync, so callers (e.g. SaleService) can compose this into one
    /// larger atomic commit alongside other staged changes.</summary>
    Task AdjustDueAsync(
        long customerId,
        decimal amount,
        CustomerLedgerEntryType entryType,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>Module 10's payment-collection endpoint - built on top of AdjustDueAsync (Payment entry
    /// type, amount applied negative and clamped at zero exactly like a sale return). Unlike AdjustDueAsync,
    /// this one commits (SaveChangesAsync) itself, since a payment is a standalone action, not staged
    /// alongside a larger unit of work the way a sale is.</summary>
    Task<CustomerDto> CollectPaymentAsync(
        long customerId,
        CollectCustomerPaymentRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Customers with CurrentDue &gt; 0, sorted highest-due-first by default - the "who owes money"
    /// report Module 10's BRD text calls for.</summary>
    Task<PagedResult<CustomerDto>> GetDueReportAsync(
        PagedRequest request, CancellationToken cancellationToken = default);
}
