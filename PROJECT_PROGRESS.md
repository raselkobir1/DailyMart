# DailyMart — Project Progress

Tracks implementation progress module-by-module, following the workflow defined in `CLAUDE.md` §10
(Design DB → Entity → Configure EF → Repository → Service → DTO → Validator → Controller → Angular → Testing).
Each step below requires explicit approval before moving to the next.

Status legend: ✅ Done · 🔄 In Progress (awaiting approval) · ⏳ Pending · 🚧 Blocked

## Module Checklist

| # | Module | Status |
|---|--------|--------|
| 0 | Cross-cutting infrastructure | ✅ Done |
| 1 | Authentication | ✅ Done |
| 2 | Settings | ✅ Done |
| 3 | Master data (Category, Brand, Unit) | ✅ Done |
| 4 | Product | ✅ Done |
| 5 | Supplier | ✅ Done |
| 6 | Customer | ✅ Done |
| 7 | Purchase | 🔄 In Progress |
| 8 | Inventory | ⏳ Pending |
| 9 | POS Sales | ⏳ Pending |
| 10 | Customer Due | ⏳ Pending |
| 11 | Supplier Due | ⏳ Pending |
| 12 | Expense | ⏳ Pending |
| 13 | Profit & Loss | ⏳ Pending |
| 14 | Reports | ⏳ Pending |
| 15 | Audit Log UI | ⏳ Pending |
| 16 | Dashboard | ⏳ Pending |

---

## Module 0 — Cross-Cutting Infrastructure

Purpose: technical foundation every other module builds on — no direct end-user business workflow of its own.
It exists to satisfy the BRD's non-functional requirements (§9 of CLAUDE.md): audit trail, soft delete,
CreatedBy/UpdatedBy, global exception handling, logging — plus JWT plumbing that Module 1 (Authentication)
will plug real login into.

### Step 1: Design Database — ✅ Done

**Scope decision:** Module 0 introduces exactly one real table, `audit_logs`, plus a *convention* (not a
table) of shared audit columns that every future entity/table will carry. It deliberately does **not**
create a `users` table — that belongs to Module 1 (Authentication), so infra stays decoupled from auth.
`created_by`/`updated_by` are stored as plain username strings for now, not a foreign key, since the User
entity doesn't exist yet.

**Primary key convention:** `bigint` identity (Postgres `bigserial`) for every table, not `uuid`. Reasoning:
simpler, smaller indexes, better join/insert performance for a single-shop system with no
distributed-ID-generation requirement (no offline/multi-node writes here).

**Shared audit columns** (added to every entity table via a common base, defined once):

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `created_at` | `timestamptz` | not null |
| `created_by` | `varchar(256)` | not null, username |
| `updated_at` | `timestamptz` | nullable |
| `updated_by` | `varchar(256)` | nullable |
| `is_deleted` | `boolean` | not null, default `false` — global EF query filter excludes these |

**`audit_logs` table** (captures create/update/delete/"sold" events across all modules per BRD §19):

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `entity_name` | `varchar(128)` | e.g. `"Product"` |
| `entity_id` | `varchar(64)` | stored as text so it works for any PK type |
| `action` | `varchar(32)` | `Created` / `Updated` / `Deleted` / `Sold` |
| `old_values` | `jsonb`, nullable | snapshot before change (null on Create) |
| `new_values` | `jsonb`, nullable | snapshot after change (null on Delete) |
| `changed_columns` | `jsonb`, nullable | list of column names that changed (Update only) |
| `performed_by` | `varchar(256)` | not null, username |
| `performed_at` | `timestamptz` | not null |

Indexes: `(entity_name, entity_id)` for "history of this record" lookups, `(performed_at)` for time-ranged
audit queries.

**How it's populated:** an EF Core `SaveChanges` interceptor inspects the `ChangeTracker` on every
`SaveChangesAsync` call and writes one `audit_logs` row per added/modified/(soft-)deleted entity — so
every module gets audit logging for free without calling anything explicitly.

---

### Step 2: Create Entity — 🔄 In Progress (awaiting approval)

Scaffolded the backend solution per CLAUDE.md §3 layout:
- `backend/DailyMart.sln`
- `backend/src/DailyMart.Domain`, `DailyMart.Application`, `DailyMart.Infrastructure` (net8.0 class libs)
- `backend/src/DailyMart.API` (net8.0 ASP.NET Core Web API, controllers, template placeholders removed)
- `backend/tests/DailyMart.UnitTests` (net8.0 xUnit)
- References wired: `Application → Domain`, `Infrastructure → Application`, `API → Application, Infrastructure`,
  `UnitTests → Domain, Application, Infrastructure`.

Entities created in `DailyMart.Domain`:
- `Common/AuditableEntity.cs` — abstract base with `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`,
  `IsDeleted`. Every future business entity (Product, Purchase, Customer, ...) will inherit this.
- `Auditing/AuditAction.cs` — enum `Created | Updated | Deleted | Sold`.
- `Auditing/AuditLog.cs` — the `audit_logs` table entity from the Step 1 design. Deliberately does **not**
  inherit `AuditableEntity`: a log row is append-only, never itself updated or soft-deleted.

`dotnet build` on `DailyMart.Domain` succeeds with 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

Packages added to `DailyMart.Infrastructure`: `Npgsql.EntityFrameworkCore.PostgreSQL`,
`EFCore.NamingConventions` (snake_case per CLAUDE.md §5), `Microsoft.EntityFrameworkCore.Design`
(also added to `DailyMart.API` as the startup project — required by `dotnet ef` tooling).
`FrameworkReference: Microsoft.AspNetCore.App` added to Infrastructure so it can use
`IHttpContextAccessor` without pulling in the full Web SDK.

Files created:
- `Persistence/DailyMartDbContext.cs` — `DbSet<AuditLog>`; applies all `IEntityTypeConfiguration<>` in
  the assembly; auto-applies a soft-delete query filter (`IsDeleted == false`) to every entity type that
  derives from `AuditableEntity`, by reflecting over the model in `OnModelCreating` — new modules get this
  for free, nothing to configure per-entity.
- `Persistence/Configurations/AuditLogConfiguration.cs` — table `audit_logs`, `jsonb` columns for
  old/new/changed values, composite index `(entity_name, entity_id)` + index on `performed_at`.
- `Persistence/Interceptors/AuditingSaveChangesInterceptor.cs` — an `SaveChangesInterceptor` that on every
  `SaveChanges(Async)`: stamps `CreatedAt/CreatedBy` (Added) or `UpdatedAt/UpdatedBy` (Modified), **converts
  hard deletes into soft deletes** (sets `IsDeleted = true` instead of issuing a `DELETE`), and adds one
  `AuditLog` row per affected entity with before/after JSON snapshots + changed-column list. Deliberately
  never emits `AuditAction.Sold` — EF's ChangeTracker can't tell a sale-driven stock update from any other
  update, so that distinction is left for Module 9 (POS Sales) to log explicitly.
- `Application/Common/Interfaces/ICurrentUserService.cs` + `Infrastructure/Services/CurrentUserService.cs`
  — resolves the acting username from `HttpContext.User.Identity.Name`, falling back to `"system"` until
  Module 1 (Authentication) wires up real JWT identities.
- `DependencyInjection.cs` — `AddInfrastructure(IServiceCollection, IConfiguration)` registers
  `IHttpContextAccessor`, `ICurrentUserService`, the interceptor, and `DailyMartDbContext` (Npgsql +
  snake_case naming + interceptor attached). Called from `DailyMart.API/Program.cs`.
- `appsettings.json` — added a local-dev `ConnectionStrings:DefaultConnection` placeholder.

**Migration**: `dotnet ef migrations add InitialCreate` generated successfully. Reviewed the output —
`audit_logs` table matches the Step 1 design exactly (bigserial PK, snake_case columns, `jsonb` value
columns, both indexes). **Not yet applied** to a real database: Docker Desktop isn't running locally, so
there's no reachable Postgres instance in this environment. `dotnet build DailyMart.slnx` succeeds with
0 warnings/errors across all 5 projects.

**Known follow-up (not blocking, noted for later):** `AuditLog` snapshots serialize every column via
`JsonSerializer`; when a future entity has a large binary column (e.g. `Product.Image` in Module 4), that
column should be excluded from the audit snapshot rather than base64-encoded into `jsonb` — revisit then.

### Step 4: Repository — 🔄 In Progress (awaiting approval)

Files created:
- `Application/Common/Models/PagedRequest.cs` / `PagedResult.cs` — the shared pagination/filtering/sorting
  convention from CLAUDE.md §4/§9. `PagedRequest.PageSize` is clamped to a max of 100 in its setter so no
  caller can accidentally request unbounded pages.
- `Application/Common/Interfaces/IRepository.cs` — generic contract: `GetByIdAsync`, `GetAllAsync`,
  `FindAsync(predicate)`, `GetPagedAsync(PagedRequest, predicate?)`, `ExistsAsync`, `AddAsync`, `Update`,
  `Remove`, and an escape-hatch `Query()` (`IQueryable<T>`) for module-specific repositories to compose
  further LINQ. Constrained to `T : AuditableEntity`.
- `Application/Common/Interfaces/IUnitOfWork.cs` — `Repository<T>()` factory + `SaveChangesAsync()`.
- `Infrastructure/Persistence/Repositories/Repository.cs` — EF Core implementation. `GetPagedAsync` sorts
  by `PagedRequest.SortBy` (a column name, e.g. from a query string) via a small reflection/Expression-tree
  helper — deliberately avoided taking a dynamic-LINQ package dependency for this. Falls back to
  `OrderByDescending(Id)` when `SortBy` is absent or doesn't match a real property. `Remove(entity)` just
  marks the entity for deletion — Module 0's `AuditingSaveChangesInterceptor` (Step 3) is what actually
  turns that into a soft delete; the repository itself has no delete-vs-soft-delete branching logic.
- `Infrastructure/Persistence/UnitOfWork.cs` — caches one `Repository<T>` instance per entity type per
  scope (`ConcurrentDictionary`), wraps `DbContext.SaveChangesAsync`.
- `DependencyInjection.cs` updated: `services.AddScoped<IUnitOfWork, UnitOfWork>()`.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors across all 5 projects.

**Design note:** no module-specific repositories (e.g. `IProductRepository`) exist yet — Module 0 only
ships the generic `IRepository<T>`/`IUnitOfWork`. Each future module adds its own repository interface
extending `IRepository<T>` only when it needs a query the generic contract can't express (e.g.
`ISupplierRepository.GetWithLedgerAsync`), per CLAUDE.md §4.

### Step 5: Service — 🔄 In Progress (awaiting approval)

**Correction to the already-approved Step 4 design:** building this step surfaced a real conflict —
`AuditLog` (Step 2) deliberately does **not** inherit `AuditableEntity`, but Step 4's `IRepository<T>`/
`IUnitOfWork.Repository<T>()` were constrained to `T : AuditableEntity`, so `AuditLog` couldn't go through
the generic repository at all. Fixed by introducing `Domain/Common/IEntity.cs` (`{ long Id { get; set; } }`),
having `AuditableEntity : IEntity` and `AuditLog : IEntity` directly, and re-pointing every Step 4
constraint from `T : AuditableEntity` to `T : class, IEntity`. No behavior changed for audit-column
stamping/soft-delete (that logic still explicitly checks for `AuditableEntity`) — only the repository's
generic constraint got looser so a non-auditable entity type can still be queried/paged generically.

Files created:
- `Application/AuditLogs/IAuditLogService.cs` + `AuditLogService.cs` — `GetPagedAsync(PagedRequest)` over
  `IUnitOfWork.Repository<AuditLog>()`. Returns the domain `AuditLog` entity directly for now; Step 6
  introduces `AuditLogDto` and the service will be updated then to map to it, since DTOs come after
  Service in the step order requested for this module.
- `Application/DependencyInjection.cs` — new `AddApplication()` extension registering
  `IAuditLogService`. Kept separate from Infrastructure's `AddInfrastructure()` so Application's own
  services are registered by Application itself, not reached into from Infrastructure. Both are called
  from `DailyMart.API/Program.cs`.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

Files created:
- `Application/AuditLogs/AuditLogDto.cs` — response shape (`Id, EntityName, EntityId, Action, OldValues,
  NewValues, ChangedColumns, PerformedBy, PerformedAt`). `Action` is exposed as `string` (not the
  `AuditAction` enum) to keep the DTO fully decoupled from the Domain type.
- `Application/AuditLogs/AuditLogMappingExtensions.cs` — `AuditLog.ToDto()`, an internal manual-mapping
  extension (no AutoMapper/Mapster dependency for a one-field mapping this small).

`IAuditLogService`/`AuditLogService` (Step 5) updated to return `PagedResult<AuditLogDto>` instead of the
domain entity, as flagged when Step 5 was written. `dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

**Another correction surfaced by this step:** `PagedRequest.PageSize` (Step 4) silently clamped out-of-range
values in its setter (e.g. `501` became `100`, `0` became `20`) instead of rejecting them. That's in tension
with adding real validation — the validator's `PageSize` rule could never fail, since by the time a
validator would see it, the value was already force-corrected. Changed `PagedRequest` to a plain
auto-property model (no clamping) so bad input is now explicitly rejected with a clear 400 error instead of
being silently rewritten.

Files created:
- `Application/Common/Validators/PagedRequestValidator.cs` — `PageNumber >= 1`, `PageSize` in `[1, 100]`,
  `SearchTerm`/`SortBy` length caps. Shared by every module's list endpoint, not just audit logs.
- `API/Filters/ValidationFilter.cs` — a global `IAsyncActionFilter`: for each action argument, resolves
  `IValidator<T>` from DI (if one is registered) and runs it, collecting failures into `ModelState` and
  short-circuiting with `400 ValidationProblemDetails` on failure. This is the "validation filter/pipeline
  behavior in the API layer" from CLAUDE.md §4 — implemented as an MVC filter rather than a MediatR pipeline
  behavior, since MediatR is explicitly excluded (CLAUDE.md §2).
- `Application/DependencyInjection.cs` updated: `AddValidatorsFromAssembly(...)` scans the Application
  assembly and registers every `AbstractValidator<T>` (just `PagedRequestValidator` so far) as `IValidator<T>`.
- `Program.cs` updated: `AddControllers(options => options.Filters.Add<ValidationFilter>())` applies it globally.

Packages added: `FluentValidation` (Application, API), `FluentValidation.DependencyInjectionExtensions`
(Application). `dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

This step also finished the remaining pieces of Module 0's cross-cutting scope that naturally live in
`Program.cs`'s pipeline setup alongside the first controller (global exception handling, structured
logging) — these were part of Module 0's original brief in CLAUDE.md §7 but didn't map to their own step
in the 10-step list, so they're bundled in here rather than skipped.

Files created:
- `Controllers/AuditLogsController.cs` — `GET /api/audit-logs` accepting `[FromQuery] PagedRequest`,
  delegating to `IAuditLogService`. Explicitly a thin end-to-end smoke-test endpoint for the Module 0
  stack, not the real Module 15 Audit Log UI (which gets proper filtering/UX later).
- `ExceptionHandling/GlobalExceptionHandler.cs` — `IExceptionHandler` (the .NET 8 built-in mechanism, no
  custom middleware needed) catching anything unhandled, logging it, and returning a `ProblemDetails` 500
  instead of leaking a stack trace. Validation failures never reach this — `ValidationFilter` (Step 7)
  already short-circuits those with a 400.
- `Program.cs` rewritten as a Serilog-bootstrapped host: console + rolling daily file sinks configured via
  `appsettings.json`'s new `Serilog` section, `Log.Fatal`/`Log.CloseAndFlush` around startup, plus
  `app.UseExceptionHandler()` and `AddExceptionHandler<GlobalExceptionHandler>()` wired in.

Packages added: `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Settings.Configuration` (API project).

**Smoke test performed** (`dotnet run`, no live Postgres available in this environment):
- `GET /api/audit-logs` → `500` with a clean `ProblemDetails` body (DB unreachable, caught by
  `GlobalExceptionHandler` instead of crashing the process) — confirms the exception-handling pipeline works.
- `GET /api/audit-logs?pageSize=0` → `400` with `"'Page Size' must be between 1 and 100. You entered 0."`
  — confirms `ValidationFilter` + `PagedRequestValidator` (Step 7) actually run.
- `GET /swagger/v1/swagger.json` → `200` — confirms Swagger/DI/controller registration all wired correctly.

Process was stopped after the smoke test. Full happy-path verification (a real `200` with real audit rows)
still needs a live Postgres instance — deferred to whenever Docker Desktop / a DB is available, or to
Module 1 when a real login round-trip is needed anyway.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

