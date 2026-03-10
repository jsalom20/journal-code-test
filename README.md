# Frenda Library Code Test - Developed by Codex - GPT 5.4

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

## First-time setup

### Prerequisites

- `.NET 10 SDK`
- `Node.js 22+`
- `npm`

### 1. Clone and enter the repo

```bash
git clone <your-repo-url>
cd journal-code-test
```

### 2. Restore backend dependencies

```bash
cd src/apps/server
dotnet restore
```

### 3. Install frontend dependencies

In a second terminal, or after returning to the repo root:

```bash
cd src/apps/client/journal-code-test
npm install
```

### 4. Start the app

Run the backend first. On first start it applies migrations, creates the SQLite database, and seeds demo data automatically.

## Run locally

### 1. Start the backend

```bash
cd src/apps/server
dotnet run --project Library.Api/Library.Api.csproj
```

API default URL:

- `http://localhost:5031`

The backend creates and seeds the SQLite database automatically on first run.

### Reset the local database

If you want to wipe the local development database and reseed from scratch, run:

```bash
./scripts/reset-library-db.sh
```

This deletes the local SQLite files and runs the backend in a one-off `--seed-only` mode so migrations and seed data are applied without leaving the API running.

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

## Prompt that was used to generate this project

Library prompt

ok so we have a new code test for a library app

Kodtest: Fullstackutvecklare
Syfte
I den här rollen jobbar du end-to-end: du bygger en liten produkt med ett backend-API och ett enkelt
gränssnitt. Kodtestet är designat för att visa hur du tänker kring struktur, datamodellering, API-design
och användarflöden.
Förutsättningar
• Språk: C#
• Backend: .NET med Entity Framework
• Frontend: Next.js
Uppgift
Bygg en enkel boklåningsapplikation (”bibliotek”) för en användare. Lösningen ska göra det möjligt att:
• Söka bland böcker och se vilka som är tillgängliga
• Låna en bok
• Se sina aktuella lån
• Lämna tillbaka en bok
Du väljer själv hur du modellerar böcker/exemplar, hur du identifierar användaren och hur du designar
API:t.
Det är helt okej att förpopulera databasen. Fokus är på låneflödet och API/UI-flöden.
Skicka in
Mejla ditt kodtest som en länk till ett github-repo senast 24h innan intervjun till
annie.astrom@frenda.se
Bedömning
Vi tittar främst på helheten och dina avvägningar: struktur, robusthet i låneflödet, API/UI-sammanhang,
och hur enkelt det är att förstå och köra.
Tidsriktmärke: bygg något som känns klart inom några timmar.
Kodtest – Fullstack (.NET + EF + Next.js) Sida 1


so we have a folder structure already in the repo where we are separating the backend and next.js client. now, we want to think like a senior backend and frontend dev. we want a really solid table structure that can scale a library with thousands of unique books with multiple copies of each book. traceability is important when you are running a library with physical books. for the frontend we are running next.js. we have a next.js starter project here but we will continue to build on it.

let me know if im missing anything. i know we need to have a page for searching for books, managing borrowers, managing books that are borrowed and status of thise books. and any other data and management we would need for a library. no idea or feature is too small for us, remember that

make this a plan
