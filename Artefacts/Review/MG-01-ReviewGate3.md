# MG-01 – Human Review Gate 3 (Final Sign-Off)

**Move group:** MG-01 – Transaction Type Maintenance (CTLI → COTRTLIC, CTTU → COTRTUPC)
**Date:** 2026-11-20 · **Path:** Refactor · **Branch:** `devin/1795154209-cics-modernization-mg01` (local, not pushed)
**Recommendation:** **Ready for sign-off.** The Gate 3 decisions are in §6.

## 1. Pipeline status for MG-01

| Phase | Output | Status |
|---|---|---|
| 1 Discovery | `Artefacts/Discovery/TransVsPgm.md`, `CicsXref.md`, `Screen-metadata.md` | Done |
| 2 Gate 1 | `Artefacts/Review/ReviewGate1.md` | Approved by the user |
| 3 Move groups | `Artefacts/Discovery/MoveGroup.md` | Done (12 groups, MG-01 first) |
| 4 BDD / BSTS | `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-{BDD,BSTS}.md` | Done |
| 5 Database | `…-DB-Details.md`, `Artefacts/DDL/…-schema.sql`, `Artefacts/Database/…-DB-Status.md` | Done; live schema verified |
| 6 Specs + build | `Artefacts/Specs/Refactor/MG-01-{UI,Backend,reconcile}-Spec.md`, `Architecture/MG-01.Architecture.md`, `MG-01-Run-Guide.md`, `Target/MG-01/` | Done |
| 7 Gate 2 | `Artefacts/Review/MG-01-ReviewGate2.md` | Approved by the user |
| 8 Testing | `Artefacts/QA/Test Case/MG-01-Refactor-TestCase.md`, `Artefacts/QA/Test Result/MG-01-Refactor-TestResult.md` | 72 passed, 0 failed, 2 blocked (non-admin OAuth identity) |
| 9 Quality & security | `Artefacts/Code Quality & Security/MG-01-Security-Review.md`, `MG-01-CodeQuality-Review.md` | Reviewed; approved fixes applied (§3) |

`run_pipeline.py --phase 9 --group MG-01` reports "Phase 9 outputs present" and is paused at GATE 3.

## 2. Review results (before fixes)

| Report | Grade | Critical | High | Medium | Low/Info | Verdict |
|---|---|---|---|---|---|---|
| Security | C | 0 | 2 | 5 | 4 | Proceed with fixes |
| Code quality | B | 0 | 0 | 6 | 6 | Fix, then sign off |

The user approved "High + Medium" for fixing: SEC-001–006, SEC-008 and CQ-001–006. Following the fix agent's order, code-quality fixes were applied first and security fixes second. Both review reports were left unchanged as read-only inputs, and all edits are confined to `Target/MG-01/`.

## 3. Fix log

### Code quality (6 in scope: 5 fixed, 1 skipped)

