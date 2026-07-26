using FluentValidation;

namespace DailyMart.Application.Tenancy;

public class PlatformAdminLoginRequestValidator : AbstractValidator<PlatformAdminLoginRequestDto>
{
    public PlatformAdminLoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}
