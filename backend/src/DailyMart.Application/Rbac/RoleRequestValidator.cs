using FluentValidation;

namespace DailyMart.Application.Rbac;

public class RoleRequestValidator : AbstractValidator<RoleRequestDto>
{
    public RoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
