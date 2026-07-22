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
