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
- `ConnectionStrings.BibDedupeDb`: SQL Server connection string for the app's main data access and Hangfire persistence.
- `ConnectionStrings.BibDedupeBatchDb`: SQL Server connection string used specifically for batch/stored-procedure execution. This is required whenever `BibDedupeDb` is configured.
- `PairAssignmentCleanup.MinimumAssignmentAge`: Age threshold for cleaning stale assignments.
- `AuthorizedUsers`: Optional list of allowed user emails (if omitted/empty, SQL-based auth service is used).
- `LeapBibLinkFormat`: URL format for rendering bib links.
- `Papi`: Polaris API credentials/settings (optional; if missing, app falls back to bundled XML test records).

### 3) Initialize the database schema

Run the initialization script against your SQL Server database:

```powershell
# Example using sqlcmd
sqlcmd -S <server> -i sql/BibDedupe_Initialize.sql
```

This script creates the `clcdb` database if needed, switches context to `clcdb`, and then creates/updates the `BibDedupe` schema, tables, constraints, and indexes in an idempotent way.

### 4) (Optional) Populate candidate pairs

Use `sql/populate_pairs.sql` to generate duplicate candidate pairs from Polaris bibliographic tables.

> Note: this script expects a Polaris SQL schema and source bibliographic tables to be available.

### 5) Restore dependencies and run

```powershell
dotnet restore src/Clc.BibDedupe.sln
dotnet run --project src/Clc.BibDedupe.Web/Clc.BibDedupe.Web.csproj
```

By default, development profiles run on:

- `http://localhost:5119`
- `https://localhost:7077`

Open the app in your browser and sign in with an authorized account.

## Running tests

```powershell
dotnet test src/Clc.BibDedupe.sln
```

## Configuration behavior notes

- **No DB connection string**: app uses in-memory/session services and test pair data.
- **No batch DB connection string while `BibDedupeDb` is configured**: app startup fails with an invalid configuration error.
- **No PAPI settings**: app uses local test XML records instead of Polaris API.
- **Authorized users list empty**: app uses SQL-backed authorization service.

## Deployment notes

For production deployments, make sure to:

- Provide secure secrets management for `settings.json` values (or override with environment variables).
- Use a persistent SQL Server for both app data and Hangfire storage.
- Configure HTTPS, reverse proxy settings, and host-level logging/monitoring.
