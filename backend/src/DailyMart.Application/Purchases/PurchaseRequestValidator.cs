using FluentValidation;

namespace DailyMart.Application.Purchases;

public class PurchaseRequestValidator : AbstractValidator<PurchaseRequestDto>
{
    public PurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.PurchaseDate).NotEmpty();
        RuleFor(x => x.PaymentType).IsInEnum();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PurchaseItemRequestValidator());
    }
}
