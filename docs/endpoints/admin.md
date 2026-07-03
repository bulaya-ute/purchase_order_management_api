# Admin endpoints — Companies, Users, Roles, Currencies, PO Types

## Authorization notes

- **Admin-tier gate**: `Companies`, `Users`, and `Currencies` mutations require the acting user to hold the role `Super Admin` or `Admin`. The gate is enforced in the service layer and returns `403 Forbidden` with a `ProblemDetails` body (not a bare 403 redirect).
- **Seniority-ceiling gate**: `Roles` create/update/delete is governed by the acting user's position in the role tree, not the admin-tier names. Enforced in `RoleService`.
- **Read endpoints**: accessible to any authenticated user for all domains in this file.

---

## Companies

### `GET /api/companies`

Lists companies (paginated).

**Query parameters**: `page` (default 1), `pageSize` (default 20, max 100).

**Response `200 OK`**

```json
{
  "items": [
    { "id": 1, "name": "Head Office", "parentCompanyId": null, "parentCompanyName": null },
    { "id": 2, "name": "Branch A", "parentCompanyId": 1, "parentCompanyName": "Head Office" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 2
}
```

---

### `GET /api/companies/{id}`

Returns a single company.

**Response `200 OK`** — `{ id, name, parentCompanyId, parentCompanyName }`

**Errors**: `404`

---

### `POST /api/companies`

Creates a company. **Admin-tier only.**

**Request body**

```json
{
  "name": "Branch B",        // string, required, max 256
  "parentCompanyId": 1       // int?, null for a root company
}
```

**Response `201 Created`** with `Location` header.

**Errors**: `403`, `404` (parentCompanyId not found), `422` (cycle in hierarchy).

---

### `PUT /api/companies/{id}`

Updates a company's name and/or parent. **Admin-tier only.**

**Request body**: same shape as create.

**Response `200 OK`**

**Errors**: `403`, `404`, `422`.

---

### `DELETE /api/companies/{id}`

Soft-deletes a company. **Admin-tier only.**

**Response `204 No Content`**

**Errors**: `403`, `404`.

---

## Users

### `GET /api/users`

Lists users (paginated).

**Query parameters**: `page`, `pageSize`, `companyId` (int?, filter by company).

**Response `200 OK`**

```json
{
  "items": [
    {
      "id": 1, "fullName": "Super Admin", "email": "admin@local",
      "isActive": true, "companyId": 1, "companyName": "Head Office",
      "roles": [{ "id": 1, "name": "Super Admin" }]
    }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1
}
```

---

### `GET /api/users/{id}`

Returns a single user. Password hash/salt are never included.

**Response `200 OK`** — same per-item shape as the list.

**Errors**: `404`

---

### `POST /api/users`

Creates a user. **Admin-tier only.**

**Request body**

```json
{
  "fullName": "Alice",          // string, required, max 256
  "email": "alice@co.com",      // string, required, valid email, max 256
  "companyId": 1,               // int, required
  "isActive": true,             // bool, default true
  "password": "SecurePass!1",   // string, required, min 8, max 256
  "roleIds": [2]                // int[], roles to assign (may be empty)
}
```

**Response `201 Created`**

**Errors**: `403`, `409` (email already in use), `404` (companyId or a roleId not found).

---

### `PUT /api/users/{id}`

Updates a user (full replace of name, email, company, active flag, and role assignments). **Admin-tier only.** `UserRoles` are reconciled to match `roleIds`: missing links are added, removed links are soft-deleted.

**Request body**

```json
{
  "fullName": "Alice Updated",
  "email": "alice@co.com",
  "companyId": 1,
  "isActive": true,
  "roleIds": [2, 3]
}
```

**Response `200 OK`**

**Errors**: `403`, `404`, `409`.

---

### `DELETE /api/users/{id}`

Soft-deletes a user. **Admin-tier only.**

**Response `204 No Content`**

**Errors**: `403`, `404`.

---

### `POST /api/users/{id}/reset-password`

Admin-sets a new password for a user. **Admin-tier only.**

**Request body**

```json
{ "newPassword": "NewPass!1" }   // string, required, min 8, max 256
```

**Response `204 No Content`**

**Errors**: `403`, `404`.

---

## Roles

### `GET /api/roles`

Returns the full flat list of roles. Clients reconstruct the tree using `parentRoleId`.

**Response `200 OK`** — array

```json
[
  { "id": 1, "name": "Super Admin", "parentRoleId": null, "isSystemRole": true },
  { "id": 2, "name": "Admin",       "parentRoleId": 1,    "isSystemRole": false }
]
```

---

### `GET /api/roles/allowed-parents`

