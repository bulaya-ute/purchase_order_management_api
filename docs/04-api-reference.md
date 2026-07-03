# API Reference

This document is an index. The full endpoint documentation is split by domain into the files listed below.

## Common conventions

### Base URL

In development: `https://localhost:29739` (https profile) or `http://localhost:29739` (http profile).

### Authentication

Every endpoint requires an authenticated `pom_session` cookie unless noted as **[Public]**. The cookie is issued by `POST /api/auth/login`. Unauthenticated requests return `401 Unauthorized`.

### Response format

All responses are JSON. Errors use the RFC 7807 `ProblemDetails` envelope:

```json
{
  "status": 422,
  "title": "Unprocessable Entity",
  "detail": "Currency XYZ was not found."
}
```

| HTTP status | When |
|---|---|
| `400 Bad Request` | Malformed input |
| `401 Unauthorized` | Missing or expired session |
| `403 Forbidden` | Authenticated but not permitted |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Concurrency mismatch or duplicate |
| `422 Unprocessable Entity` | Business rule violation |

### Pagination

List endpoints that paginate accept `?page=1&pageSize=20` (default page 1, default page size 20, maximum 100). The response envelope:

```json
{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 47 }
```

### Concurrency tokens

Financially sensitive resources include a `rowVersion` field in their response DTOs (a base64-encoded PostgreSQL `xmin`). Echo it back in write requests to enable optimistic concurrency detection. If the token is stale, the response is `409 Conflict`. If omitted, the write proceeds without a concurrency check.

### Enum serialization

All enums are serialized as strings. Values:

| Enum | Values |
|---|---|
| `PurchaseOrderStatus` | `Draft`, `Open`, `Approved`, `Rejected`, `Cancelled` |
| `ApprovalStatus` | `Pending`, `Approved`, `Rejected`, `Skipped` |
| `FileSourceType` | `Path`, `Url` |

---

## Endpoint domains

