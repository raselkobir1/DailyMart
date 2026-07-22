using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Suppliers;

internal static class SupplierMappingExtensions
{
    public static SupplierDto ToDto(this Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        ContactPerson = supplier.ContactPerson,
        Phone = supplier.Phone,
        Email = supplier.Email,
        Address = supplier.Address,
        OpeningBalance = supplier.OpeningBalance,
        CurrentDue = supplier.CurrentDue
    };

    public static Supplier ToEntity(this CreateSupplierRequestDto request) => new()
    {
        Name = request.Name,
        ContactPerson = request.ContactPerson,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        OpeningBalance = request.OpeningBalance
    };

    /// <summary>Doesn't touch OpeningBalance/CurrentDue - see Module 5 Step 1's scope decision.</summary>
    public static void ApplyTo(this SupplierRequestDto request, Supplier supplier)
    {
        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
    }

    public static SupplierLedgerEntryDto ToDto(this SupplierLedgerEntry entry) => new()
    {
        Id = entry.Id,
        SupplierId = entry.SupplierId,
        EntryType = entry.EntryType.ToString(),
        Description = entry.Description,
        Amount = entry.Amount,
        BalanceAfter = entry.BalanceAfter,
        TransactionDate = entry.TransactionDate
    };
}
