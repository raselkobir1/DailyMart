using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Customers;

namespace DailyMart.Application.Customers;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Customer> Repository => _unitOfWork.Repository<Customer>();

    private IRepository<CustomerLedgerEntry> LedgerRepository => _unitOfWork.Repository<CustomerLedgerEntry>();

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Customer, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : customer => customer.Name.Contains(request.SearchTerm)
                || (customer.Phone != null && customer.Phone.Contains(request.SearchTerm));

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<CustomerDto>
        {
            Items = result.Items.Select(c => c.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<CustomerDto> CreateAsync(
        CustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsurePhoneIsUniqueAsync(request.Phone, excludeId: null, cancellationToken);

        var customer = request.ToEntity();
        await Repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateAsync(
        long id, CustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var customer = await GetEntityAsync(id, cancellationToken);

        await EnsurePhoneIsUniqueAsync(request.Phone, id, cancellationToken);

        request.ApplyTo(customer);

        Repository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await GetEntityAsync(id, cancellationToken);

        Repository.Remove(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<CustomerLedgerEntryDto>> GetLedgerAsync(
        long customerId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (!await Repository.ExistsAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        var effectiveRequest = string.IsNullOrWhiteSpace(request.SortBy)
            ? new PagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = nameof(CustomerLedgerEntry.TransactionDate),
                SortDescending = false
            }
            : request;

        var result = await LedgerRepository.GetPagedAsync(
            effectiveRequest, entry => entry.CustomerId == customerId, cancellationToken);

        return new PagedResult<CustomerLedgerEntryDto>
        {
            Items = result.Items.Select(e => e.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task AdjustDueAsync(
        long customerId,
        decimal amount,
        CustomerLedgerEntryType entryType,
        string description,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetEntityAsync(customerId, cancellationToken);

        var previousDue = customer.CurrentDue;
        var newDue = previousDue + amount;
        if (newDue < 0)
        {
            // Clamp rather than throw - CLAUDE.md §8: "collection is capped at outstanding due... not a
            // negative due." The ledger entry below records what was actually applied (appliedAmount), not
            // the raw requested amount, so CurrentDue always reconciles to the sum of its ledger entries.
            newDue = 0;
        }
        var appliedAmount = newDue - previousDue;

        customer.CurrentDue = newDue;
        Repository.Update(customer);

        var entry = new CustomerLedgerEntry
        {
            CustomerId = customerId,
            EntryType = entryType,
            Description = description,
            Amount = appliedAmount,
            BalanceAfter = customer.CurrentDue,
            TransactionDate = DateTimeOffset.UtcNow
        };

        await LedgerRepository.AddAsync(entry, cancellationToken);
    }

    private async Task<Customer> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Customer), id);

    /// <summary>A no-op when phone is null/blank - unlike Name, Phone is optional, and only enforced
    /// unique when the caller actually provides one (Module 6 Step 1's scope decision).</summary>
    private async Task EnsurePhoneIsUniqueAsync(string? phone, long? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        var duplicateExists = await Repository.ExistsAsync(
            customer => customer.Phone == phone && (excludeId == null || customer.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A customer with phone '{phone}' already exists.");
        }
    }
}