Scaffolded `frontend/dailymart-ui` per CLAUDE.md §6: `ng new` with routing, SCSS, standalone
components/signals (Angular 22's default - no NgModules), no SSR. Added Angular Material
(`ng add @angular/material`, azure-blue theme, animations) since this is the first UI feature and the
theme/typography shell applies to every future module. Had to manually `npm install @angular/animations`
- `ng add`'s animations wiring didn't actually add the package to `package.json`.

**Correction to CLAUDE.md:** its Testing section said Angular CLI defaults to Jasmine/Karma - that's no
longer true as of Angular 22, which defaults to a Vitest-based unit-test builder. Updated §11 to reflect
this.

Structure created (matches CLAUDE.md §6 exactly):
- `core/interceptors/api-base-url.interceptor.ts` - prefixes relative API calls with
  `environment.apiBaseUrl` so feature services never hardcode a host.
- `core/interceptors/error.interceptor.ts` - logs failed API calls; 401-redirect logic deferred to
  Module 1 (no login route exists yet to redirect to).
- `shared/models/paged-result.model.ts` - TypeScript mirror of the backend's `PagedRequest`/`PagedResult<T>`
  (camelCase, matching ASP.NET Core's default JSON casing).
- `features/audit-log/` - `audit-log.model.ts` (`AuditLogDto`), `audit-log.service.ts` (HttpClient-based,
  one API service per module per §6), `audit-log-list/` standalone component (Material table + paginator,
  signals for state). Lazy-loaded via `app.routes.ts`. Same as the backend controller, this is explicitly
  a thin smoke-test page for Module 0's plumbing, not Module 15's real Audit Log UI.
- `environments/environment.ts` / `environment.development.ts` - `apiBaseUrl` per environment (generated
  via `ng generate environments`, since newer Angular CLI no longer scaffolds these by default).
- `app.config.ts` - `provideHttpClient(withInterceptors([...]))`, `provideAnimationsAsync()`, `provideRouter()`.
- Removed the default Angular welcome template from `app.html`/`app.spec.ts`.

**Smoke test performed:**
- `ng build` - succeeds (lazy chunk for `audit-log-list` confirmed present).
- `ng test --watch=false` - 1/1 passing (Vitest-based runner, jsdom environment).
- `ng serve` - dev server responded `200` on `/`. Didn't verify the live data path since the backend's
  own live-DB path is also unverified (no Postgres in this environment, per Step 8) - the page's error
  branch (`"Could not load audit logs..."`) is what would render against a down API, which is the
  expected/correct behavior here.

### Step 10: Testing — ✅ Done

**Another correction surfaced by writing tests:** `Repository<T>`'s constructor took the concrete
`DailyMartDbContext`, and `UnitOfWork`'s constructor did too - but neither actually uses anything
`DailyMartDbContext`-specific (only `DbContext.Set<T>()` and `DbContext.SaveChangesAsync()`). That coupling
made it impossible to unit-test them against a throwaway test context. Loosened both constructors to the
`DbContext` base class. Consequence: `AddInfrastructure()`'s `IUnitOfWork` registration could no longer
rely on constructor auto-resolution (only `DailyMartDbContext` is registered with DI, not the `DbContext`
base type), so it's now an explicit factory:
`services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<DailyMartDbContext>()))`.
Re-ran the Step 8 smoke test afterward to confirm DI still resolves correctly end-to-end (it does - the
request now fails only at the actual Postgres connection attempt, per the stack trace).

Also extracted the soft-delete query-filter reflection loop out of `DailyMartDbContext.OnModelCreating`
into `SoftDeleteQueryFilterExtensions.ApplySoftDeleteQueryFilter()`, so a test context can apply the exact
same convention instead of a hand-copied re-implementation of it.

Test infrastructure added (`tests/DailyMart.UnitTests/TestSupport/`):
- `TestWidget : AuditableEntity` - Module 0 ships no real business entity yet (Product/Customer/etc. come
  in later modules), so this throwaway entity stands in for "some future auditable entity" to test the
  generic mechanisms against.
- `TestDbContext` - applies the same `ApplySoftDeleteQueryFilter()` convention as `DailyMartDbContext`.
- `TestDbContextFactory` - builds an isolated EF Core InMemory context per test with the real
  `AuditingSaveChangesInterceptor` attached.
- `FakeCurrentUserService` - configurable `ICurrentUserService` stand-in.

Packages added to `DailyMart.UnitTests`: `Microsoft.EntityFrameworkCore.InMemory`, `Moq`.

Tests written (15 total, all passing):
- `AuditingSaveChangesInterceptorTests` (4) - Created stamps CreatedAt/CreatedBy + writes a Created audit
  log; Modified stamps UpdatedAt + writes old/new/changed-columns; Remove converts to a soft delete
  (`EntityState.Unchanged`, `IsDeleted = true`) instead of a real delete; a soft-deleted row is excluded by
  the default query but still present with `IgnoreQueryFilters()`.
- `RepositoryTests` (4) - default paging orders by Id descending; `SortBy` sorts ascending by a real
  property; an unrecognized `SortBy` falls back to ordering by Id (respecting the requested direction);
  `Remove` + `SaveChangesAsync` through `UnitOfWork`/`Repository` together is a working soft delete.
- `PagedRequestValidatorTests` (4) - default request valid; `PageNumber < 1` invalid; `PageSize` outside
  `[1,100]` invalid; `SearchTerm` over 128 chars invalid.
- `AuditLogServiceTests` (1) - maps a mocked `IUnitOfWork`/`IRepository<AuditLog>` result to
  `AuditLogDto`, preserving paging metadata.

One test had to be corrected during the run: the "unknown `SortBy` falls back to Id" test initially assumed
the fallback always sorts descending, but the real implementation honors the request's `SortDescending`
flag even for the fallback (a deliberate, reasonable behavior) - fixed the test to set
`SortDescending = true` rather than changing working code to match a wrong assumption.

`dotnet test DailyMart.slnx`: **15/15 passing**. Frontend testing was already exercised in Step 9
(`ng test --watch=false`: 1/1 passing, Vitest-based runner).

---

**Module 0 (Cross-cutting infrastructure) is complete.** All 10 steps done and approved.

---

## Module 1 — Authentication

**Business process.** Single shop, single admin account for now (Role column exists for the BRD's "future
role extensibility" but only "Admin" is enforced today - no other role logic exists yet). Login exchanges
username/password for a short-lived JWT access token + a longer-lived opaque refresh token. The access
token is sent as a Bearer header on every subsequent request. When it expires, the client calls
`/auth/refresh` with the refresh token to get a new access token without re-entering credentials, until the
refresh token itself expires or is revoked. Logout revokes the refresh token server-side - a stolen refresh
token stops working immediately, though the (short-lived) access token can't be individually revoked before
its natural expiry, which is the inherent trade-off of stateless JWTs and why the access token lifetime is
kept short. Changing password revokes all of that user's existing refresh tokens, so a compromised session
doesn't survive a deliberate password change.

**Scope decision - "reset password":** the BRD lists it, but a self-service "forgot password" flow needs
email/SMS delivery, which CLAUDE.md §12 explicitly defers to Future scope. Building a reset-token endpoint
with nothing to deliver it through would be speculative infrastructure for a flow that can't work yet. Since
there is exactly one admin account, "reset" for now is an ops-level action (update the password hash
directly, or delete the user row so the app reseeds a fresh default admin on next boot) - not a new
endpoint. Revisit this once an Email/SMS module exists.

**Bootstrap decision:** there's no self-registration (single shop, single admin, per BRD). On startup, if
no `users` row exists, a data seeder creates exactly one default admin from configuration
(`Admin:DefaultUsername` / `Admin:DefaultPassword`), so there's a way to log in at all.

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`users`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `username` | `varchar(100)` | unique, not null |
| `password_hash` | `varchar(256)` | not null - `IPasswordHasher<User>` output, never plaintext |
| `full_name` | `varchar(200)` | not null |
| `role` | `varchar(50)` | not null, default `'Admin'` - future role extensibility |
| `is_active` | `boolean` | not null, default `true` |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` (via `AuditableEntity`) |

**`refresh_tokens`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `user_id` | `bigint` FK → `users.id` | not null |
| `token_hash` | `varchar(256)` | not null, SHA-256 of the actual token - plaintext never persisted |
| `expires_at` | `timestamptz` | not null |
| `revoked_at` | `timestamptz` | nullable |
| + shared audit columns | | `created_at` = issued time; `updated_at`/`updated_by` set on revoke |

Indexes: `users(username)` unique; `refresh_tokens(token_hash)` unique (fast lookup on refresh calls);
`refresh_tokens(user_id)` (fast "revoke all tokens for this user" on logout/password change).

Both entities inherit `AuditableEntity` for consistency with every other module, even though
`refresh_tokens.is_deleted` will realistically stay `false` always (tokens are revoked, not deleted).

### Step 2: Create Entity — ✅ Done

Files created in `DailyMart.Domain/Auth/`:
- `User.cs` - matches the Step 1 schema.
- `RefreshToken.cs` - matches the Step 1 schema, plus a computed `IsActive => RevokedAt is null &&
  ExpiresAt > UtcNow` property, so "is this token still usable" is a single domain-level check instead of
  being re-derived ad hoc in every service/query that needs it.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

**Bug found and fixed while generating this step's migration:** `dotnet ef migrations add` triggers
`Program.cs`'s top-level `try/catch`, and EF's design-time tooling deliberately throws
`HostAbortedException` to extract a configured host without actually running the app - but the catch block
(added in Module 0) treated that as a real crash and logged `Log.Fatal("...terminated unexpectedly")`. The
migration still worked (that exception is expected and harmless), but every future `dotnet ef` command
would have spammed a false fatal-error log line. Fixed: `catch (Exception ex) when (ex is not
HostAbortedException)`.

Files created:
- `Persistence/Configurations/UserConfiguration.cs` - table `users`, unique index on `username`, matches
  Step 1 column definitions/defaults exactly.
- `Persistence/Configurations/RefreshTokenConfiguration.cs` - table `refresh_tokens`, unique index on
  `token_hash`, index on `user_id`, FK to `users` with cascade delete. `builder.Ignore(t => t.IsActive)` -
  required because EF Core would otherwise try to map that computed property to a column.
- `DailyMartDbContext` updated: `DbSet<User> Users`, `DbSet<RefreshToken> RefreshTokens`.

**Migration**: `dotnet ef migrations add AddUsersAndRefreshTokens` generated successfully. Reviewed the
output - matches the Step 1 design exactly (both tables, both indexes, the FK with cascade delete). Not yet
applied to a live database (still no Postgres instance in this environment, same as Module 0's Step 3).
`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 4: Repository — 🔄 In Progress (awaiting approval)

**Design decision:** module-specific repositories are injected directly into services, not fetched via
`IUnitOfWork.Repository<T>()` - that method is constrained to the generic `IRepository<T>` contract and
has no way to return a specialized interface like `IUserRepository`. They still share the same scoped
`DailyMartDbContext` instance as `IUnitOfWork`, so `unitOfWork.SaveChangesAsync()` commits their changes too
- there's still one transaction boundary per request, just not one accessed exclusively through
`IUnitOfWork`. Made `Repository<T>`'s backing `DbSet<T>` field `protected` (renamed `Entities`) so
module-specific repositories can extend it instead of re-implementing the generic CRUD surface.

Files created:
- `Application/Auth/IUserRepository.cs` - adds `GetByUsernameAsync` to the generic contract.
- `Application/Auth/IRefreshTokenRepository.cs` - adds `GetByTokenHashAsync` and
  `RevokeAllActiveForUserAsync` (bulk-revoke for logout/password-change; stages changes only, caller still
  calls `SaveChangesAsync`).
- `Infrastructure/Persistence/Repositories/UserRepository.cs` / `RefreshTokenRepository.cs` - extend
  `Repository<T>`, implement the two interfaces above.
- `DependencyInjection.cs` updated: both registered via the same explicit-factory pattern as `IUnitOfWork`
  (constructor takes `DbContext`, only `DailyMartDbContext` is registered with DI).

`dotnet build DailyMart.slnx` succeeds; full test suite still **15/15 passing** (the `Entities` field
rename didn't touch any public API, so Module 0's tests were unaffected).

### Step 5: Service — 🔄 In Progress (awaiting approval)

`IAuthService`/`AuthService` returns a plain `(AccessToken, RefreshToken, ExpiresAtUtc)` tuple for now, not a
formal response DTO - Step 6 introduces `AuthResponseDto` and updates the signature, mirroring how Module
0's `AuditLogService` existed before `AuditLogDto` did.

**Login** (`LoginAsync`): look up by username, reject with `AuthenticationFailedException` (generic "Invalid
username or password" - doesn't reveal which part was wrong) if the user doesn't exist, is inactive, or the
password fails `IPasswordHasher<User>.VerifyHashedPassword`. Handles
`PasswordVerificationResult.SuccessRehashNeeded` by re-hashing and saving (standard ASP.NET Core Identity
hasher behavior - lets the hasher's work-factor be bumped later without forcing a manual rehash migration).
On success, issues an access token + refresh token pair.

**Refresh** (`RefreshAsync`): hash the presented token, look it up, reject if missing/inactive
(expired/revoked). **Rotates** the refresh token - the presented one is revoked and a new one issued in the
same call, so a captured/replayed refresh token stops working the instant the legitimate client redeems it.

**Logout** (`LogoutAsync`): revoke the token if it's found and still active; unknown or already-revoked
tokens are a silent no-op - logout must be idempotent and shouldn't leak whether a token ever existed.

**Change password** (`ChangePasswordAsync`): verify the current password, hash and store the new one, then
`RevokeAllActiveForUserAsync` - per the Step 1 business process, a deliberate password change must not leave
other existing sessions valid.

**Token generation:** `IJwtTokenGenerator`/`JwtTokenGenerator` (Infrastructure) signs a JWT with `sub`,
`unique_name`, `name`, and a `role` claim (the BRD's "future role extensibility" - only `"Admin"` is issued
today, but `[Authorize(Roles = "...")]` will work in later modules with zero JWT changes). Refresh tokens
are separate, opaque, cryptographically random strings (`RefreshTokenHasher.GenerateToken`, 64 bytes) -
only their SHA-256 hash is ever persisted (`RefreshTokenHasher.Hash`), so a database leak alone can't be
exchanged for a working session.

New config: `Jwt` section (`Secret`/`Issuer`/`Audience`/`AccessTokenMinutes`/`RefreshTokenDays`) and `Admin`
section (`DefaultUsername`/`DefaultPassword`, for the Step 1 bootstrap seeder - not implemented yet, coming
with Program.cs wiring in a later step) added to `appsettings.Development.json` with a locally-generated
dev-only secret. **Production deployment must override `Jwt:Secret` via environment variable/user
secrets/key vault before going live** - this dev secret must never be used outside local development.

Packages added: `Microsoft.Extensions.Identity.Core` (Application, for `IPasswordHasher<TUser>`),
`System.IdentityModel.Tokens.Jwt` (Infrastructure, for signing).

`dotnet build DailyMart.slnx` succeeds; test suite still **15/15 passing** (no existing behavior touched).

### Step 6: DTO — 🔄 In Progress (awaiting approval)

Files created:
- `LoginRequestDto` (`Username`, `Password`).
- `RefreshTokenRequestDto` (`RefreshToken`) - shared by both `/auth/refresh` and `/auth/logout`, since both
  are keyed by the same presented token; a second near-identical DTO would've just been duplication.
- `ChangePasswordRequestDto` (`CurrentPassword`, `NewPassword`).
- `AuthResponseDto` (`AccessToken`, `RefreshToken`, `ExpiresAtUtc`, `Username`, `FullName`, `Role`) - includes
  basic profile fields so the Angular client can render "logged in as ..." without a second API call.

`IAuthService`/`AuthService` updated: `LoginAsync`/`RefreshAsync`/`LogoutAsync` now take the DTOs above
instead of raw strings, and return `AuthResponseDto` instead of a tuple. `ChangePasswordAsync` still takes
`userId` as a separate parameter (not part of the DTO) - it comes from the authenticated caller's JWT
claims in the controller, never from the request body, so a user can never target anyone's password but
their own.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

Files created (auto-registered by the existing `AddValidatorsFromAssembly` scan from Module 0 - no extra
wiring needed, and the existing global `ValidationFilter` will run them automatically once Step 8 adds the
controller):
- `LoginRequestValidator` - `Username`/`Password` not empty, `Username` max 100 chars.
- `RefreshTokenRequestValidator` - `RefreshToken` not empty.
- `ChangePasswordRequestValidator` - `CurrentPassword` not empty; `NewPassword` not empty, minimum 8
  characters, must contain at least one letter and one digit, and must differ from `CurrentPassword`
  (rejects a no-op "change" to the same password).

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

Files created:
- `Controllers/AuthController.cs` - `POST /api/auth/login`, `/refresh`, `/logout` (all `[AllowAnonymous]`),
  `/change-password` (no `[AllowAnonymous]` - requires a valid access token; `userId` is read from the
  token's claims, never the request body).
- `Infrastructure/Persistence/Seed/AdminSeeder.cs` - if `users` is empty, seeds one admin from
  `Admin:DefaultUsername`/`Admin:DefaultPassword` config (there's no self-registration endpoint, per the
  Step 1 bootstrap decision). Runs through the normal `DbContext.SaveChangesAsync`, so it goes through
  Module 0's audit interceptor too - the very first user gets a real `Created` audit log entry, attributed
  to `"system"` (no HttpContext exists yet at startup).
- `ExceptionHandling/GlobalExceptionHandler.cs` updated: `AuthenticationFailedException` now maps to `401`
  with the exception's own message as the ProblemDetails title, logged as a `Warning` (expected, client-caused)
  rather than an `Error` (unexpected, server-caused) - everything else still falls through to the generic 500.
- `Program.cs` updated:
  - JWT Bearer authentication wired (`AddAuthentication().AddJwtBearer(...)`) using the `Jwt` config section,
    validating issuer/audience/signing key/lifetime.
  - **`[Authorize]` by default** (CLAUDE.md §4): `AuthorizationOptions.FallbackPolicy` requires an
    authenticated user on every endpoint unless explicitly `[AllowAnonymous]`. This also now applies to
    Module 0's `AuditLogsController` with no code change there - it will return `401` without a token from
    this point on, where it previously would attempt (and fail) a DB call directly.
  - `app.UseAuthentication()` added before `app.UseAuthorization()` - this middleware was missing entirely
    before now (harmless while nothing needed to be authorized yet).
  - `AdminSeeder.SeedAsync()` called once at startup via a created scope, before the app starts serving requests.
  - Swagger now has a Bearer auth scheme configured, so tokens can be pasted into Swagger UI's "Authorize"
    dialog for manual testing.

**Package version conflict found and fixed:** adding `Microsoft.AspNetCore.Authentication.JwtBearer`
8.0.10 pulled in `Microsoft.IdentityModel.Protocols`/`Protocols.OpenIdConnect` 7.1.2, while Step 5's
`System.IdentityModel.Tokens.Jwt` had resolved to the newest 8.20.0 - a genuine mismatch across the
`Microsoft.IdentityModel.*` family that the build flagged as a warning (`IDX00001`) and that could cause a
runtime `TypeLoadException`/`MissingMethodException`. Fixed by pinning
`System.IdentityModel.Tokens.Jwt` to `7.1.2` in Infrastructure to match what the JwtBearer package actually
brings in. `dotnet build DailyMart.slnx` now succeeds with **0 warnings**, not just 0 errors.

**Startup verified, live DB still unavailable:** ran the API - it fails fast and cleanly exactly where
expected (`AdminSeeder.SeedAsync`'s first database call), logs `Log.Fatal("...terminated unexpectedly")`
via the real global handler (not a crash dump), and exits. This confirms the auth wiring/DI/seeder all
construct correctly; a full login-round-trip smoke test still needs a live Postgres instance (same
limitation noted since Module 0). Compensating for that, Step 10 will unit-test `AuthService` directly with
mocked repositories/hasher/token generator, the way `AuditLogServiceTests` did in Module 0.

Test suite still **15/15 passing** (nothing existing was touched).

### Step 9: Angular — 🔄 In Progress (awaiting approval)

**Token storage decision:** both the access and refresh tokens are kept in `localStorage`, managed
entirely by `AuthService` - not an httpOnly cookie for the refresh token (which resists XSS token theft
better, at the cost of cookie/CORS/SameSite plumbing on the API side). This is a single-shop,
single-admin internal tool, not public-facing/multi-tenant, so the simpler approach was chosen. Revisit if
this app's threat model ever changes.

Files created under `core/auth/`:
- `auth.models.ts` - `LoginRequest`, `RefreshTokenRequest`, `ChangePasswordRequest`, `AuthResponse`,
  `AuthenticatedUser` (TS mirrors of the backend DTOs, camelCase).
- `auth.service.ts` - signals for `isAuthenticated`/`currentUser`; `login`/`refresh`/`logout`/
  `changePassword` call the API and update stored session state via `tap`.
- `jwt.interceptor.ts` - attaches `Authorization: Bearer <token>` to outgoing requests (skipping the
  `/auth/login`, `/auth/refresh`, `/auth/logout` endpoints themselves); on a `401`, attempts exactly one
  silent refresh-and-retry, falling back to `clearSession()` + redirect to `/login` if that also fails.
  **Known simplification, called out explicitly rather than silently shipped:** concurrent requests that
  401 at the same moment each trigger their own independent `refresh()` call rather than sharing one
  in-flight refresh; since refresh tokens are rotated/single-use server-side, the second concurrent
  refresh would fail. Acceptable for now given how rarely that race should actually occur - a shared
  in-flight-refresh subject would fix it if it turns out to matter in practice.
- `auth.guard.ts` - functional `CanActivateFn`, redirects to `/login` when not authenticated.
- `safe-storage.ts` - defensive `localStorage` wrapper. **Needed because of a real environment gap found
  while testing this step**: Angular 22's Vitest-based unit-test runner doesn't provide a working
  `localStorage` (`Cannot read properties of undefined (reading 'getItem')`), which crashed `AuthService`'s
  field initializers under test. Wrapping every access in try/catch fixes the test failure and is also
  good defensive practice for any environment where storage might be unavailable (private browsing, etc.).

Files created under `features/auth/login/`: `login.component.ts/.html/.scss` - standalone component,
Reactive Forms, Material card/form-field/button. On success, navigates to `/audit-log`; on failure, shows
"Invalid username or password."

Routing/config updated:
- `app.routes.ts` - `/login` (public) and everything else behind `authGuard`.
- `app.config.ts` - registered `jwtInterceptor`, ordered last in the interceptor array so it's innermost
  and sees the response (and can silently retry a 401) before `errorInterceptor` would log it as a hard
  failure.
- `app.ts`/`app.html`/`app.scss` - a minimal top toolbar (shown only when authenticated) with the current
  user's name and a Logout button, since there's no dedicated app-shell/navigation module yet.

`ng build`: succeeds (new `login-component` lazy chunk present). `ng test --watch=false`:
**1/1 passing** (after the `safe-storage` fix above). `ng serve`: dev server responded `200`. Didn't
attempt a full browser-driven check of the guard's redirect behavior (would need a headless-browser tool
not currently in this toolchain) - the guard logic itself is simple enough that build success + unit test
coverage is the verification for this step, consistent with Module 0's precedent.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/Auth/`), all against mocked repositories/hasher/token generator -
no DB required, compensating for the still-unavailable live Postgres:
- `AuthServiceTests` (12) - login success (asserts the *persisted* `TokenHash` is really the SHA-256 of
  the plaintext token handed back to the client - the actual hashing code runs for real even though the
  repository is mocked, so this catches a real hashing bug, not just a mock interaction); login failures
  (unknown user, inactive user, wrong password); the `SuccessRehashNeeded` path re-hashes and persists;
  refresh success rotates the old token and issues new ones; refresh failures (unknown/already-revoked
  token); logout revokes an active token and is a no-op (no `SaveChangesAsync` call) for an unknown one;
  change-password success revokes all other sessions; change-password failure touches nothing.
- `JwtTokenGeneratorTests` (3) - the token actually carries `sub`/name/role claims matching the user,
  issuer/audience/expiry come from `JwtSettings`, and two tokens for the same user get different `jti`s.
- `RefreshTokenTests` (3) - the domain `IsActive` computed property: true when neither revoked nor
  expired, false once revoked (even if not yet expired), false once expired (even if never revoked).

`dotnet test DailyMart.slnx`: **33/33 passing** (15 from Module 0 + 18 new).

**Frontend** (`core/auth/*.spec.ts`):
- `auth.service.spec.ts` (3) - starts unauthenticated; `login()` stores the session and flips
  `isAuthenticated`; `logout()` clears it.
- `auth.guard.spec.ts` (2) - allows navigation when authenticated; returns a redirect `UrlTree` to
  `/login` when not.

**Bug found and fixed by writing these tests:** `AuthService.getRefreshToken()` read directly from
`localStorage` on every call instead of the in-memory signal `accessToken` uses - harmless when storage
works, but in this test environment (same missing-`localStorage` gap as Step 9) it silently returned `null`
even right after a successful login, which the new `login()` test caught immediately. Fixed by giving the
refresh token its own signal (`refreshTokenSignal`), same pattern as the access token: storage is now
purely a write-behind persistence layer for surviving page reloads, never the source of truth during a
running session.

`ng test --watch=false`: **6/6 passing** (1 app shell + 3 AuthService + 2 guard). `ng build`: still succeeds.

---

**Module 1 (Authentication) is complete.** All 10 steps built, tested, and approved.

---

## Module 2 — Settings

**Business process.** Single shop, so Settings is a **singleton** - exactly one row, never a list. It
holds shop-wide configuration that other modules read as defaults rather than each module reinventing its
own copy: shop identity (name/address/phone/email/logo) for invoice headers; invoice footer text and a
number prefix for the receipts Module 9 (POS Sales) will print; currency code/symbol for how every money
value is displayed; a default VAT % and default discount % that Product (Module 4) and POS Sales
(Module 9) fall back to unless overridden per-product/per-sale; and backup/system preference flags. A
default row is seeded at startup (same pattern as Module 1's `AdminSeeder`) so the app never has "no
settings" as a state the rest of the code needs to handle. There's no create/delete - only "get the one
row" and "update the one row" (plus a dedicated logo-upload action).

**Scope decision - "Backup Settings":** the BRD lists this as a Settings concern, but no module anywhere
in the BRD calls for an actual backup *execution* engine (a scheduled `pg_dump`/file-copy job, a "restore"
endpoint, etc.) - building one would be speculative infrastructure with nothing consuming it yet. This
module stores the *preferences* only (enabled/frequency) as configuration for a future ops job to read;
implementing that job is explicitly out of scope here.

**Scope decision - "System Preferences":** deliberately minimal (`DateFormat`, `TimeZone`) rather than
inventing speculative fields for a BRD heading with no concrete requirements behind it yet. More
preferences get added here later, if and when another module actually needs one.

**Shop Logo storage:** saved to disk (a local `uploads/` folder under the API's content root) via a small
`IFileStorageService`, with only the resulting relative URL stored in the `settings` row - not the image
bytes in Postgres. Reused as-is when Product (Module 4) needs image uploads later.

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`settings`** (singleton - application logic guarantees exactly one row; no unique/check constraint is
needed to enforce that since only the seeder ever inserts a row and the service never exposes a "create"):

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | always `1` in practice |
| `shop_name` | `varchar(200)` | not null |
| `shop_address` | `varchar(500)` | nullable |
| `shop_phone` | `varchar(50)` | nullable |
| `shop_email` | `varchar(200)` | nullable |
| `shop_logo_url` | `varchar(500)` | nullable - relative URL from `IFileStorageService`, not the image itself |
| `invoice_prefix` | `varchar(20)` | not null, default `'INV-'` |
| `invoice_footer_text` | `varchar(500)` | nullable |
| `currency_code` | `varchar(10)` | not null, default `'BDT'` |
| `currency_symbol` | `varchar(10)` | not null, default `'৳'` |
| `default_vat_percentage` | `numeric(5,2)` | not null, default `0`, `0..100` |
| `default_discount_percentage` | `numeric(5,2)` | not null, default `0`, `0..100` |
| `backup_enabled` | `boolean` | not null, default `false` |
| `backup_frequency` | `varchar(20)` | not null, default `'Daily'` (`Daily`/`Weekly`/`Monthly`) |
| `date_format` | `varchar(20)` | not null, default `'dd/MM/yyyy'` |
| `time_zone` | `varchar(100)` | not null, default `'UTC'` |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` (via `AuditableEntity`) |

No other tables - this module is entirely one row plus the file-storage side effect of the logo upload
endpoint.

### Step 2: Create Entity — ✅ Done

Files created in `DailyMart.Domain/Settings/`:
- `ShopSettings.cs` - matches the Step 1 schema. Named `ShopSettings` rather than `Settings` to avoid a
  type name colliding with its own containing namespace (`DailyMart.Domain.Settings`).
- `BackupFrequency.cs` - enum `Daily | Weekly | Monthly`.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

Files created:
- `Persistence/Configurations/ShopSettingsConfiguration.cs` - table `settings`, matches the Step 1
  column definitions/defaults exactly (including `numeric(5,2)` for the two percentage columns and the
  `BackupFrequency` enum stored as a `varchar` string, consistent with how `AuditAction` is stored in
  Module 0's `audit_logs`).
- `DailyMartDbContext` updated: `DbSet<ShopSettings> ShopSettings`.

**Migration**: `dotnet ef migrations add AddShopSettings` generated cleanly (no `HostAbortedException`
noise this time - confirms Module 1's fix is working). Reviewed the output - matches the Step 1 design
exactly. Not yet applied to a live database (still no Postgres instance in this environment).
`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 4: Repository — 🔄 In Progress (awaiting approval)

Files created:
- `Application/Settings/IShopSettingsRepository.cs` - adds `GetSingletonAsync()` to the generic contract,
  so callers never need to know or track the row's `Id`.
- `Infrastructure/Persistence/Repositories/ShopSettingsRepository.cs` - extends `Repository<T>`,
  `GetSingletonAsync` is just `Entities.FirstOrDefaultAsync(...)`.
- `DependencyInjection.cs` updated: registered via the same explicit-factory pattern as
  `IUserRepository`/`IRefreshTokenRepository`.

`dotnet build DailyMart.slnx` succeeds; test suite still **33/33 passing**.

### Step 5: Service — 🔄 In Progress (awaiting approval)

`IShopSettingsService`/`ShopSettingsService` returns/accepts the domain `ShopSettings` entity directly for
now, not a DTO - Step 6 introduces the DTOs and updates this, same pattern as Module 0/1.

- **`GetAsync`**: returns the singleton row, or throws `InvalidOperationException` if it's somehow missing
  (shouldn't happen - a seeder guarantees it exists, added in Step 8 alongside Program.cs wiring, matching
  how Module 1's `AdminSeeder` was introduced in its own Step 8).
- **`UpdateAsync`**: copies every field from the caller's payload onto the tracked singleton **except**
  `ShopLogoUrl` - that's deliberately untouched here so a plain settings-form save can never accidentally
  wipe the logo; only `UploadLogoAsync` changes it.
- **`UploadLogoAsync`**: validates the file extension against an allow-list (`.jpg/.jpeg/.png/.webp`) and a
  2 MB size cap, rejecting violations with a new `BusinessRuleException` (400 - distinct from
  `AuthenticationFailedException`'s 401; its `GlobalExceptionHandler` mapping is added in Step 8, same
  pairing as how Module 1 wired its own exception type alongside its controller). On success, saves via
  `IFileStorageService` and updates `ShopLogoUrl`.

New supporting pieces (genuinely cross-cutting, not Settings-specific, so they'll be reused later -
e.g. Product images in Module 4):
- `Application/Common/Interfaces/IFileStorageService.cs` - `SaveAsync(content, fileName, subFolder)` →
  relative URL.
- `Infrastructure/Files/LocalFileStorageService.cs` - saves under `wwwroot/uploads/{subFolder}`;
  **replaces the caller-supplied filename with a fresh GUID** (keeping only the original extension) so a
  crafted filename (path traversal segments, etc.) never reaches the filesystem.
- `Application/Common/Exceptions/BusinessRuleException.cs`.

`dotnet build DailyMart.slnx` succeeds; test suite still **33/33 passing**.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

Files created:
- `ShopSettingsDto` - full response shape including `ShopLogoUrl`; `BackupFrequency` exposed as `string`
  (consistent with how `AuditLogDto.Action` handles its enum).
- `UpdateShopSettingsRequestDto` - every editable field **except `ShopLogoUrl`** - reinforcing at the DTO
  level (not just the service) that logo changes only ever happen through the dedicated upload endpoint.
- `ShopSettingsMappingExtensions.cs` - `ToDto()` and `ApplyTo()` (request → tracked entity). `ApplyTo`
  calls `Enum.Parse<BackupFrequency>` on the incoming string, documented as relying on Step 7's validator
  having already rejected anything else before this code runs.

`IShopSettingsService`/`ShopSettingsService` updated to use these DTOs instead of the raw domain entity;
added a private `GetEntityAsync` helper so `UpdateAsync`/`UploadLogoAsync` can still get the tracked entity
while `GetAsync` returns the DTO.

`dotnet build DailyMart.slnx` succeeds; test suite still **33/33 passing**.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

`UpdateShopSettingsRequestValidator`: `ShopName`/`InvoicePrefix`/`CurrencyCode`/`CurrencySymbol`/
`DateFormat`/`TimeZone` required with length caps; `ShopEmail` validated as a real email address only when
provided (it's optional); `DefaultVatPercentage`/`DefaultDiscountPercentage` clamped to `[0, 100]`;
`BackupFrequency` must parse to a real `BackupFrequency` enum value; `TimeZone` must be a time zone
`TimeZoneInfo.TryFindSystemTimeZoneById` actually recognizes - catches a typo'd zone at request time
instead of it silently breaking date calculations wherever `TimeZone` eventually gets used.

`dotnet build DailyMart.slnx` succeeds; test suite still **33/33 passing**.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

Files created:
- `Controllers/SettingsController.cs` - `GET /api/settings`, `PUT /api/settings`,
  `POST /api/settings/logo` (`multipart/form-data`, `IFormFile? file`). No `[AllowAnonymous]` anywhere -
  shop configuration sits behind the global `[Authorize]` fallback policy like everything else since
  Module 1. `UploadLogoAsync` returning the full `ShopSettingsDto` (refined during this step, see Step 6)
  means the endpoint doesn't need a second round-trip to hand back fresh settings after a logo change.
- `Infrastructure/Persistence/Seed/ShopSettingsSeeder.cs` - same shape as Module 1's `AdminSeeder`: if
  `settings` is empty, inserts one row (`ShopName = "DailyMart"`, everything else its entity-level default).
  Unlike the admin username/password, `"DailyMart"` doesn't need to come from configuration - it's the
  literal name of this application (BRD: "Shop Name: DailyMart"), a safe zero-config default.
- `ExceptionHandling/GlobalExceptionHandler.cs` updated: `BusinessRuleException` now maps to `400`.
- `Program.cs` updated: both seeders now run at startup (`AdminSeeder` then `ShopSettingsSeeder`);
  `app.UseStaticFiles()` added so whatever `LocalFileStorageService` saves under `wwwroot/uploads` is
  actually servable back out at the relative URL stored in `ShopLogoUrl`.

**Startup re-verified:** ran the API - fails at exactly the same point as Module 1 (`AdminSeeder`'s first
DB call, no live Postgres here), confirming nothing about this module's DI wiring broke startup before
that point. A full request-level smoke test (hitting `/api/settings`) still needs a live database, same
limitation noted since Module 0/1.

Test suite still **33/33 passing**.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

Files created under `features/settings/`:
- `settings.model.ts` - `ShopSettingsDto`, `UpdateShopSettingsRequest`, `BackupFrequency` union type
  (TS mirrors of the backend DTOs, camelCase).
- `settings.service.ts` - `get()`/`update()`/`uploadLogo(file: File)`. `uploadLogo` builds a `FormData`
  and posts it directly - `HttpClient` sets the correct multipart `Content-Type`/boundary itself, so it's
  never set manually (doing so would break the boundary).
- `settings-form/settings-form.component.ts/.html/.scss` - Reactive Forms, Material card/form-field/
  select/checkbox. Logo upload is a separate action from the main "Save Settings" button (a plain
  `<input type="file">` triggers `uploadLogo` immediately on selection) - matching the backend design
  where `PUT /settings` never touches `ShopLogoUrl`.

Routing/nav updated:
- `app.routes.ts` - `/settings` added alongside `/audit-log`, both behind `authGuard`.
- `app.html`/`app.ts` - added "Audit Log" and "Settings" nav links to the toolbar (`RouterLink` added to
  `App`'s imports).

`ng build`: succeeds (`settings-form-component` lazy chunk present). `ng test --watch=false`: still
**6/6 passing** (no new frontend unit tests added this step - covered in Step 10). `ng serve`: dev server
responded `200`.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/Settings/`), against mocked repository/unit-of-work/file-storage -
no DB required:
- `ShopSettingsServiceTests` (7) - `GetAsync` returns the mapped DTO, throws `InvalidOperationException`
  if the singleton is missing; `UpdateAsync` applies every field **except** `ShopLogoUrl` (explicitly
  asserts an existing logo URL survives an unrelated settings update untouched) and saves; `UploadLogoAsync`
  rejects an unsupported extension and an oversized file *without ever calling* `IFileStorageService`
  (verified via `Times.Never`), and on a valid file calls storage, updates `ShopLogoUrl`, and saves.
- `UpdateShopSettingsRequestValidatorTests` (7) - a fully valid request passes; empty `ShopName` invalid;
  VAT % outside `[0,100]` invalid; an unrecognized `BackupFrequency` invalid; an unrecognized `TimeZone`
  invalid; a malformed (but present) `ShopEmail` invalid; a missing `ShopEmail` valid (it's optional).

**Bug avoided while writing these tests:** `UpdateShopSettingsRequestDto` is a plain class with `init`
properties (matching every other DTO in the project), not a `record` - an initial draft of the validator
tests used `request with { ... }` non-destructive mutation syntax, which only compiles for records. Fixed
before it ever hit a build by switching to a parameterized `ValidRequest(...)` factory method instead of
introducing an inconsistent record type just to support `with`.

`dotnet test DailyMart.slnx`: **47/47 passing** (33 from Modules 0-1 + 14 new).

**Frontend** (`features/settings/settings.service.spec.ts`, 3 tests): `get()` issues a `GET`; `update()`
issues a `PUT` with the exact request body; `uploadLogo()` posts a `FormData` body (asserted via
`toBeInstanceOf(FormData)`, confirming the service never manually sets a `Content-Type` header that would
break the multipart boundary `HttpClient` sets itself).

`ng test --watch=false`: **9/9 passing** (6 from Module 1 + 3 new). `ng build`: still succeeds.

---

**Module 2 (Settings) is complete.** All 10 steps built, tested, and approved.

---

## Module 3 — Master Data (Category, Brand, Unit)

**Business process.** Three independent lookup lists a shop owner maintains so Product (Module 4) can
classify items: which department a product belongs to (Category: Grocery, Beverage, Cosmetics, Snacks,
Household), who makes it (Brand), and how it's measured/sold (Unit: Piece, kg, Liter, Packet, Box). None
of them have their own workflow beyond CRUD - no approval process, no state machine - so all three are
handled together as one module (matching how CLAUDE.md's build order already groups them as a single
numbered module) rather than repeating all 10 steps three separate times. Each step below covers all
three entities together, with one approval gate per step.

**Scope decisions:**
- **No separate `IsActive` flag.** Unlike `User` (Module 1), where deactivating a login is meaningfully
  different from deleting it, there's no BRD requirement or real workflow distinction here between
  "temporarily disabled" and "deleted" - a shop owner who doesn't want a category just deletes it (soft
  delete via the Module 0 interceptor already hides it from active lists while preserving history/audit).
  Adding a second flag alongside `IsDeleted` with near-identical meaning would just be two overlapping
  on/off switches for the same real-world action.
- **No module-specific repositories.** Every operation these three need - list/page, get by id, check for
  a duplicate name, add, update, remove - is already expressible through the generic `IRepository<T>`
  (`ExistsAsync(predicate)` covers the duplicate-name check). Module 1/2 needed specialized repositories
  because they needed a *named* lookup (`GetByUsernameAsync`, `GetSingletonAsync`); nothing here does, so
  introducing `ICategoryRepository` etc. would be pure ceremony. Step 4 for this module is expected to be
  "no new repository code" - a one-line justification is still a valid Step 4 outcome, not a shortcut being
  skipped.
- **Three separate services/controllers, not one generic `CrudService<T>`.** Category and Brand happen to
  be shape-identical (`Name` + `Description`) today, but they're independent concepts that Product will
  reference separately and may diverge later - collapsing them into a shared generic service now would be
  the premature abstraction CLAUDE.md's guidance warns against. Three small, explicit, near-identical
  classes are preferred over one generic one built to save writing three.

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`categories`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `name` | `varchar(100)` | unique, not null |
| `description` | `varchar(500)` | nullable |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` (via `AuditableEntity`) |

**`brands`** - identical shape to `categories` (`name` unique not null, `description` nullable, + audit
columns) - a separate table, not a shared one, since Product will hold independent `CategoryId`/`BrandId`
foreign keys to each.

**`units`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `name` | `varchar(50)` | unique, not null - e.g. "Piece", "Kilogram" |
| `symbol` | `varchar(10)` | not null - e.g. "pc", "kg", "L" (compact form for invoices/labels) |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` |

**Uniqueness rule:** the DB unique index on `name` catches exact duplicates, but Postgres's default
collation is case-sensitive, so `"Grocery"` and `"grocery"` wouldn't collide at the DB level. The service
layer (Step 5) additionally checks for a case-insensitive duplicate before insert/update and rejects it
with a `BusinessRuleException` (400) - the same pattern Module 2 introduced for logo-upload validation.

### Step 2: Create Entity — ✅ Done

Files created in `DailyMart.Domain/MasterData/`: `Category.cs`, `Brand.cs` (both just `Name`/`Description`),
`Unit.cs` (`Name`/`Symbol`) - matching the Step 1 schema exactly. No shared base type between
`Category`/`Brand` despite being shape-identical, per the Step 1 scope decision against premature
abstraction.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

**Real bug found and fixed, retroactively affecting Module 1 too:** a plain `.IsUnique()` index on
`name`/`username` interacts badly with soft delete. Once a row is soft-deleted (`IsDeleted = true`
instead of a real `DELETE`, per Module 0's interceptor), it still physically occupies that unique value in
Postgres forever - so deleting a category called "Grocery" and trying to create a new "Grocery" category
would fail with a duplicate-key error, even though the old one is supposed to be gone. This bug has existed
since Module 1's `users.username` unique index; it just hadn't been exercised yet. Fixed everywhere by
switching to a **partial (filtered) unique index** - `HasFilter("is_deleted = false")` - so the constraint
only applies among *active* rows. Applied to `users.username` (retroactive fix) and the three new
`name` indexes.

Files created:
- `Persistence/Configurations/CategoryConfiguration.cs`, `BrandConfiguration.cs`, `UnitConfiguration.cs` -
  match the Step 1 schema, each with the filtered unique index above.
- `UserConfiguration.cs` updated with the same filtered-index fix.
- `DailyMartDbContext` updated: `DbSet<Category>`, `DbSet<Brand>`, `DbSet<Unit>`.

**Migration**: `dotnet ef migrations add AddMasterDataAndFixSoftDeleteUniqueIndexes` generated cleanly.
Reviewed the output - creates all three tables, drops and recreates `ix_users_username` with the `WHERE
is_deleted = false` filter, and creates the three new indexes with the same filter. Not yet applied to a
live database. `dotnet build DailyMart.slnx` succeeds; test suite still **47/47 passing**.

### Step 4: Repository — ✅ Done (no new code)

As decided in Step 1: `IUnitOfWork.Repository<Category>()` / `Repository<Brand>()` / `Repository<Unit>()`
(the fully generic path, no specialized interfaces) already provide everything the three services need -
`GetPagedAsync` for listing, `GetByIdAsync`, `AddAsync`, `Update`, `Remove`, and `ExistsAsync(predicate)`
for the case-insensitive duplicate-name check Step 5 needs. Nothing to add here; recorded explicitly so
this step isn't mistaken for one that got skipped.

### Step 5: Service — 🔄 In Progress (awaiting approval)

Three parallel services (`CategoryService`, `BrandService`, `UnitService`), each: paged list (with an
optional `Name.Contains(SearchTerm)` filter), get-by-id (`NotFoundException` if missing/soft-deleted),
create, update, delete (soft, via `Repository.Remove`) - accepting/returning the domain entity directly for
now (DTOs come in Step 6, same pattern as every service so far).

**Duplicate-name check**: each service has an `EnsureNameIsUniqueAsync(name, excludeId, ...)` that compares
lower-cased names (`ExistsAsync` predicate), throwing `BusinessRuleException` on a hit. `excludeId` is
`null` on create and the entity's own `id` on update (so renaming "Grocery" to itself, or making an
unrelated edit, doesn't trip over its own name). This is the case-insensitive layer sitting on top of the
DB's case-sensitive filtered unique index from Step 3 - together they close the gap either one alone would
leave open.

New shared exception: `Application/Common/Exceptions/NotFoundException.cs` - maps to 404 in Step 8, same
pairing as `AuthenticationFailedException`/`BusinessRuleException` before it. Genuinely reusable - every
future module's "get by id" will use this, not just master data.

`DependencyInjection.cs` updated: `ICategoryService`, `IBrandService`, `IUnitService` registered.

`dotnet build DailyMart.slnx` succeeds; test suite still **47/47 passing**.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

Six DTOs (`CategoryDto`/`CategoryRequestDto`, `BrandDto`/`BrandRequestDto`, `UnitDto`/`UnitRequestDto`) +
three mapping-extension files. **Each entity uses one request DTO for both create and update**, not
separate `Create.../Update...` DTOs - the writable shape is identical either way, so a second near-identical
type would just be duplication (same reasoning as `RefreshTokenRequestDto` in Module 1).

All three services updated to accept/return these DTOs: `GetByIdAsync` now returns the DTO, with a private
`GetEntityAsync` helper (same shape as Module 2's `ShopSettingsService`) so `UpdateAsync`/`DeleteAsync` can
still get the tracked entity. `GetPagedAsync` maps the whole page's `Items` through `.ToDto()`.

`dotnet build DailyMart.slnx` succeeds; test suite still **47/47 passing**.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

`CategoryRequestValidator`/`BrandRequestValidator` (`Name` required, max 100; `Description` max 500) and
`UnitRequestValidator` (`Name` required, max 50; `Symbol` required, max 10) - matching the Step 1 column
lengths exactly. Auto-registered by the existing assembly scan.

`dotnet build DailyMart.slnx` succeeds; test suite still **47/47 passing**.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

`CategoriesController`, `BrandsController`, `UnitsController` - identical shape:
`GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` under `api/categories`, `api/brands`, `api/units`.
`Create` returns `201` via `CreatedAtAction`. None are `[AllowAnonymous]` - protected by the global
`[Authorize]` fallback policy like everything since Module 1.

`GlobalExceptionHandler` updated: `NotFoundException` now maps to `404` (the mapping deferred from Step 5,
same pairing pattern as every other exception type so far).

**Startup re-verified:** fails at exactly the same point as Module 1/2 (`AdminSeeder`'s DB call, no live
Postgres here) - nothing about this module's DI/routing wiring broke startup before that point. Test suite
still **47/47 passing**.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

**Design decision:** three separate feature folders (`master-data/category`, `.../brand`, `.../unit`),
each with its own model/service/list component - not one generic parameterized "lookup CRUD" component.
Mirrors the backend's Step 1 decision against a shared abstraction, even though Angular component reuse
for this kind of mechanical UI is generally lower-risk than a backend generic-service abstraction would've
been - kept consistent with the rest of the module rather than introducing a different philosophy on just
the frontend half.

**UI simplification, called out explicitly:** each list page uses one component with an inline add/edit
form (toggled by a signal), not a separate `MatDialog` component - matching the existing login/settings
pages' one-component-per-screen pattern. Delete confirmation uses the browser's `confirm()` rather than a
custom confirm dialog. Both keep this MVP CRUD screen to a single component instead of two per entity;
revisit if a nicer confirm/edit UX becomes a real ask later.

Files created (× 3, for category/brand/unit): `{entity}.model.ts`, `{entity}.service.ts`,
`{entity}-list/{entity}-list.component.ts/.html/.scss`. Each service exposes `getPaged`/`create`/
`update`/`delete` against its own endpoint (`/categories`, `/brands`, `/units`).

Routing/nav updated: `/categories`, `/brands`, `/units` added under `authGuard`; toolbar links added.

`ng build`: succeeds (all three `*-list-component` lazy chunks present). `ng test --watch=false`: still
**9/9 passing** (no new unit tests this step - covered in Step 10). `ng serve`: dev server responded `200`.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/MasterData/`), against mocked `IUnitOfWork`/`IRepository<T>`:
- `CategoryServiceTests` (8) - paged-to-DTO mapping; 404 on missing id; duplicate-name rejection
  (case-insensitive) with `IRepository<T>.AddAsync` verified as **never called**; successful create;
  **and** a compiled-predicate test that captures the actual `Expression<Func<Category,bool>>` passed to
  `ExistsAsync` and invokes it against sample entities - proving the entity being updated is excluded from
  its own duplicate check (`Id = 5` renaming to its own name passes) while a *different* row with the same
  name still fails, rather than just asserting the service didn't throw.
- `BrandServiceTests` (6), `UnitServiceTests` (6) - the same CRUD/exception coverage as Category, since the
  services are structurally identical. Deliberately **don't** repeat the compiled-predicate assertion three
  times - it verifies one shared pattern, already proven once against Category.
- `CategoryRequestValidatorTests` (4), `BrandRequestValidatorTests` (2), `UnitRequestValidatorTests` (3).

`dotnet test DailyMart.slnx`: **76/76 passing** (47 from Modules 0-2 + 29 new).

**Frontend** (`features/master-data/category/category.service.spec.ts`, 4 tests, representative of all
three - Brand/Unit services are the same shape): `getPaged()` sends the right query params; `create()`
posts the exact body; `update()`/`delete()` hit the right per-id URL.

`ng test --watch=false`: **13/13 passing** (9 from Modules 0-2 + 4 new). `ng build`: still succeeds.

---

**Module 3 (Master Data) is complete.** All 10 steps built, tested, and approved.

---

## Module 4 — Product

**Business process.** Product is the item catalog - what's actually sold in the shop. Every product
belongs to a Category (required) and a Unit of measure (required), optionally a Brand. It carries three
prices - purchase/cost, selling/retail, and an optional wholesale price for bulk buyers - plus a
per-product discount % and tax % (both default from `Settings.DefaultDiscountPercentage`/
`DefaultVatPercentage` at creation time but freely overridable per product afterward), a barcode for POS
scanning, an internal code, current stock, a minimum-stock threshold (Dashboard/Inventory alerts will read
this later), and an optional image.

**Scope decision - stock is set once, not edited here.** `CurrentStock` can only be set at creation (the
product's "opening stock"). Product's *update* endpoint deliberately excludes it. Once Purchase/Inventory/
POS Sales (Modules 7-9) exist, they become the only things that ever move stock again, each writing an
auditable `InventoryTransaction` row per CLAUDE.md §5. If Product's own edit form could silently overwrite
`CurrentStock`, that would be a second, untracked path to change stock - defeating the whole point of that
audit trail before it's even built. Revisit only if a genuine "correct a data-entry mistake" admin
adjustment tool is asked for later, and even then it should almost certainly live in Inventory (Module 8),
not here.

**Scope decision - barcode generation vs. printing.** If a shop owner doesn't already have a barcode for
a product, the backend generates a real, valid **EAN-13** numeric barcode (prefixed `20`, GS1's reserved
"in-store use" range - exactly this scenario) so every product is scannable. *Printing* an actual barcode
graphic is a frontend concern (a client-side JS barcode-rendering library) - the backend only ever
generates/stores/validates the barcode **value** (a string), never an image.

**Scope decision - Import deferred, Export included.** The BRD lists Import/Export together, but they're
very different in risk: Export (dump the catalog to CSV) is simple and low-risk, done in this pass. Import
(parse a CSV, resolve Category/Brand/Unit *names* back to ids, validate every row, report partial
success/failure) is materially more complex and isn't blocking any other module's build order - proposing
to build it as a focused fast-follow once Product's core CRUD has been used for a while, rather than
rushing it into the same pass as everything else this module already needs. Flagging this clearly now;
happy to pull it forward if that's not the right call.

**Divergence from Module 3's "no specialized repository" decision.** Unlike Category/Brand/Unit, Product
genuinely needs exact-match lookups beyond generic CRUD: `GetByBarcodeAsync` (what the POS barcode-scanner
workflow will call in Module 9 - "scan → product auto-selected" needs an exact barcode match, not a
paged/filtered list) and `GetByCodeAsync` (uniqueness checks). `IProductRepository` is warranted here for
the same reason Module 1 warranted `IUserRepository` - a real, current need, not a speculative one.

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`products`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `code` | `varchar(50)` | unique (filtered, `is_deleted = false`), not null - shop's own internal SKU |
| `barcode` | `varchar(50)` | unique (filtered), not null - user-supplied or auto-generated EAN-13 |
| `name` | `varchar(200)` | not null |
| `category_id` | `bigint` FK → `categories.id` | not null |
| `brand_id` | `bigint` FK → `brands.id` | nullable - not every product has a brand |
| `unit_id` | `bigint` FK → `units.id` | not null |
| `purchase_price` | `numeric(12,2)` | not null, `>= 0` |
| `selling_price` | `numeric(12,2)` | not null, `>= 0`, `>= purchase_price` unless `allow_price_below_cost` |
| `wholesale_price` | `numeric(12,2)` | nullable, `>= 0` |
| `discount_percentage` | `numeric(5,2)` | not null, `0..100`, defaults from Settings at creation |
| `tax_percentage` | `numeric(5,2)` | not null, `0..100`, defaults from Settings at creation |
| `current_stock` | `numeric(18,3)` | not null, `>= 0` - set at creation only, see scope decision above |
| `minimum_stock` | `numeric(18,3)` | not null, `>= 0`, default `0` |
| `allow_price_below_cost` | `boolean` | not null, default `false` |
| `image_url` | `varchar(500)` | nullable - relative URL from `IFileStorageService`, same pattern as the shop logo |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` |

Indexes: `code` unique filtered; `barcode` unique filtered; standard FK indexes on `category_id`/
`brand_id`/`unit_id` (EF Core convention); an index on `name` for search.

`numeric(18,3)` for stock quantities (not a whole-number type) so fractional-unit products (e.g. `2.5 kg`)
are representable - the same column type is used regardless of whether a product's unit happens to be
whole (Piece, Box) or fractional (kg, Liter); simpler than a variant type per unit kind.

### Step 2: Create Entity — ✅ Done

`DailyMart.Domain/Products/Product.cs` - matches the Step 1 schema exactly. `CategoryId`/`BrandId`/
`UnitId` are plain FK properties, no navigation collections back from `Category`/`Brand`/`Unit` - nothing
needs to traverse "all products in this category" through EF navigation yet, so none was added
speculatively.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

`Persistence/Configurations/ProductConfiguration.cs` - matches Step 1 exactly: `numeric(12,2)` prices,
`numeric(5,2)` percentages, `numeric(18,3)` stock quantities, filtered unique indexes on `code`/`barcode`
(same fix pattern as Module 3), plain index on `name`, and `DeleteBehavior.Restrict` on all three FKs
(`CategoryId`/`BrandId`/`UnitId`) - deleting a Category/Brand/Unit should never silently delete products;
in practice this rarely even triggers since normal deletes go through Module 0's soft-delete interceptor
(an `UPDATE`, never a real `DELETE`). `DailyMartDbContext` updated: `DbSet<Product> Products`.

**Migration**: `dotnet ef migrations add AddProducts` generated cleanly. Reviewed the output - matches the
design exactly, including both filtered unique indexes, the three FK constraints with `Restrict`, and the
plain `name` index. Not yet applied to a live database. `dotnet build DailyMart.slnx` succeeds; test suite
still **76/76 passing**.

### Step 4: Repository — 🔄 In Progress (awaiting approval)

`Application/Products/IProductRepository.cs` - adds `GetByBarcodeAsync`/`GetByCodeAsync` to the generic
contract, per the Step 1 divergence decision. `Infrastructure/Persistence/Repositories/ProductRepository.cs`
implements both as straightforward `Entities.FirstOrDefaultAsync(...)` calls. Registered in DI via the
same explicit-factory pattern as every other module-specific repository so far.

`dotnet build DailyMart.slnx` succeeds; test suite still **76/76 passing**.

### Step 5: Service — 🔄 In Progress (awaiting approval)

`IProductService`/`ProductService` accepts/returns the domain `Product` directly for now (DTOs in Step 6).

- **`Ean13BarcodeGenerator`** (new, `internal`) - generates a valid EAN-13 value prefixed `20` (GS1's
  reserved in-store-use range) using `RandomNumberGenerator`, with a real checksum digit. `CreateAsync`
  calls it only when no barcode was supplied, retrying up to 5 times against `ExistsAsync` on collision
  (astronomically unlikely - 10 random digits - but checked anyway rather than trusted).
- **FK existence checks** (`ValidateReferencesExistAsync`) - Category/Unit (required) and Brand (only if
  provided) are checked via `_unitOfWork.Repository<T>().ExistsAsync(...)` directly, not by depending on
  `ICategoryService`/`IBrandService`/`IUnitService` - only an existence boolean is needed, not a full DTO,
  and this keeps `ProductService` decoupled from the other modules' services. Failures throw
  `BusinessRuleException` (400) rather than `NotFoundException` (404) - deliberately distinguishing "the
  *referenced* id in your request body doesn't exist" from "the *primary resource in the URL* doesn't
  exist" (`GetByIdAsync`/`GetByBarcodeAsync` still use `NotFoundException` for that).
- **Pricing rule** (`ValidatePricing`) - `SellingPrice >= PurchasePrice` unless `AllowPriceBelowCost` is
  set, checked on both create and update (after applying the update, so it validates the *new* prices).
- **Code/barcode uniqueness** - same case-insensitive-with-`excludeId` pattern as Module 3's master data,
  reused for `Code`; barcode uniqueness is checked case-sensitively (`Barcode ==`, not `.ToLower() ==`) -
  unlike names, EAN-13 values are pure digit strings with no case to normalize.
- **`UpdateAsync` never assigns `CurrentStock`** - the Step 1 scope decision enforced in code, not just
  documentation: `updatedValues.CurrentStock` is simply never read.
- **`UploadImageAsync` returns the full updated `Product`**, not just the URL - applying the lesson from
  Module 2's logo-upload refinement proactively this time, rather than shipping the narrower shape and
  fixing it in a follow-up step.

`DependencyInjection.cs` updated: `IProductService` registered. `dotnet build DailyMart.slnx` succeeds;
test suite still **76/76 passing**.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

**Design decision - denormalized names in the response, without adding EF navigation properties.**
`Product` still has no navigation properties to `Category`/`Brand`/`Unit` (Step 2's decision stands), but
a product list showing raw FK ids would be unusable UI. Rather than reversing Step 2, added
`ProductLookups` (internal) + `BuildLookupsAsync`: for any set of products, batch-fetch the distinct
Category/Brand/Unit ids referenced across them (one query per table, not one per product) and build
`Dictionary<long, ...>` lookups, then map `CategoryName`/`BrandName`/`UnitName`/`UnitSymbol` onto each DTO
from those dictionaries. Keeps `Product` itself decoupled while still giving the UI real names.

DTOs: `ProductDto` (full response, including the resolved names above); `ProductRequestDto` (the update
shape - no `CurrentStock`); `CreateProductRequestDto : ProductRequestDto` (adds only `CurrentStock`, the
one field only creation can set) - inheritance chosen here specifically because it's a strict superset
relationship, unlike Module 3's separate-DTO decision where the two operations' shapes were identical
rather than one containing the other.

`ProductMappingExtensions.cs`: `CreateProductRequestDto.ToEntity()`, `ProductRequestDto.ApplyTo(Product)`
(mirrors `ShopSettingsService`'s pattern from Module 2 - doesn't touch `Barcode` or `CurrentStock`, both
handled separately by the caller), `Product.ToDto(ProductLookups)`.

`IProductService`/`ProductService` updated throughout to use these DTOs; `GetEntityAsync`/`MapToDtoAsync`
private helpers added (same shape as every DTO-introducing step so far).

`dotnet build DailyMart.slnx` succeeds; test suite still **76/76 passing**.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

`ProductRequestValidator` - `Code`/`Name` required with length caps; `CategoryId`/`UnitId > 0`;
prices `>= 0`; `WholesalePrice >= 0` only when provided; `DiscountPercentage`/`TaxPercentage` clamped to
`[0,100]`; `MinimumStock >= 0`. `CreateProductRequestValidator` **includes** those rules via
`Include(new ProductRequestValidator())` (valid since `CreateProductRequestDto` extends
`ProductRequestDto`) and adds only `CurrentStock >= 0` on top - avoiding a second full copy of every
field-length/range rule.

Note: the deeper cross-field rule (`SellingPrice >= PurchasePrice` unless `AllowPriceBelowCost`) stays in
`ProductService`, not here - FluentValidation validates the *shape* of a single request; that rule is too
tied to persistence/business context to belong in a stateless validator.

`dotnet build DailyMart.slnx` succeeds; test suite still **76/76 passing**.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

`Controllers/ProductsController.cs` - `GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` under
`api/products`, plus `GET /barcode/{barcode}` (what Module 9's scanner workflow will call) and
`POST /{id}/image` (multipart, same pattern as Module 2's logo upload). None `[AllowAnonymous]`.

**Export** (`GET /api/products/export`) - added `IProductService.GetAllForExportAsync` (every product,
unpaginated, mapped through the same `ProductLookups` batching as the paged list) and a small CSV builder
in the controller itself (proper quote/comma/newline escaping). CSV formatting is an HTTP-response-shape
concern, not a business rule, so it lives at the controller layer rather than in the service - consistent
with keeping the multipart-upload handling there too. Per Step 1's scope decision, there is still no
Import counterpart.

**Startup re-verified:** fails at exactly the same point as every module since Module 1 (`AdminSeeder`'s
DB call, no live Postgres here). Test suite still **76/76 passing**.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

**Barcode printing implemented** (the frontend half of Step 1's generation/printing split): added the
`jsbarcode` package + `shared/utils/barcode-print.ts` - renders a barcode into a new browser tab as an SVG
and triggers the print dialog. Deliberately uses **CODE128**, not EAN13: a user-supplied barcode isn't
guaranteed to satisfy EAN13's checksum (only the server's auto-generated ones do), and CODE128 encodes any
string without that constraint while still being scannable by standard readers. Added
`allowedCommonJsDependencies: ["jsbarcode"]` to `angular.json` to silence the resulting CommonJS build
warning (the package isn't published as ESM).

**Dedicated form page, not inline** (unlike Module 3's master data pages) - Product has far more fields
(pricing, dropdowns, image, barcode) than a single-line inline form could reasonably hold, so
`/products/new` and `/products/:id/edit` are their own route, landing back on the list after save from
edit mode, or redirecting `new → :id/edit` after a successful create (that's where image upload and
barcode printing become available, both of which need an existing product id).

**Known MVP limitation, called out explicitly:** Category/Brand/Unit dropdowns are populated via a single
`pageSize=100` fetch, not a dedicated unpaginated "list all" endpoint - the shared `PagedRequest`
validator caps `pageSize` at 100 anyway (Module 0), so this is already the ceiling that convention allows.
Revisit if a shop ever has more than 100 of any of these.

Files created: `features/products/product.model.ts`, `product.service.ts` (incl. `exportCsv()` returning
a `Blob`, downloaded via an object URL + a programmatically-clicked anchor - there's no simpler
browser-native way to save an arbitrary response body to disk); `product-list/` (search, paginated table,
Export CSV button, add/edit/delete); `product-form/` (Reactive Forms, Category/Brand/Unit selects, image
upload, barcode display + print - image/barcode sections only shown in edit mode).

Routing/nav updated: `/products`, `/products/new`, `/products/:id/edit` under `authGuard`; toolbar link
added.

`ng build`: succeeds, 0 warnings (after the `angular.json` fix). `ng test --watch=false`: still **13/13
passing** (no new unit tests this step - covered in Step 10). `ng serve`: dev server responded `200`.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/Products/`), against mocked `IProductRepository`/`IUnitOfWork`
(with `Repository<Category>()`/`Repository<Brand>()`/`Repository<Unit>()` each returning their own mocked
`IRepository<T>` for FK-existence checks and name-lookup building) and `IFileStorageService`:

- `ProductServiceTests` (19) - paged results resolve Category/Brand/Unit names; 404 on missing
  id/barcode; each of the three FK-existence checks (Category/Unit/Brand) independently rejects an
  unknown id; duplicate-code rejection (`AddAsync` verified **never called**); a **duplicate-barcode**
  test that (like Module 3's "excludes self" test) doesn't just assert an exception was thrown - it
  configures a single sentinel `Product` whose `Code` differs from the request's but whose `Barcode`
  matches, proving the service can actually tell the two uniqueness checks apart rather than one check
  accidentally masking the other; barcode auto-generation verified against a **real EAN-13 checksum
  recomputed independently in the test** (not just "is a 13-digit string") and confirmed prefixed `20`;
  the pricing rule both ways (rejected by default, allowed when `AllowPriceBelowCost` is set);
  `UpdateAsync` **provably never changes `CurrentStock`** (asserted on the returned DTO *and* the tracked
  entity) and keeps the existing barcode when the request's is blank; delete/image-upload success and
  404/rejection paths.
- `ProductRequestValidatorTests` (6), `CreateProductRequestValidatorTests` (3) - including one test that
  specifically proves `Include(new ProductRequestValidator())` actually pulled in the base rules (an empty
  `Code` fails validation on the *derived* validator), not just that the derived validator has its own
  rules.

`dotnet test DailyMart.slnx`: **105/105 passing** (76 from Modules 0-3 + 29 new).

**Frontend** (`features/products/product.service.spec.ts`, 4 tests): `getPaged()` query params;
`create()` posts the body; `uploadImage()` posts `FormData` (never manually sets `Content-Type`, same
check as Module 2's logo upload); `exportCsv()` requests with `responseType: 'blob'`.

`ng test --watch=false`: **17/17 passing** (13 from Modules 0-3 + 4 new). `ng build`: still succeeds, 0 warnings.

---

**Module 4 (Product) is complete.** All 10 steps built, tested, and approved.

---

## Module 5 — Supplier

**Business process.** A supplier is who the shop buys stock from. Each supplier can start with an
**opening balance** - what the shop already owed them before this software existed - and from then on
owes/is owed money as purchases and payments happen (Modules 7 and 11, not built yet). BRD splits this
into two modules: Supplier (CRUD, opening balance, ledger, due report) here, and Supplier Due (payable
ledger, payment history) later in Module 11. The dividing line: **Module 5 builds the ledger
infrastructure and records the opening balance as its first real entry; Module 11 is what actually adds
Purchase/Payment entries to it once those modules exist.** Per CLAUDE.md's business rule "supplier due
must always match unpaid purchases," a supplier's due is never just an editable number - it's the
running total of a ledger, the same way a bank balance is the sum of its transactions, not a field you
overwrite.

**Scope decision - build the real ledger table now, not a placeholder.** Rather than a single
`CurrentDue` field, Module 5 introduces `supplier_ledger_entries` (supplier id, entry type, signed amount,
running balance snapshot, transaction date) *now*. This isn't speculative infrastructure for modules that
don't exist yet - the BRD's own "Opening balance" and "Ledger" bullets are two separate concepts precisely
because the opening balance is meant to be the ledger's first entry, not a bare column. Purchase (Module 7)
and the payment side of Supplier Due (Module 11) will later just add more rows of `EntryType = Purchase` /
`Payment` to this same table - no redesign needed then.

**Scope decision - opening balance is write-once.** Same pattern as Product's `CurrentStock` (Module 4):
`OpeningBalance` can only be set at creation (where it also creates the one ledger entry) and is excluded
from the update DTO entirely. Editing a supplier's balance after the fact isn't a "profile edit" - it's a
financial adjustment, which (if ever needed) belongs in the ledger as its own `Adjustment` entry type, not
a silent overwrite. No "add adjustment" endpoint is being built in this pass either, since the BRD doesn't
ask for one yet - `Adjustment` exists in the enum now so it's ready when it is asked for.

**Scope decision - no separate "Due Report" endpoint yet.** With no Purchase/Payment data to aggregate,
a dedicated due-report endpoint would show nothing a plain supplier list (with `CurrentDue` on each row)
doesn't already show. Real due reporting (aging, total payable across all suppliers, etc.) is Module 11's
job once there's actual purchase/payment activity to report on.

**Divergence, allowed vs. Customer:** `CurrentDue`/`OpeningBalance` may be **negative** here (the supplier
owes the shop - an overpayment or return credit) - unlike Customer (Module 6), where the BRD explicitly
states "customer due cannot become negative." No such restriction exists for suppliers in the BRD.

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`suppliers`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `name` | `varchar(200)` | unique (filtered, `is_deleted = false`), not null |
| `contact_person` | `varchar(200)` | nullable |
| `phone` | `varchar(50)` | nullable |
| `email` | `varchar(200)` | nullable |
| `address` | `varchar(500)` | nullable |
| `opening_balance` | `numeric(12,2)` | not null, default `0` - write-once, set at creation only |
| `current_due` | `numeric(12,2)` | not null, default `0` - cached running balance, never client-writable |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` |

**`supplier_ledger_entries`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `supplier_id` | `bigint` FK → `suppliers.id` | not null, `Restrict` (same reasoning as Product's FKs) |
| `entry_type` | `varchar(20)` | `OpeningBalance \| Purchase \| Payment \| Adjustment` - only `OpeningBalance` produced by this module |
| `description` | `varchar(500)` | nullable |
| `amount` | `numeric(12,2)` | not null - **signed**: positive increases what's owed, negative decreases it |
| `balance_after` | `numeric(12,2)` | not null - running-balance snapshot, standard ledger UX |
| `transaction_date` | `timestamptz` | not null - when the underlying event happened (same as `created_at` for an opening-balance entry) |
| + shared audit columns | | |

Indexes: `suppliers.name` unique filtered (same pattern/reasoning as Module 3's master data);
`supplier_ledger_entries.supplier_id` for fast per-supplier ledger lookups.

### Step 2: Create Entity — ✅ Done

Files created in `DailyMart.Domain/Suppliers/`: `Supplier.cs`, `SupplierLedgerEntry.cs`,
`SupplierLedgerEntryType.cs` (enum `OpeningBalance | Purchase | Payment | Adjustment`) - matching the
Step 1 schema exactly.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

`SupplierConfiguration` (filtered unique index on `name`, same as Module 3's master data),
`SupplierLedgerEntryConfiguration` (`EntryType` enum stored as `varchar`, `Restrict` on the FK to
`suppliers`, index on `supplier_id`). `DailyMartDbContext` updated with both `DbSet`s.

**Migration**: `dotnet ef migrations add AddSuppliers` generated cleanly. Reviewed the output - matches
the design exactly: both tables, the filtered unique index, the FK with `Restrict`, and the
`supplier_id` index. Not yet applied to a live database. `dotnet build DailyMart.slnx` succeeds; test
suite still **105/105 passing**.

### Step 4: Repository — ✅ Done (no new code)

Neither `Supplier` nor `SupplierLedgerEntry` needs a specialized repository. The duplicate-name check
reuses the generic `ExistsAsync(predicate)` pattern (Module 3). The ledger listing is "all entries where
`SupplierId == x`, paged, sorted by `TransactionDate`" - already fully expressible through the generic
`GetPagedAsync(PagedRequest, predicate)` (the predicate supplies the `SupplierId` filter; `PagedRequest.
SortBy = "TransactionDate"` handles the ordering via the existing reflection-based sort). Recorded
explicitly, same as Module 3's Step 4, so it isn't mistaken for a skipped step.

### Step 5: Service — 🔄 In Progress (awaiting approval)

`ISupplierService`/`SupplierService` accepts/returns the domain entity for now (DTOs in Step 6).

- **`CreateAsync` does two `SaveChangesAsync` calls, not one** - a first for this module. The opening-
  balance ledger entry's `SupplierId` FK needs the new supplier's database-generated `Id`, and with no
  navigation property between `Supplier`/`SupplierLedgerEntry` (deliberately, per Step 1) EF Core has no
  way to fix that up automatically within a single `SaveChanges` call - the `Id` genuinely isn't populated
  until after the first save completes. Every other module so far only ever creates one new root entity
  per operation, so this is a new (and correct) pattern, not an oversight. No entry is created at all when
  `OpeningBalance == 0` - a zero balance has no real ledger event to record.
- **`UpdateAsync` never assigns `OpeningBalance`/`CurrentDue`** - Step 1's scope decision enforced in code.
- **`GetLedgerAsync` defaults to oldest-first** (`TransactionDate` ascending) when the caller doesn't
  specify a sort - deliberately overriding the generic repository's usual newest-first default, since a
  ledger reads top-to-bottom like a bank statement, not like an activity feed. Throws `NotFoundException`
  up front if the supplier itself doesn't exist, rather than silently returning an empty page for a
  nonexistent parent resource.
- **Duplicate-name check** - same case-insensitive-with-`excludeId` pattern as Module 3/Module 4.

`DependencyInjection.cs` updated: `ISupplierService` registered. `dotnet build DailyMart.slnx` succeeds;
test suite still **105/105 passing**.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

`SupplierDto` (full response, including `OpeningBalance`/`CurrentDue`); `SupplierRequestDto` (update
shape - no balance fields); `CreateSupplierRequestDto : SupplierRequestDto` (adds only `OpeningBalance`) -
same inheritance choice as Module 4's `CreateProductRequestDto`, for the same reason (a genuine strict
superset, not two independently-shaped DTOs). `SupplierLedgerEntryDto` (`EntryType` as `string`,
consistent with how every other enum-backed DTO field in this project is exposed).

`ISupplierService`/`SupplierService` updated throughout to use these DTOs; `GetEntityAsync` private helper
added (same shape as every DTO-introducing step so far).

`dotnet build DailyMart.slnx` succeeds; test suite still **105/105 passing**.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

`SupplierRequestValidator` - `Name` required with a length cap; `ContactPerson`/`Phone`/`Address` length
caps; `Email` validated as a real address only when provided. `CreateSupplierRequestValidator` **includes**
those rules (`OpeningBalance` gets no range rule - it may legitimately be negative, per Step 1).

Worth flagging: `CreateSupplierRequestValidator` isn't just for DRY here, it's **required to exist at
all** - `ValidationFilter` looks up `IValidator<T>` by the request's exact runtime type
(`CreateSupplierRequestDto`), so without a validator specifically for that type, create requests would go
completely unvalidated even though `SupplierRequestValidator` exists for the (different) update type.
Same reason Module 4 needed `CreateProductRequestValidator`.

`dotnet build DailyMart.slnx` succeeds; test suite still **105/105 passing**.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

`Controllers/SuppliersController.cs` - `GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` under
`api/suppliers`, plus `GET /{id}/ledger` (paged). None `[AllowAnonymous]`. The ledger endpoint will only
ever show an `OpeningBalance` entry until Purchase (Module 7) and Supplier Due's payment side (Module 11)
start adding `Purchase`/`Payment` rows to the same table.

**Startup re-verified:** fails at exactly the same point as every module since Module 1 (`AdminSeeder`'s
DB call, no live Postgres here). Test suite still **105/105 passing**.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

Files created under `features/suppliers/`: `supplier.model.ts`, `supplier.service.ts`;
`supplier-list/` - inline add/edit form (Module 3's pattern, not a `MatDialog`), `OpeningBalance` field
shown only while creating (matches the backend excluding it from the update DTO entirely), a "Ledger"
row-action navigating to a dedicated ledger page; `supplier-ledger/` - a read-only paged table (date,
type, description, signed amount, running balance) with the supplier's name/current due in the header -
will only ever show one `OpeningBalance` row until Purchase/Supplier Due (Modules 7/11) add more.

Routing/nav updated: `/suppliers` and `/suppliers/:id/ledger` under `authGuard`; toolbar link added.

`ng build`: succeeds (`supplier-list-component` and the ledger component's lazy chunks present).
`ng test --watch=false`: still **17/17 passing** (no new unit tests this step - covered in Step 10).
`ng serve`: dev server responded `200`.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/Suppliers/`), against mocked
`IRepository<Supplier>`/`IRepository<SupplierLedgerEntry>`/`IUnitOfWork`:

- `SupplierServiceTests` (13) - paged mapping; 404 on missing id; duplicate-name rejection
  (`AddAsync` verified **never called**); a zero opening balance creates **no** ledger entry (verified
  via `Times.Never`); a non-zero opening balance creates a matching ledger entry - captured via callback
  and asserted directly: `EntryType == OpeningBalance`, `Amount`/`BalanceAfter` both equal the opening
  balance, and `SupplierId` matches the **simulated database-assigned `Id`** (the callback sets `s.Id = 42`
  on the captured supplier before the ledger entry is built, mirroring what EF Core actually does between
  the two `SaveChangesAsync` calls); `UpdateAsync` provably never changes `OpeningBalance`/`CurrentDue`
  (asserted on the returned DTO *and* the tracked entity); delete success/404; `GetLedgerAsync` 404s when
  the supplier doesn't exist, and two tests around the sort-default behavior - one capturing the
  `PagedRequest` actually passed to the ledger repository to confirm it defaults to
  `TransactionDate`/ascending, another confirming an explicit caller-supplied sort is passed through
  unchanged rather than overridden.
- `SupplierRequestValidatorTests` (4), `CreateSupplierRequestValidatorTests` (3) - including one test
  that a **negative** `OpeningBalance` is valid (Step 1's supplier-may-be-owed-money divergence from
  Customer) and one proving `Include()` actually pulled in the base validator's rules.

`dotnet test DailyMart.slnx`: **124/124 passing** (105 from Modules 0-4 + 19 new).

**Frontend** (`features/suppliers/supplier.service.spec.ts`, 3 tests): `getPaged()`, `create()`,
`getLedger()` each hit the right URL/method.

`ng test --watch=false`: **20/20 passing** (17 from Modules 0-4 + 3 new). `ng build`: still succeeds.

---

**Module 5 (Supplier) is complete.** All 10 steps built, tested, and approved.

---

## Module 6 — Customer

**Business process.** A customer is who buys from the shop, sometimes on credit. BRD splits this the
same way it split Supplier: Customer (CRUD, ledger, due report) here, and Customer Due (receivable
ledger, outstanding due, payment collection) later in Module 10.

**Scope decision - this module is CRUD only, no ledger table yet (a real divergence from Module 5).**
Supplier got its ledger table built immediately because Supplier's own BRD bullets include **"Opening
balance"** - a genuine Module-5-owned event that needed somewhere real to be recorded on day one. Customer
has **no equivalent bullet** - re-reading the BRD, Supplier lists "CRUD, Opening balance, Ledger, Due
report" while Customer lists only "CRUD, Ledger, Due report" (no opening balance). That's not an
oversight to paper over by copying Supplier's design - it's the BRD telling us customers start at zero due
with nothing to seed a ledger with, since the *only* thing that will ever create a customer due is a
credit sale, and Sale doesn't exist until Module 9. Building `CustomerLedgerEntry` now would be a table
with zero possible rows until then - exactly the kind of speculative infrastructure this project has
avoided elsewhere (Product's Import, Settings' backup execution). Ledger and Due Report are deferred in
full to Modules 9 (credit sale writes the first entries) and 10 (payment collection + the ledger/due-report
UI), not just "the payment part" like Supplier's split.

**Consequence: no `CurrentDue` field either.** Since there is no way to create a due in this module's own
scope, a `CurrentDue` column would only ever read `0` for every customer until Module 9 exists - adding it
now buys nothing over adding it when Module 9 actually needs it.

**"Customer due cannot become negative"** (CLAUDE.md's business rule) has no code to write yet either - it
constrains Module 10's payment-collection logic (a payment can never exceed the outstanding due), not
anything in Module 6.

**Divergence from Category/Brand/Supplier: customer `Name` is not unique.** Real people share names
often enough ("Karim", "Rahim") that enforcing name uniqueness would incorrectly block legitimate
customers. `Phone`, however, is a much stronger real-world identity signal - enforced unique when
provided (optional: a walk-in customer without a phone number on file is still a valid record).

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`customers`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `name` | `varchar(200)` | not null - **not unique** (see above) |
| `phone` | `varchar(50)` | nullable, unique (filtered, `is_deleted = false`) - Postgres already treats multiple `NULL`s as non-colliding in a unique index, so customers with no phone on file never conflict with each other |
| `email` | `varchar(200)` | nullable |
| `address` | `varchar(500)` | nullable |
| + shared audit columns | | `created_at/by`, `updated_at/by`, `is_deleted` |

No other tables this module - `customer_ledger_entries` (mirroring `supplier_ledger_entries`) is deferred
to Module 9, when there's an actual event to write to it.

### Step 2: Create Entity — ✅ Done

`DailyMart.Domain/Customers/Customer.cs` - matches the Step 1 schema exactly.

`dotnet build DailyMart.slnx` succeeds, 0 warnings/errors.

### Step 3: Configure EF Core — 🔄 In Progress (awaiting approval)

`CustomerConfiguration` - filtered unique index on `Phone` (not `Name`), matching the Step 1 design.
`DailyMartDbContext` updated with `DbSet<Customer>`.

**Migration**: `dotnet ef migrations add AddCustomers` generated cleanly. Reviewed the output - matches
the design exactly: one table, one filtered unique index on `phone`. Not yet applied to a live database.
`dotnet build DailyMart.slnx` succeeds; test suite still **124/124 passing**.

### Step 4: Repository — ✅ Done (no new code)

No specialized repository needed. The duplicate-phone check reuses the generic `ExistsAsync(predicate)`
pattern (same as Category/Brand/Unit/Supplier's name checks); listing/search is plain `GetPagedAsync` with
a name/phone-contains predicate. Recorded explicitly, same as Modules 3/5's Step 4.

### Step 5: Service — 🔄 In Progress (awaiting approval)

`ICustomerService`/`CustomerService` accepts/returns the domain entity for now (DTOs in Step 6). Simple
CRUD - no ledger/opening-balance logic (nothing for Module 6 to write, per Step 1). The one business rule:
**`EnsurePhoneIsUniqueAsync` is a no-op when `Phone` is null/blank** - unlike every other module's
uniqueness check so far, this one is conditional, since `Phone` is optional and only enforced unique when
the caller actually provides one. Search matches `Name` **or** `Phone`, reflecting how a cashier would
actually look a customer up (by name or by phone number).

`DependencyInjection.cs` updated: `ICustomerService` registered. `dotnet build DailyMart.slnx` succeeds;
test suite still **124/124 passing**.

### Step 6: DTO — 🔄 In Progress (awaiting approval)

`CustomerDto` (response) + **one** `CustomerRequestDto` used for both create and update - unlike
Product/Supplier, there's no field only one operation can set (no opening balance/stock equivalent here),
so the two shapes are identical, same reasoning as Module 3's master data DTOs (not Module 4/5's
inheritance pattern, since there's no superset relationship to model).

`ICustomerService`/`CustomerService` updated throughout to use these DTOs; `GetEntityAsync` private helper
added (same shape as every DTO-introducing step so far).

`dotnet build DailyMart.slnx` succeeds; test suite still **124/124 passing**.

### Step 7: Validator — 🔄 In Progress (awaiting approval)

**Only one validator needed for this module** - `CustomerRequestValidator`, targeting `CustomerRequestDto`
directly. Since that same DTO is used for both create and update (Step 6), `ValidationFilter`'s
exact-runtime-type lookup finds it either way - no second "CreateCustomerRequestValidator" needed the way
Product/Supplier required, since there's no second DTO type to register a validator for.

Rules: `Name` required with a length cap; `Phone`/`Address` length caps; `Email` validated as a real
address only when provided.

`dotnet build DailyMart.slnx` succeeds; test suite still **124/124 passing**.

### Step 8: Controller — 🔄 In Progress (awaiting approval)

`Controllers/CustomersController.cs` - `GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` under
`api/customers`. No ledger endpoint - none exists yet (Step 1). None `[AllowAnonymous]`.

**Startup re-verified:** fails at exactly the same point as every module since Module 1 (`AdminSeeder`'s
DB call, no live Postgres here). Test suite still **124/124 passing**.

### Step 9: Angular — 🔄 In Progress (awaiting approval)

Files created under `features/customers/`: `customer.model.ts`, `customer.service.ts`, `customer-list/` -
inline add/edit form (Modules 3/5's pattern, not a `MatDialog`) with a search box (name or phone). No
"ledger" row-action like Supplier's list has - there's nothing to show yet (Step 1).

Routing/nav updated: `/customers` under `authGuard`; toolbar link added.

`ng build`: succeeds (`customer-list-component` lazy chunk present). `ng test --watch=false`: still
**20/20 passing** (no new unit tests this step - covered in Step 10). `ng serve`: dev server responded `200`.

### Step 10: Testing — ✅ Done

**Backend** (`tests/DailyMart.UnitTests/Customers/`), against mocked `IRepository<Customer>`/`IUnitOfWork`:

- `CustomerServiceTests` (9) - paged mapping; 404 on missing id; creating **without** a phone skips the
  uniqueness check entirely (`ExistsAsync` verified **never called** - not just "no error", proving the
  conditional check is genuinely conditional); duplicate-phone rejection on create; a unique phone
  succeeds; update 404; update rejects a phone that belongs to a *different* customer (the excludeId
  pattern); delete success/404.
- `CustomerRequestValidatorTests` (4) - valid request passes; empty name invalid; malformed email invalid
  when provided; missing email valid since it's optional.

`dotnet test DailyMart.slnx`: **137/137 passing** (124 from Modules 0-5 + 13 new).

**Frontend** (`features/customers/customer.service.spec.ts`, 3 tests): `getPaged()`, `create()`,
`delete()` each hit the right URL/method.

`ng test --watch=false`: **23/23 passing** (20 from Modules 0-5 + 3 new). `ng build`: still succeeds.

---

**Module 6 (Customer) is complete.** All 10 steps built, tested, and approved.

---

## Module 7 — Purchase

This is the largest module yet - it's where stock actually starts moving and supplier due actually starts
accruing, and it introduces the general-purpose inventory-transaction mechanism CLAUDE.md §5 already
promised ("every stock-affecting table change also writes an InventoryTransaction row").

**Business process.** A purchase records buying stock from a supplier: a header (supplier, date, payment
type) plus line items (product, quantity, unit price, discount). Posting a purchase (1) increases each
product's stock, with an `InventoryTransaction` row per line item recording exactly why the stock changed
and preserving history/traceability CLAUDE.md's business rules require; (2) computes what's owed
(Cash → nothing owed; Credit → the whole total owed; Partial → total minus whatever was paid up front) and,
if anything is owed, adds a `SupplierLedgerEntry` and updates the supplier's `CurrentDue` - the exact
mechanism Module 5 built the ledger table to receive.

**Scope decision - `InventoryTransaction` is built now, for real reasons, not speculatively.** Product's
`CurrentStock` (Module 4) was deliberately write-once-at-creation specifically because *no module before
this one* had a legitimate reason to change it afterward. Purchase is that module. Building the general
`inventory_transactions` table now (with the full `Purchase | PurchaseReturn | Sale | SaleReturn |
Adjustment | Damaged` transaction-type vocabulary, mirroring how Module 5 pre-defined `Payment` in
`SupplierLedgerEntryType` before anything wrote one) means Module 8 (Inventory) and Module 9 (POS Sales)
extend an existing, already-correct mechanism instead of redesigning it.

**Scope decision - shared `PaymentType` enum, not a Purchase-only one.** The BRD calls out identical
Cash/Credit/Partial payment types for both Purchase and POS Sales (Module 9). Defined once as
`DailyMart.Domain.Common.PaymentType`, not duplicated as a Purchase-specific enum Sales would either
duplicate or awkwardly reuse from the wrong namespace.

**Scope decision - "Purchase Update" is a real reverse-and-reapply, not a silent field overwrite.**
Editing a posted purchase (wrong quantity, wrong price) can't just rewrite numbers in place - stock and
supplier due have already moved because of the *original* values. Update instead: (1) reverses the old
purchase's effects - one negative `InventoryTransaction` per old line item and one negative
`SupplierLedgerEntry` if it had created a due - then (2) applies the new request's effects exactly like a
fresh purchase would. Both the reversal and the reapplication are real, visible rows; nothing is ever
silently overwritten. This is the same principle as soft delete (Module 0) and the supplier ledger
(Module 5): a financial/stock event, once posted, is corrected by recording *another* event, never edited
in place.

**Scope decision - "keep the cached total in sync with the ledger" logic is centralized, not
duplicated.** `ISupplierService` gets a new `AdjustDueAsync` method (adds a ledger entry + updates
`CurrentDue` together, exactly like `CreateAsync`'s opening-balance logic already does) so Purchase - and
later Payment/Sale - call into it rather than each reimplementing "always keep these two in lockstep."
Likewise, a new minimal `IInventoryService.RecordTransactionAsync` centralizes "adjust `Product.
CurrentStock` + write the matching `InventoryTransaction` row" for the same reason. **Neither of these
helper methods calls `SaveChangesAsync` itself** - they only stage changes via their repositories. A
purchase touches multiple products' stock and (possibly) a supplier's due in what must be one atomic
operation; `PurchaseService` calls both helpers as many times as needed and commits **once** at the end.
This is a new convention worth calling out explicitly: every service so far has committed its own work
per-call, but a helper meant to be *composed into* a larger transaction must not do that, or multi-step
operations couldn't be atomic.

**Purchase Return**, tracked as its own header+items pair (`purchase_returns`/`purchase_return_items`),
referencing the original purchase's line items so returns can't exceed what was actually bought (minus
whatever's already been returned - computed from existing rows, not a separate cached counter). A return
decreases stock (a negative `InventoryTransaction`) and decreases what's owed (a new
`SupplierLedgerEntryType.PurchaseReturn` - distinct from generic `Adjustment`, so the ledger reads clearly).

**Purchase/return numbers are computed, not stored.** `PUR-000001`/`PRET-000001` are derived from the
row's own `Id` at DTO-mapping time - simpler and race-condition-free versus a separately-tracked sequence
column, and avoids repeating Module 5's "need the generated Id before a related insert" two-`SaveChanges`
dance for something that doesn't actually need to be a stored column.

### Step 1: Design Database — ✅ Done

**`inventory_transactions`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `product_id` | `bigint` FK → `products.id`, `Restrict` | |
| `transaction_type` | `varchar(20)` | `Purchase\|PurchaseReturn\|Sale\|SaleReturn\|Adjustment\|Damaged` - only the first two are ever produced by this module |
| `quantity_change` | `numeric(18,3)` | signed - positive = stock in, negative = stock out |
| `balance_after` | `numeric(18,3)` | running stock snapshot, same pattern as the supplier ledger's `balance_after` |
| `reference_type` | `varchar(50)` | e.g. `"Purchase"`, `"PurchaseReturn"` |
| `reference_id` | `bigint` | id of the document that caused this movement |
| `notes` | `varchar(500)` | nullable |
| `transaction_date` | `timestamptz` | |
| + shared audit columns | | |

Indexes: `product_id` (per-product stock history); `(reference_type, reference_id)` ("show everything
this purchase caused").

**`purchases`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `supplier_id` | `bigint` FK → `suppliers.id`, `Restrict` | |
| `purchase_date` | `timestamptz` | |
| `payment_type` | `varchar(20)` | `Cash\|Credit\|Partial` (shared `PaymentType` enum) |
| `subtotal_amount` | `numeric(12,2)` | sum of line items before discount/VAT |
| `discount_amount` | `numeric(12,2)` | default `0` |
| `vat_amount` | `numeric(12,2)` | default `0` |
| `total_amount` | `numeric(12,2)` | `subtotal - discount + vat` |
| `paid_amount` | `numeric(12,2)` | default `0` |
| `due_amount` | `numeric(12,2)` | default `0` - `total - paid` |
| `notes` | `varchar(500)` | nullable |
| + shared audit columns | | |

**`purchase_items`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `purchase_id` | `bigint` FK → `purchases.id`, **`Cascade`** | a line item has no life apart from its purchase - the one deliberate exception to this project's usual `Restrict` |
| `product_id` | `bigint` FK → `products.id`, `Restrict` | |
| `quantity` | `numeric(18,3)` | |
| `unit_price` | `numeric(12,2)` | the price paid *this* time - may differ from `Product.PurchasePrice` |
| `discount_amount` | `numeric(12,2)` | default `0` |
| `line_total` | `numeric(12,2)` | `quantity * unit_price - discount_amount` |
| + shared audit columns | | |

Index: `purchase_id`.

**`purchase_returns`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `purchase_id` | `bigint` FK → `purchases.id`, `Restrict` | the original purchase - an independent prior document, not owned by this return |
| `return_date` | `timestamptz` | |
| `total_amount` | `numeric(12,2)` | |
| `notes` | `varchar(500)` | nullable |
| + shared audit columns | | |

**`purchase_return_items`**

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `purchase_return_id` | `bigint` FK → `purchase_returns.id`, **`Cascade`** | same reasoning as `purchase_items` |
| `purchase_item_id` | `bigint` FK → `purchase_items.id`, `Restrict` | which original line this return is against |
| `quantity` | `numeric(18,3)` | must not exceed that line's `quantity` minus whatever's already been returned against it |
| `unit_price` | `numeric(12,2)` | copied from the original line at return time |
| `line_total` | `numeric(12,2)` | |
| + shared audit columns | | |

Index: `purchase_return_id`.

**New shared/reserved enum values, defined now:**
- `DailyMart.Domain.Common.PaymentType`: `Cash | Credit | Partial` (shared with Module 9).
- `DailyMart.Domain.Suppliers.SupplierLedgerEntryType` gets one addition: `PurchaseReturn` (alongside the
  existing `OpeningBalance | Purchase | Payment | Adjustment`) - no migration needed, it's stored as
  `varchar` and the column already accepts any value up to its length.

### Step 2: Create Entity — ✅ Done

Files created:
- `Domain/Common/PaymentType.cs` (shared enum).
- `Domain/Inventory/InventoryTransactionType.cs`, `InventoryTransaction.cs`.
- `Domain/Purchases/Purchase.cs`, `PurchaseItem.cs`, `PurchaseReturn.cs`, `PurchaseReturnItem.cs`.
- `Domain/Suppliers/SupplierLedgerEntryType.cs` updated with the new `PurchaseReturn` value (each existing
  value's doc comment also updated to say which module writes it, now that Purchase is real).

All match the Step 1 schema exactly. `dotnet build DailyMart.slnx` succeeds; full test suite still
**137/137 passing** (confirms the `SupplierLedgerEntryType` change didn't break Module 5's existing tests).

### Step 3: Configure EF Core — ✅ Done

Files created:
- `Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs` — `transaction_type` stored
  as `varchar(20)` string conversion; indexes on `product_id` and `(reference_type, reference_id)`; FK to
  `Product` as `Restrict`.
- `Infrastructure/Persistence/Configurations/PurchaseConfiguration.cs` — `payment_type` as `varchar(20)`
  string conversion; FK to `Supplier` as `Restrict`.
- `Infrastructure/Persistence/Configurations/PurchaseItemConfiguration.cs` — index on `purchase_id`; FK to
  `Purchase` as **`Cascade`** (the deliberate exception), FK to `Product` as `Restrict`.
- `Infrastructure/Persistence/Configurations/PurchaseReturnConfiguration.cs` — FK to `Purchase` as `Restrict`.
- `Infrastructure/Persistence/Configurations/PurchaseReturnItemConfiguration.cs` — index on
  `purchase_return_id`; FK to `PurchaseReturn` as **`Cascade`**, FK to `PurchaseItem` as `Restrict`.
- `Infrastructure/Persistence/DailyMartDbContext.cs` — updated with the 5 new `DbSet`s and their `using`s.

Migration: `dotnet ef migrations add AddPurchasesAndInventoryTransactions` generated cleanly. Reviewed the
generated migration file in full — it matches the Step 1 design exactly: all 5 tables with the correct
columns/types/defaults, `Cascade` only on `purchase_items → purchases` and `purchase_return_items →
purchase_returns`, `Restrict` on every other FK, and both `inventory_transactions` indexes plus the
`purchase_id`/`purchase_return_id` indexes on the two item tables.

`dotnet build DailyMart.slnx` succeeds; full test suite still **137/137 passing**.

### Step 4: Repository — ✅ Done (no new code)

No specialized repositories needed. Purchase/PurchaseItem/PurchaseReturn/PurchaseReturnItem/
InventoryTransaction have no unique-lookup requirement (unlike Product's `GetByBarcode`/`GetByCode`) —
every query this module needs (get-with-items, sum of returned quantity per line, per-product transaction
history) is plain LINQ reachable through the existing generic `IUnitOfWork.Repository<T>()` and its
`Query()` escape hatch, exactly how Module 5/6 read/wrote `SupplierLedgerEntry` without a dedicated
repository. Recorded explicitly, same as Modules 3/5/6's Step 4.

### Step 5: Service — ✅ Done

Files created/updated:
- `Application/Suppliers/ISupplierService.cs`/`SupplierService.cs` — added `AdjustDueAsync(supplierId,
  amount, entryType, description)`: updates `CurrentDue` and adds a `SupplierLedgerEntry` together, mirroring
  `CreateAsync`'s opening-balance logic. **Stage-only** — does not call `SaveChangesAsync`, so callers
  compose it into a larger atomic commit (Module 7 Step 1's scope decision).
- `Application/Inventory/IInventoryService.cs`/`InventoryService.cs` (new) —
  `RecordTransactionAsync(productId, transactionType, quantityChange, referenceType, referenceId, notes)`:
  adjusts `Product.CurrentStock` and writes the matching `InventoryTransaction` row together, throws
  `BusinessRuleException` if the resulting stock would go negative (the "stock can never go negative"
  business rule from CLAUDE.md §8). Also stage-only.
- `Application/Purchases/IPurchaseService.cs`/`PurchaseService.cs` (new) — `GetPagedAsync`/`GetByIdAsync`/
  `GetItemsAsync` (items fetched separately, no nav property, same shape as `GetLedgerAsync`) plus
  `CreateAsync`/`UpdateAsync`/`DeleteAsync`, operating on the domain entity directly for now (DTOs land in
  Step 6, same as every prior module). `ComputeAmounts` sets each item's `LineTotal` and the header's
  `Subtotal`/`Total`/`Paid`/`Due` — **`PaidAmount` is derived from `PaymentType`, not trusted verbatim**:
  Cash always pays the full total, Credit always pays nothing, and only Partial takes the caller's
  `PaidAmount` (validated strictly between 0 and the total) — this is the Cash/Credit/Partial-vs-PaidAmount
  consistency rule from the Step 5 design, enforced in the service (matching Product's convention for
  business rules, not the validator). `CreateAsync` posts one `InventoryTransaction` per item plus one
  `AdjustDueAsync` call if anything is owed, committed in one atomic `SaveChangesAsync` (after the one extra
  early save needed to get `Purchase.Id`, same two-phase pattern Supplier's opening balance already uses).
  `UpdateAsync`/`DeleteAsync` share a private `ReverseEffectsAsync` helper that undoes the original
  stock/due effects with new, visible correction rows — tagged `InventoryTransactionType.Adjustment` and
  `SupplierLedgerEntryType.Adjustment` (not `PurchaseReturn`, since this is a correction, not a real
  return) — before `UpdateAsync` reapplies the new request exactly like `CreateAsync` would, and before
  `DeleteAsync` soft-deletes the purchase and its items.
- `Application/Purchases/IPurchaseReturnService.cs`/`PurchaseReturnService.cs` (new) — **Create + read
  only**, no Update/Delete (a return is itself a correction; re-editing one isn't a BRD requirement).
  `CreateAsync` validates each requested item belongs to the given purchase, computes `UnitPrice`/
  `LineTotal` from the *original* purchase line rather than trusting the caller, and caps `Quantity` at
  that line's quantity minus whatever's already been returned (`SumAsync` over existing
  `PurchaseReturnItem` rows — live-computed, not a cached counter). Posts one negative
  `InventoryTransaction` per item and one `SupplierLedgerEntryType.PurchaseReturn` ledger entry (decreasing
  what's owed — even below zero for a return against a Cash purchase, since unlike Customer Due, nothing in
  the BRD floors Supplier `CurrentDue` at zero), all in one atomic commit.
- `Application/Purchases/PurchaseNumberFormatter.cs` (new, `internal`) — `PUR-{id:D6}`/`PRET-{id:D6}`,
  shared by both new services so the "computed, not stored" purchase/return number (Step 1) is formatted
  in exactly one place.
- `Application/DependencyInjection.cs` — registered `IInventoryService`, `IPurchaseService`,
  `IPurchaseReturnService`.

`dotnet build DailyMart.slnx` succeeds; full test suite still **137/137 passing** (Step 10 is where this
module's own tests are added, same as every prior module).

### Step 6: DTO — ✅ Done

Files created:
- `PurchaseDto`/`PurchaseItemDto` (response) — `PurchaseNumber` computed via `PurchaseNumberFormatter`,
  `SupplierName`/`ProductName`/`ProductCode` denormalized via a new `PurchaseLookups` record (same
  batched-dictionary pattern as `Products/ProductLookups.cs`). `Items` is embedded directly in
  `PurchaseDto` rather than a separate paginated endpoint (unlike the supplier ledger) - a purchase's line
  items are small, bounded, and integral to reading the document, not an open-ended timeline.
- `PurchaseRequestDto`/`PurchaseItemRequestDto` — **one shape for both create and update**, same reasoning
  as Customer (Module 6): no field only one operation can set. `PaidAmount` is documented as
  "only meaningful when PaymentType is Partial" since the service derives it otherwise (Step 5).
- `PurchaseReturnDto`/`PurchaseReturnItemDto` (response) — `ReturnNumber` + the parent's `PurchaseNumber`
  both computed, `ProductName`/`ProductId` denormalized via a new `PurchaseReturnLookups` record that maps
  `PurchaseItemId → (ProductId, ProductName)` (`PurchaseReturnItem` itself has no `ProductId` column).
- `PurchaseReturnRequestDto`/`PurchaseReturnItemRequestDto` — deliberately omit `UnitPrice`/`LineTotal`;
  the service always computes those from the original purchase line, never trusts the caller for them.
  `PurchaseId` isn't in the body - it comes from the route (`/api/purchases/{purchaseId}/returns`).
- `PurchaseMappingExtensions`/`PurchaseReturnMappingExtensions` (internal, static).

`IPurchaseService`/`PurchaseService` and `IPurchaseReturnService`/`PurchaseReturnService` updated
throughout to accept/return these DTOs instead of raw entities - all the Step 5 business logic
(reverse-and-reapply, PaymentType→PaidAmount derivation, return-quantity capping) is unchanged, just now
wrapped by DTO mapping at the boundary.

`dotnet build DailyMart.slnx` succeeds; full test suite still **137/137 passing**.

### Step 7: Validator — ✅ Done

Files created: `PurchaseRequestValidator` (+ `PurchaseItemRequestValidator` via `RuleForEach(...)
.SetValidator(...)`) and `PurchaseReturnRequestValidator` (+ `PurchaseReturnItemRequestValidator`) - the
first nested/list validators in this project, since every prior module's DTOs were flat.

Rules are pure shape/range checks (`GreaterThan(0)`, `IsInEnum()`, `MaximumLength`, `NotEmpty` on the
`Items` collection itself) - existence checks and the Cash/Credit/Partial-vs-PaidAmount consistency rule
stay in the service (Step 5), matching Product's convention. `PurchaseReturnItemRequestValidator`
deliberately doesn't validate `UnitPrice`/`LineTotal` - that DTO has no such fields; the service computes
both from the original purchase line.

**Cleanup**: removed the `items.Count == 0` `BusinessRuleException` guards from `PurchaseService.CreateAsync`/
`UpdateAsync` and `PurchaseReturnService.CreateAsync` added during Step 5 - now that `RuleFor(x => x.Items)
.NotEmpty()` covers this at the API boundary, keeping a duplicate check in the service would just be
validation for a scenario the validator already rules out before the service ever sees it (no other
service in this project duplicates a validator's shape checks either).

`dotnet build DailyMart.slnx` succeeds; full test suite still **137/137 passing**.

### Step 8: Controller — ✅ Done

`Controllers/PurchasesController.cs` under `api/purchases`:
- `GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` for Purchase itself.
- `GET {purchaseId}/returns` (paged list), `POST {purchaseId}/returns` (create - the only write endpoint,
  matching `IPurchaseReturnService`'s create-only contract), `GET {purchaseId}/returns/{returnId}` - nested
  under the parent purchase, same shape as Supplier's `{id}/ledger`.

`BusinessRuleException`→400 and `NotFoundException`→404 were already mapped by `GlobalExceptionHandler`
since Module 3/4 - no changes needed there. None `[AllowAnonymous]`.

**Startup re-verified:** fails at exactly the same point as every module since Module 1 (`AdminSeeder`'s DB
call, no live Postgres here). Test suite still **137/137 passing**.

### Step 9: Angular — ✅ Done

Files created under `features/purchases/`:
- `purchase.model.ts`/`purchase.service.ts`/`purchase.service.spec.ts` - `PAYMENT_TYPES`/`PAYMENT_TYPE_VALUES`
  bridge the mismatch between the request DTO's `paymentType` (a **number** - the API has no
  `JsonStringEnumConverter` registered, confirmed by grepping `Program.cs`) and the response DTO's
  `paymentType` (already a **string**, since `PurchaseDto` converts the enum server-side).
- `purchase-list/` - list page (`mat-table` + paginator, same shape as `supplier-list`), but **not** an
  inline add/edit form like Supplier/Customer - Purchase's line-item complexity needs a full-page form,
  so "Add"/"Edit" navigate to `purchases/new`/`purchases/:id/edit` instead (same reasoning as Product).
- `purchase-form/` - the first `FormArray` in this codebase (grepped - zero prior usage). Header fields
  (Supplier/Product `mat-select` populated via the same `pageSize: 100` pragmatic-MVP fetch as
  `ProductFormComponent`'s dropdowns) plus a dynamic item-row array (`addItem`/`removeItem`,
  `[disabled]` on remove when only one row remains). `subtotal`/`total`/`due` are `computed()` signals
  derived from `toSignal(form.valueChanges)` rather than read imperatively, keeping the whole component
  signal-driven like the rest of the app. `onProductChange` pre-fills a row's unit price from the
  selected product's purchase price only if the price field is still at its default 0 (a convenience,
  not a hard business rule). Payment-type-driven UI: the Paid Amount field only appears when
  Payment Type is Partial, matching the backend's PaidAmount-is-derived-except-for-Partial rule (Step 5).
  Date fields use a plain `<input type="date">` with manual `yyyy-MM-dd` ⇄ ISO string conversion (no
  existing date-input helper to reuse - this is the app's first date-input field).
- `returns/purchase-return.model.ts`/`purchase-return.service.ts`/`.spec.ts` - **no update/delete
  methods**, matching `IPurchaseReturnService`'s create+read-only contract.
- `returns/purchase-return-list/` - nested read-only list under a purchase (`purchases/:id/returns`),
  same shape as `supplier-ledger`, plus an "Add Return" button.
- `returns/purchase-return-form/` - one row per original purchase item (quantity defaults to 0 = "not
  returning this line"); **the backend exposes no "already returned" quantity per line**, so
  over-returning is only caught (and surfaced via snack bar) when the request is submitted, not
  pre-validated client-side - a documented, deliberate limitation rather than an invented backend field.

`app.routes.ts` updated with `purchases` (`''`/`new`/`:id/edit`/`:id/returns`/`:id/returns/new`) - `app.html`
updated with a `Purchases` nav link next to Suppliers/Customers (Returns is reached from within a purchase,
not top-level nav, same as Supplier's ledger).

`ng build` succeeds. `ng test` (Vitest): **30/30 passing** (was 23 - the two new `.spec.ts` files for
`PurchaseService`/`PurchaseReturnService` add 7 tests, mirroring `supplier.service.spec.ts`'s pattern
exactly).

**Not verified in a live browser** - there is no browser automation tool available in this sandbox and no
live Postgres/API to exercise end-to-end (same limitation noted for every prior module's Angular step).
Verification here is limited to a successful production build and passing unit tests; the actual UI has
not been visually confirmed to render/function correctly in a real browser.

### Step 10: Testing — ✅ Done

Backend test files created/updated (all pure-Moq, mocking `IUnitOfWork.Repository<T>()` plus
`IInventoryService`/`ISupplierService` directly at the service boundary - no real `DbContext` needed,
same convention as every prior module's service tests):

- `Inventory/InventoryServiceTests.cs` (new, 5 tests) - `NotFoundException` when the product is missing;
  stock increases/decreases correctly for positive/negative `quantityChange`; `BusinessRuleException` (and
  no `Update`/`AddAsync` calls at all) when the result would go negative; confirms `SaveChangesAsync` is
  never called (stage-only).
- `Suppliers/SupplierServiceTests.cs` (+4 tests for the new `AdjustDueAsync`) - `NotFoundException` when
  missing; `CurrentDue` and the new ledger entry's `BalanceAfter` update together correctly; a negative
  amount can take `CurrentDue` below zero (no floor, unlike Customer Due); confirms `SaveChangesAsync` is
  never called.
- `Purchases/PurchaseServiceTests.cs` (new, 16 tests) - line-total/subtotal/total math; all three
  Cash/Credit/Partial `PaidAmount`-derivation branches, including both Partial failure cases (zero, and
  ≥ total); supplier/product existence checks; one `RecordTransactionAsync` call per item with the
  `Purchase` type and a positive quantity; `AdjustDueAsync` called only when a due amount is actually
  created (`Times.Once`/`Times.Never` sentinel pattern, same technique used since Module 5); `GetByIdAsync`
  lookup-mapping; **`UpdateAsync`'s full reverse-and-reapply** - one test asserts the old item is reversed
  via `Adjustment`/negative-quantity, the old due is reversed via a negative `AdjustDueAsync` call, the new
  item is reapplied via `Purchase`/positive-quantity, and (since the new request is Cash) no second
  `AdjustDueAsync` call happens - proving the reversal and reapplication are both real and correctly
  distinct; `DeleteAsync`'s parallel reverse-then-remove behavior.
- `Purchases/PurchaseReturnServiceTests.cs` (new, 7 tests) - `NotFoundException` when the purchase is
  missing; `BusinessRuleException` when a requested purchase item belongs to a different purchase;
  `UnitPrice`/`LineTotal` are computed from the *original* purchase line, not any caller input (there is
  none - the request DTO has no such fields); the return-quantity cap correctly accounts for
  already-returned quantity (`SumAsync` over existing return-item rows, live-computed); rejects a
  zero quantity; posts a negative `InventoryTransaction` and a negative `AdjustDueAsync` call together;
  `GetPagedAsync`'s existence check.

**Bug caught during this step**: the first run of `PurchaseReturnServiceTests` failed 2 tests with
`ArgumentNullException` inside `PurchaseReturnService.BuildLookupsAsync` - the test setup mocked
`IRepository<PurchaseItem>.GetByIdAsync` but never `FindAsync`, so Moq returned `null` for the unconfigured
call and `.Select()` on it threw. This was a **test setup gap, not a production bug** - fixed by adding the
missing `FindAsync` mock setup.

Frontend: no new Vitest specs added this step - `purchase.service.spec.ts` and
`purchase-return.service.spec.ts` were already written and counted during Step 9, matching how every
prior module's HTTP-service specs land in the same step as the service class itself.

**Backend: 169/169 passing** (was 137, +32). **Frontend: 30/30 passing** (unchanged from Step 9).

---

**Module 7 (Purchase) is complete.** All 10 steps built, tested, and approved.

## Module 8 — Inventory

**Business process.** Stock levels are already tracked - `Product.CurrentStock` (a cached running total)
and `InventoryTransaction` (the full, append-only history) were both built in Module 7, and Purchase
already writes to both automatically. Module 8 adds the ways a *staff member* changes stock directly,
without a purchase or sale document causing it:
- **Stock Adjustment** - a physical stock count doesn't match what the system shows. Staff enters the
  *actual counted quantity*; the system computes the delta itself (`counted - CurrentStock`) rather than
  asking staff to do that subtraction by hand - less error-prone, and matches how a real stock take works.
  The delta can be positive (found more) or negative (found less).
- **Damaged Stock** - goods are written off (broken, expired, spoiled). Staff enters *how many units were
  damaged* (always a positive count); this is always a stock decrease, never an increase.

Both require a reason to be recorded (a manual, non-document-backed stock movement needs its own audit
trail, same principle behind every other financial/stock event in this project) and both post through the
existing `IInventoryService.RecordTransactionAsync` from Module 7, so `Product.CurrentStock` and
`InventoryTransaction` stay the single source of truth - Module 8 doesn't introduce a second stock-tracking
mechanism, only a new *reason* for the existing one to fire.

Module 8 also surfaces two read-only views that need no new tables, since they're just queries over data
Module 7 already produces:
- **Transaction history** - the full `InventoryTransaction` log, per product or across all products.
- **Low stock alerts** - products where `CurrentStock <= MinimumStock` (both already exist on `Product`
  since Module 4).

### Step 1: Design Database — 🔄 In Progress (awaiting approval)

**`inventory_adjustments`** (new table - the only schema change this module needs)

| Column | Type | Notes |
|---|---|---|
| `id` | `bigserial` PK | |
| `product_id` | `bigint` FK → `products.id`, `Restrict` | |
| `adjustment_type` | `varchar(20)` | `Adjustment` \| `Damaged` - reuses `InventoryTransactionType`'s existing vocabulary, no new enum |
| `quantity_change` | `numeric(18,3)` | signed - the final computed delta (either sign for `Adjustment`; always negative for `Damaged`) |
| `reason` | `varchar(500)` | required |
| `adjustment_date` | `timestamptz` | |
| + shared audit columns | | |

Index: `product_id` (per-product adjustment history, same shape as `purchase_items`/`inventory_transactions`).

**Why a new table at all**, given `InventoryTransaction.ReferenceType`/`ReferenceId` already exist: every
other transaction type has a real backing document to reference (`Purchase`, `PurchaseReturn`, and later
`Sale`/`SaleReturn`) - a manual adjustment/damaged entry needs one too (`ReferenceType = "InventoryAdjustment"`,
`ReferenceId` = this row's `Id`), rather than inventing a fake reference or leaving the reason only on the
`InventoryTransaction.Notes` free-text field. Same reasoning as Module 7's `Purchase`/`PurchaseReturn`
tables existing alongside the shared `InventoryTransaction` log.

**Create + read only** - no update/delete, the same "once posted, a stock movement is corrected by a new
row, never edited in place" principle as `Purchase`/`PurchaseReturn`/`SupplierLedgerEntry`.

**No schema change for transaction history or low stock alerts** - both are plain queries over
`InventoryTransaction`/`Product`, exposed as new `IInventoryService` methods and controller endpoints in
later steps, not new tables.

### Step 2: Create Entity — ✅ Done

File created: `Domain/Inventory/InventoryAdjustment.cs` - matches the Step 1 schema exactly.
`AdjustmentType` reuses `InventoryTransactionType` directly rather than a second, narrower enum (the
service layer is responsible for rejecting any value other than `Adjustment`/`Damaged` at creation time).

`dotnet build DailyMart.slnx` succeeds; full test suite still **169/169 passing**.

### Step 3: Configure EF Core — ✅ Done

Files created/updated:
- `Infrastructure/Persistence/Configurations/InventoryAdjustmentConfiguration.cs` - `adjustment_type`
  stored as `varchar(20)` string conversion (same as `InventoryTransaction.TransactionType`); index on
  `product_id`; FK to `Product` as `Restrict`.
- `Infrastructure/Persistence/DailyMartDbContext.cs` - new `DbSet<InventoryAdjustment>`.

Migration: `dotnet ef migrations add AddInventoryAdjustments` generated cleanly. Reviewed the generated
migration file in full - matches the Step 1 design exactly: one table, the `product_id` index, and
`Restrict` on the FK to `products`.

`dotnet build DailyMart.slnx` succeeds; full test suite still **169/169 passing**.

### Step 5: Service — ✅ Done

`IInventoryService`/`InventoryService` extended with four new members (the existing `RecordTransactionAsync`
from Module 7 is unchanged):
- `RecordAdjustmentAsync(productId, newStockCount, reason)` - fetches the product, computes
  `quantityChange = newStockCount - CurrentStock` itself (per Step 1's design decision), then delegates to
  a shared private `CreateAdjustmentAsync` helper.
- `RecordDamagedAsync(productId, quantity, reason)` - rejects `quantity <= 0`, then calls the same helper
  with `-quantity` (Damaged can only ever decrease stock).
- Shared private `CreateAdjustmentAsync(productId, adjustmentType, quantityChange, reason)` - creates the
  `InventoryAdjustment` row, saves it (the same two-phase "get the Id before a dependent insert needs it"
  pattern as Purchase/Supplier), then calls `RecordTransactionAsync` (referencing
  `nameof(InventoryAdjustment)`/the new row's `Id`) and commits again. **Unlike `RecordTransactionAsync`
  itself**, these are top-level create operations, not helpers meant to be composed into someone else's
  transaction, so they own their own commits - the inverse of the "stage-only" convention `RecordTransactionAsync`
  established in Module 7.
- `GetTransactionHistoryAsync(request, productId?)` - plain paged read over `InventoryTransaction`,
  optionally filtered to one product. Defaults to **newest-first** (`TransactionDate` descending) when no
  sort is requested - the opposite default from the supplier ledger's oldest-first bank-statement style,
  since this reads more like a recent-activity feed than a statement.
- `GetLowStockAsync(request)` - paged read over `Product` filtered to `CurrentStock <= MinimumStock`
  (a same-row column-to-column comparison, translated straight to SQL by EF Core - no new column needed).

`dotnet build DailyMart.slnx` succeeds; full test suite still **169/169 passing** (Step 10 is where this
module's own tests are added, same as every prior module).

### Step 6: DTO — ✅ Done

**Design correction found while drafting DTOs, raised and approved before implementing**: `GetLowStockAsync`
was approved on `IInventoryService` in Step 1, but it only ever queries `Product` - no
`InventoryTransaction`/`InventoryAdjustment` data at all. Keeping it on `IInventoryService` would have meant
either duplicating `ProductService`'s Category/Brand/Unit lookup-and-mapping logic inside Inventory, or
returning bare `Product` entities for something else to map (breaking "controllers stay thin"). **Moved
to `IProductService.GetLowStockAsync`** instead, reusing `ProductService`'s existing private
`BuildLookupsAsync`/`ToDto` machinery with zero duplication - the low-stock query itself
(`CurrentStock <= MinimumStock`) is a single-line addition there.

Files created:
- `InventoryTransactionDto`/`InventoryAdjustmentDto` (response) - `ProductName`/`ProductCode` denormalized
  via a new `InventoryLookups` record (same batched-dictionary pattern as `ProductLookups`/`PurchaseLookups`).
- `StockAdjustmentRequestDto` (`ProductId`, `NewStockCount`, `Reason`) / `DamagedStockRequestDto`
  (`ProductId`, `Quantity`, `Reason`) - two separate request shapes, not one shared shape, since
  Adjustment and Damaged have genuinely different semantics (recount vs. always-negative write-off), per
  Step 1's design.
- `InventoryMappingExtensions` (internal, static).

`IInventoryService`/`InventoryService` updated to accept/return these DTOs; `RecordAdjustmentAsync`/
`RecordDamagedAsync` now build a single-product `InventoryLookups` for their response, and
`GetTransactionHistoryAsync` builds one batched across the whole page. All Step 5 business logic
(recount-delta computation, Damaged's positive-quantity requirement) is unchanged.

`dotnet build DailyMart.slnx` succeeds; full test suite still **169/169 passing**.

### Step 7: Validator — ✅ Done

Files created: `StockAdjustmentRequestValidator` (`ProductId > 0`, `NewStockCount >= 0`, `Reason` required
+ length cap) and `DamagedStockRequestValidator` (`ProductId > 0`, `Quantity > 0`, `Reason` required +
length cap).

**Cleanup**: removed the `request.Quantity <= 0` `BusinessRuleException` guard from
`InventoryService.RecordDamagedAsync` added during Step 5 - now that `DamagedStockRequestValidator`
covers this at the API boundary, the same "don't duplicate a validator's shape check in the service"
cleanup applied to Purchase in Module 7 Step 7.

`dotnet build DailyMart.slnx` succeeds; full test suite still **169/169 passing**.

### Step 8: Controller — ✅ Done

`Controllers/InventoryController.cs` (new) under `api/inventory`: `POST adjustments`, `POST damaged`,
`GET transactions` (paged, optional `?productId=` filter). Both `POST` endpoints return `200 Ok` rather
than `201 CreatedAtAction` - there is no per-adjustment `GetById` endpoint to point at (by design - an
adjustment is only ever surfaced through the transaction history list, per Step 6).

`Controllers/ProductsController.cs` updated with `GET low-stock` (paged) - lives here rather than on
`InventoryController`, consistent with Step 6's ownership correction.

`BusinessRuleException`→400 and `NotFoundException`→404 were already mapped by `GlobalExceptionHandler`
since Module 3/4 - no changes needed.

**Startup re-verified:** fails at exactly the same point as every module since Module 1 (`AdminSeeder`'s DB
call, no live Postgres here). Test suite still **169/169 passing**.

### Step 9: Angular — ✅ Done

Files created under `features/inventory/`:
- `inventory.model.ts`/`inventory.service.ts`/`.spec.ts` - `recordAdjustment`/`recordDamaged`/
  `getTransactionHistory` (optional `productId` filter), mirroring `PurchaseService`'s style.
- `inventory-list/` - one page combining two toggleable inline forms (Record Adjustment / Record Damaged -
  Supplier-list's `formVisible`-toggle pattern extended to two mutually exclusive forms via an
  `activeForm: 'adjustment' | 'damaged' | null` signal) plus the transaction history table underneath,
  with its own "filter by product" `mat-select` (`null` = All Products) and pagination. A "Low Stock"
  header button navigates to the low-stock page - reached from within Inventory, not top-level nav, same
  as how Purchase Returns is reached from within a purchase.
- `low-stock/low-stock-list.component.*` - reuses `ProductService.getLowStock()` and plain `ProductDto`
  directly rather than an inventory-specific model, matching the backend's Step 6 ownership decision (low
  stock is a Product query, not an Inventory-log query).

`ProductService` (existing) gained `getLowStock()` + a matching spec test, and `app.routes.ts`/`app.html`
updated with `inventory` (`''`/`low-stock`) routes and an `Inventory` nav link next to Purchases.

`ng build` succeeds. `ng test` (Vitest): **34/34 passing** (was 30 - `getLowStock()`'s spec test plus the
three new `InventoryService` spec tests add 4).

**Not verified in a live browser** - same limitation as every prior module's Angular step (no browser
automation tool or live Postgres/API in this sandbox). Verification is limited to a successful build and
passing unit tests.

### Step 10: Testing — ✅ Done

**Real bug found and fixed before writing tests** (not caught by a failing test - caught by re-reading
`CreateAdjustmentAsync` while designing the "Damaged exceeds current stock" test case): the method saved
the new `InventoryAdjustment` row (`AddAsync` + `SaveChangesAsync`) *before* calling
`RecordTransactionAsync`, which is what actually validates that stock can't go negative. If a Damaged
quantity exceeded current stock, the `InventoryAdjustment` document would already be permanently committed
by the time `RecordTransactionAsync` threw - an orphaned audit row with no matching transaction and no
stock change, and the whole point of `InventoryAdjustment` (Step 1) was to be a *reliable* backing document
for the paired transaction. Fixed by moving the negative-stock check into `CreateAdjustmentAsync` itself,
running *before* the adjustment is ever saved - this is not a duplicate of `RecordTransactionAsync`'s own
check (that one still runs and still protects every other caller); it exists specifically to keep the two
writes from ever landing in an inconsistent partial state. A regression test
(`RecordDamagedAsync_throws_BusinessRuleException_and_saves_nothing_when_quantity_exceeds_current_stock`)
asserts `AddAsync`/`SaveChangesAsync` are never called in this case.

Backend test files updated:
- `Inventory/InventoryServiceTests.cs` (+6 tests) - `RecordAdjustmentAsync` computes the delta from the
  counted quantity and maps the resulting DTO's product name; `NotFoundException` when the product is
  missing; `RecordDamagedAsync` applies a negative change; the regression test above; `GetTransactionHistoryAsync`
  defaults to `TransactionDate` descending and correctly maps product names/codes via lookups.
- `Products/ProductServiceTests.cs` (+1 test) - `GetLowStockAsync`'s predicate is captured and
  *compiled and invoked* against sentinel `Product` instances (the same technique used since Module 3) to
  prove it actually distinguishes `CurrentStock <= MinimumStock` from `CurrentStock > MinimumStock`,
  not just "some predicate was passed."

Frontend: no new Vitest specs added this step - `inventory.service.spec.ts` and the `getLowStock()` spec
test were already written and counted during Step 9, same as every prior module's HTTP-service specs.

**Backend: 176/176 passing** (was 169, +7). **Frontend: 34/34 passing** (unchanged from Step 9).

---

**Module 8 (Inventory) is complete.** All 10 steps built, tested, and approved.
