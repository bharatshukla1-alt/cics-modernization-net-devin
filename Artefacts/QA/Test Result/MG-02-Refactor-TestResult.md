# MG-02 — Refactor Test Result

## 1. Run Metadata
- **Tran Group:** MG-02 — Sign-on and Navigation Shell (CC00 / COSGN00C, CM00 / COMEN01C, CA00 / COADM01C)
- **Test-case document:** `Artefacts/QA/Test Case/MG-02-Refactor-TestCase.md` (63 cases)
- **Target exercised:**
  - Shell UI: `http://localhost:5173` (Vite, React)
  - API via the UI proxy: `/api/v1/auth` and `/api/v1/menus` go to SignOnService `http://localhost:5001`; `/api/v1/transaction-types` goes to the MG-01 TransactionTypeService `http://localhost:5000` (JWKS from SignOnService)
  - PostgreSQL 15 `carddemo` in container `carddemo-mg02-db-1`, port 5433
  - Docker Compose stack `Target/MG-02/demo`
- **Identities:** admin (ADMIN001 token), user (USER0001 token), anonymous.
- **Method:**
  - API cases: HTTP calls, with the database checked directly through `psql` (owner role and the `carddemo_signon` runtime role).
  - UI cases: Chrome, driven by Playwright over CDP.
  - BR-005: `dotnet test --filter MenuServiceTests` (6 tests).
  - HTTP-006 and HTTP-007: run separately with the default rate limit, and with the database stopped.
- **Date:** 2026-11-20

## 2. Result Summary (run 1)
| # | Test Scenario | Cases | Passed | Failed | Blocked | Result (Pass/Fail) | Expected Deviation |
|---|---|---|---|---|---|---|---|
| 1 | FE Functional Equivalence | 8 | 8 | 0 | 0 | Pass | 0 |
| 2 | HP Happy Path | 6 | 6 | 0 | 0 | Pass | 0 |
| 3 | AUTHN Authentication | 5 | 5 | 0 | 0 | Pass | 0 |
| 4 | AUTHZ Authorization | 5 | 5 | 0 | 0 | Pass | 0 |
| 5 | FV Field Validation | 8 | 8 | 0 | 0 | Pass | 0 |
| 6 | BR Business Rules | 6 | 6 | 0 | 0 | Pass | 0 |
| 7 | DB Side Effects | 3 | 3 | 0 | 0 | Pass | 0 |
| 8 | CON Concurrency | 2 | 2 | 0 | 0 | Pass | 0 |
| 9 | TX Transaction Integrity | 0 | 0 | 0 | 0 | N/A | 0 |
| 10 | HTTP Semantics | 9 | 9 | 0 | 0 | Pass | 0 |
| 11 | UI Rendering | 6 | 6 | 0 | 0 | Pass | 0 |
| 12 | PSF Pagination/Filter | 0 | 0 | 0 | 0 | N/A | 0 |
| 13 | BND Boundary | 5 | 4 | 1 | 0 | Fail | 0 |
| | **TOTAL** | **63** | **62** | **1** | **0** | **Fail** | **0** |

## 3. Overall Verdict
Run 1 ran 63 cases: **62 passed, 1 failed, 0 blocked, 0 expected deviations.** The pass rate is 98.4%. **Run 1 verdict: FAIL** because of BND-002.

The user approved correcting the BND-002 test input. Run 2 re-ran BND-002 only: **63 passed, 0 failed, 0 blocked, so the verdict is PASS.**

## 4. Failed Scenarios
Run 2 has no open failures. The run 1 entry below is kept for the record.

> **Boundary & numeric edge cases — ❌ FAIL (4/5)**
> - **MG02-BND-002 — Leading space in password:** sign in `USER0001` with password ` PASSWORD` (a leading space plus 8 characters, 9 in total).
>   - Expected: `401 WRONG_PASSWORD` · Actual: `400 FIELD_TOO_LONG` "User ID and password are at most 8 characters." ❌
>   - Evidence: `POST /api/v1/auth/sign-in` returned 400 with `code=FIELD_TOO_LONG`. The 9-character input is rejected by the 8-character rule that MG02-FV-005 verifies (passing).
>   - Severity: **Low**. There is no functional or data risk, and the length rule behaves as specified. See §7, note N1.