| ID | Status | Change | Files |
|---|---|---|---|
| CQ-001 | Fixed | The list page is split into `FilterBar`, `TransactionTypeTable` and `Pager` and shrinks from 296 to 187 lines. | `Frontend/src/screens/TransactionTypeListPage.jsx`, `src/components/{FilterBar,TransactionTypeTable,Pager}.jsx` |
| CQ-002 | Fixed | A shared `useFormField` hook now holds field state, blur and submit validation and the UI-005 submit guard for both Add and Details. | `src/hooks/useFormField.js`, `TransactionTypeAddPage.jsx`, `TransactionTypeDetailPage.jsx` |
| CQ-003 | Fixed | XML docs are added on the public API surface (controller with response codes, DTOs, interfaces, errors) and `GenerateDocumentationFile` is on. An OpenAPI document is served at `/openapi/v1.json`, Development only. JSDoc is added to components and hooks. 32 of 35 source files are now documented, up from 12 of 28. | backend `Controllers/`, `Dtos/`, `Services/`, `Repositories/`, `.csproj`; frontend `src/**` |
| CQ-004 | **Skipped – needs Phase 5** | The fix agent may not emit DDL, and the schema belongs to `/05-CreateDB`. The next step would be to baseline the Phase 5 DDL as EF Core (or Flyway) migrations, run through Phase 5. | — |
| CQ-005 | Fixed (partly) | `ci/quality-gate.sh` runs the backend build with `-warnaserror`, tests with a ≥70 % line-coverage gate and a NuGet vulnerability check, then frontend `npm ci`, lint, tests with coverage thresholds, build and `npm audit --audit-level=high`. The backend Dockerfile runs as the non-root `$APP_UID` user. Hooking the script into the repository pipeline (`.github/…`) is outside the writable `Target/MG-01/` area, so that needs a decision (§6). | `Target/MG-01/ci/quality-gate.sh`, `Backend/TransactionTypeService/Dockerfile` |
| CQ-006 | Fixed | Frontend tests go from 15 to 42, using React Testing Library on jsdom. They cover the List, Add (including a UI-005 regression test) and Details screens, ConfirmDialog, the API client, the OIDC flow, the idle timeout and the App sign-in gate. Coverage is enforced: frontend 88.2 % lines (threshold 70 %), backend 91.3 % lines. | `src/**/*.test.js(x)`, `vite.config.js`, `src/test/setup.js` |

Also done, outside the approved scope:
- **CQ-009 (Low):** the service and validators now raise a transport-agnostic `ServiceException(ErrorKind…)`, and HTTP status mapping happens only in `ProblemDetailsMiddleware`. This was needed to keep SEC-008's failure auditing clean.
- **CQ-007 (Low):** resolved by SEC-008.

### Security (7 in scope: 7 fixed)

| ID | Status | Change | Files |
|---|---|---|---|
| SEC-001 | Fixed | `react-router-dom` goes from 6.28.0 to 6.30.6, which clears the High XSS/open-redirect advisory (GHSA-2w69-qvjg-hvjx). **Residual:** 2 Moderate react-router advisories are fixed only in v7 (§5). | `Frontend/package.json`, `package-lock.json` |
| SEC-002 | Fixed | A new `carddemo_app` login (NOSUPERUSER, NOCREATEDB, NOCREATEROLE) has only SELECT/INSERT/UPDATE/DELETE on `transaction_type`. CONNECT and CREATE are revoked from PUBLIC. The script is applied to the local database, the API now runs as `carddemo_app`, and a CREATE TABLE attempt by that login is rejected. Integration tests use the owner login only for fixture DDL (`ConnectionStrings__CardDemoAdmin`). The password is supplied at run time and never committed. | `Target/MG-01/Backend/db/least-privilege-app-role.sql`, `tests/…/ApiIntegrationTests.cs` |
| SEC-003 | Fixed | The SPA now does OAuth 2.0 Authorization Code + PKCE (S256) via OIDC discovery, with no new runtime dependency. It verifies state, keeps the access token in memory only, restricts return paths to same-origin (open-redirect guard), shows a sign-in gate when there is no session, and has a Sign out button that ends the IdP session. It is configured with `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID` and `VITE_OIDC_SCOPE`. | `Frontend/src/auth/oidc.js`, `src/App.jsx`, `src/api/transactionTypesApi.js` |
| SEC-004 | Fixed | `appsettings.Production.json` declares a Kestrel HTTPS endpoint with `SslProtocols: [Tls13]`, and the certificate is mounted at deploy time. Outside Development, `StartupGuards` refuses to start unless an HTTPS endpoint exists or `Tls:TerminatedUpstream=true` is set. | `appsettings.Production.json`, `Security/StartupGuards.cs`, `Program.cs` |
| SEC-005 | Fixed | Outside Development, startup fails unless `Auth:Authority` is https and `AllowedHosts` is explicit. The base `AllowedHosts` is now `localhost`; a request with Host `evil.example` returns 400. | `Security/StartupGuards.cs`, `appsettings.json` |
| SEC-006 | Fixed | Dev tooling upgraded: vite 6.4.6, vitest 4.1.12, postcss 8.5.33, eslint 9.39.5 and @vitejs/plugin-react 4.7.0. All dev-tool advisories are cleared. | `Frontend/package.json`, lockfile |
| SEC-008 | Fixed | Structured audit events go to the dedicated `Audit` category (event id 9000) and carry user, action, code, before/after values, outcome (`SUCCESS`, or `FAILED:<code or SQLSTATE>`) and traceId. Failed and rejected changes are audited too. Routing to an append-only store or SIEM is a deployment step. | `Audit/AuditLog.cs`, `Services/TransactionTypeService.cs`, `Program.cs` |

