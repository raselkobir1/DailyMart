using FluentValidation;

namespace DailyMart.Application.Tenancy;

public class SendSupportMessageRequestValidator : AbstractValidator<SendSupportMessageRequestDto>
{
    public SendSupportMessageRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}
