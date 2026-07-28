# DailyMart — Platform, Billing, RBAC & Feature Entitlement Guide

This is the operational/developer guide for everything above a single tenant's own business data:
who runs the SaaS itself, how a tenant's internal permissions work, how billing is tracked, and how to
ship a feature to only some companies. See `README.md` for running the app and `CLAUDE.md` for the
overall architecture — this file goes deep on the four systems named in its title.

## 1. The two kinds of "admin" — Platform Admin vs. Shop (Tenant) Admin

DailyMart is multi-tenant: many independent companies ("tenants") each get their own isolated data,
users, and roles. There are two completely separate identities layered on top of that, and it's easy to
conflate them, so start here.

| | **Shop Admin** (tenant's own Admin role) | **Platform Admin** (SaaS vendor's ops staff) |
|---|---|---|
| Who | The person who signed up the company / whoever they've made Admin | You / your team, running DailyMart as a business |
| Login | `/login` (normal app) | `/platform/login` (separate screen, outside the app shell) |
| Identity | `User` row, `TenantId` set, `Role = "Admin"` | `PlatformAdmin` row — a different table entirely, no `TenantId` |
| JWT | Carries `tenant_id` claim + `Admin` role claim | Carries `PlatformAdmin` role claim, **no** `tenant_id` claim |
| Can see | Only their own company's data (enforced by the tenant query filter — see CLAUDE.md §4) | Every tenant's *account* (name, plan, active/suspended, usage counts) — never their business data (products, sales, etc.) |
| Manages | Users/Roles/Menus/Permissions **within their own company** | Tenants (suspend/activate), Plans, billing/subscriptions, and per-tenant feature grants |
| Default dev credentials | `admin` / `Admin@123456` (seeded "Default Company", tenant id 1) | `platform` / `Platform@123456` |

Key point: a Platform Admin token is deliberately **fail-closed** against ordinary business endpoints —
since it has no `tenant_id` claim, the tenant-isolation query filter matches zero rows if it's ever used
where a tenant token was expected. There's no "super admin who can see everything" account; the platform
panel only ever aggregates account-level metadata (see §3 and §4), never a tenant's actual records. There
is also no impersonation feature (logging in "as" a tenant) — that's explicitly out of scope for now.

## 2. Role / Menu / Permission (RBAC) — how access works *inside* one company

This is the system a Shop Admin uses to control what their own staff can do. Three pieces:

- **`Menu`** — one row per screen/module in the app (Products, Purchases, POS, Users, ...). This list is
  **global** — every tenant shares the same catalog of possible screens. Managed at
  `/menus` (`[Authorize(Roles = "Admin")]`, so any tenant's own Admin can technically edit labels/icons/
  order here — in practice this is re-synced from code every boot, see §4.3).
- **`Role`** — e.g. "Admin", "Cashier", "Manager". Unlike `Menu`, each tenant has its **own** roles —
  `Role.Name` is unique *per tenant*, not globally. Every tenant gets a system "Admin" role automatically
  (can't be renamed or deleted) with full access to every menu it's currently entitled to (see §4).
  Managed at `/roles`.
- **`RoleMenuPermission`** — one row per (Role × Menu), with four independent flags: `CanView`,
  `CanCreate`, `CanEdit`, `CanDelete`. This is the actual "can Cashier see Purchases, and can they edit
  it" answer. Managed at `/permissions` — pick a role, get a matrix of every menu with four checkboxes
  each, save.

**How a Shop Admin sets this up day to day:**
1. `/users` → create a new staff login, assign it a `Role` (e.g. "Cashier").
2. `/roles` → create the "Cashier" role if it doesn't exist yet (or use `/roles` to make a new one).
3. `/permissions` → select "Cashier" → tick `CanView` for POS and Products, leave Purchases/Users/Roles
   unticked → Save.
4. That Cashier's sidebar now only shows POS and Products; buttons for actions they lack (`CanCreate`
   etc.) are hidden too.

**Important limitation, by design**: this CanView/CanCreate/CanEdit/CanDelete check is **frontend-only**.
The backend does not reject a Cashier's direct API call to, say, `POST /api/purchases` — every ordinary
business controller stays on the global "any authenticated user" policy. This is a deliberate, documented
scope line (see CLAUDE.md §4/§12): backend-enforced per-menu authorization for this internal model is
still an open decision, not an oversight. Contrast this with §4 below, which *is* backend-enforced,
because it protects a different kind of boundary (see §4's explanation of why).

The one place the JWT itself matters here: it only ever carries the user's **role name** (e.g.
`"Cashier"`), for coarse checks like `[Authorize(Roles = "Admin")]` on `/users`, `/roles`, `/menus`. The
fine-grained menu list comes from `GET /api/auth/me/permissions`, fetched separately — so changing a
role's permissions takes effect for that user immediately, without needing to log out and back in.

## 3. Billing — Plans, Subscriptions, Payments (manual, no payment gateway)

Billing is entirely **platform-admin-managed** and entirely **manual** — there is no payment gateway,
no webhook, no online checkout. It exists so you (the vendor) can track "who's paid, who's overdue" and
record money that already changed hands outside the app (bank transfer, mobile banking, cash).

