# Procurement endpoints — Suppliers, Files, Quotations, Supplier Bids

Read and write endpoints are accessible to any authenticated user (fine-grained role-based restrictions are a flagged open question — see `docs/06-OPEN-QUESTIONS-AND-ASSUMPTIONS.md` in the project root).

---

## Suppliers

### `GET /api/suppliers`

Lists suppliers (paginated). Optional name search.

**Query parameters**: `page`, `pageSize`, `search` (string?, case-insensitive partial match on `SupplierName`).

**Response `200 OK`**

```json
{
  "items": [
    { "id": 1, "supplierName": "Acme Ltd", "phone": "+260977000001", "email": "acme@acme.com", "address": "123 Main St, Lusaka" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1
}
```

---

### `GET /api/suppliers/{id}`

Returns a single supplier.

**Response `200 OK`** — `{ id, supplierName, phone, email, address }`

**Errors**: `404`

---

### `POST /api/suppliers`

Creates a supplier.

**Request body**

```json
{
  "supplierName": "Acme Ltd",    // string, required, max 256
  "phone": "+260977000001",      // string, required, max 64
  "email": "acme@acme.com",      // string, required, max 256
  "address": "123 Main St"       // string, required, max 512
}
```

**Response `201 Created`**

---

### `PUT /api/suppliers/{id}`

Updates a supplier. Same body shape as create.

**Response `200 OK`**

**Errors**: `404`

---

### `DELETE /api/suppliers/{id}`

Soft-deletes a supplier.

**Response `204 No Content`**

**Errors**: `404`

---

## Files

### `POST /api/files`

Uploads a file (max 50 MB). Saves to local disk and creates a `StoredFile` record.

**Request**: `multipart/form-data`, single field named `file`.

**Response `201 Created`**

```json
{
  "id": 5,
  "url": "https://localhost:5001/api/files/5",
  "originalFileName": "quotation.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 204800
}
```

`url` is always the resolved full URL, not the raw storage path.

---

### `GET /api/files/{id}`

Serves the file.

- `SourceType.Path`: streams file bytes from disk. Response includes the original `Content-Disposition` filename.
- `SourceType.Url`: returns `302 Redirect` to the external URL.

**Errors**: `404`

---

## Quotations

A quotation is a standalone library record of a document received from a supplier. It exists independently of any bid or PO and is kept for audit even if never used.

### `GET /api/quotations`

Lists quotations (summary view — no line items).

**Query parameters**:
- `supplierId` (int?, filter by supplier)
- `isExpired` (bool?, true = `ExpiresAtUtc` is in the past, false = not expired or no expiry, omit = all)
- `isUsed` (bool?, true = at least one line item has been sourced into a `SupplierBidItem`)

**Response `200 OK`** — array of:

```json
{
  "id": 1,
  "supplierId": 1, "supplierName": "Acme Ltd",
  "fileId": 5, "fileUrl": "https://localhost:5001/api/files/5",
  "originalFileName": "quotation.pdf",
  "description": "Q1 2026 supply",
  "quoteReference": "ACM-2026-001",
  "quoteDate": "2026-01-15T00:00:00Z",
  "expiresAtUtc": "2026-06-30T00:00:00Z",
  "isExpired": false,
  "currency": "ZMW",
  "notes": null,
  "lineItemCount": 3,
  "isUsed": false,
  "taxRate": 16.0, "discountRate": null,
  "subtotal": 5000.00, "taxAmount": 800.00, "discountAmount": 0.00, "grandTotal": 5800.00
}
```

---

### `GET /api/quotations/{quotationId}`

Returns a full quotation including line items.

**Response `200 OK`**

