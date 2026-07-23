using DailyMart.Domain.Customers;

namespace DailyMart.Application.Customers;

internal static class CustomerMappingExtensions
{
    public static CustomerDto ToDto(this Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address,
        CurrentDue = customer.CurrentDue
    };

    public static Customer ToEntity(this CustomerRequestDto request) => new()
    {
        Name = request.Name,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address
    };

    public static void ApplyTo(this CustomerRequestDto request, Customer customer)
    {
        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
    }

    public static CustomerLedgerEntryDto ToDto(this CustomerLedgerEntry entry) => new()
    {
        Id = entry.Id,
        CustomerId = entry.CustomerId,
        EntryType = entry.EntryType.ToString(),
        Description = entry.Description,
        Amount = entry.Amount,
        BalanceAfter = entry.BalanceAfter,
        TransactionDate = entry.TransactionDate
    };
}
