using FluentValidation;

namespace DailyMart.Application.Rbac;

public class SetPermissionsRequestValidator : AbstractValidator<SetPermissionsRequestDto>
{
    public SetPermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions).SetValidator(new MenuPermissionItemRequestValidator());
    }
}
