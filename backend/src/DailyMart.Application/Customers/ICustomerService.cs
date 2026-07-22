using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CustomerRequestDto request, CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateAsync(long id, CustomerRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
