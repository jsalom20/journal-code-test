# Frenda Library Code Test

Fullstack library circulation app built with:

- `.NET 10`
- `ASP.NET Core Web API`
- `Entity Framework Core`
- `SQLite`
- `Next.js 16`

## What is implemented

- Search books and see copy availability
- Borrow a physical copy
- View active and overdue loans
- Return a loaned copy
- Manage reservations
- Manage borrowers
- Manage copy status, condition, and shelf location
- Track copy-level audit events
- Seed realistic library data for local review

## Repo layout

- `src/apps/server`: ASP.NET Core API, EF Core data model, seed logic, tests
- `src/apps/client/journal-code-test`: Next.js frontend

## Run locally

### 1. Start the backend

```bash
cd src/apps/server
dotnet run --project Library.Api/Library.Api.csproj
```

API default URL:

- `http://localhost:5031`

The backend creates and seeds the SQLite database automatically on first run.

### 2. Start the frontend

In a second terminal:

```bash
cd src/apps/client/journal-code-test
npm run dev
```

Frontend default URL:

- `http://localhost:3000`

If needed, override the API base URL with:

```bash
NEXT_PUBLIC_LIBRARY_API_BASE_URL=http://localhost:5031
```

## Verification used

Backend:

- `dotnet build Library.Api/Library.Api.csproj --no-restore -m:1 -v minimal`
- `dotnet test Library.Tests/Library.Tests.csproj --no-build -m:1 -v minimal`
- live endpoint check against `http://localhost:5031/api/dashboard/summary`

Frontend:

- `npm run typecheck`
- `npm run build`

## Notes

There is a short implementation note about modern C# pattern matching versus EF Core expression tree translation in:

- `summary.md`
