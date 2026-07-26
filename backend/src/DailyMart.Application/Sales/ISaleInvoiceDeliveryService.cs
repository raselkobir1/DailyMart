namespace DailyMart.Application.Sales;

/// <summary>Sends a due reminder for an existing sale to its customer - not a general "email/print this
/// receipt" utility. Both methods enforce the same business rule: this only exists to chase an outstanding
/// balance, so a sale with no customer, or a customer with no due, or no contact info for the requested
/// channel, all fail with a clear BusinessRuleException rather than silently sending nothing useful.</summary>
public interface ISaleInvoiceDeliveryService
{
    Task SendInvoiceEmailAsync(long saleId, CancellationToken cancellationToken = default);

    Task SendInvoiceSmsAsync(long saleId, CancellationToken cancellationToken = default);
}
