# MG-02 — Run Guide

## Overview
This guide starts the CardDemo shell (sign-in, customer menu, admin menu) on React/Vite + ASP.NET Core 9 (SignOnService, plus MG-01 TransactionTypeService) + PostgreSQL 15.

## Prerequisites
- Docker with Compose v2 for the one-command demo.
- Or, for running locally: .NET SDK 9, Node 20 and PostgreSQL 15.
- `<DB_PASSWORD>`: the owner password for the local `carddemo` database.

## Database
1. Start PostgreSQL on 127.0.0.1:5433 with database `carddemo` (see the MG-01 run guide).
2. Load `Artefacts/DDL/MG-01-TransactionTypeMaintenance-schema.sql` and `Artefacts/DDL/MG-02-SignOnAndNavigationShell-vsam-schema.sql`, which seeds 10 users.
3. Create the runtime logins with `Target/MG-01/Backend/db/least-privilege-app-role.sql` and `Target/MG-02/Backend/db/least-privilege-signon-role.sql`.

## Backend
1. SignOnService:
   ```
   cd Target/MG-02/Backend/SignOnService
   ConnectionStrings__SignOn="Host=127.0.0.1;Port=5433;Database=carddemo;Username=carddemo_signon;Password=<pw>" \
   ASPNETCORE_ENVIRONMENT=Development Auth__Issuer=http://localhost:5001 \
   dotnet run --project src/SignOnService.Api --urls http://localhost:5001
   ```
2. TransactionTypeService (MG-01), set to trust SignOnService:
   ```
   Auth__DevBypass=false Auth__Authority=http://localhost:5001 Auth__Audience=carddemo-api \
   dotnet run --project ../../../MG-01/Backend/TransactionTypeService/src/TransactionTypeService.Api --urls http://localhost:5000
   ```

## Frontend
```
cd Target/MG-02/Frontend && npm ci && npm run dev    # http://localhost:5173
```
Vite sends `/api/v1/auth` and `/api/v1/menus` to port 5001, and `/api/v1/transaction-types` to port 5000.

## Verify the screen
1. Open http://localhost:5173/signin.
2. Sign in as `ADMIN001` / `PASSWORD`. You should land on the Administration menu.
3. Click "Transaction type list". You should see the 7 seeded types.
4. Sign out, then sign in as `USER0001` / `PASSWORD`. You should see the Customer services menu with 11 tiles.

## Start/Stop order & Troubleshooting
- **Start order:** database → SignOnService → TransactionTypeService → UI.
- **Stop:** Ctrl+C in each terminal, or `docker compose down`.
- **401 from MG-01:** check that `Auth:Authority` points at SignOnService and that its `Auth:Issuer` matches that URL exactly.
- **Port in use:** free ports 5000, 5001 and 5173.
- **Database unreachable:** check port 5433 and the runtime role passwords.
