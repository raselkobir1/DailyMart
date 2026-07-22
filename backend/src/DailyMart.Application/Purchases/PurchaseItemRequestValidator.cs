using FluentValidation;

namespace DailyMart.Application.Purchases;

public class PurchaseItemRequestValidator : AbstractValidator<PurchaseItemRequestDto>
{
    public PurchaseItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}
