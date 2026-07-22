using FluentValidation;

namespace DailyMart.Application.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Must(pw => pw.Any(char.IsLetter)).WithMessage("'New Password' must contain at least one letter.")
            .Must(pw => pw.Any(char.IsDigit)).WithMessage("'New Password' must contain at least one digit.")
            .NotEqual(x => x.CurrentPassword).WithMessage("'New Password' must be different from the current password.");
    }
}
