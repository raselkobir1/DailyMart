using FluentValidation;

namespace DailyMart.Application.Products;

/// <summary>Includes ProductRequestValidator's rules (valid since CreateProductRequestDto IS a
/// ProductRequestDto) rather than repeating every field-length/range rule a second time.</summary>
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequestDto>
{
    public CreateProductRequestValidator()
    {
        Include(new ProductRequestValidator());

        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0);
    }
}
