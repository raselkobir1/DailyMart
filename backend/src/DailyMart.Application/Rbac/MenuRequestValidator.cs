using FluentValidation;

namespace DailyMart.Application.Rbac;

public class MenuRequestValidator : AbstractValidator<MenuRequestDto>
{
    public MenuRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Route).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Icon).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ParentId).GreaterThan(0).When(x => x.ParentId.HasValue);
    }
}
