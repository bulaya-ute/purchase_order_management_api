# Data Model

## Cross-cutting columns

Every entity that inherits `BaseEntity` carries the following columns in addition to its own. They are not repeated in the per-table sections below.

| Column | DB type | Nullable | Purpose |
|---|---|---|---|
| `Id` | `int` (IDENTITY) | No | Primary key |
| `CreatedAtUtc` | `timestamp` | No | Set on insert by `AppDbContext` |
| `CreatedByUserId` | `int` FK → Users | Yes | Nullable to allow system/seed ops with no acting user |
| `UpdatedAtUtc` | `timestamp` | Yes | Set on every update |
| `UpdatedByUserId` | `int` FK → Users | Yes | Acting user for the last update |
| `IsDeleted` | `boolean` DEFAULT false | No | Soft-delete flag. A global EF query filter excludes rows where `IsDeleted = true`. |
| `DeletedAtUtc` | `timestamp` | Yes | Set when soft-deleted |
| `DeletedByUserId` | `int` FK → Users | Yes | Acting user who deleted the row |

The audit FK columns (`CreatedByUserId`, `UpdatedByUserId`, `DeletedByUserId`) all reference `Users.Id` with `ON DELETE RESTRICT`.

---

## Tables

### `Currencies`

Natural-key reference table. Does **not** inherit `BaseEntity` (no audit/soft-delete columns).

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| `Code` | `char(3)` | No | Primary key. ISO 4217 (e.g. `ZMW`, `USD`). |
| `Name` | `varchar(128)` | No | Human-readable name (e.g. "Zambian Kwacha"). |
| `IsActive` | `boolean` DEFAULT true | No | Only active currencies are accepted by CurrencyValidation. |

---

### `Companies`

Self-referencing hierarchy. The root company has `ParentCompanyId = null`.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `Name` | `varchar(256)` | No | |
| `ParentCompanyId` | `int` FK → Companies | Yes | Null for the root (Head Office). |

---

### `Roles`

Self-referencing role tree. The root role (`Super Admin`) has `ParentRoleId = null`.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `Name` | `varchar(256)` | No | |
| `ParentRoleId` | `int` FK → Roles | Yes | Null only for the root role. |
| `IsSystemRole` | `boolean` DEFAULT false | No | Protected system roles (Super Admin, Admin) cannot be deleted via the API. |

---

### `Users`

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `FullName` | `varchar(256)` | No | |
| `Email` | `citext` | No | Case-insensitive unique login identifier. Requires PostgreSQL `citext` extension. |
| `PasswordHash` | `bytea` | No | PBKDF2 hash |
| `PasswordSalt` | `bytea` | No | PBKDF2 salt |
| `IsActive` | `boolean` DEFAULT true | No | Inactive users cannot log in. |
| `CompanyId` | `int` FK → Companies | No | |

**Indexes**: `IX_Users_Email` (unique).

---

### `UserRoles`

Many-to-many join between `Users` and `Roles`. A user may hold multiple roles simultaneously.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `UserId` | `int` FK → Users | No | |
| `RoleId` | `int` FK → Roles | No | |

**Indexes**: `IX_UserRoles_UserId_RoleId` (unique, filtered to `IsDeleted = false` — a user cannot hold the same role twice among active rows).

---

### `Suppliers`

Global supplier catalogue shared across all companies.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `SupplierName` | `varchar(256)` | No | |
| `Phone` | `varchar(64)` | No | |
| `Email` | `varchar(256)` | No | |
| `Address` | `varchar(512)` | No | |

---

### `Files` (entity: `StoredFile`)

Generic file attachment. The entity is named `StoredFile` to avoid clashing with `System.IO.File`.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `SourceType` | `varchar(16)` | No | Enum stored as string: `Path` or `Url`. |
| `Source` | `varchar(2048)` | No | Server-relative path (for `Path`) or complete external URL (for `Url`). Never a pre-built absolute URL for `Path` files — resolved at API layer. |
| `OriginalFileName` | `varchar(512)` | Yes | Original filename from the upload. |
| `ContentType` | `varchar(256)` | Yes | MIME type. |
| `FileSizeBytes` | `bigint` | Yes | File size in bytes. |

---

