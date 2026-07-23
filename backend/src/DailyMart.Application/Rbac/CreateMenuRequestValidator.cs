using FluentValidation;

namespace DailyMart.Application.Rbac;

/// <summary>Includes MenuRequestValidator's rules (same reasoning as Module 4's
/// CreateProductRequestValidator) - required to exist at all, not just for DRY: ValidationFilter looks up
/// IValidator&lt;T&gt; by the request's exact runtime type (CreateMenuRequestDto).</summary>
public class CreateMenuRequestValidator : AbstractValidator<CreateMenuRequestDto>
{
    public CreateMenuRequestValidator()
    {
        Include(new MenuRequestValidator());
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
    }
}
