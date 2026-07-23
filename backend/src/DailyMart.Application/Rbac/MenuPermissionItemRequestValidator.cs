using FluentValidation;

namespace DailyMart.Application.Rbac;

public class MenuPermissionItemRequestValidator : AbstractValidator<MenuPermissionItemDto>
{
    public MenuPermissionItemRequestValidator()
    {
        RuleFor(x => x.MenuId).GreaterThan(0);
    }
}
