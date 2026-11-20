# MG-01 — Backend Build Spec (Refactor)

## 1. Spec Metadata & Traceability
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Covers:** CTLI / COTRTLIC (list, filter, inline update, delete) and CTTU / COTRTUPC (lookup, add, update, delete).
- **Component-wise target stack** (`config/Config.md`): API layer = REST APIs, API gateway (`target.api.layer`); backend = ASP.NET Core Web API (`target.backend.framework`); database = Postgres (`target.database.engine`); cache = Redis (`target.cache.store`, not needed for MG-01); messaging = Kafka/RabbitMQ (`target.messaging.intra`, not needed); architecture = Microservice-based; auth = OAuth 2.0 (`target.security.auth.protocol`), provider `http://localhost:8081`; transport = HTTPS/TLS 1.3; port 5000 (`target.backend.port`); base URL `http://localhost:5000`.
- **Mandatory backend language:** `.NET Core 9 (C#)` (`target.backend.language`), object-oriented (controllers, services, repositories, domain types), not a procedural translation of the COBOL.
- **Sources read:** `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-BSTS.md`, `-BDD.md`, `-DB-Details.md`; `Artefacts/Specs/Refactor/MG-01-UI-Spec.md`; legacy `Input/cbl/COTRTLIC.cbl` (0000-MAIN, 1210–1290, 8000/8100, 9100–9550), `Input/cbl/COTRTUPC.cbl` (0000-MAIN, 0001-CHECK-PFKEYS, 1200–1245, 9100, 9600–9800), `Input/cpy/CSDB2RPY.cpy` (9998-PRIMING-QUERY, 9999-FORMAT-DB2-MESSAGE), `Input/ddl/TRNTYPE.ddl`, `Input/ddl/TRNTYCAT.ddl`.
- **Frontend build files consulted:** none — no frontend build exists yet for MG-01 in this workspace; the UI spec is the frontend contract.

## 2. Business Context & Scope
Administrators maintain the transaction-type reference table: a 2-digit code and a description of up to 50 characters (BSTS a.). In scope for this service:
1. List types in code order, 7 per page, with keyset paging forward and backward (COTRTLIC 8000/8100).
2. Filter by exact type code and/or description contains (COTRTLIC cursor predicates, 9100-CHECK-FILTERS).
3. Read one type by code (COTRTUPC 9100-GET-TRANSACTION-TYPE).
4. Create a type (COTRTUPC 9700-INSERT-RECORD).
5. Update a type's description (COTRTLIC 9200-UPDATE-RECORD, COTRTUPC 9600-WRITE-PROCESSING).
6. Delete a type (COTRTLIC 9300-DELETE-RECORD, COTRTUPC 9800-DELETE-PROCESSING).
Out of scope: navigation to the admin menu (MG-02), any other table.

## 3. Service & API Specification
Implementation language: **C# on .NET 9**, ASP.NET Core Web API controllers. One microservice, `TransactionTypeService`, listening on port **5000**, routes under `/api/v1`. All endpoints require a bearer token (Section 8). JSON uses camelCase. Errors use RFC 7807 `application/problem+json` with extra members `code` (machine code) and `errors` (field → messages).

