using FluentValidation;

namespace DailyMart.Application.Sales;

public class SaleRequestValidator : AbstractValidator<SaleRequestDto>
{
    public SaleRequestValidator()
    {
        // No RuleFor(x => x.CustomerId) - a Cash sale may legitimately have none (walk-in). SaleService
        // enforces "Credit/Partial requires a customer" since that's conditional on PaymentType, not a
        // shape FluentValidation expresses cleanly alongside the GreaterThan(0)-when-present check below.
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.PaymentType).IsInEnum();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new SaleItemRequestValidator());
    }
}
