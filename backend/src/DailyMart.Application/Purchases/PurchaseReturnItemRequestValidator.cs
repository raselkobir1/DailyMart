using FluentValidation;

namespace DailyMart.Application.Purchases;

public class PurchaseReturnItemRequestValidator : AbstractValidator<PurchaseReturnItemRequestDto>
{
    public PurchaseReturnItemRequestValidator()
    {
        RuleFor(x => x.PurchaseItemId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