| # | Method & path | Purpose | Request | Response (success) | Status codes | UI action | Legacy origin |
|---|---|---|---|---|---|---|---|
| E1 | `GET /api/v1/transaction-types` | List / filter / page | query: `typeCode` (string, opt), `description` (string, opt), `after` (2-digit code, opt), `before` (2-digit code, opt), `pageSize` (int, default 7, 1–50) | `{ items: [{ typeCode, description }], firstCode, lastCode, hasNext, hasPrevious, filtered }` | 200, 400, 401, 403, 503 | S1 open, Search, Clear, Next, Previous | COTRTLIC 8000-READ-FORWARD, 8100-READ-BACKWARDS, 1220/1230/1290, 9100 |
| E2 | `GET /api/v1/transaction-types/{typeCode}` | Read one | path `typeCode` | `{ typeCode, description }` | 200, 400, 404, 401, 403, 503 | S3 open, Look up | COTRTUPC 9000/9100 |
| E3 | `POST /api/v1/transaction-types` | Create | `{ typeCode: string (req), description: string (req) }` | 201 + `Location: /api/v1/transaction-types/{code}` + `{ typeCode, description }` | 201, 400, 409, 401, 403, 503 | S2 Create | COTRTUPC 9700-INSERT-RECORD |
| E4 | `PUT /api/v1/transaction-types/{typeCode}` | Update description | `{ description: string (req) }` | 200 `{ typeCode, description }` | 200, 400, 404, 409, 422, 401, 403, 503 | S1 row Save, S3 Save changes | COTRTLIC 9200-UPDATE-RECORD; COTRTUPC 9600 |
| E5 | `DELETE /api/v1/transaction-types/{typeCode}` | Delete | path `typeCode` | 204 | 204, 400, 404, 409, 401, 403, 503 | S1 row Delete, S3 Delete | COTRTLIC 9300; COTRTUPC 9800 |
| E6 | `GET /health` | Liveness + DB check (anonymous) | — | `{ status: "Healthy" }` | 200, 503 | — (ops) | CSDB2RPY 9998-PRIMING-QUERY |

**Add vs Update are separate:** POST only inserts (409 if the code exists); PUT only updates (404 if missing). The legacy CTTU save is an UPDATE-then-INSERT upsert (COTRTUPC 9600 → 9700 on SQLCODE +100); see Section 11 deviation D1.