Three entities, all global (not tied to any one tenant's data, since a platform admin needs to read/join
them with no tenant context):

- **`Plan`** — a billing label: Name, Price, BillingCycle, `IsFree`. Managed at `/platform/plans`.
  **Important**: a `Plan` does **not** gate any feature or enforce any limit today — a Free-plan tenant
  gets the exact same functionality as a Pro-plan tenant. It's purely what you charge them, not what they
  can use. (Plan-tier feature gating — e.g. "Free plan tenants can only have 2 users" — is a separate,
  still-unbuilt idea; don't confuse it with §4's per-tenant feature entitlement, which already exists but
  works completely independently of `Plan`.)
- **`TenantSubscription`** — one row per tenant: which `Plan` they're on, `CurrentPeriodStart`,
  `CurrentPeriodEnd`. Every new tenant starts on the seeded "Free" plan automatically at signup.
- **`SubscriptionPayment`** — an append-only ledger: one row per payment you manually record (amount,
  the period it covers, method, notes). Never edited or deleted, only added to.

**"Overdue" is never stored** — it's recomputed on every read as
`!plan.IsFree && (CurrentPeriodEnd is null or in the past)`. A Free-plan tenant is never overdue. A
paid-plan tenant with no payment recorded yet reads as overdue immediately (prompting you to collect one).

**How you actually use it, from `/platform/tenants/:id`:**
1. **Change Plan** — switch a tenant from Free to a paid plan (or between paid plans). Switching *to*
   Free clears `CurrentPeriodEnd` (never expires again). Switching *from* Free to paid leaves it null,
   so it immediately shows Overdue until you record a payment.
2. **Record Payment** — enter an amount, the date it's paid until, and a method/notes. This both appends
   to the payment ledger and updates the subscription's `CurrentPeriodEnd`. Consecutive payments continue
   from the later of "today" or the existing period end, so back-to-back payments don't gap or overlap.
3. The main `/platform/tenants` list itself shows Plan / Paid-Until / Overdue per row, so you can see who
   to chase without opening each one.

## 4. Per-tenant feature entitlement — shipping a feature to only some companies

This is the newest piece, and the one you'll touch as a developer most often going forward. It answers:
**"I built a new feature/menu — how do I give it to just one company, not everyone?"**

### 4.1 The concept

Every existing menu is **generally available** — every tenant gets it automatically, which is the
system's original, simpler behavior and still the default for anything new unless you say otherwise.
A menu becomes **restricted/exclusive** the moment a developer marks it `IsGenerallyAvailable: false` in
its seed definition. From that point on:

- **No tenant** gets it automatically — not even the "Default Company" seed tenant, not even brand-new
  signups.
- A tenant only gets it via an explicit **`TenantFeatureGrant`** (one row: Tenant × Menu) that a
  **Platform Admin** creates for that one company from the platform panel.
- Revoking that grant makes it disappear again, immediately, for every role in that tenant that had
  access to it — not just its Admin role.

This is deliberately **independent of `Plan`/billing** (§3) — you don't need to invent a new plan tier to
give one customer a beta feature; you grant it directly to that tenant.

It's also deliberately **backend-enforced**, unlike §2's internal RBAC. The reasoning: §2's
CanView/CanCreate/etc. is an *internal* choice a tenant's own Admin makes about their own staff — trusting
the frontend to hide it is an accepted tradeoff there. Feature entitlement is different: it's *the
platform's own boundary* on what a tenant is allowed to use at all, similar in spirit to billing. A tenant
shouldn't be able to reach a feature they're not entitled to just by calling the API directly, so this one
really does reject the request server-side (HTTP 403), not just hide a sidebar link.

### 4.2 As a developer: shipping a new feature exclusive to specific companies

Say you're building a brand-new module ("Loyalty Program") and product wants to pilot it with two
specific customers before a general release. Steps:

