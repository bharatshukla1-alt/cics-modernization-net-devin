# MG-01 — Review Gate 2 (Build Readiness)

## 1. Review Metadata
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Review date:** 2026-11-20
- **Specs reviewed:** `Artefacts/Specs/Refactor/MG-01-UI-Spec.md`, `Artefacts/Specs/Refactor/MG-01-Backend-Spec.md`, `Artefacts/Specs/Refactor/MG-01-reconcile-Spec.md`, `Artefacts/Architecture/MG-01.Architecture.md`
- **Context consulted:** `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-BSTS.md`, `-BDD.md`, `-DB-Details.md`; `Artefacts/Discovery/MoveGroup.md`, `Screen-metadata.md`; `Input/cbl/COTRTLIC.cbl`, `Input/cbl/COTRTUPC.cbl`, `Input/bms/COTRTLI.bms`, `Input/bms/COTRTUP.bms`, `Input/cpy/CSDB2RPY.cpy`, `Input/ddl/TRNTYPE.ddl`, `Input/ddl/TRNTYCAT.ddl`; `config/Config.md`; `DEVIN.md`.

## 2. Verdict Summary
The four artefacts cover every BSTS rule and BDD scenario in scope. Each has a target home in the backend rules table (R1–R25), the UI field and action tables, and the reconcile binding, validation and error maps. Contracts agree across the UI, backend and reconcile specs, so no field, action or error is left unmatched. The open items are user decisions and small consistency improvements, not build blockers.
**Counts:** Critical 0 · Good to Have 6 · Architecture 4 · Advanced 4.
**Call: ready to build with fixes.** The Good-to-Have items are optional; the user decisions are G2/GH-1 and A-1/A-2.

## 3. Critical Gaps
No critical gaps found.

## 4. Good to Have
| ID | Spec / Area | Finding | Evidence | Recommended Fix |
|---|---|---|---|---|
| GH-1 | Backend D1 / Reconcile G4 | Legacy save is an upsert. The target PUT returns 404 for a missing code, so a user who opens a stale details page and saves gets an error, where the legacy code would have created the row. | Backend §3, §11 D1 ↔ `COTRTUPC.cbl` 9600-WRITE-PROCESSING (SQLCODE +100 → 9700-INSERT-RECORD) | Confirm the split with the user. If kept, the UI 404 message could offer "Create it" (as S3 not-found does). |
| GH-2 | UI §7 / Reconcile §4 (row delete) | After a delete the target reloads page 1 **keeping the filters**, but legacy re-initialises the whole program COMMAREA, which clears the filters too. | Reconcile §4 "reload page 1 (filters kept)" ↔ `COTRTLIC.cbl` 0000-MAIN lines 826-833 (`INITIALIZE … WS-THIS-PROGCOMMAREA`) | Decide whether to keep the filters (better UX, recorded deviation) or clear them (parity); record the choice in UI §13. |
| GH-3 | UI §8 (success alert) | Auto-dismissing success alerts after 6 s can conflict with WCAG 2.2.1 (Timing Adjustable) for users who need more time. | UI §8 ↔ WCAG 2.2 AA | Keep success messages until the next user action (the current build already does this); update the spec text. |
| GH-4 | UI §13 / BDD d. | Legacy "Invalid key pressed" (COTRTUPC 0001-CHECK-PFKEYS) and "remap unknown key to ENTER" (COTRTLIC) have no target counterpart. They can't occur with buttons, but the spec doesn't say so. | BDD d. "Invalid key" ↔ UI §13 (only selector codes listed) | Add one line to UI §13 that PF-key validation is replaced by enabled/disabled buttons. |
| GH-5 | Backend §8 / Reconcile G2 | Role `carddemo.admin`, audience `carddemo-api` and the scopes are assumptions, and no identity provider is configured locally. | Backend §8, §11 D7 ↔ Config `target.auth.provider.url` | Get the realm/client/role names from the user or security team before Gate 3; keep the dev bypass only for local runs. |
| GH-6 | DB-Details seed / UI | The seed keeps the source typo "REVERAL", which will show in the UI. | DB-Details §c ↔ `Input/data/ASCII/trantype.txt` ("Reversal") | User accepted the seed at Phase 5; correct it later through the UI (an update) if wanted. |

## 5. Architecture Recommendations
| ID | Area | Finding | Evidence | Recommendation |
|---|---|---|---|---|
| A-1 | Concurrency (Medium) | Last write wins under a row lock, with no optimistic concurrency check. This matches the legacy code, but a stale inline edit can overwrite another admin's change. | Architecture "Open decisions" 2 ↔ BDD Gaps (`DATA-WAS-CHANGED-BEFORE-UPDATE` unused) | Optional: add an ETag/`If-Match` header on PUT later; not needed for parity. |
| A-2 | Upsert split (Medium) | Same as GH-1 at the architecture level. | Architecture "Open decisions" 1 | User decision. |
| A-3 | Right-sizing (Info) | One microservice and one table, with no cache or messaging. This is the right size and consistent with Config, which lists Redis and Kafka as available but not required. | Architecture §Components ↔ Config `target.cache.store`, `target.messaging.intra` | No change. |
| A-4 | Gateway (Low) | The API gateway product is not named; local dev uses the Vite proxy. | Architecture "Open decisions" 4 ↔ Config `target.api.layer` | Decide at platform level; does not block MG-01. |

## 6. Advanced Suggestions
| ID | Spec / Area | Suggestion (optional) | Evidence | Benefit |
|---|---|---|---|---|
| AS-1 | Backend §9 | Add a `/health/ready` probe separate from liveness. | Backend §9 | Cleaner orchestration. |
| AS-2 | UI §7 | Keep the paging keys in the URL (`?after=`) so deep links restore the exact page. | Reconcile G3 | Shareable pages. |
| AS-3 | Backend §8 | Emit audit events to `target.messaging.intra` (Kafka/RabbitMQ) instead of logs only. | Config `target.messaging.intra` | Central audit trail. |
| AS-4 | Tests | Add Playwright accessibility (axe) checks to Phase 8. | UI §12 | Automated WCAG proof. |

## 7. Traceability Notes
- Functional completeness was checked against BDD a.–e. scenario by scenario, and against backend R1–R25 and UI §6/§7. Every scenario has a target home or a recorded deviation (UI §13, backend §11).
- Contract consistency: compared reconcile §3/§5 messages with UI §6 and backend §5. The wording is identical.
- Legacy grounding: paging (`COTRTLIC` 8000/8100), change check (1211/1205), errors (9200/9300/9600/9800), and priming query (`CSDB2RPY`).
- Conventions: no legacy identifiers appear in the UI copy (UI §8, checked against built screens); artefact paths follow DEVIN.md.
