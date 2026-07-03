# Approvals endpoints

Approval actor-facing endpoints: the current user's inbox, and approve/reject actions. PO-scoped approval definition management (add/remove definitions, list) lives under the [Purchase Orders endpoints](purchase-orders.md).

## Authorization

Any authenticated user may call these endpoints. Eligibility (role match or direct user match) and sequence gating are enforced per-approval row in `ApprovalService`. The service returns `403 Forbidden` if the acting user is not eligible.

Sequence gating rule: an approval at sequence N is only actionable when all approvals at sequence < N for the same PO are in `Approved` status. Approvals at equal sequence values are parallel and may be acted on simultaneously.

---

### `GET /api/approvals/mine`

Returns the acting user's approval inbox: approval rows that are `Pending`, where the user is eligible (by role or by direct user assignment), and where all prior sequence steps for the same PO are already `Approved`.

**Response `200 OK`** — array of:

```json
{
  "id": 30,
  "purchaseOrderId": 7,
  "poNumber": "PO-0001",
  "companyName": "Head Office",
  "totalAmount": 3480.00,
  "currency": "ZMW",
  "requiredRoleId": 2,
  "requiredRoleName": "Admin",
  "requiredUserId": null,
  "sequenceOrder": 0,
  "rowVersion": "AAAAAQ=="
}
```

---

### `POST /api/approvals/{approvalId}/approve`

Approves an approval row. Sets `Status` to `Approved`, stamps `ApprovedByUserId` and `ApprovedAtUtc`.

**Side effects**:
- If all approval rows for the PO are now `Approved`, the PO transitions to `Approved` status.
- For bid-based POs that reach `Approved` status, `PurchaseOrderLineItems` are created by copying the awarded `SupplierBidItems` (including their currency, quantities, costs, and computed money fields).

**Path parameters**: `approvalId` (int)

**Request body** (optional — send `{}` or omit body if no comment)

```json
{
  "comment": "Approved — within budget.",   // string?, max 2048
  "rowVersion": "AAAAAQ=="                   // string?, base64 xmin concurrency token from last read
}
```

**Response `200 OK`** — `ApprovalDto`

```json
{
  "id": 30, "purchaseOrderId": 7,
  "requiredRoleId": 2, "requiredRoleName": "Admin",
  "requiredUserId": null, "requiredUserName": null,
  "sequenceOrder": 0,
  "status": "Approved",
  "approvedByUserId": 1, "approvedByUserName": "Super Admin",
  "approvedAtUtc": "2026-07-03T08:00:00Z",
  "comment": "Approved — within budget.",
  "rowVersion": "AAAAAQ=="
}
```

**Errors**:
- `403` — acting user is not eligible (does not hold the required role or is not the required user).
- `404` — approval row not found.
- `409` — `rowVersion` mismatch (concurrent edit detected).
- `422` — approval is not in `Pending` status, or the sequence gate is not satisfied (a prior sequence step is still pending).

---

### `POST /api/approvals/{approvalId}/reject`

Rejects an approval row. Sets this row's `Status` to `Rejected`. All other `Pending` approval rows for the same PO are set to `Skipped`. The PO transitions to `Rejected` status.

**Path parameters**: `approvalId` (int)

**Request body** (optional)

```json
{
  "comment": "Budget exceeded for this quarter.",
  "rowVersion": "AAAAAQ=="
}
```

**Response `200 OK`** — `ApprovalDto`

```json
{
  "id": 30, "purchaseOrderId": 7,
  "requiredRoleId": 2, "requiredRoleName": "Admin",
  "requiredUserId": null, "requiredUserName": null,
  "sequenceOrder": 0,
  "status": "Rejected",
  "approvedByUserId": 1, "approvedByUserName": "Super Admin",
  "approvedAtUtc": "2026-07-03T08:15:00Z",
  "comment": "Budget exceeded for this quarter.",
  "rowVersion": "AAAAAQ=="
}
```

**Errors**: `403`, `404`, `409`, `422` (not in Pending state or sequence gate).