1. **Build the feature normally** — entities, DTOs, service, controller, Angular route — exactly like any
   other module (see CLAUDE.md §10's module workflow). Nothing about this is different yet.

2. **Add its `Menu` seed row as restricted**, in
   `backend/src/DailyMart.Infrastructure/Persistence/Seed/RbacSeeder.cs`, inside the `SeedMenus` array:

   ```csharp
   new("loyalty", "Loyalty Program", "/loyalty", "🎁", 65, null, IsGenerallyAvailable: false),
   ```

   The last argument is what makes it restricted. Everything else about a menu seed row (Key, Label,
   Route, Icon, SortOrder, optional ParentKey for nesting under a group) works exactly as it does for any
   other menu — see the existing entries in that same array for examples, and the array's own doc
   comment.

3. **Protect the controller** with the `RequireFeature` attribute, using the same Key you just seeded:

   ```csharp
   [ApiController]
   [Route("api/loyalty")]
   [RequireFeature("loyalty")]
   public class LoyaltyController : ControllerBase { ... }
   ```

   This is what makes a direct API call from a non-entitled tenant fail with 403, even if someone bypasses
   the frontend entirely (Postman, browser devtools, etc.). You can put it on the whole controller (every
   action) or on a single action if only part of a module should be gated.

4. **Add the Angular route**, guarded the same way every other route is, in `app.routes.ts`:

   ```ts
   {
     path: 'loyalty',
     canActivate: [canView('loyalty')],
     loadComponent: () => import('./features/loyalty/loyalty-list.component').then(m => m.LoyaltyListComponent)
   },
   ```

   You do **not** need to touch the sidebar/nav code — it's entirely data-driven from
   `GET /api/auth/me/permissions`, which already only returns menus the tenant is both entitled to *and*
   has role permission for (see §4.4). An unentitled tenant simply never sees it in that list, so it never
   appears in their sidebar and the route redirects if they somehow navigate to it directly.

5. **Deploy.** At this point, nobody has "Loyalty Program" — not even Default Company. That's expected:
   restricted means opt-in, always, for every tenant, including your own seed data.

6. **Grant it to your two pilot customers** — see §4.3.

A concrete, already-shipped example of exactly this pattern lives in the codebase right now:
`backend/src/DailyMart.API/Controllers/BetaAnalyticsController.cs` and
`frontend/dailymart-ui/src/app/features/beta-analytics/`, seeded as `"beta-analytics"` in `RbacSeeder`.
It's a deliberately trivial demo (a canned JSON payload, no real business logic) built to exercise this
whole mechanism end to end — read it alongside this guide, and feel free to delete it once you no longer
need a live example to point to.

### 4.3 As a Platform Admin: enabling/disabling a feature for a specific company

From the platform panel:

1. Log into `/platform/login`.
2. Go to **Companies** → open the specific tenant (e.g. "Default Company").
3. Scroll to the **Features** section. Every *restricted* menu in the system shows up here as a row with
   a **Granted** / **Not granted** badge (generally-available menus aren't listed — there's nothing to
   toggle for them, since every tenant already has them).
4. Click **Grant** to give this one company access. It takes effect **immediately** — no redeploy, no
   waiting for the next server restart, and the tenant's users don't need to log out and back in (their
   next sidebar refresh or page load already reflects it).
5. Click **Revoke** to take it away again, just as immediately.

The same is available as raw API calls, useful for scripting/automation:

```
GET  /api/platform/tenants/{tenantId}/features                 # list every menu + this tenant's grant status
POST /api/platform/tenants/{tenantId}/features/{menuId}/grant  # 204 No Content on success
POST /api/platform/tenants/{tenantId}/features/{menuId}/revoke # 204 No Content on success
```

All three require a Platform Admin bearer token (`POST /api/platform/auth/login`). `menuId` is the
numeric `Menu.Id` — read it off the `GET .../features` response for the menu whose `menuKey` you want.

### 4.4 How enforcement actually works (both layers, so you can debug it)

Two independent checks, both driven by the same source of truth (`IFeatureEntitlementService`):

1. **What shows up at all** — `GET /api/auth/me/permissions` (which drives the sidebar and route guards)
   only returns a menu if the tenant is *both* entitled to it *and* the user's role has `CanView` on it.
   A tenant's own Admin also can't work around this by hand: the `/permissions` matrix screen (§2) itself
   only ever lists menus the tenant is entitled to, and the backend rejects a submitted permission for any
   menu it isn't (`RoleService.SetPermissionsAsync`) — so a Shop Admin can neither see nor self-grant
   their company a restricted feature the platform hasn't unlocked for them.
2. **The actual API call** — any controller/action wearing `[RequireFeature("key")]` checks entitlement
   again, independently, on every request, and throws a 403 (`FeatureNotEntitledException`) if it fails.
   This is what stops a direct API call from working even if someone found the endpoint some other way.

Both checks read from the same place, so they can never disagree: `Menu.IsGenerallyAvailable` (true for
everything except a menu a developer explicitly restricted) unioned with any active `TenantFeatureGrant`
row for that tenant.

## 5. Quick reference

| Thing | Where |
|---|---|
| Shop Admin login | `http://localhost:4200/login` — `admin` / `Admin@123456` (dev default) |
| Platform Admin login | `http://localhost:4200/platform/login` — `platform` / `Platform@123456` (dev default) |
| Users / Roles / Menus / Permissions screens | `/users`, `/roles`, `/menus`, `/permissions` (Shop Admin only) |
| Platform Companies / Plans screens | `/platform/tenants`, `/platform/plans` (Platform Admin only) |
| Menu seed list (add new menus here) | `backend/src/DailyMart.Infrastructure/Persistence/Seed/RbacSeeder.cs` |
| Feature entitlement service | `backend/src/DailyMart.Application/Rbac/IFeatureEntitlementService.cs` |
| Backend enforcement attribute | `backend/src/DailyMart.API/Filters/RequireFeatureAttribute.cs` |
| Live worked example | `BetaAnalyticsController.cs` / `features/beta-analytics/` (menu key `"beta-analytics"`) |
| Billing services | `backend/src/DailyMart.Application/Billing/IPlanService.cs`, `ISubscriptionService.cs` |
