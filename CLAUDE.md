# DailyMart — Mini Departmental Store Management System

Source of truth for architecture, conventions, and build order. Full requirements: `Mini_Departmental_Store_Modular_Monolith_Requirements.txt`.

## 1. Overview

Production-ready management system for a single departmental shop ("DailyMart"): suppliers, purchasing,
inventory, barcode POS sales, customer/supplier dues, expenses, P&L, reports, dashboard, audit log.
Single shop, single admin user for now (multi-branch/multi-user are explicitly Future scope — do not build for them).

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
- **Repository + Unit of Work** (`Infrastructure`): generic `IRepository<T>` for common CRUD, module-specific
  repositories for custom queries (e.g. `ISupplierRepository.GetWithLedgerAsync`). One `IUnitOfWork` wrapping
  `SaveChangesAsync` and repository access; services commit through it.
- **Services** (`Application`): all business rules and orchestration live here (stock deduction, due
  calculation, profit calculation). Controllers stay thin — no business logic in controllers.
- **DTOs**: separate Request/Response DTOs per operation, no leaking of EF entities across the API boundary.
- **Validation**: FluentValidation validators per DTO, run via a validation filter/pipeline behavior in the API layer.
- **Auth**: JWT bearer tokens; `[Authorize]` by default; password hashing via ASP.NET Core `IPasswordHasher`
  (no external identity provider). Role field on the user modeled from day one for the "future role
  extensibility" requirement, but only an Admin role is enforced now.
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

- One database, one `DailyMartDbContext`, code-first EF Core migrations.
- Naming: snake_case for tables/columns (Npgsql convention), PascalCase in C#.
- Every stock-affecting table change also writes an `InventoryTransaction` row (purchase in, sale out,
  adjustment, damaged, return) — this is the traceability the BRD requires, not just a current-stock counter.
- Money/quantity columns use `numeric`, not floating point.

## 6. Frontend Conventions (Angular)

```
frontend/dailymart-ui/src/app/
├─ core/            # auth service, JWT interceptor, auth guard (functional), error interceptor
├─ shared/          # shared UI components (data table, barcode input, money pipe), pipes, models
├─ features/
│  ├─ auth/
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
│  └─ settings/
└─ app.routes.ts    # lazy-loaded per feature
```

- Standalone components, `inject()` over constructor DI, signals for local/component state.
- One `HttpClient`-based API service per module, typed request/response models matching backend DTOs.
- Functional route guards + HTTP interceptor for attaching JWT / handling 401.
- UI component library: **Angular Material** as the default choice for forms/tables/dialogs (revisit only if
  a specific module needs richer data-grid features than Material provides, e.g. POS line-item grid).
- Reactive Forms for all data entry (product form, purchase entry, POS billing).

## 7. Modules (from BRD) and Build Order

**Status (2026-07-23):** Modules 0–9 are implemented, tested, and verified — both via `dotnet test`/`ng test`
and a live pass (real Postgres + real HTTP calls + a full browser click-through) confirming every BRD
business rule in §8 actually holds at runtime, not just in code. Module 10 (Customer Due) is next; it
should reuse `ICustomerService.AdjustDueAsync`/`GetLedgerAsync` (added in Module 9) rather than
re-inventing them — Module 9 already had to build the customer due/ledger plumbing ahead of schedule since
Sale is what first creates a due.

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

Multi-branch, warehouse, multiple users/roles, promotions, loyalty, SMS, email, accounting integration,
mobile app. Do not add speculative extensibility for these beyond what's noted in §4 (role field on user).

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
