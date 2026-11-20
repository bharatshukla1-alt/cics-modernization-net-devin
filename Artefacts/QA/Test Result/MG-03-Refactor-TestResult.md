# MG-03 — Refactor Test Result

## 1. Run Metadata
- **Tran Group:** MG-03 — User Security Administration (CU00 / COUSR00C, CU01 / COUSR01C, CU02 / COUSR02C, CU03 / COUSR03C)
- **Test-case document:** `Artefacts/QA/Test Case/MG-03-Refactor-TestCase.md` (75 cases)
- **Target exercised:**
  - Shell UI: `http://localhost:5173`
  - `/api/v1/users` goes through the UI proxy to UserAdminService `:5002`
  - `/api/v1/auth` goes to SignOnService `:5001`
  - PostgreSQL 15 `carddemo`, container `carddemo-mg03-db-1`
  - Docker Compose stack: `Target/MG-03/demo`
- **Identities:** admin (ADMIN001 token), user (USER0001 token), anonymous.
- **Method:**
  - API cases: HTTP calls, with `usrsec` checked directly through `psql`. The owner role, `carddemo_useradmin` and `carddemo_signon` were all used.
  - UI cases: Chrome, driven by Playwright over CDP.
  - Test-created users are prefixed `ZZ` (or are USER0006) and are removed after each run.
- **Date:** 2026-11-20

## 2. Result Summary (final run)
| # | Test Scenario | Cases | Passed | Failed | Blocked | Result (Pass/Fail) | Expected Deviation |
|---|---|---|---|---|---|---|---|
| 1 | FE Functional Equivalence | 7 | 7 | 0 | 0 | Pass | 0 |
| 2 | HP Happy Path | 6 | 6 | 0 | 0 | Pass | 0 |
| 3 | AUTHN Authentication | 4 | 4 | 0 | 0 | Pass | 0 |
| 4 | AUTHZ Authorization | 4 | 4 | 0 | 0 | Pass | 0 |
| 5 | FV Field Validation | 10 | 10 | 0 | 0 | Pass | 0 |
| 6 | BR Business Rules | 8 | 8 | 0 | 0 | Pass | 0 |
| 7 | DB Side Effects | 4 | 4 | 0 | 0 | Pass | 0 |
| 8 | CON Concurrency | 4 | 4 | 0 | 0 | Pass | 0 |
| 9 | TX Transaction Integrity | 0 | 0 | 0 | 0 | N/A | 0 |
| 10 | HTTP Semantics | 8 | 8 | 0 | 0 | Pass | 0 |
| 11 | UI Rendering | 9 | 9 | 0 | 0 | Pass | 0 |
| 12 | PSF Pagination/Filter | 5 | 5 | 0 | 0 | Pass | 0 |
| 13 | BND Boundary | 6 | 6 | 0 | 0 | Pass | 0 |
| | **TOTAL** | **75** | **75** | **0** | **0** | **Pass** | **0** |

## 3. Overall Verdict
**PASS:** 75 of 75 cases passed, with 0 failed, 0 blocked and 0 expected deviations. No application defect was found.

The earlier runs failed only because of problems in the test harness. Each was fixed in the harness, not in the app:
- **API run 1:** 5 header checks (HP-002, HP-003, HTTP-001, HTTP-002, HTTP-007) failed because the harness looked up response headers case-sensitively. The Vite proxy returns them in lower case.
- **UI run 1:** the sign-in limit (5 per minute per client) blocked the UI sign-ins after the API run. AUTHZ-004 now also checks that sign-in itself succeeded.
- **UI run 2:** UI-003 and UI-007 looked for ZZUI0001 on page 1. With 11 users it is on page 2, so the harness now searches for it.

## 4. Evidence highlights
- **FE-007 (gap 2):** `zzlow01` / `pw` is stored as `ZZLOW01` / `PW`. Signing in as ZZLOW01 / PW returns 200.
- **BR-004 / BR-005 (gap 4):** 409 `CANNOT_DELETE_SELF` and `CANNOT_DEMOTE_SELF`; ADMIN001 is unchanged.
- **CON-001 / CON-004 (gap 5):**
  - 412 `STALE_USER`, and the first writer's change is kept.
  - CON-003: two parallel adds return [201, 409], leaving 1 row.
- **DB-004:**
  - `carddemo_useradmin` gets permission denied on `transaction_type`.
  - `carddemo_signon` gets permission denied on DELETE from `usrsec`.
- **Audit (gap 7):** 101 `AuditEvent` entries (action, actor, outcome, target, client IP), with no password values in the logs.
- **Data after the runs:** `usrsec` has 10 rows, 5 of them admins, the same as the seed.

## 5. Defects
None.
