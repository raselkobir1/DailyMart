namespace DailyMart.Application.Sales;

/// <summary>Sale/return numbers are computed from Id, never stored - see Sale's doc comment.</summary>
internal static class SaleNumberFormatter
{
    public static string FormatSale(long id) => $"SALE-{id:D6}";

    public static string FormatReturn(long id) => $"SRET-{id:D6}";
}
