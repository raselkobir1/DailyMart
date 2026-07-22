using FluentValidation;

namespace DailyMart.Application.Suppliers;

/// <summary>
/// Includes SupplierRequestValidator's rules (same reasoning as Module 4's
/// CreateProductRequestValidator) - and is required to exist at all, not just for DRY: the
/// ValidationFilter looks up IValidator&lt;T&gt; by the request's *exact* runtime type
/// (CreateSupplierRequestDto), so without this, create requests would go completely unvalidated even
/// though SupplierRequestValidator exists for the (different, base) update type.
/// OpeningBalance has no range rule - it may legitimately be negative (Module 5 Step 1).
/// </summary>
public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequestDto>
{
    public CreateSupplierRequestValidator()
    {
        Include(new SupplierRequestValidator());
    }
}
