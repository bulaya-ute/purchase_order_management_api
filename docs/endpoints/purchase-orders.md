# Purchase Order endpoints

Read and write accessible to any authenticated user (fine-grained role-based restrictions are a flagged open question). The list endpoint defaults to scoping results to the current user's company.

---

## Concurrency tokens

The PO header (`PurchaseOrderDto.RowVersion`), each line item (`PurchaseOrderLineItemDto.RowVersion`), and each approval (`ApprovalDto.RowVersion`) expose a base64-encoded PostgreSQL `xmin` concurrency token. Write requests that accept `RowVersion` should echo it back to detect concurrent edits. If the token is stale, the response is `409 Conflict`. If omitted, the write proceeds without a concurrency check.

---

### `GET /api/purchase-orders`

Lists purchase orders (paginated).

**Query parameters**:
- `page` (default 1), `pageSize` (default 20, max 100)
- `status` (string?, one of `Draft`/`Open`/`Approved`/`Rejected`/`Cancelled`)
- `companyId` (int?, explicit company filter; if omitted, defaults to the current user's company)

**Response `200 OK`**

```json
{
  "items": [
    {
      "id": 7, "poNumber": "PO-0001",
      "companyId": 1, "companyName": "Head Office",
      "targetCompanyId": null, "targetCompanyName": null,
      "issuerUserId": 1, "issuerUserName": "Super Admin",
      "currency": "ZMW", "status": "Draft",
      "totalAmount": 0.00, "notes": null,
      "paidAtUtc": null, "deliveredAtUtc": null,
      "createdAtUtc": "2026-07-01T10:00:00Z"
    }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1
}
```

---

### `GET /api/purchase-orders/{id}`

Returns a full PO including line items, approvals, and attached bid summaries.

**Response `200 OK`**

```json
{
  "id": 7, "poNumber": "PO-0001",
  "companyId": 1, "companyName": "Head Office",
  "targetCompanyId": null, "targetCompanyName": null,
  "issuerUserId": 1, "issuerUserName": "Super Admin",
  "currency": "ZMW", "status": "Draft",
  "notes": null,
  "purchaseOrderTypeId": null, "purchaseOrderTypeName": null,
  "awardedSupplierBidId": null, "awardedAtUtc": null, "awardedByUserId": null,
  "paidAtUtc": null, "deliveredAtUtc": null,
  "subtotal": 0.00, "taxAmount": 0.00, "totalAmount": 0.00,
  "hasMultiCurrencyTotals": false,
  "totals": [],
  "createdAtUtc": "2026-07-01T10:00:00Z",
  "lineItems": [],
  "approvals": [],
  "supplierBids": [],
  "attachedSupplierBids": [],
  "rowVersion": "AAAAAQ=="
}
```

**Totals note**: for a bid-based PO whose awarded bid items span multiple currencies, `hasMultiCurrencyTotals` is `true`. In that case `totals` (a `CurrencyTotalDto[]` vector, one entry per currency) is authoritative and the flat `subtotal`/`taxAmount`/`totalAmount` fields are `0`.

**Errors**: `404`

---

### `POST /api/purchase-orders`

Creates a new PO in `Draft` status. `IssuerUserId` is taken from the current session. `PONumber` is server-generated (e.g. `PO-0001`).

**Request body**

```json
{
  "companyId": 1,                 // int, required
  "targetCompanyId": null,        // int?, who the purchase is for (a branch)
  "currency": "ZMW",              // string?, exactly 3 chars, active currency; defaults to ZMW if omitted
  "purchaseOrderTypeId": null,    // int?, links to an admin-defined type preset with a fixed approval chain
  "notes": null                   // string?, max 2048
}
```

**Response `201 Created`** — `PurchaseOrderDto`

**Errors**: `403` (if `purchaseOrderTypeId` is set and the acting user does not hold an allowed creator role for that type), `404` (companyId, targetCompanyId, or purchaseOrderTypeId not found), `422`.

---

### `PUT /api/purchase-orders/{id}`

Updates PO header fields (currency, target company, notes). Only allowed in `Draft` status.

**Request body**

```json
{
  "currency": "USD",           // string, required, exactly 3 chars, active currency
  "targetCompanyId": 2,        // int?
  "notes": "Updated notes",    // string?, max 2048
  "rowVersion": "AAAAAQ=="     // string?, concurrency token
}
```

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `409` (rowVersion mismatch), `422` (PO not in Draft, currency inactive).

---

## Composition: direct-entry line items (Draft only)

Direct-entry line items are used when the PO does not go through a bidding process. They are entered in Draft and locked at submit.

### `POST /api/purchase-orders/{id}/line-items`

Adds a line item to a Draft PO. The server computes all money fields (`discountAmount`, `taxAmount`, `lineSubtotal`, `lineTotal`) and recomputes the PO's aggregate totals.

**Path parameters**: `id` (int, purchase order)

**Request body**

```json
{
  "description": "Office Desk",    // string, required, max 1024
  "quantity": 2,                   // decimal, > 0
  "unitCost": 1500.00,             // decimal, >= 0
  "discountPercentage": null,      // decimal?, 0–100
  "taxPercentage": 16.0            // decimal?, 0–100
}
```

**Response `200 OK`** — `PurchaseOrderLineItemDto`

```json
{
  "id": 15, "purchaseOrderId": 7,
  "sourceSupplierBidItemId": null,
  "description": "Office Desk", "quantity": 2.00, "unitCost": 1500.00,
  "currency": "ZMW",
  "discountPercentage": null, "discountAmount": 0.00,
  "taxPercentage": 16.0, "taxAmount": 480.00,
  "lineSubtotal": 3000.00, "lineTotal": 3480.00,
  "rowVersion": "AAAAAQ=="
}
```

**Errors**: `404`, `422` (PO not in Draft).

---

### `PUT /api/purchase-orders/{id}/line-items/{lineItemId}`

Updates a direct-entry line item. Money fields are recomputed. PO aggregate totals are recomputed.

**Path parameters**: `id` (int), `lineItemId` (int)

**Request body**

```json
{
  "description": "Office Desk",
  "quantity": 3,
  "unitCost": 1400.00,
  "discountPercentage": null,
  "taxPercentage": 16.0,
  "rowVersion": "AAAAAQ=="     // string?, concurrency token
}
```

**Response `200 OK`** — `PurchaseOrderLineItemDto`

**Errors**: `404`, `409`, `422`.

---

### `DELETE /api/purchase-orders/{id}/line-items/{lineItemId}`

Removes a direct-entry line item from a Draft PO. PO aggregate totals are recomputed.

**Response `204 No Content`**

**Errors**: `404`, `422` (PO not in Draft).

---

## Composition: supplier bid attachment (Draft only)

### `POST /api/purchase-orders/{id}/supplier-bids`

Attaches a supplier bid to a Draft PO. Once a bid with `IsPrimary = true` is attached, the set is locked (no further attachments allowed).

**Request body**

```json
{
  "supplierBidId": 3,    // int
  "isPrimary": false     // bool — marks this as the primary comparison bid
}
```

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (PO not in Draft, bid already attached to this PO, primary already set).

---

### `DELETE /api/purchase-orders/{id}/supplier-bids/{supplierBidId}`

Detaches a supplier bid from a Draft PO.

**Response `204 No Content`**

**Errors**: `404`, `422` (PO not in Draft).

---

### `PATCH /api/purchase-orders/{id}/supplier-bids/{supplierBidId}/set-primary`

Marks a specific attached bid as the primary bid for this PO. No request body.

**Response `204 No Content`**

**Errors**: `404`, `422` (PO not in Draft, bid not attached).

---

## Composition: awarded bid selection (Draft only)

### `POST /api/purchase-orders/{id}/awarded-bid`

Selects the winning bid for a bid-based PO while in Draft. The bid must already be attached to this PO (via `PurchaseOrderSupplierBids`). The selection is locked at submit.

Line items are **not** created at this point — they are created when the PO reaches `Approved` status (copied from the awarded `SupplierBidItems`).

**Request body**

```json
{ "supplierBidId": 3 }   // int, required
```

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (PO not in Draft, bid not attached to this PO).

---

## Composition: approval definitions (Draft only)

### `GET /api/purchase-orders/{id}/approvals`

Lists the approval rows for a PO.

**Response `200 OK`** — array of:

```json
{
  "id": 30, "purchaseOrderId": 7,
  "requiredRoleId": 2, "requiredRoleName": "Admin",
  "requiredUserId": null, "requiredUserName": null,
  "sequenceOrder": 0,
  "status": "Pending",
  "approvedByUserId": null, "approvedByUserName": null,
  "approvedAtUtc": null, "comment": null,
  "rowVersion": "AAAAAQ=="
}
```

---

### `POST /api/purchase-orders/{id}/approvals`

Adds an approval definition to a Draft PO. Exactly one of `requiredRoleId` / `requiredUserId` must be supplied. Blocked for typed POs (their approval chain is fixed by the type).

**Request body**

```json
{
  "requiredRoleId": 2,       // int? — any user holding this role may act
  "requiredUserId": null,    // int? — only this specific user may act (XOR with requiredRoleId)
  "sequenceOrder": 0         // int, default 0; equal values = parallel; lower value = earlier in sequence
}
```

**Response `200 OK`** — `ApprovalDto`

**Errors**: `404`, `422` (PO not in Draft, PO has a type preset, neither or both of requiredRoleId/requiredUserId supplied, requiredRoleId/requiredUserId not found).

---

### `DELETE /api/purchase-orders/{id}/approvals/{approvalId}`

Removes an approval definition from a Draft PO. Blocked for typed POs.

**Response `204 No Content`**

**Errors**: `404`, `422` (PO not in Draft, typed PO).

---

## PO lifecycle

### `POST /api/purchase-orders/{id}/submit`

Transitions a Draft PO to `Open`. Composition is frozen:
- Direct-entry line items are locked.
- The awarded bid selection is locked.
- For typed POs, `Approval` rows are auto-generated from the type's steps.
- For custom-chain POs, the existing `Approval` definitions become the live approval rows.

No request body.

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (not in Draft, no approval definitions, missing required composition).

---

### `POST /api/purchase-orders/{id}/pay`

Records the payment milestone (`PaidAtUtc`). Only allowed in `Approved` status.

No request body.

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (not Approved).

---

### `POST /api/purchase-orders/{id}/deliver`

Records the delivery milestone (`DeliveredAtUtc`). Only allowed in `Approved` status.

No request body.

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (not Approved).

---

### `POST /api/purchase-orders/{id}/cancel`

Cancels a PO. Allowed from `Draft`, `Open`, or `Approved`. Blocked once `PaidAtUtc` is set, and blocked for POs already in `Rejected` or `Cancelled` state.

No request body.

**Response `200 OK`** — `PurchaseOrderDto`

**Errors**: `404`, `422` (already paid, already in a terminal state).
