# Project Architecture

## Project structure

The solution lives under `purchase_order_management_api/` and contains a single project:

```
PurchaseOrderManagement.sln
PurchaseOrderManagement.Api/
  Controllers/          HTTP controllers — thin, no business logic.
  Services/             Service interfaces (I*.cs) and their implementations.
  Entities/             EF Core entity classes (domain model).
  Dtos/                 Request and response DTOs, grouped by domain.
  Data/                 AppDbContext and EF entity configurations.
  Data/Configurations/  One IEntityTypeConfiguration<T> per entity.
  Migrations/           EF-generated migration files. Do not edit manually.
  Enums/                Shared enum types (PurchaseOrderStatus, ApprovalStatus, FileSourceType).
  Infrastructure/       Cross-cutting ASP.NET infrastructure (ServiceExceptionFilter).
  Seeding/              DataSeeder — idempotent startup seed logic.
  Program.cs            App composition root and middleware pipeline.
  appsettings.json      Default configuration.
  appsettings.Development.json  Development-environment overrides.
```

---

## Layering pattern

```
HTTP Request
    |
    v
Controller  (receives request, calls service, returns ActionResult)
    |
    v
Service     (business logic, database access via AppDbContext)
    |
    v
AppDbContext (EF Core / Npgsql / PostgreSQL)
```

Controllers never touch `AppDbContext` directly. All database access is done inside services. Controllers receive service interfaces via constructor injection and translate service results (or `ServiceException` errors, caught by the global filter) into HTTP responses.

---

## Service pattern

Every service has an interface and a concrete implementation registered in `Program.cs`:

| Interface | Implementation | Lifetime |
|---|---|---|
| `IAuthService` | `AuthService` | Scoped |
| `ICurrentUser` | `CurrentUser` | Scoped |
| `IAdminAuthorizer` | `AdminAuthorizer` | Scoped |
| `IRoleHierarchyService` | `RoleHierarchyService` | Scoped |
| `ICompanyService` | `CompanyService` | Scoped |
| `IUserService` | `UserService` | Scoped |
| `IRoleService` | `RoleService` | Scoped |
| `ISupplierService` | `SupplierService` | Scoped |
| `IFileService` | `FileService` | Scoped |
| `IFileStorage` | `LocalDiskFileStorage` | Singleton |
| `IFileUrlResolver` | `FileUrlResolver` | Singleton |
| `IPasswordHasher` | `PasswordHasher` | Singleton |
| `IQuotationService` | `QuotationService` | Scoped |
| `IBidService` | `BidService` | Scoped |
| `ICurrencyService` | `CurrencyService` | Scoped |
| `IPurchaseOrderService` | `PurchaseOrderService` | Scoped |
| `IApprovalService` | `ApprovalService` | Scoped |
| `IPurchaseOrderTypeService` | `PurchaseOrderTypeService` | Scoped |

`IFileStorage` and `IFileUrlResolver` are Singleton because they hold no per-request state (they are stateless helpers reading configuration). All services that touch the database are Scoped to the HTTP request lifetime.

---

## Entity base class

Every entity (except `Currency`, `PurchaseOrderCurrencyTotal`, and `PurchaseOrderSupplierBid` — see Data Model) inherits `BaseEntity`:

```csharp
public abstract class BaseEntity : IAuditableEntity, ISoftDeletableEntity
{
    public int Id { get; set; }               // int IDENTITY primary key

    // IAuditableEntity
    public DateTime CreatedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }  // nullable: system/seed ops have no user
    public DateTime? UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }

    // ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public int? DeletedByUserId { get; set; }
}
```

`AppDbContext.SaveChangesAsync` automatically stamps these columns on every `Add`/`Modify`/`Delete` operation through the `ApplyAuditAndSoftDeleteConventions` method.

---

## Soft delete

Nothing is ever hard-deleted. When EF Core sees a `Deleted` entry state for an `ISoftDeletableEntity`, `AppDbContext` intercepts it and converts it into a `Modified` with `IsDeleted = true`, `DeletedAtUtc = utcNow`, and `DeletedByUserId = <acting user>`.

A global EF query filter applied via `BaseEntityConfiguration.ConfigureBase<T>` excludes soft-deleted rows from all queries automatically:

