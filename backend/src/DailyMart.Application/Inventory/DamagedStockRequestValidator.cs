using FluentValidation;

namespace DailyMart.Application.Inventory;

public class DamagedStockRequestValidator : AbstractValidator<DamagedStockRequestDto>
{
    public DamagedStockRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
