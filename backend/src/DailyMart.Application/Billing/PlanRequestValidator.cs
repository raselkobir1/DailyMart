using FluentValidation;

namespace DailyMart.Application.Billing;

public class PlanRequestValidator : AbstractValidator<PlanRequestDto>
{
    public PlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
