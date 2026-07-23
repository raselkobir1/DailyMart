using FluentValidation;

namespace DailyMart.Application.Suppliers;

public class PaySupplierRequestValidator : AbstractValidator<PaySupplierRequestDto>
{
    public PaySupplierRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
