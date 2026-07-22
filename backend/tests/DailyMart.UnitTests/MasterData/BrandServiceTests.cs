using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.MasterData;
using DailyMart.Domain.MasterData;
using Moq;

namespace DailyMart.UnitTests.MasterData;

/// <summary>
/// Covers the same CRUD/exception paths as CategoryServiceTests (the two services are structurally
/// identical). Doesn't repeat the compiled-predicate "excludes self" assertion there - same shared
/// pattern, already verified once - to avoid three near-duplicate copies of that specific test.
/// </summary>
public class BrandServiceTests
{
    private readonly Mock<IRepository<Brand>> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly BrandService _sut;

    public BrandServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Brand>()).Returns(_repository.Object);
        _sut = new BrandService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetPagedAsync_maps_the_page_to_dtos()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Brand, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Brand>
            {
                Items = [new Brand { Id = 1, Name = "Nestle" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        Assert.Equal("Nestle", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_case_insensitive_duplicate_name()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(new BrandRequestDto { Name = "NESTLE" }));

        _repository.Verify(r => r.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_unique_name_adds_and_saves()
    {
        _repository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new BrandRequestDto { Name = "Unilever" });

        Assert.Equal("Unilever", result.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_the_brand_does_not_exist()
    {
        _repository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Brand?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, new BrandRequestDto { Name = "Anything" }));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_brand_exists()
    {
        var existing = new Brand { Id = 7, Name = "Generic" };
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _repository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
