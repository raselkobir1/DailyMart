using FluentValidation;

namespace DailyMart.Application.Sales;

public class SaleReturnRequestValidator : AbstractValidator<SaleReturnRequestDto>
{
    public SaleReturnRequestValidator()
    {
        RuleFor(x => x.ReturnDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new SaleReturnItemRequestValidator());
    }
}
