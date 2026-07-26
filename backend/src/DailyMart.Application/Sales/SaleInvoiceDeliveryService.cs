using System.Text;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Customers;
using DailyMart.Application.Settings;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Sales;
using Microsoft.Extensions.Options;

namespace DailyMart.Application.Sales;

public class SaleInvoiceDeliveryService : ISaleInvoiceDeliveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopSettingsService _shopSettingsService;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly EmailOptions _emailOptions;
    private readonly SmsOptions _smsOptions;

    public SaleInvoiceDeliveryService(
        IUnitOfWork unitOfWork,
        IShopSettingsService shopSettingsService,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IOptions<EmailOptions> emailOptions,
        IOptions<SmsOptions> smsOptions)
    {
        _unitOfWork = unitOfWork;
        _shopSettingsService = shopSettingsService;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _emailOptions = emailOptions.Value;
        _smsOptions = smsOptions.Value;
    }

    public async Task SendInvoiceEmailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        if (!_emailOptions.Enabled)
        {
            throw new BusinessRuleException("Email sending is not configured for this shop.");
        }

        var (sale, items, customer) = await LoadDueSaleContextAsync(saleId, cancellationToken);

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            throw new BusinessRuleException($"{customer.Name} has no email address on file.");
        }

        var shop = await _shopSettingsService.GetAsync(cancellationToken);
        var saleNumber = SaleNumberFormatter.FormatSale(sale.Id);

        await _emailSender.SendAsync(
            customer.Email,
            customer.Name,
            $"Invoice {saleNumber} - {shop.ShopName}",
            BuildEmailBody(shop, saleNumber, sale, items, customer),
            cancellationToken);
    }

    public async Task SendInvoiceSmsAsync(long saleId, CancellationToken cancellationToken = default)
    {
        if (!_smsOptions.Enabled)
        {
            throw new BusinessRuleException("SMS sending is not configured for this shop.");
        }

        var (sale, _, customer) = await LoadDueSaleContextAsync(saleId, cancellationToken);

        if (string.IsNullOrWhiteSpace(customer.Phone))
        {
            throw new BusinessRuleException($"{customer.Name} has no phone number on file.");
        }

        var shop = await _shopSettingsService.GetAsync(cancellationToken);
        var saleNumber = SaleNumberFormatter.FormatSale(sale.Id);

        await _smsSender.SendAsync(customer.Phone, BuildSmsMessage(shop.ShopName, shop.CurrencySymbol, saleNumber, customer), cancellationToken);
    }

    /// <summary>Shared load + validation for both channels: the sale must have a customer, and that
    /// customer must actually owe something - this feature exists to chase a due, not to email/text every
    /// receipt, so a fully-paid sale/customer is rejected here rather than left to the caller to check.</summary>
    private async Task<(Sale Sale, IReadOnlyList<SaleItem> Items, Customer Customer)> LoadDueSaleContextAsync(
        long saleId, CancellationToken cancellationToken)
    {
        var sale = await _unitOfWork.Repository<Sale>().GetByIdAsync(saleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), saleId);

        if (sale.CustomerId is null)
        {
            throw new BusinessRuleException("This sale has no customer to send an invoice to.");
        }

        var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(sale.CustomerId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), sale.CustomerId.Value);

        if (customer.CurrentDue <= 0)
        {
            throw new BusinessRuleException($"{customer.Name} has no outstanding due - nothing to remind them about.");
        }

        var items = await _unitOfWork.Repository<SaleItem>().FindAsync(i => i.SaleId == saleId, cancellationToken);

        return (sale, items, customer);
    }

    private static string BuildEmailBody(
        ShopSettingsDto shop, string saleNumber, Sale sale, IReadOnlyList<SaleItem> items, Customer customer)
    {
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            rows.Append(
                $"<tr><td>{item.Quantity}</td><td>{item.UnitPrice:0.##}</td><td>{item.LineTotal:0.##}</td></tr>");
        }

        return $"""
            <h2>{shop.ShopName}</h2>
            <p>Dear {customer.Name},</p>
            <p>Here is your invoice <strong>{saleNumber}</strong> dated {sale.SaleDate:d}.</p>
            <table border="1" cellpadding="6" cellspacing="0">
              <thead><tr><th>Qty</th><th>Unit Price</th><th>Line Total</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p>
              Total: {shop.CurrencySymbol}{sale.TotalAmount:0.##}<br/>
              Paid: {shop.CurrencySymbol}{sale.PaidAmount:0.##}<br/>
              <strong>Outstanding due on your account: {shop.CurrencySymbol}{customer.CurrentDue:0.##}</strong>
            </p>
            <p>Please arrange payment at your earliest convenience.</p>
            <p>{shop.InvoiceFooterText}</p>
            """;
    }

    private static string BuildSmsMessage(string shopName, string currencySymbol, string saleNumber, Customer customer) =>
        $"Dear {customer.Name}, invoice {saleNumber} from {shopName} has an outstanding due of " +
        $"{currencySymbol}{customer.CurrentDue:0.##}. Please pay at your earliest convenience.";
}
