using FluentValidation;

namespace DailyMart.Application.Purchases;

public class PurchaseReturnRequestValidator : AbstractValidator<PurchaseReturnRequestDto>
{
    public PurchaseReturnRequestValidator()
    {
        RuleFor(x => x.ReturnDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PurchaseReturnItemRequestValidator());

        // PurchaseReturnService checks "already returned" per PurchaseItemId by querying the DB once per
        // item, independently - two entries in the SAME request for the same PurchaseItemId would each
        // pass that check against the same stale "already returned" total, letting the request return
        // more of that line than actually remains. Not reachable via the current UI (one row per distinct
        // purchase item), but reachable via a direct API call.
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.PurchaseItemId).Distinct().Count() == items.Count)
            .WithMessage("Each purchase item can only appear once per return request.");
    }
}
