using DailyMart.Application.MasterData;

namespace DailyMart.UnitTests.MasterData;

public class CategoryRequestValidatorTests
{
    private readonly CategoryRequestValidator _validator = new();

    [Fact]
    public void A_valid_request_passes()
    {
        var result = _validator.Validate(new CategoryRequestDto { Name = "Grocery", Description = "Food" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_name_is_invalid()
    {
        var result = _validator.Validate(new CategoryRequestDto { Name = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_name_over_100_characters_is_invalid()
    {
        var result = _validator.Validate(new CategoryRequestDto { Name = new string('a', 101) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_missing_description_is_valid_since_its_optional()
    {
        var result = _validator.Validate(new CategoryRequestDto { Name = "Grocery", Description = null });

        Assert.True(result.IsValid);
    }
}