### `Quotations`

A standalone library record of a document received from a supplier. Exists independently of any bid or PO.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `SupplierId` | `int` FK → Suppliers | No | |
| `FileId` | `int` FK → Files | No | Mandatory — every quotation must have an uploaded document. |
| `Description` | `varchar(512)` | Yes | |
| `QuoteReference` | `varchar(128)` | Yes | Supplier-assigned reference number. |
| `QuoteDate` | `timestamp` | No | Date on the quotation document. |
| `ExpiresAtUtc` | `timestamp` | Yes | Null = no stated expiry. |
| `CurrencyCode` | `char(3)` FK → Currencies | No | Currency for all line items in this quotation. |
| `TaxRate` | `numeric(5,2)` | Yes | Null = tax pre-included; 0 = no tax; >0 = rate applied to subtotal. |
| `DiscountRate` | `numeric(5,2)` | Yes | Null or 0 = no discount; >0 = applied post-tax. |
| `Notes` | `varchar(2048)` | Yes | |

---

### `QuotationLineItems`

The items as quoted by the supplier. Treated as an immutable record; to correct a line, upload a new quotation.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `QuotationId` | `int` FK → Quotations | No | |
| `Description` | `varchar(1024)` | No | |
| `Quantity` | `numeric(18,2)` | No | |
| `UnitCost` | `numeric(18,2)` | No | In the parent quotation's currency. |

---

### `SupplierBids`

A standalone library record of one supplier's competing offer. Can exist without a PO (`PurchaseOrderId` null) and later be attached to a Draft PO.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PurchaseOrderId` | `int` FK → PurchaseOrders | Yes | Null = standalone/unattached. Set when attached to a Draft PO. |
| `SupplierId` | `int` FK → Suppliers | No | |
| `Notes` | `varchar(2048)` | Yes | |
| `xmin` | `xid` (concurrency token) | — | PostgreSQL system column used as optimistic concurrency token. |

---

### `SupplierBidItems`

The editable, comparison-ready version of quotation lines, scoped to one bid.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `SupplierBidId` | `int` FK → SupplierBids | No | |
| `SourceQuotationLineItemId` | `int` FK → QuotationLineItems | No | **Non-nullable** — every bid item must trace to a quotation line from the bid's supplier. |
| `Description` | `varchar(1024)` | No | |
| `Quantity` | `numeric(18,2)` | No | May differ from the source quotation line. |
| `UnitCost` | `numeric(18,2)` | No | In the item's own currency. |
| `CurrencyCode` | `char(3)` FK → Currencies | No | Defaults from the source quotation's currency if not overridden. |
| `DiscountPercentage` | `numeric(5,2)` | Yes | |
| `DiscountAmount` | `numeric(18,2)` | No | Server-computed. |
| `TaxPercentage` | `numeric(5,2)` | Yes | |
| `TaxAmount` | `numeric(18,2)` | No | Server-computed. |
| `LineSubtotal` | `numeric(18,2)` | No | Server-computed: `Quantity × UnitCost − DiscountAmount`. |
| `LineTotal` | `numeric(18,2)` | No | Server-computed: `LineSubtotal + TaxAmount`. |
| `xmin` | `xid` (concurrency token) | — | Optimistic concurrency token. |

---

### `PurchaseOrderSupplierBids`

Junction table linking bids to a PO. Supports attaching multiple bids (primary + alternatives for approver comparison). Does **not** inherit `BaseEntity`.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| `Id` | `int` (IDENTITY) | No | Primary key. |
| `PurchaseOrderId` | `int` FK → PurchaseOrders | No | Cascade delete from PO. |
| `SupplierBidId` | `int` FK → SupplierBids | No | Restrict delete. |
| `IsPrimary` | `boolean` | No | True for the bid the creator selected as the primary comparison. Once a primary exists, the set is locked (no more attachments). |
| `AddedAtUtc` | `timestamp` | No | |

**Indexes**: `(PurchaseOrderId, SupplierBidId)` unique — each bid can be attached to a given PO at most once.

---

