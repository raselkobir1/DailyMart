using DailyMart.Domain.Settings;
using FluentValidation;

namespace DailyMart.Application.Settings;

public class UpdateShopSettingsRequestValidator : AbstractValidator<UpdateShopSettingsRequestDto>
{
    public UpdateShopSettingsRequestValidator()
    {
        RuleFor(x => x.ShopName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShopAddress).MaximumLength(500);
        RuleFor(x => x.ShopPhone).MaximumLength(50);
        RuleFor(x => x.ShopEmail).MaximumLength(200)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ShopEmail));

        RuleFor(x => x.InvoicePrefix).NotEmpty().MaximumLength(20);
        RuleFor(x => x.InvoiceFooterText).MaximumLength(500);

        RuleFor(x => x.CurrencyCode).NotEmpty().Length(2, 10);
        RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(10);

        RuleFor(x => x.DefaultVatPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.DefaultDiscountPercentage).InclusiveBetween(0, 100);

        RuleFor(x => x.BackupFrequency)
            .NotEmpty()
            .Must(value => Enum.TryParse<BackupFrequency>(value, ignoreCase: true, out _))
            .WithMessage("'Backup Frequency' must be one of: Daily, Weekly, Monthly.");

        RuleFor(x => x.DateFormat).NotEmpty().MaximumLength(20);

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeAKnownTimeZone)
            .WithMessage("'Time Zone' is not a recognized time zone identifier.");
    }

    private static bool BeAKnownTimeZone(string timeZone) =>
        !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _);
}
