# Developer Setup

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 8.0 | `dotnet --version` should print `8.x.x` |
| PostgreSQL | 14+ | Local or Docker. Extension `citext` must be available (bundled with standard PostgreSQL). |
| `dotnet-ef` CLI | Latest 8.x | Used to run and create migrations. |

### Install the EF Core CLI tool

```bash
dotnet tool install --global dotnet-ef
```

If it is already installed, update it:

```bash
dotnet tool update --global dotnet-ef
```

---

## Clone and restore

```bash
git clone <repo-url>
cd purchase_order_management/purchase_order_management_api
dotnet restore
```

---

## Database setup

### Create the database and user

The application expects a PostgreSQL database named `pom_db` accessed by user `pom_user`. The exact connection string from `appsettings.json` is:

```
Host=localhost;Database=pom_db;Username=pom_user
```

Create them in `psql` (as a superuser):

```sql
CREATE USER pom_user WITH PASSWORD '<your-password>';
CREATE DATABASE pom_db OWNER pom_user;
\c pom_db
CREATE EXTENSION IF NOT EXISTS citext;
```

The `citext` extension is required for case-insensitive email uniqueness on the `Users` table. EF migrations will fail without it.

### Run migrations

From the `purchase_order_management_api` directory (the directory that contains `PurchaseOrderManagement.sln`):

```bash
dotnet ef database update --project PurchaseOrderManagement.Api
```

This applies all pending migrations in `PurchaseOrderManagement.Api/Migrations/`.

---

## Running the API

### Development (with hot reload)

```bash
dotnet watch run --project PurchaseOrderManagement.Api
```

### Standard run

```bash
dotnet run --project PurchaseOrderManagement.Api
```

### Default ports (from `launchSettings.json`)

| Profile | URL |
|---|---|
| `https` | `https://localhost:29739` (primary) and `http://localhost:28957` |
| `http` | `http://localhost:29739` |

The `https` profile is the default when running `dotnet run`. The app redirects HTTP to HTTPS automatically.

On first startup, Swagger UI is available at `/swagger`.

---

## Build

```bash
dotnet build PurchaseOrderManagement.Api
```

---

## Configuration

### `appsettings.json` (checked in, safe defaults)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=pom_db;Username=pom_user"
  },
  "Seed": {
    "AdminEmail": "admin@local"
  },
  "FileStorage": {
    "BaseUrl": "https://localhost:5001",
    "Path": "App_Data/uploads"
  }
}
```

### `appsettings.Development.json` (checked in, dev log levels)

Only overrides logging levels. No connection string or secret overrides here.

### Overriding for your local environment

Use user secrets so you do not commit credentials:

```bash
cd PurchaseOrderManagement.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=pom_db;Username=pom_user;Password=<your-password>"
dotnet user-secrets set "Seed:AdminPassword" "<desired-admin-password>"
```

Or set environment variables (useful in CI or Docker):

```bash
export ConnectionStrings__Default="Host=localhost;Database=pom_db;Username=pom_user;Password=<pw>"
export Seed__AdminPassword="<pw>"
```

### Configuration keys reference

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Npgsql connection string |
| `Seed:AdminEmail` | Email of the seeded Super Admin user. Defaults to `admin@local`. |
| `Seed:AdminPassword` | Password for the seeded Super Admin user. If not set, falls back to `ChangeMe!123` and logs a warning — change this in any non-local environment. |
| `FileStorage:BaseUrl` | Base URL prepended when resolving `SourceType.Path` files (e.g. `https://api.example.com`). |
| `FileStorage:Path` | Local directory for uploaded files. Relative to the application content root. Defaults to `App_Data/uploads`. |

---

## Seed data

The `DataSeeder` runs automatically on every startup (idempotent). It creates the following rows if they do not already exist:

- Currency: `ZMW` (Zambian Kwacha), active.
- Company: `Head Office` (root company, `ParentCompanyId` null).
- Role: `Super Admin` (root role, `ParentRoleId` null, `IsSystemRole` true).
- User: the email from `Seed:AdminEmail`, assigned the `Super Admin` role.

Use the seeded admin credentials to log in and bootstrap further data.
