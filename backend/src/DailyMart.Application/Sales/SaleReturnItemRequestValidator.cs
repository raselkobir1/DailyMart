using FluentValidation;

namespace DailyMart.Application.Sales;

public class SaleReturnItemRequestValidator : AbstractValidator<SaleReturnItemRequestDto>
{
    public SaleReturnItemRequestValidator()
    {
        RuleFor(x => x.SaleItemId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