**E1 paging contract (keyset, mirrors legacy cursors):**
- No `after`/`before`: first page, `ORDER BY tr_type ASC LIMIT pageSize+1`.
- `after=X`: `tr_type > X ORDER BY tr_type ASC LIMIT pageSize+1`. (Legacy re-opens the forward cursor at the 8th row's key with `>=`; `> 7th key` is equivalent.)
- `before=X`: `tr_type < X ORDER BY tr_type DESC LIMIT pageSize`, then reversed to ascending (legacy C-TR-TYPE-BACKWARD).
- `hasNext` = an extra row exists beyond the page (legacy look-ahead FETCH, CA-NEXT-PAGE-EXISTS); for `before` requests `hasNext` = true.
- `hasPrevious` = a row exists with `tr_type < firstCode` under the same filters.
- `filtered` = true when a type or description filter was applied.

## 4. Business Rules & Functional Equivalence
| # | Legacy source | Legacy behaviour | Target behaviour |
|---|---|---|---|
| R1 | COTRTLIC `WS-MAX-SCREEN-LINES` = 7 | 7 rows per page | default `pageSize` = 7 |
| R2 | C-TR-TYPE-FORWARD `ORDER BY TR_TYPE` | ascending code order | ascending order for every page |
| R3 | 1220-EDIT-TYPECD | blank/spaces/zeros type filter → no filter | `typeCode` empty, "0" or "00" → filter not applied |
| R4 | 1220-EDIT-TYPECD | non-numeric type filter → error | 400 `TYPE_FILTER_INVALID` "Type code must be a 2-digit number." |
| R5 | cursor predicate `TR_TYPE = :WS-TYPE-CD-FILTER` | exact match | `tr_type = normalised code` (1 digit left-padded with "0") |
| R6 | 1230-EDIT-DESC `STRING '%' TRIM(desc) '%'`, `LIKE` | contains, case-sensitive | `tr_description LIKE '%' || trimmed || '%'` (case-sensitive); `%`, `_`, `\` in input escaped so they match literally |
| R7 | 1290-CROSS-EDITS / 9100-CHECK-FILTERS `COUNT(1) = 0` | filters with no match → error "No Records found for these filter conditions" | 200 with `items: []`, `filtered: true`; UI shows the error (reconcile spec) |
| R8 | 8000-READ-FORWARD, screen 1 row 0 | empty table → "No records found for this search condition." | 200 with `items: []`, `filtered: false` |
| R9 | 8000 look-ahead FETCH | next page exists flag | `hasNext` |
| R10 | 2500-SETUP-MESSAGE (PF7 on first page) | "No previous pages to display" | `hasPrevious` = false; `before` on first page returns the first page |
| R11 | 1245-EDIT-NUM-REQD | type code required, numeric, non-zero | POST/path validation (Section 5) |
| R12 | 1210-EDIT-TRANTYPE (NUMVAL, zero-pad) | "5" → "05" | normalise to 2 digits before any DB access |
| R13 | 1240/1230-EDIT-ALPHANUM-REQD | description required; letters/digits/spaces only | Section 5 rules |
| R14 | 1211-EDIT-ARRAY-DESC / 1205-COMPARE-OLD-NEW | update with no change (case- and trim-insensitive) → "No change detected …" | PUT compares `upper(trim(new))` with `upper(trim(stored))`; equal → 422 `NO_CHANGES` "No change detected with respect to database values." |
| R15 | 9200 / 9600 `TRIM` into DCL-TR-DESCRIPTION | stored trimmed | description trimmed before INSERT/UPDATE |
| R16 | 9200 / 9300 / 9600 / 9800 `SYNCPOINT` on success | commit per operation | each write runs in its own DB transaction, committed on success, rolled back on error |
| R17 | 9200 SQLCODE +100 | update of missing row → "Record not found. Deleted by others ?" | 404 `TYPE_NOT_FOUND` "Transaction type 05 was not found. It may have been deleted by someone else." |
| R18 | 9200/9600 SQLCODE -911 | lock/deadlock → "Deadlock. Someone else updating ?" / "Could not lock record for update" | Postgres `40P01` / `55P03` / `40001` → 409 `RECORD_LOCKED` "The record is being changed by someone else. Try again." |
| R19 | 9300/9800 SQLCODE -532 | delete blocked by child rows → "Please delete associated child records first:" | Postgres `23503` → 409 `HAS_DEPENDENT_RECORDS` "Please delete associated child records first." |
| R20 | 9100 (COTRTUPC) SQLCODE +100 | "No record found for this key in database" | E2 → 404 `TYPE_NOT_FOUND` "No transaction type with code 12." |
| R21 | 9700 insert failure | "Error inserting record into: TRANSACTION_TYPE Table…" | duplicate key `23505` → 409 `TYPE_ALREADY_EXISTS` "Transaction type 12 already exists."; other → 500 `INTERNAL_ERROR` |
| R22 | CSDB2RPY 9998-PRIMING-QUERY | Db2 unavailable → plain-text error, conversation ends | connection failure → 503 `DB_UNAVAILABLE` "The service is temporarily unavailable. Try again later."; `/health` reports 503 |
| R23 | 9999-FORMAT-DB2-MESSAGE | SQLCODE + DSNTIAC text shown to user | SQL state and driver text logged server-side only (with trace id); user gets the safe message + `traceId` |
| R24 | 9300 delete success → reset to first page | list restarts at page 1 | DELETE returns 204; client reloads page 1 (reconcile spec) |
| R25 | 0000-MAIN USRTYP-ADMIN on entry | admin function | all MG-01 endpoints require role `carddemo.admin` (Section 8) |

## 5. Server-Side Validation
Validation runs in a dedicated validator class per request DTO (FluentValidation-style rules implemented in C#) before the service layer; failures return 400 `VALIDATION_FAILED` with `errors`.

| Field | Where | Rules | Error message (exact) |
|---|---|---|---|
| `typeCode` | E1 query | optional; trimmed; if present and not "", "0", "00": 1–2 ASCII digits | "Type code must be a 2-digit number." |
| `description` | E1 query | optional; trimmed; ≤ 50 chars | "Description filter can be at most 50 characters." |
| `after`, `before` | E1 query | optional; exactly 2 digits; not both | "Paging key must be a 2-digit code." / "Use either after or before, not both." |
| `pageSize` | E1 query | 1–50 | "Page size must be between 1 and 50." |
| `typeCode` | E2/E4/E5 path, E3 body | required; trimmed; 1–2 ASCII digits; numeric value ≠ 0; normalised to 2 digits | "Type code is required." / "Type code must be numeric." / "Type code must not be zero." |
| `description` | E3, E4 body | required; trimmed length 1–50; regex `^[A-Za-z0-9 ]+$` | "Description is required." / "Description can be at most 50 characters." / "Description can contain only letters, numbers and spaces." |
| body | E3, E4 | JSON object, max 4 KB, unknown members rejected | "Request body is invalid." |

## 6. Data Model & Data Access
- **Table** (from DB-Details b., dialect postgresql, database `carddemo`, schema `public`):

| Column | Type | Null | Key |
|---|---|---|---|
| `tr_type` | `CHAR(2)` | NOT NULL | PK (`transaction_type_pkey`), unique index `xtran_type` |
| `tr_description` | `VARCHAR(50)` | NOT NULL | — |

- **Entity:** `TransactionType { string TypeCode; string Description; }` mapped with `ToTable("transaction_type")`, `HasKey(TypeCode)`, `TypeCode` → `tr_type` `char(2)`, `Description` → `tr_description` `varchar(50)` (`HasMaxLength(50)`). No migrations are run by the service; the schema is owned by Phase 5 (`Artefacts/DDL/MG-01-TransactionTypeMaintenance-schema.sql`).
- **CRUD per endpoint:** E1 R (two queries: page + `hasPrevious` existence); E2 R; E3 C; E4 R + U (load, compare, update); E5 D.
- **Transactions:** E3, E4, E5 each use one `IDbContextTransaction` (Read Committed); E4 loads the row with `SELECT … FOR UPDATE` inside the transaction so the change check and update are atomic (equivalent to the legacy single UPDATE).
- **Data access:** EF Core 9 with LINQ (parameterised SQL only; the LIKE pattern uses `EF.Functions.Like(col, pattern, "\\")`). Layering: Controller → `ITransactionTypeService` → `ITransactionTypeRepository` → `CardDemoDbContext`. Npgsql connection pooling (default pool, `Maximum Pool Size=20`).
- **Provider (Step 4A, dialect `postgresql`):** NuGet `Npgsql.EntityFrameworkCore.PostgreSQL`; connection string format `Host=<host>;Port=<port>;Database=<database>;Username=<user>;Password=<pass>`, supplied via environment variable `ConnectionStrings__CardDemo` (never committed). Local dev value: `Host=127.0.0.1;Port=5433;Database=carddemo;Username=carddemo;Password=<from env>`.

## 7. Error Handling & Edge Cases
| Condition | Source | HTTP | `code` | User-safe message |
|---|---|---|---|---|
| Invalid query/body/path field | 1220, 1240, 1245 | 400 | `VALIDATION_FAILED` / `TYPE_FILTER_INVALID` | per Section 5 |
| Type not found (read) | COTRTUPC 9100 +100 | 404 | `TYPE_NOT_FOUND` | "No transaction type with code {code}." |
| Type not found (update/delete) | COTRTLIC 9200 +100; delete of missing row | 404 | `TYPE_NOT_FOUND` | "Transaction type {code} was not found. It may have been deleted by someone else." |
| Duplicate on create | 9700 insert failure | 409 | `TYPE_ALREADY_EXISTS` | "Transaction type {code} already exists." |
| No change on update | 1211 / 1205 | 422 | `NO_CHANGES` | "No change detected with respect to database values." |
| Lock / deadlock / serialization | 9200 / 9600 -911 | 409 | `RECORD_LOCKED` | "The record is being changed by someone else. Try again." |
| Delete blocked by dependent rows | 9300 / 9800 -532 | 409 | `HAS_DEPENDENT_RECORDS` | "Please delete associated child records first." |
| Database unreachable | 9998-PRIMING-QUERY | 503 | `DB_UNAVAILABLE` | "The service is temporarily unavailable. Try again later." |
| Other DB error | 9200/9300/9600/9800 `WHEN OTHER`, cursor errors 9400–9550 | 500 | `INTERNAL_ERROR` | "Something went wrong. Reference: {traceId}." |
| Missing/invalid token | — | 401 | `UNAUTHENTICATED` | "Sign in to continue." |
| Token without admin role | 0000-MAIN admin context | 403 | `FORBIDDEN` | "You do not have permission to maintain transaction types." |
| Unhandled exception | COTRTUPC ABEND-ROUTINE ("UNEXPECTED DATA SCENARIO") | 500 | `INTERNAL_ERROR` | as above; exception logged with trace id |

Edge cases (BDD e.): empty table (R8); `before` on page 1 returns page 1 with `hasPrevious=false` (R10); exactly 7 remaining rows → `hasNext=false` (R9); update after concurrent delete → 404 (R17); delete of referenced type → 409 (R19); "5" and "05" address the same row (R12).

## 8. Security Specification (mandatory build instruction)
- **OAuth 2.0 + JWT:** `AddAuthentication(JwtBearer)` with `Authority` = `target.auth.provider.url` (`http://localhost:8081`), audience `carddemo-api`; validate issuer, audience, lifetime (clock skew 2 min), signature (JWKS from authority). Authorization policy `TransactionTypeAdmin` requires role claim `carddemo.admin` on E1–E5 (legacy admin-only function, COTRTLIC/COTRTUPC set `CDEMO-USRTYP-ADMIN`). Scope `transaction-types.write` additionally required for E3–E5, `transaction-types.read` for E1–E2.
- **Dev bypass:** only when `ASPNETCORE_ENVIRONMENT=Development` **and** `Auth:DevBypass=true`, a custom authentication handler accepts `Bearer dummy-dev-token` and issues a principal with role `carddemo.admin` and both scopes. The handler is not registered in any other environment; startup fails if `Auth:DevBypass=true` outside Development.
- **TLS 1.3:** outside Development, Kestrel endpoints configure `SslProtocols = Tls13`; `UseHsts()` and `UseHttpsRedirection()`. Local dev may use HTTP on 5000 behind the Vite proxy.
- **OWASP Top 10:**
  - A01 Broken access control: policy on every controller action; no anonymous endpoints except `/health`.
  - A02 Crypto failures: TLS 1.3; no secrets in config files; connection string from environment.
  - A03 Injection: EF Core parameterised queries; LIKE wildcards escaped (R6); strict regex validation.
  - A04 Insecure design: separate POST/PUT; confirmation is a UI step, server validates every write.
  - A05 Misconfiguration: ProblemDetails without stack traces outside Development; security headers (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'`).
  - A07 Auth failures: JWT validation as above; 401/403 with no detail leak.
  - A08 Integrity: request size limit 4 KB; unknown JSON members rejected.
  - A09 Logging: structured logs (Serilog/Microsoft.Extensions.Logging) with user id (`sub`), action, type code, outcome, trace id for E3–E5 (audit trail); no tokens logged.
  - A10 SSRF: no outbound calls except DB and JWKS.
  - Rate limiting: fixed window 100 requests/min per user (`AddRateLimiter`).
- **CORS:** Development allows `http://localhost:5173` only; other environments go through the API gateway (same origin).
- **GDPR:** no personal data is processed (codes and descriptions only). Audit logs hold the admin user id (`sub`) as the only personal data, retained per the platform log-retention policy.
- **WCAG support:** every error response carries a stable `code` and human-readable `message`, plus field-level `errors`, so the UI can announce them; otherwise front-end responsibility.

## 9. Non-Functional Requirements
- p95 latency ≤ 200 ms for E1/E2, ≤ 300 ms for E3–E5 at 50 concurrent users against local Postgres.
- Stateless service (no server session); horizontal scaling allowed.
- Idempotency: PUT and DELETE are idempotent by HTTP semantics (DELETE of a missing row → 404, no side effect); POST is protected by the PK (duplicate → 409).
- Observability: `/health` (DB probe = `SELECT 1`, mirrors priming query); request logging with trace id (`traceparent`) propagated to responses.
- Locale: messages in English; descriptions stored as entered (ASCII letters/digits/spaces only).

## 10. Backend Folder Structure
```text
Target/MG-01/Backend/TransactionTypeService/
├── TransactionTypeService.sln
├── src/TransactionTypeService.Api/
│   ├── TransactionTypeService.Api.csproj   # net9.0; Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Authentication.JwtBearer
│   ├── Program.cs                           # DI, auth, CORS, rate limit, ProblemDetails, health
│   ├── appsettings.json / appsettings.Development.json  # no secrets
│   ├── Controllers/TransactionTypesController.cs
│   ├── Services/ITransactionTypeService.cs, TransactionTypeService.cs
│   ├── Repositories/ITransactionTypeRepository.cs, TransactionTypeRepository.cs
│   ├── Data/CardDemoDbContext.cs
│   ├── Domain/TransactionType.cs, TransactionTypeCode.cs   # code normalisation value object
│   ├── Dtos/TransactionTypeDto.cs, CreateTransactionTypeRequest.cs, UpdateTransactionTypeRequest.cs, TransactionTypePageDto.cs, ListQuery.cs
│   ├── Validation/TransactionTypeValidators.cs
│   ├── Errors/ApiException.cs, ErrorCodes.cs, ProblemDetailsMiddleware.cs
│   └── Security/DevBypassAuthHandler.cs, AuthorizationPolicies.cs
└── tests/TransactionTypeService.Tests/
    ├── TransactionTypeService.Tests.csproj  # xUnit, Microsoft.AspNetCore.Mvc.Testing
    ├── ValidationTests.cs
    ├── ServiceTests.cs
    └── ApiIntegrationTests.cs               # against the Phase 5 Postgres
```

## 11. Acceptance Criteria & Deviations
**Acceptance criteria**
- [ ] E1 returns seed rows 01–07 ordered ascending; `pageSize=7`; `hasNext=false`, `hasPrevious=false` with seed data.
- [ ] E1 `after`/`before` paging returns correct keyset pages with ≥ 8 rows present.
- [ ] E1 `typeCode=5` returns only 05; `typeCode=AB` → 400 `TYPE_FILTER_INVALID`; `typeCode=00` → unfiltered.
- [ ] E1 `description=PAY` returns 02 PAYMENT; `description=pay` returns none (case-sensitive); `description=%` matches literally.
- [ ] E2 `05` → 200; `12` → 404 `TYPE_NOT_FOUND`; `5` → returns 05.
- [ ] E3 creates `{ "typeCode":"8", "description":"Fee reversal" }` as `08`; duplicate → 409; missing/zero/non-numeric code and bad description → 400 with Section 5 messages.
- [ ] E4 updates trimmed description; same text in other case → 422 `NO_CHANGES`; missing row → 404.
- [ ] E5 deletes → 204; missing → 404; row referenced by a child FK → 409 `HAS_DEPENDENT_RECORDS`.
- [ ] DB down → 503 `DB_UNAVAILABLE`; `/health` 503.
- [ ] No token → 401; token without `carddemo.admin` → 403; `dummy-dev-token` accepted only in Development with `Auth:DevBypass=true`.
- [ ] Every rule R1–R25 has at least one automated test.

**Deviations, assumptions, gaps**
- **D1 — Upsert split:** legacy CTTU save is UPDATE-then-INSERT (COTRTUPC 9600/9700). The target separates POST (insert only) and PUT (update only) per the agent mandate; a PUT for a missing code returns 404 instead of inserting.
- **D2 — Optimistic concurrency not added:** legacy has no concurrency check (`DATA-WAS-CHANGED-BEFORE-UPDATE` is never set, BDD Gaps). The target matches legacy (last write wins inside a row lock). Adding an ETag/`If-Match` check is a Gate 2 decision.
- **D3 — Delete -532 in COTRTUPC** leaves an undefined screen state (BDD Gaps); the target returns 409 consistently for both programs.
- **D4 — Child table not provisioned:** `TRANSACTION_TYPE_CATEGORY` (FK `ON DELETE RESTRICT`) is not part of MG-01 (DB-Details Gaps). R19 is implemented and tested with a temporary child table in integration tests only.
- **D5 — User-type flag:** COTRTLIC sets `CDEMO-USRTYP-USER` on F2 (BSTS Gaps); neither program enforces user type. The target enforces admin role on all endpoints (stricter).
- **D6 — Error text:** legacy shows SQLCODE and DSNTIAC text to the user; the target logs it and returns a safe message with a trace id (OWASP A05).
- **D7 — Auth provider:** `http://localhost:8081` is configured but no provider runs in this workspace; local runs use the dev bypass. Production realm/role names (`carddemo.admin`, scopes) are assumptions to confirm.
- **D8 — TLS:** Config says HTTPS/TLS 1.3 — consistent with the instruction; no discrepancy.
