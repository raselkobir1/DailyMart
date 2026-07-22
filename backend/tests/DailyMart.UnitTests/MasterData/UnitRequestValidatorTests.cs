using DailyMart.Application.MasterData;

namespace DailyMart.UnitTests.MasterData;

public class UnitRequestValidatorTests
{
    private readonly UnitRequestValidator _validator = new();

    [Fact]
    public void A_valid_request_passes()
    {
        var result = _validator.Validate(new UnitRequestDto { Name = "Kilogram", Symbol = "kg" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_name_is_invalid()
    {
        var result = _validator.Validate(new UnitRequestDto { Name = "", Symbol = "kg" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_symbol_is_invalid()
    {
        var result = _validator.Validate(new UnitRequestDto { Name = "Kilogram", Symbol = "" });

        Assert.False(result.IsValid);
    }
}
