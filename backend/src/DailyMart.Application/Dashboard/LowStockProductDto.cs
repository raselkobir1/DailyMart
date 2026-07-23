namespace DailyMart.Application.Dashboard;

public class LowStockProductDto
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }
}
