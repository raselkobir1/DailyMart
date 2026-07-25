using FluentValidation;

namespace DailyMart.Application.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Must(pw => pw.Any(char.IsLetter)).WithMessage("'New Password' must contain at least one letter.")
            .Must(pw => pw.Any(char.IsDigit)).WithMessage("'New Password' must contain at least one digit.");
    }
}
