# Auth endpoints

## Common conventions

Every endpoint requires an authenticated session cookie (`pom_session`) unless noted as **[Public]**. Unauthenticated requests return `401 Unauthorized`. All responses are JSON.

Error responses use the RFC 7807 `ProblemDetails` format:

```json
{ "status": 422, "title": "Unprocessable Entity", "detail": "..." }
```

---

### `POST /api/auth/login` [Public]

Validates credentials and issues a `pom_session` cookie (httpOnly, Secure, SameSite=Strict, 8-hour sliding expiration).

**Request body**

```json
{
  "email": "admin@local",    // string, required, valid email
  "password": "ChangeMe!123" // string, required
}
```

**Response `200 OK`**

```json
{
  "id": 1,
  "fullName": "Super Admin",
  "email": "admin@local",
  "companyId": 1,
  "roles": ["Super Admin"]
}
```

**Errors**: `401` — invalid credentials.

---

### `POST /api/auth/logout`

Invalidates the current session server-side and clears the cookie.

**Response `204 No Content`**

---

### `GET /api/auth/me`

Returns the identity of the currently authenticated user from session claims (no DB lookup).

**Response `200 OK`** — same shape as login response.

**Errors**: `401` — not authenticated.
