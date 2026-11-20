# MG-01 — Run Guide (Transaction Type Maintenance)

## Overview
Starts the Transaction Types screens: React + Tailwind (Vite) frontend → ASP.NET Core 9 Web API (C#) → PostgreSQL `carddemo`.

## Prerequisites
- .NET SDK 9.0 (`dotnet --version` → 9.x)
- Node.js 20+ and npm
- Docker (for the local PostgreSQL 15 container from Phase 5) or any PostgreSQL 15 instance
- Values to supply: `<DB_PASSWORD>` for the `carddemo` user (local container: set when the container was created)

## Database
1. Start the Phase 5 container if it is stopped: `docker start cics-mg-postgres` (listens on `127.0.0.1:5433`).
2. Check the schema: from `cics-modernization/`, with `DB_HOST=127.0.0.1 DB_PORT=5433 DB_NAME=carddemo DB_USER=carddemo DB_PASSWORD=<DB_PASSWORD>` exported, run `node tools/db-apply.mjs --dialect postgresql --list-tables` → `transaction_type` is listed.
3. On a fresh database, provision with `node tools/db-apply.mjs --dialect postgresql --create-database` then `--sql-file Artefacts/DDL/MG-01-TransactionTypeMaintenance-schema.sql`.

## Backend
1. `cd Target/MG-01/Backend/TransactionTypeService/src/TransactionTypeService.Api`
2. Set environment:
   - `ConnectionStrings__CardDemo="Host=127.0.0.1;Port=5433;Database=carddemo;Username=carddemo;Password=<DB_PASSWORD>"`
   - `ASPNETCORE_ENVIRONMENT=Development`
   - `Auth__DevBypass=true` (local only, accepts `dummy-dev-token`)
   - `ASPNETCORE_URLS=http://localhost:5000`
3. `dotnet run` → API at `http://localhost:5000`, health at `http://localhost:5000/health`.

## Frontend
1. `cd Target/MG-01/Frontend`
2. `cp .env.example .env` and keep `VITE_AUTH_BYPASS=true`, `VITE_API_BASE=/api/v1`.
3. `npm install`
4. `npm run dev` → `http://localhost:5173` (Vite proxies `/api` → `http://localhost:5000`).

## Verify the screen
1. Open `http://localhost:5173/transaction-types` — seed codes 01–07 are listed (no login needed with dev bypass).
2. Search Type code `05` → only REFUND is shown.
3. Click Edit on 05, change the description, Save → "Transaction type 05 updated." and the new text persists after reload.

## Start/Stop order & Troubleshooting
- Start: database → backend → frontend. Stop: Ctrl+C the Vite and `dotnet run` processes; `docker stop cics-mg-postgres`.
- **Port in use (5000/5173/5433):** `lsof -i :5000` and stop the other process, or change `ASPNETCORE_URLS` / Vite `server.port` and the proxy target together.
- **503 / DB not reachable:** container not running or wrong password in `ConnectionStrings__CardDemo`; check `/health`.
- **401 in the UI:** backend not in Development or `Auth__DevBypass` not set while the UI sends `dummy-dev-token`.
- **CORS error:** call the API through the Vite proxy (`/api/...`), not `http://localhost:5000` directly from the browser.