| File | Endpoints covered |
|---|---|
| [endpoints/auth.md](endpoints/auth.md) | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me` |
| [endpoints/admin.md](endpoints/admin.md) | Companies, Users, Roles, Currencies, Purchase Order Types |
| [endpoints/procurement.md](endpoints/procurement.md) | Suppliers, Files, Quotations, Supplier Bids |
| [endpoints/purchase-orders.md](endpoints/purchase-orders.md) | Purchase Orders (CRUD, composition, lifecycle) |
| [endpoints/approvals.md](endpoints/approvals.md) | Approvals inbox and approve/reject actions |

---

## Quick endpoint index

### Auth (`/api/auth`)
- `POST /api/auth/login` [Public] — issue session cookie
- `POST /api/auth/logout` — invalidate session
- `GET  /api/auth/me` — current user identity

### Companies (`/api/companies`)
- `GET    /api/companies` — list (paginated)
- `GET    /api/companies/{id}` — get one
- `POST   /api/companies` — create (admin-tier)
- `PUT    /api/companies/{id}` — update (admin-tier)
- `DELETE /api/companies/{id}` — soft-delete (admin-tier)

### Users (`/api/users`)
- `GET    /api/users` — list (paginated, optional `companyId` filter)
- `GET    /api/users/{id}` — get one
- `POST   /api/users` — create (admin-tier)
- `PUT    /api/users/{id}` — update (admin-tier)
- `DELETE /api/users/{id}` — soft-delete (admin-tier)
- `POST   /api/users/{id}/reset-password` — set password (admin-tier)

### Roles (`/api/roles`)
- `GET    /api/roles` — full flat list
- `GET    /api/roles/allowed-parents` — eligible parent roles for the current user
- `GET    /api/roles/{id}` — get one
- `POST   /api/roles` — create (seniority-ceiling gate)
- `PUT    /api/roles/{id}` — rename (seniority-ceiling gate)
- `DELETE /api/roles/{id}` — soft-delete (seniority-ceiling gate)

### Currencies (`/api/currencies`)
- `GET  /api/currencies` — list (optional `isActive` filter)
- `GET  /api/currencies/{code}` — get one by ISO code
- `POST /api/currencies` — create (admin-tier)
- `PUT  /api/currencies/{code}` — update name/active flag (admin-tier)

### Files (`/api/files`)
- `POST /api/files` — upload (multipart/form-data, max 50 MB)
- `GET  /api/files/{id}` — serve file (stream or redirect)

### Suppliers (`/api/suppliers`)
- `GET    /api/suppliers` — list (paginated, optional `search`)
- `GET    /api/suppliers/{id}` — get one
- `POST   /api/suppliers` — create
- `PUT    /api/suppliers/{id}` — update
- `DELETE /api/suppliers/{id}` — soft-delete

### Quotations (`/api/quotations`)
- `GET  /api/quotations` — list summaries (optional `supplierId`, `isExpired`, `isUsed`)
- `GET  /api/quotations/{quotationId}` — get one with line items
- `POST /api/quotations` — create with line items

### Supplier Bids
- `GET    /api/supplier-bids` — list summaries (optional `supplierId`, `purchaseOrderId`, `unattachedOnly`)
- `GET    /api/supplier-bids/{id}` — get one with items
- `POST   /api/supplier-bids` — create standalone bid
- `POST   /api/supplier-bids/{supplierBidId}/attach` — attach to a Draft PO
- `GET    /api/purchase-orders/{purchaseOrderId}/bids` — list bids for a PO
- `POST   /api/purchase-orders/{purchaseOrderId}/bids` — create bid scoped to a PO
- `POST   /api/supplier-bids/{supplierBidId}/items` — add item to bid
- `PUT    /api/supplier-bids/{supplierBidId}/items/{itemId}` — update bid item
- `DELETE /api/supplier-bids/{supplierBidId}/items/{itemId}` — remove bid item
- `POST   /api/supplier-bids/{supplierBidId}/items/seed-from-quotation` — bulk-copy quotation lines

### Purchase Orders (`/api/purchase-orders`)
- `GET    /api/purchase-orders` — list (paginated, optional `status`, `companyId`)
- `GET    /api/purchase-orders/{id}` — get full PO
- `POST   /api/purchase-orders` — create in Draft
- `PUT    /api/purchase-orders/{id}` — update header (Draft only)
- `POST   /api/purchase-orders/{id}/line-items` — add direct-entry line item (Draft only)
- `PUT    /api/purchase-orders/{id}/line-items/{lineItemId}` — update line item (Draft only)
- `DELETE /api/purchase-orders/{id}/line-items/{lineItemId}` — remove line item (Draft only)
- `POST   /api/purchase-orders/{id}/supplier-bids` — attach supplier bid (Draft only)
- `DELETE /api/purchase-orders/{id}/supplier-bids/{supplierBidId}` — detach supplier bid (Draft only)
- `PATCH  /api/purchase-orders/{id}/supplier-bids/{supplierBidId}/set-primary` — set primary bid (Draft only)
- `POST   /api/purchase-orders/{id}/awarded-bid` — select winning bid (Draft only)
- `GET    /api/purchase-orders/{id}/approvals` — list approval rows
- `POST   /api/purchase-orders/{id}/approvals` — add approval definition (Draft only, custom chain only)
- `DELETE /api/purchase-orders/{id}/approvals/{approvalId}` — remove approval definition (Draft only, custom chain only)
- `POST   /api/purchase-orders/{id}/submit` — submit Draft → Open (locks composition)
- `POST   /api/purchase-orders/{id}/pay` — record payment (Approved only)
- `POST   /api/purchase-orders/{id}/deliver` — record delivery (Approved only)
- `POST   /api/purchase-orders/{id}/cancel` — cancel (Draft/Open/Approved, blocked if paid)

### Approvals (`/api/approvals`)
- `GET  /api/approvals/mine` — current user's actionable inbox
- `POST /api/approvals/{approvalId}/approve` — approve
- `POST /api/approvals/{approvalId}/reject` — reject

### Purchase Order Types (`/api/purchase-order-types`)
- `GET    /api/purchase-order-types` — list all types
- `GET    /api/purchase-order-types/{id}` — get one
- `POST   /api/purchase-order-types` — create (admin-tier)
- `PUT    /api/purchase-order-types/{id}` — full replace (admin-tier)
- `DELETE /api/purchase-order-types/{id}` — soft-delete (admin-tier)