### `PurchaseOrders`

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PONumber` | `varchar(32)` | No | Server-generated sequential identifier (e.g. `PO-0001`). |
| `CompanyId` | `int` FK → Companies | No | The issuing entity. |
| `TargetCompanyId` | `int` FK → Companies | Yes | Who the purchase is for (a branch). Editable only in Draft. |
| `IssuerUserId` | `int` FK → Users | No | Set from the current user on creation. |
| `CurrencyCode` | `char(3)` FK → Currencies | No | Nominal/header currency. For multi-currency bid-based POs, authoritative totals are in `PurchaseOrderCurrencyTotals`. |
| `Status` | `varchar(16)` | No | Enum string: `Draft`, `Open`, `Approved`, `Rejected`, `Cancelled`. |
| `PurchaseOrderTypeId` | `int` FK → PurchaseOrderTypes | Yes | Null = free-form/custom approval chain. When set, the chain is auto-generated from the type's steps. |
| `AwardedSupplierBidId` | `int` FK → SupplierBids | Yes | The winning bid. Set while in Draft; locked at submit. |
| `AwardedAtUtc` | `timestamp` | Yes | |
| `AwardedByUserId` | `int` FK → Users | Yes | |
| `PaidAtUtc` | `timestamp` | Yes | Payment milestone. Independent of delivery. |
| `DeliveredAtUtc` | `timestamp` | Yes | Delivery milestone. Independent of payment. |
| `Subtotal` | `numeric(18,2)` | No | Server-computed aggregate. Authoritative for direct-entry POs. |
| `TaxAmount` | `numeric(18,2)` | No | Server-computed aggregate. |
| `TotalAmount` | `numeric(18,2)` | No | Server-computed aggregate. |
| `Notes` | `varchar(2048)` | Yes | |
| `xmin` | `xid` (concurrency token) | — | Optimistic concurrency token. |

**Indexes**: `PONumber` unique.

---

### `PurchaseOrderLineItems`

Final, locked-in lines for a PO. For bid-based POs, these are created at the moment the PO reaches `Approved` status (copied from the awarded `SupplierBidItems`). For direct-entry POs, they are entered in Draft and locked at submit.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PurchaseOrderId` | `int` FK → PurchaseOrders | No | |
| `SourceSupplierBidItemId` | `int` FK → SupplierBidItems | Yes | Null for direct-entry POs. Set for bid-based POs for traceability. |
| `Description` | `varchar(1024)` | No | |
| `Quantity` | `numeric(18,2)` | No | |
| `UnitCost` | `numeric(18,2)` | No | |
| `CurrencyCode` | `char(3)` FK → Currencies | No | Copied from the bid item for bid-based POs; equals the PO's `CurrencyCode` for direct-entry. |
| `DiscountPercentage` | `numeric(5,2)` | Yes | |
| `DiscountAmount` | `numeric(18,2)` | No | Server-computed. |
| `TaxPercentage` | `numeric(5,2)` | Yes | |
| `TaxAmount` | `numeric(18,2)` | No | Server-computed. |
| `LineSubtotal` | `numeric(18,2)` | No | Server-computed. |
| `LineTotal` | `numeric(18,2)` | No | Server-computed. |
| `xmin` | `xid` (concurrency token) | — | Optimistic concurrency token. |

---

### `PurchaseOrderCurrencyTotals`

Per-currency aggregate rows for a bid-based PO whose awarded bid's line items span more than one currency. Fully recomputed whenever line items change. Does **not** inherit `BaseEntity` (no audit/soft-delete).

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| `Id` | `int` (IDENTITY) | No | Primary key. |
| `PurchaseOrderId` | `int` FK → PurchaseOrders | No | Cascade delete from PO. |
| `CurrencyCode` | `char(3)` FK → Currencies | No | |
| `Subtotal` | `numeric(18,2)` | No | |
| `TaxAmount` | `numeric(18,2)` | No | |
| `TotalAmount` | `numeric(18,2)` | No | |

**Indexes**: `(PurchaseOrderId, CurrencyCode)` unique — one row per currency per PO.

---

### `PurchaseOrderTypes`

Admin-defined PO presets. Defines a fixed approval chain and optional creator-role restriction.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `Name` | `varchar(256)` | No | |
| `IsActive` | `boolean` DEFAULT true | No | Inactive types are not available for selection when creating a new PO. |

---

