# CLC Bib Dedupe

CLC Bib Dedupe is an ASP.NET Core web application for reviewing likely duplicate bibliographic records and recording deduplication decisions. It presents candidate record pairs, allows staff to select actions (for example, keep left/right, not duplicate, or skip), and tracks decision processing history.

## What the application does

- Loads candidate duplicate record pairs from SQL Server (or local test data when a DB is not configured).
- Displays side-by-side bibliographic records for manual review.
- Lets authenticated/authorized users submit dedupe decisions.
- Stores decisions and batch processing metadata in SQL Server.
- Runs background processing/cleanup jobs with Hangfire.

## Tech stack

- .NET 8 (ASP.NET Core MVC)
- SQL Server (application data + Hangfire storage in persistent mode)
- Microsoft Identity (Azure AD / Entra ID sign-in)
- Hangfire (background jobs)

## Prerequisites

Install these before setting up your own environment:

1. **.NET SDK 8.0+**
2. **SQL Server** instance (local, containerized, or hosted)
3. **Azure AD / Entra ID app registration** for OpenID Connect login
4. (Optional) **Polaris PAPI credentials** to load live MARC data

## Installation and local setup

### 1) Clone and enter the repository

```powershell
git clone <your-repo-url>
Set-Location bib-dedupe
```

### 2) Create application configuration

The app reads custom settings from `src/Clc.BibDedupe.Web/Config/settings.json`.

Create this from the provided template:

```powershell
Copy-Item src/Clc.BibDedupe.Web/Config/settings.json.template src/Clc.BibDedupe.Web/Config/settings.json
```

Then edit `settings.json` with your values:

- `AzureAd`: Tenant/client settings for authentication.
- `ConnectionStrings.BibDedupeDb`: SQL Server connection string.
- `PairAssignmentCleanup.MinimumAssignmentAge`: Age threshold for cleaning stale assignments.
- `AuthorizedUsers`: Optional list of allowed user emails (if omitted/empty, SQL-based auth service is used).
- `LeapBibLinkFormat`: URL format for rendering bib links.
- `Papi`: Polaris API credentials/settings (optional; if missing, app falls back to bundled XML test records).

### 3) Configure Azure AD / Entra ID authentication

Create or reuse an Entra app registration for the web app:

1. In Azure Portal, go to **Microsoft Entra ID → App registrations → New registration**.
2. Choose the supported account type your org needs (typically single-tenant).
3. Under **Redirect URI**, add a **Web** URI for local dev:
   - `https://localhost:7077/signin-oidc`
4. After creation, copy:
   - **Application (client) ID** → `AzureAd.ClientId`
   - **Directory (tenant) ID** → `AzureAd.TenantId`
5. Set `AzureAd.Instance` to `https://login.microsoftonline.com/`.
6. Set `AzureAd.CallbackPath` to `/signin-oidc`.
7. Set `AzureAd.Domain` to your tenant's primary domain (for example, `contoso.onmicrosoft.com`).

Minimal `AzureAd` block:

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "YOUR_DOMAIN",
  "TenantId": "YOUR_TENANT_ID",
  "ClientId": "YOUR_CLIENT_ID",
  "CallbackPath": "/signin-oidc"
}
```

> Tip: Keep secrets out of source control. Prefer user-secrets, environment variables, or your deployment platform's secret store.

### 4) Initialize the database schema

Run the initialization script against your SQL Server database:

```powershell
# Example using sqlcmd
sqlcmd -S <server> -d <database> -i sql/BibDedupe_Initialize.sql
```

This script creates/updates the `BibDedupe` schema, tables, constraints, and indexes in an idempotent way.

### 5) Configure authorization model (roles/claims)

The app has one authorization policy (`AuthorizedUser`) that grants access when the signed-in user has either the `Access` role or the `Administrator` role.

You can populate those roles in one of two ways:

#### Option A: `AuthorizedUsers` list in `settings.json`

If `AuthorizedUsers` contains one or more emails, each listed user is automatically treated as both:

- `Access`
- `Administrator`

This is easiest for local development/smaller teams, but it does **not** let you differentiate normal reviewers from admins.

Example:

```json
"AuthorizedUsers": [
  "reviewer1@contoso.org",
  "admin1@contoso.org"
]
```

#### Option B: SQL-backed claims via `BibDedupe.UserClaims`

If `AuthorizedUsers` is empty or omitted, authorization is read from SQL table `BibDedupe.UserClaims`.

- `UserEmail` is the signed-in email.
- `Claim` is a role string (use exact values shown below).

Example seed data:

```sql
INSERT INTO BibDedupe.UserClaims (UserEmail, Claim)
VALUES
  ('reviewer1@contoso.org', 'Access'),
  ('admin1@contoso.org', 'Access'),
  ('admin1@contoso.org', 'Administrator');
```

#### What each role controls

- `Access`
  - Can use the main dedupe app (review queue, decisions, submission).
- `Administrator`
  - Can use the main dedupe app.
  - Can access Hangfire dashboard at `/hangfire`.

In other words, `Administrator` is effectively a superset because it satisfies app access checks and is specifically required for Hangfire dashboard access.

#### Identity/email claim note

The app matches users by email claim from the signed-in identity. It checks:

1. `email` claim (`ClaimTypes.Email`), then
2. `preferred_username`

Make sure your Entra tokens provide one of those values in a format that matches `AuthorizedUsers` or `BibDedupe.UserClaims.UserEmail`.

### 6) (Optional) Populate candidate pairs

Use `sql/populate_pairs.sql` to generate duplicate candidate pairs from Polaris bibliographic tables.

> Note: this script expects a Polaris SQL schema and source bibliographic tables to be available.

### 7) Restore dependencies and run

```powershell
dotnet restore src/Clc.BibDedupe.sln
dotnet run --project src/Clc.BibDedupe.Web/Clc.BibDedupe.Web.csproj
```

By default, development profiles run on:

- `https://localhost:7077`

Open the app in your browser and sign in with an authorized account.

## Running tests

```powershell
dotnet test src/Clc.BibDedupe.sln
```

## Configuration behavior notes

- **No DB connection string**: app uses in-memory/session services and test pair data.
- **No PAPI settings**: app uses local test XML records instead of Polaris API.
- **Authorized users list empty**: app uses SQL-backed authorization service.

## Deployment notes

For production deployments, make sure to:

- Provide secure secrets management for `settings.json` values (or override with environment variables).
- Use a persistent SQL Server for both app data and Hangfire storage.
- Configure HTTPS, reverse proxy settings, and host-level logging/monitoring.
