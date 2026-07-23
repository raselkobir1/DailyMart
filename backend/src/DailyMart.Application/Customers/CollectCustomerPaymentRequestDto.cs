namespace DailyMart.Application.Customers;

public class CollectCustomerPaymentRequestDto
{
    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}