### `PurchaseOrderTypeApprovalSteps`

One template step in a `PurchaseOrderType`'s fixed approval chain. When a PO of this type is created/submitted, these steps are copied 1:1 into `Approvals` rows.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PurchaseOrderTypeId` | `int` FK → PurchaseOrderTypes | No | |
| `RequiredRoleId` | `int` FK → Roles | Yes | Exactly one of RequiredRoleId / RequiredUserId must be non-null (DB check constraint). |
| `RequiredUserId` | `int` FK → Users | Yes | See above. |
| `SequenceOrder` | `int` DEFAULT 0 | No | Lower values are processed first. Equal values run in parallel. |

**Check constraint**: `CK_PurchaseOrderTypeApprovalSteps_ExactlyOneRequiredRoleOrUser` — enforces the XOR rule at the database level.

---

### `PurchaseOrderTypeAllowedCreatorRoles`

Many-to-many: roles allowed to create a PO of a given type. The creator must hold at least one of these roles; the check is enforced in `PurchaseOrderService`.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PurchaseOrderTypeId` | `int` FK → PurchaseOrderTypes | No | |
| `RoleId` | `int` FK → Roles | No | |

**Indexes**: `(PurchaseOrderTypeId, RoleId)` unique.

---

### `Approvals`

One required approval row per PO. A row's existence means the approval is required; the `Status` column tracks whether it has been acted on.

| Column | DB type | Nullable | Notes |
|---|---|---|---|
| + BaseEntity | | | |
| `PurchaseOrderId` | `int` FK → PurchaseOrders | No | |
| `RequiredRoleId` | `int` FK → Roles | Yes | If set, any user holding this role may act. Exactly one of RequiredRoleId / RequiredUserId must be non-null (check constraint). |
| `RequiredUserId` | `int` FK → Users | Yes | If set, only this specific user may act. |
| `SequenceOrder` | `int` DEFAULT 0 | No | A row at sequence N is only actionable when all rows at sequence < N are Approved. Equal sequence = parallel. |
| `Status` | `varchar(16)` DEFAULT `'Pending'` | No | Enum string: `Pending`, `Approved`, `Rejected`, `Skipped`. |
| `ApprovedByUserId` | `int` FK → Users | Yes | The actual user who acted (relevant when RequiredRoleId is set). |
| `ApprovedAtUtc` | `timestamp` | Yes | |
| `Comment` | `varchar(2048)` | Yes | Optional comment from the approver. |
| `xmin` | `xid` (concurrency token) | — | Optimistic concurrency token. |

**Check constraint**: `CK_Approvals_ExactlyOneRequiredRoleOrUser`.

---

## Entity relationship summary

```
Companies ──< Companies (self-referencing: parent/child)
Companies ──< Users
Companies ──< PurchaseOrders (issued by)
Companies ──< PurchaseOrders (TargetCompany, optional)

Roles ──< Roles (self-referencing: parent/child hierarchy)
Users ──< UserRoles >── Roles

Suppliers ──< Quotations >── Files
Quotations ──< QuotationLineItems
                    |
Suppliers ──< SupplierBids ──< SupplierBidItems >── QuotationLineItems
                    |                              (SourceQuotationLineItemId, non-nullable)
                    |
PurchaseOrders >──< SupplierBids (via PurchaseOrderSupplierBids junction)
PurchaseOrders ──── SupplierBids (AwardedSupplierBidId, optional)
PurchaseOrders ──< PurchaseOrderLineItems >── SupplierBidItems (optional traceability)
PurchaseOrders ──< PurchaseOrderCurrencyTotals
PurchaseOrders ──< Approvals

PurchaseOrderTypes ──< PurchaseOrderTypeApprovalSteps
PurchaseOrderTypes ──< PurchaseOrderTypeAllowedCreatorRoles >── Roles
PurchaseOrders >── PurchaseOrderTypes (optional)

Currencies ─── Quotations
Currencies ─── SupplierBidItems
Currencies ─── PurchaseOrders
Currencies ─── PurchaseOrderLineItems
Currencies ─── PurchaseOrderCurrencyTotals
```

**Legend**: `──<` one-to-many, `>──<` many-to-many (via junction), `───` FK reference, `>──` optional FK.
