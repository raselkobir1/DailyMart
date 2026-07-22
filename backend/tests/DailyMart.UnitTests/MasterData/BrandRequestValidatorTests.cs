using DailyMart.Application.MasterData;

namespace DailyMart.UnitTests.MasterData;

public class BrandRequestValidatorTests
{
    private readonly BrandRequestValidator _validator = new();

    [Fact]
    public void A_valid_request_passes()
    {
        var result = _validator.Validate(new BrandRequestDto { Name = "Nestle" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_name_is_invalid()
    {
        var result = _validator.Validate(new BrandRequestDto { Name = "" });

        Assert.False(result.IsValid);
    }
}