Not in the approved scope: SEC-007, SEC-009 and SEC-010 (Low) and SEC-011 (Info). SEC-010 is partly covered by the CQ-005 gate script and the non-root Dockerfile.

## 4. Verification after fixes

| Check | Result |
|---|---|
| `ci/quality-gate.sh`, run end to end against the live Phase 5 database as `carddemo_app` | **Passed** |
| Backend `dotnet build -warnaserror` | 0 warnings |
| Backend `dotnet test` | 53/53 passed (46 earlier + 7 new StartupGuard tests); line coverage 91.3 % |
| `dotnet list package --vulnerable --include-transitive` | No vulnerable packages |
| Frontend `eslint`, `vitest --coverage`, `vite build` | Lint clean; 42/42 passed; lines 88.2 %, branches 82.2 %; build ok |
| `npm audit` | 0 High/Critical; 2 Moderate (react-router v6 line) |
| Live smoke test through the Vite proxy with the API as `carddemo_app` | List 200; POST 201, PUT 200, DELETE 204, each with an `Audit[9000]` SUCCESS event carrying before/after values; anonymous 401; bad Host 400; UI 200 |

The full Phase 8 browser suite (74 cases) was not re-run after these fixes. The behaviour it covers is protected by the new component tests and the unchanged API integration tests. Re-running `/08-TestTarget` is recommended before release.

## 5. Residual risks and open items

| # | Item | Severity | Owner / next step |
|---|---|---|---|
| R1 | Two Moderate react-router advisories (backslash open redirect in `<Link>`/`useNavigate`, constructor injection) are fixed only in react-router 7.17.1+. MG-01 does not pass user-controlled paths to `navigate()`, and the OIDC return path is sanitised. | Medium | Decide whether to upgrade to React Router v7 (major) before MG-02 |
| R2 | Real OAuth realm, client ID, audience, role and scope names are still unconfirmed, so AUTHZ-001/002 remain blocked. | Medium | Provide IdP details, then re-run AUTHZ cases |
| R3 | No versioned schema migrations (CQ-004). | Medium | Phase 5 (`/05-CreateDB`) owns this |
| R4 | The quality gate script is not yet wired into the repository CI. | Low | Approve adding a pipeline file outside `Target/MG-01/` |
| R5 | Gate 2 deviations still stand as approved: POST/PUT split instead of upsert, no optimistic concurrency, filters kept after delete. | Info | Accepted at Gate 2 |
| R6 | The review reports describe the code before fixes. | Info | Re-run `/09-Quality-Security-Review MG-01` to regenerate scorecards if required for audit evidence |

## 6. Decisions for Gate 3 sign-off

1. **Approve MG-01** as complete, with residual items R1–R6 tracked.
2. **R1:** keep React Router v6 for now, or upgrade to v7 (this changes the major version for all MGs).
3. **R4:** add the CI pipeline file at repository level (outside `Target/MG-01/`).
4. **Next:** after approval, start **MG-02 (sign-on and the user/admin menus, CC00/CM00/CA00)** at Phase 4, per `MoveGroup.md`.

## 7. Sign-off

- **Gate 3: APPROVED** by the user ("ok proceed", 2026-11-20 13:01 IST).
- R1: keep React Router v6 for now (default; no upgrade requested). R4: no repository-level CI file added (default; `ci/quality-gate.sh` stays under `Target/MG-01/`).
- Post-approval cosmetic fix: the header "Sign out" button now has readable contrast on the dark header.
- Next: MG-02 (CC00 → CM00 → CA00) from Phase 4.
