namespace DailyMart.Application.Dashboard;

public class TopSellingProductDto
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}