```json
{
  "id": 1,
  "supplierId": 1, "supplierName": "Acme Ltd",
  "file": { "id": 5, "url": "https://localhost:5001/api/files/5", "originalFileName": "quotation.pdf", "contentType": "application/pdf", "fileSizeBytes": 204800 },
  "description": "Q1 2026 supply",
  "quoteReference": "ACM-2026-001",
  "quoteDate": "2026-01-15T00:00:00Z",
  "expiresAtUtc": "2026-06-30T00:00:00Z",
  "isExpired": false,
  "currency": "ZMW",
  "notes": null,
  "isUsed": false,
  "taxRate": 16.0, "discountRate": null,
  "subtotal": 5000.00, "taxAmount": 800.00, "discountAmount": 0.00, "grandTotal": 5800.00,
  "lineItems": [
    { "id": 10, "description": "Widget A", "quantity": 100.00, "unitCost": 50.00 }
  ]
}
```

**Errors**: `404`

---

### `POST /api/quotations`

Creates a quotation with its line items in one call. Once created, line items are immutable — upload a new quotation to correct them.

**Request body**

```json
{
  "supplierId": 1,                    // int, required
  "fileId": 5,                        // int, required — must reference an uploaded file
  "description": "Q1 2026 supply",    // string?, max 512
  "quoteReference": "ACM-2026-001",   // string?, max 128
  "quoteDate": "2026-01-15",          // DateTime, required
  "expiresAtUtc": "2026-06-30",       // DateTime?
  "currency": "ZMW",                  // string, required, exactly 3 chars, active currency
  "notes": null,                      // string?, max 2048
  "taxRate": 16.0,                    // decimal?, 0–100 (null = tax pre-included; 0 = no tax)
  "discountRate": null,               // decimal?, 0–100 (null or 0 = no discount)
  "lineItems": [                      // list, required, at least 1 item
    {
      "description": "Widget A",      // string, required, max 1024
      "quantity": 100,                // decimal, > 0
      "unitCost": 50.00               // decimal, >= 0
    }
  ]
}
```

**Response `201 Created`** — full `QuotationDto` (with line items).

**Errors**: `404` (supplierId or fileId not found), `422` (currency not found/inactive, no line items supplied).

---

## Supplier Bids

A supplier bid is a standalone library record of one supplier's competing offer. Bids can exist without a PO and be attached later.

### `GET /api/supplier-bids`

Lists bids (summary view).

**Query parameters**:
- `supplierId` (int?, filter by supplier)
- `purchaseOrderId` (int?, filter by the PO the bid is attached to)
- `unattachedOnly` (bool?, true = only standalone bids with `PurchaseOrderId = null`)

**Response `200 OK`** — array of:

```json
{
  "id": 3,
  "purchaseOrderId": null,
  "supplierId": 1, "supplierName": "Acme Ltd",
  "notes": null,
  "totals": [{ "currency": "ZMW", "subtotal": 4500.00, "taxAmount": 720.00, "totalAmount": 5220.00 }],
  "itemCount": 1,
  "quotationCount": 1,
  "hasExpiredQuotation": false,
  "earliestQuotationExpiryUtc": "2026-06-30T00:00:00Z"
}
```

`totals` is a vector — one entry per currency present among the bid's items. Totals are never combined across currencies.

---

### `GET /api/supplier-bids/{id}`

Returns a full bid including its items and concurrency tokens.

**Response `200 OK`**

```json
{
  "id": 3,
  "purchaseOrderId": null,
  "supplierId": 1, "supplierName": "Acme Ltd",
  "notes": null,
  "totals": [{ "currency": "ZMW", "subtotal": 4500.00, "taxAmount": 720.00, "totalAmount": 5220.00 }],
  "itemCount": 1,
  "items": [
    {
      "id": 20, "supplierBidId": 3,
      "sourceQuotationLineItemId": 10,
      "sourceQuotationId": 1, "sourceQuotationReference": "ACM-2026-001",
      "description": "Widget A",
      "quantity": 100.00, "unitCost": 45.00, "currency": "ZMW",
      "discountPercentage": null, "discountAmount": 0.00,
      "taxPercentage": 16.0, "taxAmount": 720.00,
      "lineSubtotal": 4500.00, "lineTotal": 5220.00,
      "rowVersion": "AAAAAQ=="
    }
  ],
  "rowVersion": "AAAAAQ=="
}
```

