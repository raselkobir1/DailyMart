using DailyMart.Application.Common.Models;
using DailyMart.Application.Common.Validators;

namespace DailyMart.UnitTests.Common;

public class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [Fact]
    public void Default_request_is_valid()
    {
        var result = _validator.Validate(new PagedRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageNumber_below_1_is_invalid(int pageNumber)
    {
        var result = _validator.Validate(new PagedRequest { PageNumber = pageNumber });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PagedRequest.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_outside_1_to_100_is_invalid(int pageSize)
    {
        var result = _validator.Validate(new PagedRequest { PageSize = pageSize });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PagedRequest.PageSize));
    }

    [Fact]
    public void SearchTerm_over_128_characters_is_invalid()
    {
        var result = _validator.Validate(new PagedRequest { SearchTerm = new string('a', 129) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PagedRequest.SearchTerm));
    }
}
