namespace DailyMart.Application.Purchases;

public class PurchaseReturnDto
{
    public long Id { get; init; }

    /// <summary>Computed from Id, never stored - same as PurchaseDto.PurchaseNumber.</summary>
    public string ReturnNumber { get; init; } = string.Empty;

    public long PurchaseId { get; init; }

    public string PurchaseNumber { get; init; } = string.Empty;

    public DateTimeOffset ReturnDate { get; init; }

    public decimal TotalAmount { get; init; }

    public string? Notes { get; init; }

    public List<PurchaseReturnItemDto> Items { get; init; } = [];
}
