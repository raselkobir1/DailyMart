namespace DailyMart.Application.Dashboard;

public class DashboardTrendPointDto
{
    public DateOnly Date { get; set; }

    public decimal Sales { get; set; }

    public decimal Purchases { get; set; }

    public decimal Profit { get; set; }
}