## 5. Message-Intent Equivalences
| Case | Expected (legacy) | Actual (target) | Why equivalent |
|---|---|---|---|
| FV-006 | `Please enter User ID ...` | UI `Enter your user ID.` (the API keeps the legacy text, FV-001) | Same trigger (blank user ID), same field focus |
| FV-007 | `Please enter Password ...` | UI `Enter your password.` | Same trigger, same focus |
| BR-006 / UI-002 | `Wrong Password. Try again ...` | UI `Wrong password. Try again.` (the API keeps the legacy text, FE-006) | Same event, same cursor field |
| BR-004 | `This option … is not installed...` | `This option Account View is not installed yet.` | Same event (target function not available) |
| HP-005 / UI-006 | `Thank you for using CardDemo application...` | `Thank you for using CardDemo.` | Same sign-off outcome |

### 5A. Expected Functionality — Intentional Deviations from Legacy
None.

## 6. Blocked / Not Run
None.

## 7. Environment & Notes
- Services were confirmed up before the run: `/health` returned 200 on 5001 and 5000, and the UI returned 200.
- For the functional run, the sign-in rate limit was raised with `SIGNON_RATE_LIMIT=1000`, a new compose variable that defaults to 5. HTTP-006 ran afterwards with the default of 5 per minute: requests 1–5 returned 200 and request 6 returned 429 `TOO_MANY_REQUESTS`.
- HTTP-007 stopped the PostgreSQL container for one request (503 `UNABLE_TO_VERIFY`, legacy message "Unable to verify the User ...") and then restarted it.
- After each SignOnService restart, the MG-01 API was restarted too. In Development the signing key is ephemeral (Gate 2 GH-4), and MG-01 caches the JWKS.
- **MG-01 follow-up:** MG02-AUTHZ-003 and MG02-AUTHZ-004 run the scenarios MG-01 had blocked (AUTHZ-001 and AUTHZ-002, which needed a non-admin token). A user token now gets 403 from MG-01 for both read and write.
- No test data was left behind. `usrsec` is unchanged (same checksum before and after), and `transaction_type` 05 is unchanged.
- **N1 — test-case defect (test-bug-fix agent, self-fix mode):** BND-002 is *provably wrong*. Its input has 9 characters, but the legacy password field holds 8 (COSGN00.bms `PASSWD LENGTH=8`; CSUSR01Y `SEC-USR-PWD PIC X(08)`). The target rejects input over 8 characters by design, and FV-005 verifies that. The scenario's intent (a leading space is significant) needs an 8-character input such as ` PASSWOR`. Per the bug-fix agent's change discipline, the agent changed no code or test document. The decision went to the user, who chose to correct the input to ` PASSWOR`, which is 8 characters. On re-run the case returned `401 WRONG_PASSWORD` "Wrong Password. Try again ...", so it passes.

## 8. Overall Scorecard
| # | Test Group | Passed | Total Cases | Pass % |
|---|---|---|---|---|
| 1 | FE | 8 | 8 | 100% |
| 2 | HP | 6 | 6 | 100% |
| 3 | AUTHN | 5 | 5 | 100% |
| 4 | AUTHZ | 5 | 5 | 100% |
| 5 | FV | 8 | 8 | 100% |
| 6 | BR | 6 | 6 | 100% |
| 7 | DB | 3 | 3 | 100% |
| 8 | CON | 2 | 2 | 100% |
| 9 | TX | 0 | 0 | N/A |
| 10 | HTTP | 9 | 9 | 100% |
| 11 | UI | 6 | 6 | 100% |
| 12 | PSF | 0 | 0 | N/A |
| 13 | BND | 4 | 5 | 80% |

**Overall:** run 1 passed 62 of 63 (98.4%). Run 2 passed **63 of 63 (100%)**; BND is now 5/5.

**Quality judgment:** **Low risk.** The only failure was a test-case input defect, now corrected. No application defect was found.
