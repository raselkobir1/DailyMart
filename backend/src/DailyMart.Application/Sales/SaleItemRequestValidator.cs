using FluentValidation;

namespace DailyMart.Application.Sales;

public class SaleItemRequestValidator : AbstractValidator<SaleItemRequestDto>
{
    public SaleItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}
