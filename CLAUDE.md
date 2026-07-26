# DailyMart — Mini Departmental Store Management System

Source of truth for architecture, conventions, and build order. Full requirements: `Mini_Departmental_Store_Modular_Monolith_Requirements.txt`.

## 1. Overview

Production-ready **multi-tenant SaaS** departmental-store management system ("DailyMart"): suppliers,
purchasing, inventory, barcode POS sales, customer/supplier dues, expenses, P&L, reports, dashboard, audit
log — sold to many independent shop owners/companies, each getting their own isolated `Tenant` with its own
data, users, roles, and settings (see §4's Multi-tenancy bullet). This supersedes the original "single shop,
single admin" decision (and the RBAC multi-user decision before it), once the product turned from an
internal tool into something sold to many customers. **Multi-branch is still explicitly Future scope** —
don't conflate the two: a tenant is one company/shop owner's isolated account, not a company operating
several physical store locations sharing one dataset; that remains unbuilt.

## 2. Tech Stack

- **Backend**: .NET 8 Web API, ASP.NET Core, EF Core (Npgsql), JWT auth
- **Database**: PostgreSQL
- **Frontend**: Angular (latest stable), standalone components, signals
- **Architecture**: Clean Architecture (layered), Repository + Service + Unit of Work
- **Explicitly excluded**: CQRS, MediatR

## 3. Repository Layout

Monorepo:
```
DailyMart/
├─ backend/
│  ├─ DailyMart.sln
│  ├─ src/
│  │  ├─ DailyMart.Domain/          # Entities, Enums, shared kernel, no dependencies
│  │  ├─ DailyMart.Application/     # DTOs, service interfaces, services, FluentValidation, mapping
│  │  ├─ DailyMart.Infrastructure/  # EF Core DbContext, Repositories, UnitOfWork, external services
│  │  └─ DailyMart.API/             # Controllers, DI wiring, middleware, JWT config, Program.cs
│  └─ tests/
│     └─ DailyMart.UnitTests/
├─ frontend/
│  └─ dailymart-ui/                 # Angular app
├─ Mini_Departmental_Store_Modular_Monolith_Requirements.txt
└─ CLAUDE.md
```

Dependency direction: `API → Application → Domain`, `Infrastructure → Application → Domain`. Domain has zero
outward dependencies. Every layer is organized internally by **module folders** (Products, Purchases, Sales,
Inventory, Suppliers, Customers, Expenses, Reports, ...) so each module's DTOs/services/validators/repositories
live together within their layer, even though the layer itself is one project.

## 4. Backend Conventions

- **Entities** (`Domain`): inherit a common `AuditableEntity` base (`Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`,
  `UpdatedBy`, `IsDeleted`). Soft delete everywhere — a global EF Core query filter excludes `IsDeleted = true`.
  Every entity that belongs to one tenant inherits `TenantOwnedEntity : AuditableEntity` instead (adds
  `TenantId`) — see the Multi-tenancy bullet below for which entities don't.
- **Multi-tenancy**: `Tenant` (one row per company/shop) and `PlatformAdmin` (the SaaS vendor's own login,
  entirely separate identity) are the only entities that inherit `AuditableEntity` directly rather than
  `TenantOwnedEntity` — along with `Menu`, the shared nav-item list common to every tenant. Isolation is
  enforced at the EF Core model level, not per-service: `TenancyModelExtensions.ApplyTenancyQueryFilters`
  applies `!IsDeleted && TenantId == CurrentTenantId` to every `TenantOwnedEntity`, and
  `ApplyTenantForeignKeys` adds the FK to `Tenant`, both wired once in `DailyMartDbContext.OnModelCreating` —
  so every existing repository/service query is automatically tenant-scoped with zero code changes needed
  per module. `ICurrentTenantService` (mirrors `ICurrentUserService`) reads the `tenant_id` JWT claim;
  `AuditingSaveChangesInterceptor` stamps it on newly-created entities the same way it stamps `CreatedBy`.
  Deliberately fail-closed: a null current-tenant (a platform-admin token, or seed-time code) makes the
  filter match zero rows on any tenant table rather than "every tenant" — never trust a missing tenant
  context to mean "show everything." A brand new tenant is created via `ITenantProvisioningService`
  (self-service `POST /api/auth/register`, or once at boot for the seeded "Default Company" — see
  `AdminSeeder`), not by an admin inside another tenant. `User.Username` is deliberately kept globally
  unique (not per-tenant) so `POST /api/auth/login` can look up which tenant a user belongs to without a
  separate "pick your company" step first — see `UserConfiguration`'s doc comment. A separate, "basic"
  platform-admin panel (`api/platform/auth`, `api/platform/tenants` — list every tenant, suspend/activate
  one) exists for the SaaS vendor's own ops staff, entirely apart from any tenant's own Admin role: its own
  `PlatformAdmin` login (`[Authorize(Roles = "PlatformAdmin")]`, no tenant claim, no refresh-token flow),
  own frontend session/guard (`PlatformAuthService`/`platformAuthGuard`, `/platform/login` +
  `/platform/tenants`, outside the normal app shell). No impersonation, no usage analytics, no billing yet
  — see §12.
- **Repository + Unit of Work** (`Infrastructure`): generic `IRepository<T>` for common CRUD, module-specific
  repositories for custom queries (e.g. `ISupplierRepository.GetWithLedgerAsync`). One `IUnitOfWork` wrapping
  `SaveChangesAsync` and repository access; services commit through it.
- **Services** (`Application`): all business rules and orchestration live here (stock deduction, due
  calculation, profit calculation). Controllers stay thin — no business logic in controllers.
- **DTOs**: separate Request/Response DTOs per operation, no leaking of EF entities across the API boundary.
- **Validation**: FluentValidation validators per DTO, run via a validation filter/pipeline behavior in the API layer.
- **Auth**: JWT bearer tokens; `[Authorize]` by default; password hashing via ASP.NET Core `IPasswordHasher`
  (no external identity provider). `User.Role` is a plain string (matches the JWT's `ClaimTypes.Role`
  claim), not a foreign key.
- **RBAC (Role/Menu/Permission)**: `Role`, `Menu` (the nav item/screen, `Key`+`Route`+`Icon`+`SortOrder`,
  optional `ParentId` for nesting), and `RoleMenuPermission` (one row per role×menu, four independent
  `CanView`/`CanCreate`/`CanEdit`/`CanDelete` flags — not a generic action-string list). The JWT only ever
  carries the user's role *name* (coarse `[Authorize(Roles="Admin")]` checks); the fine-grained permitted-menu
  list is fetched separately via `GET /api/auth/me/permissions`, so changing a role's permissions takes
  effect immediately without re-issuing tokens. `RolesController`/`MenusController`/`UsersController` are
  `[Authorize(Roles = "Admin")]` — the only controllers in this codebase with an explicit role requirement,
  since letting any authenticated user manage roles/permissions directly would let them self-escalate
  regardless of what the frontend hides. Every other business controller (Products, Purchases, ...) stays on
  the global "any authenticated user" fallback policy — per-menu CRUD enforcement for those is a frontend
  concern (hide the button/route), not a backend one; don't add per-endpoint permission checks there without
  discussing the tradeoff first, since that's a deliberate scope line, not an oversight.
  `Role`/`RoleMenuPermission` are `TenantOwnedEntity` — every tenant gets its **own** "Admin" role, not one
  shared globally, so `Role.Name` is unique per-tenant rather than platform-wide. `RbacSeeder` runs on every
  startup and upserts the global `Menu` list (add a new module's `Menu` row to its seed list and it's
  available immediately), then loops over every existing tenant granting its Admin role full CRUD on every
  current menu (via `ITenantProvisioningService.EnsureAdminRoleHasFullMenuAccessAsync`) — the same method a
  brand-new tenant's signup uses once, so "a new menu reaches everyone automatically" still holds across
  every tenant, not just ones created after the menu existed.
- **Global exception handling**: middleware mapping domain/validation exceptions to consistent
  `ProblemDetails` responses.
- **CORS**: a named policy (`Cors:AllowedOrigins` config, empty by default) restricts cross-origin calls to
  known frontend origins — needed for the `ng serve` (`:4200`) → API (`:5299`) local dev split. The Docker
  Compose deployment doesn't need this at all: nginx reverse-proxies `/api/*` to the API container, so the
  browser only ever sees one origin. See §13.
- **Logging**: Serilog, structured, request logging + business event logging (sales, stock changes).
- **Audit log**: a dedicated `AuditLog` module/table capturing entity, action (created/updated/deleted/sold),
  old value, new value, user, timestamp — written via an EF Core `SaveChanges` interceptor so every module gets
  it for free rather than hand-adding calls in every service.
- **Pagination/filtering/sorting**: a shared `PagedRequest`/`PagedResult<T>` convention reused by every list
  endpoint.
- **Numbers/money**: `decimal` for all prices/amounts, never `float`/`double`.

## 5. Database (PostgreSQL)

- One database, one `DailyMartDbContext`, code-first EF Core migrations — shared schema, not
  database-per-tenant: every tenant-owned table has a `tenant_id` column (see §4's Multi-tenancy bullet),
  isolation is enforced by the EF Core query filter, not by physical separation.
- Naming: snake_case for tables/columns (Npgsql convention), PascalCase in C#.
- Every stock-affecting table change also writes an `InventoryTransaction` row (purchase in, sale out,
  adjustment, damaged, return) — this is the traceability the BRD requires, not just a current-stock counter.
- Money/quantity columns use `numeric`, not floating point.

## 6. Frontend Conventions (Angular)

```
frontend/dailymart-ui/src/app/
├─ core/            # auth service, JWT interceptor, auth guard (functional), error interceptor,
│                    # perms.ts (RBAC permission signals), theme.ts (light/dark + accent), toast.ts,
│                    # platform-auth.service.ts + platform-auth.guard.ts (separate platform-admin session)
├─ shared/          # pagination component, toast-container, barcode-print util, models
├─ features/
│  ├─ auth/          # login + register (self-service tenant signup)
│  ├─ platform/      # platform-admin panel: login + tenant list (suspend/activate) - outside the normal
│  │                 # app shell entirely, gated by platformAuthGuard not authGuard/canView
│  ├─ dashboard/
│  ├─ products/     # + categories/brands/units as sub-routes or sibling features
│  ├─ suppliers/
│  ├─ customers/
│  ├─ purchases/
│  ├─ pos/          # POS sales + barcode workflow + invoice printing
│  ├─ inventory/
│  ├─ expenses/
│  ├─ reports/       # P&L, sales/purchase/inventory/due reports, closing reports
│  ├─ audit-log/
│  ├─ settings/
│  ├─ users/        # RBAC: user management (Admin-only)
│  ├─ roles/        # RBAC: role management (Admin-only)
│  ├─ menus/         # RBAC: menu/screen management (Admin-only)
│  └─ permissions/   # RBAC: the role-selector + View/Create/Edit/Delete matrix screen
└─ app.routes.ts    # lazy-loaded per feature; every tenant-scoped route (besides /login, /register) has
                    # a canView(menuKey) guard - /platform/* is a separate top-level branch with its own
                    # platformAuthGuard instead, not nested under the tenant-scoped shell
```

- Standalone components, `inject()` over constructor DI, signals for local/component state.
- One `HttpClient`-based API service per module, typed request/response models matching backend DTOs.
- Functional route guards + HTTP interceptor for attaching JWT / handling 401. `authGuard` gates the whole
  authenticated layout (must be signed in **and** have ≥1 visible menu); `canView(menuKey)` is a per-route
  factory guard that redirects to the user's first permitted menu if denied.
- **No UI component library** — no Angular Material, no PrimeNG. Hand-written CSS in `src/styles.scss`:
  CSS custom-property design tokens (`--brand`, `--ink`, `--panel`, ...) overridden under
  `[data-theme="dark"]`, soft tint colors derived at runtime via `color-mix()` so they auto-adapt to
  light/dark and to a runtime-selectable accent color (`core/theme.ts`) with no second palette to maintain.
  Reusable utility classes (`.card`, `.card-pad`, `.table-wrap table`, `.btn`/`.btn-primary`, `.field`/`.input`,
  `.badge`, `.chip`, `.spinner`, ...) are consumed directly in templates — there is no wrapping Angular
  component for a button or a card. The two exceptions that ARE real shared components:
  `shared/pagination/pagination.component.ts` (replaces `mat-paginator`) and
  `shared/toast-container/toast-container.component.ts` + `core/toast.ts` (replaces `MatSnackBar`).
  List pages are an inline form-card above a `.card > table`, not a modal; forms are dedicated routes, not
  dialogs. Icons are emoji for nav/action buttons and small inline SVGs for chrome (search icon, etc.) — no
  icon font/library dependency.
- Reactive Forms for all data entry (product form, purchase entry, POS billing).

## 7. Modules (from BRD) and Build Order

**Status (2026-07-23):** Modules 0–9 are implemented, tested, and verified — both via `dotnet test`/`ng test`
and a live pass (real Postgres + real HTTP calls + a full browser click-through) confirming every BRD
business rule in §8 actually holds at runtime, not just in code. Module 10 (Customer Due) is next; it
should reuse `ICustomerService.AdjustDueAsync`/`GetLedgerAsync` (added in Module 9) rather than
re-inventing them — Module 9 already had to build the customer due/ledger plumbing ahead of schedule since
Sale is what first creates a due.

A cross-cutting RBAC system (Users/Roles/Menus/Permissions — see §4) and a full UI redesign (no Angular
Material, hand-written design system — see §6) were also completed on top of Modules 0-9, verified the same
way (live Postgres + HTTP + browser click-through, including logging in as a deliberately restricted
"Cashier" role and confirming both the frontend sidebar/buttons AND the backend API itself reject what that
role can't do). Every future module's Angular UI should follow §6's design system from the start, and its
seed/setup should add a `Menu` row (see `RbacSeeder`) so Admin can see it immediately.

A full multi-tenant SaaS conversion (branch `SASS-integration`) was completed on top of all of the above —
see §4's Multi-tenancy bullet for the mechanism. Verified via real Postgres (both a fresh install and a
pre-multi-tenant database with existing data, confirming the migration backfills it into one "Default
Company" tenant rather than losing it), real HTTP round-trips (registration → login → cross-tenant business
endpoints proving isolation both ways, suspend/reactivate, platform-admin fail-closed against ordinary
business data), and a full browser click-through (self-service registration, the platform-admin panel,
and a regression pass over every pre-existing module's list/detail pages and the sidebar). Every existing
Application-layer service needed zero code changes for this — the query filter is applied once at the EF
Core model level (§4) — confirming that's a safe pattern to keep relying on for future modules too.

Build strictly module-by-module, in this order (later modules depend on earlier ones):

0. **Cross-cutting infrastructure** — solution/project scaffolding, DbContext + Npgsql, JWT plumbing, global
   exception handling, Serilog, audit interceptor, soft delete filter, pagination helpers, base entity.
1. **Authentication** — admin login/logout, JWT issue/refresh, change/reset password.
2. **Settings** — shop info, logo, invoice settings, currency, VAT, default discount, backup settings (needed
   before Product/Invoice work since tax/discount defaults and invoice template come from here).
3. **Master data** — Category, Brand, Unit (simple CRUD, prerequisite for Product).
4. **Product** — code/barcode/pricing/stock fields, barcode generation + printing, import/export.
5. **Supplier** — CRUD, opening balance, ledger, due report.
6. **Customer** — CRUD, ledger, due report.
7. **Purchase** — entry/update/return, stock increase, supplier payable + due calculation.
8. **Inventory** — stock in/out, adjustment, damaged stock, history, low stock alerts (built directly on top
   of the stock-transaction plumbing introduced by Purchase).
9. **POS Sales** — barcode scanner workflow, fast billing, cash/credit/partial payment, sales return,
   automatic stock deduction, profit calculation, invoice printing/receipts.
10. **Customer Due** — receivable ledger, outstanding due, payment collection (formalizes dues created by
    credit sales in module 9).
11. **Supplier Due** — payable ledger, payment history (formalizes dues created by credit purchases in module 7).
12. **Expense** — rent/salary/electricity/internet/misc.
13. **Profit & Loss** — revenue, COGS, gross/net profit, daily/weekly/monthly/yearly, computed from Sales +
    Purchase + Expense data.
14. **Reports** — sales/purchase/inventory/due/expense/P&L/daily & monthly closing/yearly, PDF/Excel/print export.
15. **Audit Log UI** — viewer/filter over the audit trail captured since module 0.
16. **Dashboard** — aggregates everything above (today's sales/purchase/profit/expense, cash in hand, dues,
    inventory value, low stock, top sellers, charts); built last since it depends on every other module's data.

Note: "Barcode Scanner Workflow" and "Invoice Printing" are BRD sections but are implemented as part of the
POS Sales module, not standalone modules.

## 8. Business Rules (enforced in the Application/service layer, not just DB constraints)

- Selling price ≥ purchase price unless explicitly overridden.
- Barcode unique; Product code unique.
- Stock can never go negative.
- Purchase increases stock; Sale decreases stock; every movement recorded in `InventoryTransaction`.
- Cash sale → increases cash balance. Credit sale → creates/increases customer due. Credit purchase →
  creates/increases supplier due. Payment → reduces the corresponding due. Partial payment updates both
  cash and due simultaneously.
- Customer due cannot go negative (can't "overpay" into negative below zero — collection is capped at
  outstanding due, or the excess is handled as a separate credit, not a negative due).
- Payment history is append-only/preserved (never overwritten).
- Supplier due must always reconcile to unpaid purchases (recompute-and-compare in tests, not just trust
  incremental updates).

## 9. Non-Functional Requirements

Responsive UI, pagination/filtering/sorting on all lists, structured logging, FluentValidation on all inputs,
audit trail, global exception handling, soft delete + CreatedBy/UpdatedBy on all entities, indexed/efficient
SQL (avoid N+1, use `AsNoTracking` for reads), fast product search (for POS/barcode lookups).

## 10. Module Development Workflow (apply to every module above)

For each module, in order, before moving to the next module:
1. Explain the business process/workflow in plain language.
2. Design the DB schema for that module.
3. Add entities + EF Core configuration.
4. Add DTOs.
5. Add FluentValidation validators.
6. Add Repository (+ Unit of Work usage) — follow SOLID, keep Clean Architecture boundaries.
7. Add Service (business logic).
8. Add Controller.
9. Add Angular UI (feature folder, standalone components, API service, routes).
10. State the design decisions and business rules applied, before/while implementing.
11. Integrate with previously built modules (e.g. Purchase → Inventory, Sales → Customer Due).
12. Add tests (backend unit tests at minimum for service-layer business rules).
13. Confirm module is production-ready before starting the next module.

## 11. Testing

- Backend: xUnit + a mocking library (Moq or NSubstitute) for service/business-rule unit tests; consider
  `WebApplicationFactory` + Testcontainers(Postgres) for integration tests on critical flows (purchase →
  stock, sale → stock + due).
- Frontend: Angular CLI default for component/service unit tests - as of Angular 22 the CLI's default
  unit-test builder (`@angular/build:unit-test`) runs on Vitest, not Jasmine/Karma.

## 12. Future (explicitly out of scope for now)

Multi-branch (one company/tenant operating several physical store locations sharing one dataset —
different from the multi-*tenancy* now built, see §1), warehouse, promotions, loyalty, accounting
integration, mobile app. Multi-user role-based permissions are no longer on this list — see §4's RBAC
bullet — but per-field/per-action permissions beyond the four CanView/CanCreate/CanEdit/CanDelete flags, and
backend-enforced (not just frontend-hidden) per-menu authorization on business controllers, both still are;
don't add either without discussing the tradeoff first.

Billing/subscription plans are also explicitly out of scope for now — every tenant is active/unpaid
regardless of plan. Platform-admin impersonation and usage analytics are out of scope too (see §4's
platform-admin note) — the panel is deliberately "basic": list + suspend/activate only.

SMS and email are also no longer on this list, once a real shop turned out to need a way to chase
customers with an outstanding due — see the Sales module's invoice-delivery feature (`IEmailSender`/
`ISmsSender` in `Application/Common/Interfaces`, `SaleInvoiceDeliveryService`). Both send through
ops-configured credentials (`Email:*`/`Sms:*` in appsettings/docker-compose env vars, same placement as
`Jwt:Secret`) rather than admin-UI-editable settings, and both are narrowly scoped to "remind this
customer about their due" (require `CustomerId` + `CurrentDue > 0` + contact info on file) — not a
general-purpose notification system. Extending either sender to other modules, or making their config
admin-editable, is a new scope decision, not something this change already covers.

## 13. Deployment

`docker-compose up --build` at the repo root brings up the whole stack (`db` = Postgres 16, `api` = the .NET
backend, `web` = the Angular app served by nginx) — see `README.md` for the full quick-start and default
admin credentials. Two things any future module should keep intact:

- **Auto-migration on startup**: `Program.cs` calls `Database.MigrateAsync()` (with a short retry loop for
  the Postgres-not-ready-on-first-boot race) before seeding. New migrations just need to exist in the
  Infrastructure project — nobody has to run `dotnet ef database update` by hand, in Docker or otherwise.
- **Same-origin `/api` proxy, not CORS**: production (`environment.ts`) uses a relative `apiBaseUrl: '/api'`,
  and `frontend/dailymart-ui/nginx.conf` proxies `/api/*` to the `api` container. Don't hardcode an absolute
  API URL in a service or environment file — it would bypass this and reintroduce a cross-origin call that
  only the dev-only CORS policy (§4) covers.
