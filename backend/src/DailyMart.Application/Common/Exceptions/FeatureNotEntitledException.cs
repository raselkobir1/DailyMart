namespace DailyMart.Application.Common.Exceptions;

/// <summary>
/// Thrown by <see cref="DailyMart.API.Filters.RequireFeatureAttribute"/> (API layer - see that class'
/// doc comment) when the current tenant hits an endpoint backing a restricted (Menu.IsGenerallyAvailable
/// = false) menu it hasn't been granted. Maps to 403, distinct from BusinessRuleException's 400 - this
/// isn't a bad request, it's a real request the tenant simply isn't entitled to make. Deliberately a
/// separate case from the existing per-role CanView/CanCreate/... checks (which this codebase leaves
/// frontend-only, see CLAUDE.md §4/§12): entitlement is a platform/billing boundary, not an internal
/// permission, so it's enforced here even though ordinary business controllers otherwise trust the
/// frontend to hide what a role can't do.
/// </summary>
public class FeatureNotEntitledException : Exception
{
    public FeatureNotEntitledException(string message) : base(message)
    {
    }
}
