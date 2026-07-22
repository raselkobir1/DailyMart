using FluentValidation;

namespace DailyMart.Application.MasterData;

public class UnitRequestValidator : AbstractValidator<UnitRequestDto>
{
    public UnitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
    }
}
