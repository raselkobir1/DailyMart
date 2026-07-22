using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Customers;
using DailyMart.Domain.Customers;
using Moq;

namespace DailyMart.UnitTests.Customers;

public class CustomerServiceTests
{
    private readonly Mock<IRepository<Customer>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Customer>()).Returns(_repository.Object);
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new CustomerService(_unitOfWork.Object);
    }

    private static CustomerRequestDto ValidRequest(string name = "Karim Ahmed", string? phone = null) => new()
    {
        Name = name,
        Phone = phone
    };

    [Fact]
    public async Task GetPagedAsync_maps_customers_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Customer, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Customer>
            {
                Items = [new Customer { Id = 1, Name = "Karim Ahmed", Phone = "01711111111" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Karim Ahmed", dto.Name);
        Assert.Equal("01711111111", dto.Phone);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_without_a_phone_skips_the_uniqueness_check_entirely()
    {
        var result = await _sut.CreateAsync(ValidRequest(phone: null));

        Assert.Equal("Karim Ahmed", result.Name);
        _repository.Verify(
            r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_phone()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(ValidRequest(phone: "01711111111")));

        _repository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_unique_phone_succeeds()
    {
        var result = await _sut.CreateAsync(ValidRequest(phone: "01711111111"));

        Assert.Equal("01711111111", result.Phone);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, ValidRequest()));
    }

    [Fact]
    public async Task UpdateAsync_rejects_a_duplicate_phone_belonging_to_a_different_customer()
    {
        var existing = new Customer { Id = 5, Name = "Karim Ahmed", Phone = "01711111111" };
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.UpdateAsync(5, ValidRequest(phone: "01799999999")));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_customer_exists()
    {
        var existing = new Customer { Id = 7, Name = "Karim Ahmed" };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _repository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }
}
