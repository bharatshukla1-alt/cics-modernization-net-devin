# MG-01 — Refactor Test Result

## 1. Run Metadata
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Test-case document:** `Artefacts/QA/Test Case/MG-01-Refactor-TestCase.md` (74 cases)
- **Target exercised:** UI `http://localhost:5173` (Vite, React), API via proxy `http://localhost:5173/api/v1` → ASP.NET Core 9 on `http://localhost:5000` (Development, dev-bypass token), PostgreSQL 15 `carddemo` on `127.0.0.1:5433`
- **Method:** API cases driven with HTTP calls and DB state checked directly with `psql`; UI cases driven in Chrome (Playwright over CDP). Test rows use codes 90–99 and are cleaned after every case; seed restored.
- **Date:** 2026-11-20

## 2. Result Summary (run 1)
| # | Test Scenario | Cases | Passed | Failed | Blocked | Result (Pass/Fail) |
|---|---|---|---|---|---|---|
| 1 | FE Functional Equivalence | 8 | 8 | 0 | 0 | Pass |
| 2 | HP Happy Path | 8 | 8 | 0 | 0 | Pass |
| 3 | AUTHN Authentication | 3 | 3 | 0 | 0 | Pass |
| 4 | AUTHZ Authorization | 2 | 0 | 0 | 2 | Blocked |
| 5 | FV Field Validation | 9 | 9 | 0 | 0 | Pass |
| 6 | BR Business Rules | 6 | 6 | 0 | 0 | Pass |
| 7 | DB Side Effects | 4 | 4 | 0 | 0 | Pass |
| 8 | CON Concurrency | 4 | 4 | 0 | 0 | Pass |
| 9 | TX Transaction Integrity | 2 | 2 | 0 | 0 | Pass |
| 10 | HTTP Semantics | 8 | 8 | 0 | 0 | Pass |
| 11 | UI Rendering | 8 | 7 | 1 | 0 | Fail |
| 12 | PSF Pagination/Filter | 6 | 6 | 0 | 0 | Pass |
| 13 | BND Boundary | 6 | 6 | 0 | 0 | Pass |

## 3. Overall Verdict
Run 1: 74 cases — 71 passed, 1 failed, 2 blocked (pass rate of executed cases 98.6%). **Run 1 verdict: FAIL** (UI-005).
After the fix (Section 7), run 2 re-executed all 72 runnable cases: **72 passed, 0 failed, 2 blocked → PASS.**

## 4. Failed Scenarios
> **MG01-UI-005 — Field errors shown on add**
> - Input: open Add screen, click Create with both fields empty
> - Expected: messages under both fields · Actual: only "Type code is required." shown; description error missing ❌
> - Evidence: `Target/MG-01/Frontend/src/screens/TransactionTypeAddPage.jsx` — the auto-focused Type code field's `onBlur` validation inserted an error line, shifting the layout during the click so the form submit never fired.
> - Severity: Medium (validation feedback incomplete; no data risk)

## 5. Message-Intent Equivalences
| Case | Expected (legacy) | Actual (target) | Why equivalent |
|---|---|---|---|
| FV-001 | `TYPE CODE FILTER,IF SUPPLIED MUST BE A 2 DIGIT NUMBER` | `Type code must be a 2-digit number.` | same rule and 2-digit constraint |
| FV-002 | `Tran Type code must be supplied.` | `Type code is required.` | same trigger (required), same outcome |
| FV-005 / BND-003 | `Transaction Desc must be supplied.` | `Description is required.` | same trigger |
| FV-006 | `Transaction Desc can have numbers or alphabets only.` | `Description can contain only letters, numbers and spaces.` | same rule (spaces also allowed in legacy) |
| HP-007 / UI-006 | `No record found for this key in database` | `No transaction type with code 12.` | same not-found event |
| BR-001 | `No Records found for these filter conditions` | `No transaction types match these filters.` | same event |
| CON-002 | `Record not found. Deleted by others ?` | `Transaction type 94 was not found. It may have been deleted by someone else.` | same event and cause |
| CON-003 | `Deadlock. Someone else updating ?` | `The record is being changed by someone else. Try again.` | same event, retry possible |
| TX-001 | `Please delete associated child records first:` | `Please delete associated child records first.` | same |
| HTTP-008 | `Db2 access failure.` | `The service is temporarily unavailable. Try again later.` | same outcome (DB unavailable) |

## 6. Blocked / Not Run
| Case ID(s) | Reason | What is needed to run it |
|---|---|---|
| AUTHZ-001, AUTHZ-002 | No OAuth provider is running locally (`http://localhost:8081`), so no token for an authenticated non-admin user can be issued; the dev-bypass token is always admin | A running identity provider (realm/client per Gate 2 GH-5) issuing a token without role `carddemo.admin`. Policy code is in place (`Security/AuthorizationPolicies.cs`). |

## 7. Environment & Notes
- Services confirmed up before the run (`/health` 200, UI 200).
- HTTP-008 stopped the Postgres container for one request and restarted it.
- UI-007 moved the seed rows to a backup table for the empty-table check and restored them.
- **Bug fix (test-bug-fix, self-fix mode):** UI-005 fixed in `TransactionTypeAddPage.jsx` and `TransactionTypeDetailPage.jsx`. The blur validation now skips when focus moves to the submit button (`e.relatedTarget?.type === 'submit'`), so a submit always validates every field. Lint and build pass. Re-run: all 72 runnable cases pass.
- No test data left behind; seed rows 01–07 unchanged.

## 8. Overall Scorecard
| Metric | Run 1 | Run 2 (after fix) |
|---|---|---|
| Total cases | 74 | 74 |
| Passed | 71 | 72 |
| Failed | 1 | 0 |
| Blocked | 2 | 2 |
| Pass rate (executed) | 98.6% | 100% |
| Verdict | FAIL | PASS |
