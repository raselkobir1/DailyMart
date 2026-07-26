using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Sales;
using DailyMart.Application.Settings;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Sales;
using Microsoft.Extensions.Options;
using Moq;

namespace DailyMart.UnitTests.Sales;

public class SaleInvoiceDeliveryServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<SaleItem>> _itemRepository = new();
    private readonly Mock<IRepository<Customer>> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShopSettingsService> _shopSettingsService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<ISmsSender> _smsSender = new();
    private readonly EmailOptions _emailOptions = new() { Enabled = true };
    private readonly SmsOptions _smsOptions = new() { Enabled = true };

    public SaleInvoiceDeliveryServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleItem>()).Returns(_itemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Customer>()).Returns(_customerRepository.Object);

        _itemRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SaleItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _shopSettingsService
            .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopSettingsDto { ShopName = "DailyMart", CurrencySymbol = "৳" });
    }

    private SaleInvoiceDeliveryService CreateSut() => new(
        _unitOfWork.Object,
        _shopSettingsService.Object,
        _emailSender.Object,
        _smsSender.Object,
        Options.Create(_emailOptions),
        Options.Create(_smsOptions));

    [Fact]
    public async Task SendInvoiceEmailAsync_throws_when_email_sending_is_disabled()
    {
        _emailOptions.Enabled = false;
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceEmailAsync(1));
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_throws_when_the_sale_has_no_customer()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = null });
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceEmailAsync(1));
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_throws_when_the_customer_has_no_outstanding_due()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 5 });
        _customerRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 5, Name = "Jane Doe", Email = "jane@example.com", CurrentDue = 0 });
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceEmailAsync(1));
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_throws_when_the_customer_has_no_email_on_file()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 5 });
        _customerRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 5, Name = "Jane Doe", Email = null, CurrentDue = 100 });
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceEmailAsync(1));
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_sends_to_the_customers_email_when_everything_is_valid()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 5, TotalAmount = 500, PaidAmount = 200, DueAmount = 300 });
        _customerRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 5, Name = "Jane Doe", Email = "jane@example.com", CurrentDue = 300 });
        var sut = CreateSut();

        await sut.SendInvoiceEmailAsync(1);

        _emailSender.Verify(s => s.SendAsync(
            "jane@example.com", "Jane Doe", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendInvoiceSmsAsync_throws_when_sms_sending_is_disabled()
    {
        _smsOptions.Enabled = false;
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceSmsAsync(1));
    }

    [Fact]
    public async Task SendInvoiceSmsAsync_throws_when_the_customer_has_no_phone_on_file()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 5 });
        _customerRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 5, Name = "Jane Doe", Phone = null, CurrentDue = 100 });
        var sut = CreateSut();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.SendInvoiceSmsAsync(1));
    }

    [Fact]
    public async Task SendInvoiceSmsAsync_sends_to_the_customers_phone_when_everything_is_valid()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 5, TotalAmount = 500, PaidAmount = 200, DueAmount = 300 });
        _customerRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 5, Name = "Jane Doe", Phone = "01700000000", CurrentDue = 300 });
        var sut = CreateSut();

        await sut.SendInvoiceSmsAsync(1);

        _smsSender.Verify(s => s.SendAsync("01700000000", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
