namespace DailyMart.Application.Purchases;

/// <summary>Purchase/return numbers are computed from Id, never stored - see Purchase's doc comment.</summary>
internal static class PurchaseNumberFormatter
{
    public static string FormatPurchase(long id) => $"PUR-{id:D6}";

    public static string FormatReturn(long id) => $"PRET-{id:D6}";
}
