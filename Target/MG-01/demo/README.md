# MG-01 customer demo – Transaction Type Maintenance

The legacy CICS transactions CTLI (COTRTLIC) and CTTU (COTRTUPC) running as a React UI, a .NET 9 Web API and PostgreSQL.

## Option A – one command (Docker)

Prerequisite: Docker Desktop or Docker Engine with Compose v2.

```bash
git clone https://github.com/TCS-Devin-Sandbox/ts-cobol-carddemo.git
cd ts-cobol-carddemo
git checkout devin/1795154209-cics-modernization-mg01
./cics-modernization/Target/MG-01/demo/start-demo.sh
```

Open http://localhost:5173/transaction-types. Ports can be changed with `UI_PORT`, `API_PORT` and `DB_PORT`.
On first run, `demo/.env` is created with random local database passwords; it is not committed.

Stop the demo with `docker compose -f cics-modernization/Target/MG-01/demo/docker-compose.yml down`. Add `-v` to reset the data to the seven seed rows.

## Option B – without Docker for the app (needs .NET 9 SDK, Node 20+, PostgreSQL 15)

1. Create database `carddemo`, then run `Artefacts/DDL/MG-01-TransactionTypeMaintenance-schema.sql` and
   `psql -v app_password=<pwd> -d carddemo -f Target/MG-01/Backend/db/least-privilege-app-role.sql`.
2. API:
   `cd Target/MG-01/Backend/TransactionTypeService/src/TransactionTypeService.Api`, then
   `ConnectionStrings__CardDemo="Host=localhost;Port=5432;Database=carddemo;Username=carddemo_app;Password=<pwd>" Auth__DevBypass=true dotnet run`
   (the API listens on http://localhost:5000).
3. UI: `cd Target/MG-01/Frontend && cp .env.example .env && npm ci && npm run dev` (http://localhost:5173).

## Suggested demo script (about 5 minutes)

| Step | Show | Legacy equivalent |
|---|---|---|
| 1 | The list loads 7 rows per page; use Next and Previous | CTLI keyset paging with PF7/PF8 |
| 2 | Filter by type code `5`, then by description `PAY`; try `AB` to see the inline validation error | Filter edits on COTRTLI |
| 3 | Edit a row inline, Save, and confirm the dialog | `U` row selector + PF10 confirmation |
| 4 | **Add transaction type**: submit an empty form (both errors appear), then create `08 Refund` | CTTU add path |
| 5 | Try to add `01` again: duplicate error with a link to the existing record | Duplicate key handling |
| 6 | Open `08`, change the description, save, then delete it | CTTU update and delete with confirmation |
| 7 | Optional: `docker compose logs api | grep Audit` shows the audit trail with before and after values | — |

## Notes

- The demo uses the local development token (`Auth:DevBypass`, allowed only in Development). Production uses OAuth 2.0 Authorization Code + PKCE: set `VITE_OIDC_AUTHORITY` and `VITE_OIDC_CLIENT_ID` for the UI, and `Auth__Authority` and `Auth__Audience` for the API.
- The API connects as the least-privilege `carddemo_app` login, never as the database owner.
- The OpenAPI document is at http://localhost:5000/openapi/v1.json (Development only).
