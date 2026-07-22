using FluentValidation;

namespace DailyMart.Application.MasterData;

public class CategoryRequestValidator : AbstractValidator<CategoryRequestDto>
{
    public CategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