Returns the roles the current user may set as a parent when creating a new role (those within their seniority ceiling). Used by UI role pickers.

**Response `200 OK`** — same array shape as the full list, filtered to eligible parents.

---

### `GET /api/roles/{id}`

Returns a single role.

**Response `200 OK`** — `{ id, name, parentRoleId, isSystemRole }`

**Errors**: `404`

---

### `POST /api/roles`

Creates a role. The new role is parented to `parentRoleId`, which must be within the acting user's seniority ceiling.

**Request body**

```json
{
  "name": "Procurement Officer",  // string, required, max 256
  "parentRoleId": 2               // int, required
}
```

**Response `201 Created`**

**Errors**: `403` (parent outside seniority ceiling), `404` (parentRoleId not found).

---

### `PUT /api/roles/{id}`

Renames a role. Re-parenting is not supported. System roles are protected.

**Request body**

```json
{ "name": "Senior Procurement Officer" }
```

**Response `200 OK`**

**Errors**: `403`, `404`, `422` (system role, or acting user's ceiling violation).

---

### `DELETE /api/roles/{id}`

Soft-deletes a role. Protected system roles cannot be deleted.

**Response `204 No Content`**

**Errors**: `403`, `404`, `422`.

---

## Currencies

Currency reads are accessible to any authenticated user (every currency-bearing form needs the active list). Create/update are **admin-tier only** (enforced in `CurrencyService`). There is no delete endpoint — currencies are deactivated via the update endpoint.

### `GET /api/currencies`

Lists currencies.

**Query parameters**: `isActive` (bool?, optional).

**Response `200 OK`** — array

```json
[{ "code": "ZMW", "name": "Zambian Kwacha", "isActive": true }]
```

---

### `GET /api/currencies/{code}`

Returns a single currency by its ISO code (e.g. `ZMW`).

**Response `200 OK`** — `{ code, name, isActive }`

**Errors**: `404`

---

### `POST /api/currencies`

Creates a currency. **Admin-tier only.**

**Request body**

```json
{
  "code": "USD",         // string, required, exactly 3 chars (stored upper-invariant)
  "name": "US Dollar",   // string, required, max 128
  "isActive": true       // bool, default true
}
```

**Response `201 Created`**

**Errors**: `403`, `409` (code already exists).

---

### `PUT /api/currencies/{code}`

Updates a currency's name and/or active flag. **Admin-tier only.** The code is immutable.

**Request body**

```json
{ "name": "US Dollar", "isActive": false }
```

**Response `200 OK`**

**Errors**: `403`, `404`.

---

## Purchase Order Types

PO types are admin-defined presets: a fixed approval chain and an optional set of allowed creator roles. Read endpoints accessible to any authenticated user. Mutations are **admin-tier only** (enforced in `PurchaseOrderTypeService`).

### `GET /api/purchase-order-types`

Lists all PO types (active and inactive).

**Response `200 OK`** — array

```json
[
  {
    "id": 1, "name": "Standard Procurement", "isActive": true,
    "approvalSteps": [
      {
        "id": 10,
        "requiredRoleId": 2, "requiredRoleName": "Admin",
        "requiredUserId": null, "requiredUserName": null,
        "sequenceOrder": 0
      }
    ],
    "allowedCreatorRoleIds": [3],
    "allowedCreatorRoleNames": ["Procurement Officer"]
  }
]
```

---

### `GET /api/purchase-order-types/{id}`

Returns a single PO type.

**Response `200 OK`** — same shape as list item.

**Errors**: `404`

---

### `POST /api/purchase-order-types`

Creates a PO type with its approval steps and allowed creator roles. **Admin-tier only.**

**Request body**

```json
{
  "name": "Standard Procurement",    // string, required, max 256
  "isActive": true,
  "approvalSteps": [
    {
      "requiredRoleId": 2,           // int? — exactly one of requiredRoleId/requiredUserId must be set
      "requiredUserId": null,        // int?
      "sequenceOrder": 0             // int
    }
  ],
  "allowedCreatorRoleIds": [3]       // int[]
}
```

**Response `201 Created`**

**Errors**: `403`, `404`, `422` (step XOR rule violated).

---

### `PUT /api/purchase-order-types/{id}`

Full replace of the type (name, active flag, all approval steps, all allowed creator roles). **Admin-tier only.** Existing POs that already generated their approval rows are unaffected.

**Request body**: same shape as create.

**Response `200 OK`**

**Errors**: `403`, `404`, `422`.

---

### `DELETE /api/purchase-order-types/{id}`

Soft-deletes a PO type. **Admin-tier only.**

**Response `204 No Content`**

**Errors**: `403`, `404`.
