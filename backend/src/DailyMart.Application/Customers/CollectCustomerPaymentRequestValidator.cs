using FluentValidation;

namespace DailyMart.Application.Customers;

public class CollectCustomerPaymentRequestValidator : AbstractValidator<CollectCustomerPaymentRequestDto>
{
    public CollectCustomerPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