**Errors**: `404`

---

### `POST /api/supplier-bids`

Creates a standalone bid (not yet attached to any PO).

**Request body**

```json
{
  "supplierId": 1,    // int, required
  "notes": null       // string?, max 2048
}
```

**Response `201 Created`** — `SupplierBidDto`

---

### `POST /api/supplier-bids/{supplierBidId}/attach`

Attaches an existing standalone bid to a Draft PO.

**Path parameters**: `supplierBidId` (int)

**Request body**

```json
{ "purchaseOrderId": 7 }   // int, required
```

**Response `200 OK`** — `SupplierBidDto`

**Errors**: `404`, `422` (PO not in Draft).

---

### `GET /api/purchase-orders/{purchaseOrderId}/bids`

Lists bids attached to a specific PO.

**Response `200 OK`** — array of `SupplierBidSummaryDto` (same shape as the list endpoint above).

---

### `POST /api/purchase-orders/{purchaseOrderId}/bids`

Creates a new bid already scoped to a PO (equivalent to creating standalone and attaching, but in one call).

**Path parameters**: `purchaseOrderId` (int)

**Request body**: same shape as standalone create.

**Response `201 Created`** — `SupplierBidDto`

**Errors**: `404`, `422` (PO not in Draft).

---

### `POST /api/supplier-bids/{supplierBidId}/items`

Adds a line item to a bid. All money fields are server-computed — do not send them.

**Path parameters**: `supplierBidId` (int)

**Request body**

```json
{
  "description": "Widget A",              // string, required, max 1024
  "quantity": 100,                        // decimal, > 0
  "unitCost": 45.00,                      // decimal, >= 0
  "currency": "ZMW",                      // string?, exactly 3 chars, active currency (defaults from source quotation currency if omitted)
  "discountPercentage": null,             // decimal?, 0–100
  "taxPercentage": 16.0,                  // decimal?, 0–100
  "sourceQuotationLineItemId": 10         // int, required — must belong to a quotation from the bid's supplier
}
```

**Response `200 OK`** — `SupplierBidItemDto`

**Errors**: `404`, `422` (source line item not found or does not belong to the bid's supplier, currency inactive).

---

### `PUT /api/supplier-bids/{supplierBidId}/items/{itemId}`

Updates a bid line item. Money fields are recomputed server-side.

**Path parameters**: `supplierBidId` (int), `itemId` (int)

**Request body**

```json
{
  "description": "Widget A revised",
  "quantity": 120,
  "unitCost": 44.00,
  "currency": "ZMW",                // string, required
  "discountPercentage": 5.0,        // decimal?, 0–100
  "taxPercentage": 16.0,
  "rowVersion": "AAAAAQ=="          // string?, base64 xmin concurrency token
}
```

**Response `200 OK`** — `SupplierBidItemDto`

**Errors**: `404`, `409` (rowVersion mismatch), `422`.

---

### `DELETE /api/supplier-bids/{supplierBidId}/items/{itemId}`

Removes a bid line item.

**Response `204 No Content`**

**Errors**: `404`

---

### `POST /api/supplier-bids/{supplierBidId}/items/seed-from-quotation`

Copies all line items from a quotation into the bid as editable bid items. Each quotation line becomes one `SupplierBidItem` (same description, quantity, unit cost; currency inherited from the quotation). Existing bid items are not removed.

**Path parameters**: `supplierBidId` (int)

**Request body**

```json
{ "quotationId": 1 }   // int, required
```

**Response `200 OK`** — updated `SupplierBidDto` (full bid with all items)

**Errors**: `404`, `422` (quotation's supplier does not match the bid's supplier).
