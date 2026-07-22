using FluentValidation;

namespace DailyMart.Application.Customers;

/// <summary>
/// Only one validator needed for this module - CustomerRequestDto is used directly for both create and
/// update (Step 6), so ValidationFilter's exact-runtime-type lookup finds it for both without needing a
/// second "CreateCustomerRequestValidator" the way Product/Supplier did.
/// </summary>
public class CustomerRequestValidator : AbstractValidator<CustomerRequestDto>
{
    public CustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(200)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