```csharp
builder.HasQueryFilter(e => !e.IsDeleted);
```

To query deleted rows (e.g. for admin audit views), use `.IgnoreQueryFilters()` on the query.

---

## Concurrency tokens

Financially sensitive tables use PostgreSQL's `xmin` system column as an optimistic concurrency token. `xmin` is a `uint` that PostgreSQL increments on every row update; it costs nothing to maintain.

Configured via `BaseEntityConfiguration.ConfigureXminConcurrency<T>`:

```csharp
builder.Property<uint>("xmin")
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

Tables that have xmin concurrency: `PurchaseOrders`, `PurchaseOrderLineItems`, `Approvals`, `SupplierBids`, `SupplierBidItems`.

The `ConcurrencyToken` static utility encodes the `uint` xmin as a base64 string for round-tripping through DTOs:

```csharp
ConcurrencyToken.Encode(uint xmin) -> string   // to DTO
ConcurrencyToken.TryDecode(string, out uint)   // from request
```

DTOs that expose concurrency tokens have a `RowVersion` property (e.g. `PurchaseOrderDto.RowVersion`, `ApprovalDto.RowVersion`). Write requests that accept `RowVersion` (e.g. `UpdatePurchaseOrderRequest`, `ActOnApprovalRequest`) should echo it back. If the token mismatches, EF throws `DbUpdateConcurrencyException` and the service returns 409 Conflict.

---

## Authentication

Cookie-based server session. No JWT.

**Cookie settings** (configured in `Program.cs`):

| Setting | Value |
|---|---|
| Cookie name | `pom_session` |
| HttpOnly | true |
| Secure | Always |
| SameSite | Strict |
| Sliding expiration | true |
| Expiration window | 8 hours |

**Login flow**: `POST /api/auth/login` validates credentials via `IAuthService.ValidateCredentialsAsync`, then calls `HttpContext.SignInAsync` to issue the cookie. The cookie contains claims for `UserId`, `FullName`, `Email`, `CompanyId`, and all role names.

**Logout**: `POST /api/auth/logout` calls `HttpContext.SignOutAsync`, which invalidates the session server-side.

**Global protection**: All controllers carry `[Authorize]`. Only `POST /api/auth/login` is `[AllowAnonymous]`.

**401/403 behaviour**: The cookie middleware is configured to return HTTP status codes instead of login redirects (appropriate for a JSON API consumed by a SPA):

- Unauthenticated request → 401 Unauthorized (not a redirect).
- Authenticated but forbidden → 403 Forbidden (not a redirect).

---

## Authorization

There is no fine-grained permission system yet (this is a flagged open question). Authorization is handled at two coarse levels:

### Admin-tier gate (`IAdminAuthorizer`)

Used by company and user management. The acting user must hold the role name `"Super Admin"` or `"Admin"` (case-insensitive match against the role names in the session claims).

The gate is checked at the controller level by calling `_adminAuthorizer.IsAdmin()` and throwing `ServiceException.Forbidden(...)` when the check fails. It is not an `[Authorize(Roles=...)]` attribute so that the error response is a consistent `ProblemDetails` JSON body rather than a bare 403.

### Seniority-ceiling gate (role management)

Role creation is not admin-gated; it is governed by a role hierarchy seniority rule enforced in `RoleService` via `IRoleHierarchyService`. A user may only create/manage roles that are at or below their own seniority level in the tree.

### Approval eligibility (approval actors)

Approvals are not gated by a role claim on the endpoint. Any authenticated user may call the approve/reject endpoints; eligibility (role match or user match) and sequence gating are enforced per-approval row inside `ApprovalService`.

---

## Current user

`ICurrentUser` provides the acting user's identity extracted from the cookie claims:

```csharp
public interface ICurrentUser
{
    int? UserId { get; }
    int? CompanyId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

`CurrentUser` (the concrete implementation) reads from `IHttpContextAccessor`. `UserId` and `CompanyId` are nullable because the interface is also used during seed operations that run outside an HTTP context (those operations have no acting user and leave `CreatedByUserId` null).

Services receive `ICurrentUser` (or `AppDbContext` which in turn holds it) through constructor injection.

---

## Error handling

Services throw `ServiceException` for expected, client-facing failures. `ServiceException` carries an HTTP status code and a message:

| Factory method | HTTP status | Meaning |
|---|---|---|
| `ServiceException.NotFound(msg)` | 404 | Resource not found |
| `ServiceException.Validation(msg)` | 422 Unprocessable Entity | Business rule violation |
| `ServiceException.Conflict(msg)` | 409 Conflict | Concurrency or duplicate |
| `ServiceException.Forbidden(msg)` | 403 Forbidden | Authorization failure |
| `ServiceException.BadRequest(msg)` | 400 Bad Request | Malformed/invalid input |

`ServiceExceptionFilter` (registered globally in `Program.cs`) catches any `ServiceException` that escapes a controller action and converts it into a standard `ProblemDetails` JSON response with the correct HTTP status code:

```json
{
  "status": 422,
  "title": "Unprocessable Entity",
  "detail": "Currency XYZ was not found."
}
```

Unhandled exceptions (not `ServiceException`) are not caught by this filter and bubble up to the ASP.NET Core default exception handler.

---

## Money and currency

- **Amount columns**: `decimal(18,2)` (`numeric(18,2)` in PostgreSQL).
- **Percentage columns**: `decimal(5,2)` (validated at the DTO level with `[Range(0, 100)]`).
- **Currency code columns**: `char(3)` in PostgreSQL, stored as ISO 4217 codes (e.g. `ZMW`, `USD`).

**Currency validation**: The `CurrencyValidation` static class normalizes (trim + upper) and validates a supplied code against the `Currencies` table (active rows only). Called by every service that accepts a currency field. The default currency when none is supplied is `ZMW`.

**Multi-currency bid totals**: Supplier bid line items may span multiple currencies. Totals are never combined or converted. They are returned as a `CurrencyTotalDto[]` vector (one entry per currency). The same pattern applies to bid-based purchase orders whose awarded bid items span multiple currencies — see `PurchaseOrderCurrencyTotal` and `PurchaseOrderDto.HasMultiCurrencyTotals`.

**Server-computed totals**: Line-item money fields (`LineSubtotal`, `DiscountAmount`, `TaxAmount`, `LineTotal`) and PO aggregate fields (`Subtotal`, `TaxAmount`, `TotalAmount`) are always server-computed using `BidItemMath` / `PurchaseOrderTotalsRecompute`. Client-supplied money amounts are ignored.

---

## File storage

Uploaded files are stored on local disk by `LocalDiskFileStorage` at the path configured in `FileStorage:Path` (default: `App_Data/uploads`).

File records in the `Files` table (entity `StoredFile`) store either:

- `SourceType = Path`: a server-relative path. The full URL is resolved at runtime by `FileUrlResolver` as `{FileStorage:BaseUrl}/api/files/{id}`.
- `SourceType = Url`: an already-complete external URL. Returned as-is; no base URL is prepended.

The raw stored path is never returned in a DTO. DTOs include only the resolved `Url` string (e.g. `FileDto.Url`, `QuotationSummaryDto.FileUrl`).

`GET /api/files/{id}` serves `SourceType.Path` files directly from disk, or issues a redirect for `SourceType.Url` files.

Changing the base URL or moving the storage location only requires updating `FileStorage:BaseUrl` and `FileStorage:Path` in configuration — no data migration needed.

---

## Data seeder

`DataSeeder.SeedAsync` runs on every application startup (idempotent). It creates:

1. Currency `ZMW` (Zambian Kwacha, active).
2. Company `Head Office` (root company, no parent).
3. Role `Super Admin` (root role, `ParentRoleId` null, `IsSystemRole = true`).
4. User with email from `Seed:AdminEmail` (default `admin@local`), password from `Seed:AdminPassword` (falls back to `ChangeMe!123` with a warning), assigned the `Super Admin` role.

Each step checks for an existing row before inserting. The seeder runs without an acting user, so `CreatedByUserId` is null on seeded rows.

---

## Enum serialization

All enums are serialized as strings (not integers) in JSON responses, configured globally in `Program.cs`:

```csharp
o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
```

This applies to `PurchaseOrderStatus`, `ApprovalStatus`, and `FileSourceType`.
