using FluentValidation;

namespace DailyMart.Application.Billing;

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequestDto>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaidUntil).NotEmpty();
        RuleFor(x => x.Method).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
