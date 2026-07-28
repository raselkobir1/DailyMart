using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Rbac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DailyMart.API.Filters;

/// <summary>
/// Opt-in, backend-enforced gate for a controller/action backing a menu a developer has marked
/// IsGenerallyAvailable=false in RbacSeeder - throws FeatureNotEntitledException (403) if the current
/// tenant has no TenantFeatureGrant for it, even if the request is made directly (Postman, devtools),
/// bypassing whatever the frontend hides. This is deliberately separate from - and stricter than - the
/// existing per-role CanView/CanCreate/... checks, which CLAUDE.md §4/§12 keep frontend-only: those are
/// an internal "which of MY roles can use this" choice a tenant's own Admin makes, while this is "does
/// this tenant have the feature at all," a platform/billing boundary the tenant can't self-grant.
///
/// Existing controllers need zero changes - this attribute is only ever added to a controller/action
/// backing a menu that's actually restricted; every generally-available menu's controller is completely
/// unaffected by this filter's existence.
///
/// Usage: [RequireFeature("some-restricted-menu-key")] on the controller class or a specific action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFeatureAttribute : TypeFilterAttribute
{
    public RequireFeatureAttribute(string menuKey) : base(typeof(RequireFeatureFilter))
    {
        Arguments = [menuKey];
    }

    private class RequireFeatureFilter : IAsyncActionFilter
    {
        private readonly string _menuKey;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IFeatureEntitlementService _featureEntitlementService;

        public RequireFeatureFilter(
            string menuKey, ICurrentTenantService currentTenantService, IFeatureEntitlementService featureEntitlementService)
        {
            _menuKey = menuKey;
            _currentTenantService = currentTenantService;
            _featureEntitlementService = featureEntitlementService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // No tenant context (e.g. a platform-admin token) is never entitled to a tenant feature -
            // same fail-closed stance ApplyTenancyQueryFilters takes for a missing tenant claim.
            var tenantId = _currentTenantService.TenantId;
            var isEntitled = tenantId is not null
                && await _featureEntitlementService.IsMenuAvailableAsync(tenantId.Value, _menuKey, context.HttpContext.RequestAborted);

            if (!isEntitled)
            {
                throw new FeatureNotEntitledException("Your company does not have access to this feature.");
            }

            await next();
        }
    }
}
