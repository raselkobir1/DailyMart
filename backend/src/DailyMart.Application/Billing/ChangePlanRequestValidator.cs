using FluentValidation;

namespace DailyMart.Application.Billing;

public class ChangePlanRequestValidator : AbstractValidator<ChangePlanRequestDto>
{
    public ChangePlanRequestValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0);
    }
}
