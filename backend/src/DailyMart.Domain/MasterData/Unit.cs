using DailyMart.Domain.Common;

namespace DailyMart.Domain.MasterData;

/// <summary>e.g. Name "Kilogram", Symbol "kg" - the symbol is the compact form used on invoices/labels.</summary>
public class Unit : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;
}
