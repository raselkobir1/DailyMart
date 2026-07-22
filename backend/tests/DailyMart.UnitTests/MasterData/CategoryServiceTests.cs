using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.MasterData;
using DailyMart.Domain.MasterData;
using Moq;

namespace DailyMart.UnitTests.MasterData;

public class CategoryServiceTests
{
    private readonly Mock<IRepository<Category>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Category>()).Returns(_repository.Object);
        _sut = new CategoryService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetPagedAsync_maps_the_page_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Category, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Category>
            {
                Items = [new Category { Id = 1, Name = "Grocery", Description = "Food items" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Grocery", dto.Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_case_insensitive_duplicate_name()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new CategoryRequestDto { Name = "GROCERY" }));

        _repository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_unique_name_adds_and_saves()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CategoryRequestDto { Name = "Snacks", Description = "Chips etc." });

        Assert.Equal("Snacks", result.Name);
        _repository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_excludes_the_entity_being_updated_from_its_own_duplicate_check()
    {
        var existing = new Category { Id = 5, Name = "Grocery" };
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        Expression<Func<Category, bool>>? capturedPredicate = null;
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Category, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(false);

        await _sut.UpdateAsync(5, new CategoryRequestDto { Name = "Grocery" });

        var isDuplicate = capturedPredicate!.Compile();
        // The row being updated itself must never count as a "duplicate" of its own unchanged name...
        Assert.False(isDuplicate(new Category { Id = 5, Name = "grocery" }));
        // ...but a different row with the same (lower-cased) name still must.
        Assert.True(isDuplicate(new Category { Id = 6, Name = "grocery" }));
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_the_category_does_not_exist()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(404, new CategoryRequestDto { Name = "Anything" }));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_category_exists()
    {
        var existing = new Category { Id = 7, Name = "Household" };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _repository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_the_category_does_not_exist()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }
}
