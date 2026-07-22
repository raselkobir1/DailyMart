using FluentValidation;

namespace DailyMart.Application.Inventory;

public class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequestDto>
{
    public StockAdjustmentRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.NewStockCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
