using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.MasterData;
using DailyMart.Domain.MasterData;
using Moq;

namespace DailyMart.UnitTests.MasterData;

public class UnitServiceTests
{
    private readonly Mock<IRepository<Unit>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UnitService _sut;

    public UnitServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Unit>()).Returns(_repository.Object);
        _sut = new UnitService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetPagedAsync_maps_the_page_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Unit, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Unit>
            {
                Items = [new Unit { Id = 1, Name = "Kilogram", Symbol = "kg" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Kilogram", dto.Name);
        Assert.Equal("kg", dto.Symbol);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Unit?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_case_insensitive_duplicate_name()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Unit, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new UnitRequestDto { Name = "PIECE", Symbol = "pc" }));

        _repository.Verify(r => r.AddAsync(It.IsAny<Unit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_unique_name_adds_and_saves()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Unit, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new UnitRequestDto { Name = "Liter", Symbol = "L" });

        Assert.Equal("Liter", result.Name);
        Assert.Equal("L", result.Symbol);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_the_unit_does_not_exist()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Unit?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(404, new UnitRequestDto { Name = "Anything", Symbol = "x" }));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_unit_exists()
    {
        var existing = new Unit { Id = 7, Name = "Box", Symbol = "box" };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _repository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
