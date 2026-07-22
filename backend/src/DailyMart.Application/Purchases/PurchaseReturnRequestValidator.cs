using FluentValidation;

namespace DailyMart.Application.Purchases;

public class PurchaseReturnRequestValidator : AbstractValidator<PurchaseReturnRequestDto>
{
    public PurchaseReturnRequestValidator()
    {
        RuleFor(x => x.ReturnDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PurchaseReturnItemRequestValidator());
    }
}
