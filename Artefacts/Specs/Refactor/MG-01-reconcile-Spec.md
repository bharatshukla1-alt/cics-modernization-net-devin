# MG-01 — Reconciliation Spec (Refactor)

## 1. Reconciliation Metadata
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Specs reconciled:** `Artefacts/Specs/Refactor/MG-01-UI-Spec.md`, `Artefacts/Specs/Refactor/MG-01-Backend-Spec.md`
- **Stack** (`config/Config.md`): React + Tailwind CSS (Vite, port 5173, `/api` proxy) → ASP.NET Core Web API on .NET 9 (C#, port 5000) → PostgreSQL (`carddemo`); OAuth 2.0 JWT; HTTPS/TLS 1.3; microservice-based.

## 2. Reconciled Screen Overview
Three screens served by one service. **S1 Transaction types** (`/transaction-types`) lists types 7 per page via E1, filters by code/description, edits a description in place (E4) and deletes a row (E5). **S2 Add** (`/transaction-types/new`) creates a type in one step (E3). **S3 Details** (`/transaction-types/:typeCode`) reads one type (E2), saves a new description (E4) or deletes it (E5). Every write is confirmed in a UI dialog, then validated again on the server and committed in its own transaction. Errors come back as ProblemDetails with `code`, `message` and field `errors`, which the UI shows in its error alert or under the field.

## 3. Field Binding Map
| UI field (screen) | Backend attribute | Type / format | Req. | Mode |
|---|---|---|---|---|
| Type code filter (S1) | E1 query `typeCode` | 1–2 digits; ""/"0"/"00" = none | No | editable |
| Description contains (S1) | E1 query `description` | text ≤ 50, case-sensitive contains | No | editable |
| Page key (S1, hidden state) | E1 query `after` = current `lastCode` / `before` = current `firstCode` | 2 digits | No | client state |
| Page N (S1) | — (client counter; reset to 1 on new search) | int | — | read-only |
| Previous / Next enabled (S1) | E1 response `hasPrevious` / `hasNext` | bool | — | derived |
| Table Code (S1) | E1 `items[].typeCode` | 2 digits | — | read-only |
| Table Description (S1) | E1 `items[].description`; edit → E4 body `description` | text ≤ 50 | Yes on save | read-only / editable in edit mode |
| No-match error (S1) | E1 `items=[]` and `filtered=true` | — | — | derived |
| Empty state (S1) | E1 `items=[]` and `filtered=false` | — | — | derived |
| Type code (S2) | E3 body `typeCode` (response normalised 2 digits) | 1–2 digits, ≠ 0 | Yes | editable |
| Description (S2) | E3 body `description` | `^[A-Za-z0-9 ]{1,50}$` after trim | Yes | editable |
| Type code (S3) | path `{typeCode}`, E2 `typeCode` | 2 digits | — | read-only |
| Description (S3) | E2 `description`; E4 body `description` | as S2 | Yes | editable |
| Look up another code (S3) | route param → E2 path | 1–2 digits | — | editable |
| Error alert / field errors (all) | ProblemDetails `message`, `errors{field}`, `code`, `traceId` | — | — | read-only |

No unmatched fields on either side.

## 4. Action ↔ Endpoint Map
| UI action / transition | Endpoint | Request | Success | Error statuses handled |
|---|---|---|---|---|
| S1 open / Clear | E1 `GET /api/v1/transaction-types` | `pageSize=7` | 200 page | 401, 403, 503 |
| S1 Search | E1 | `typeCode`, `description`, `pageSize=7` | 200 | 400 `TYPE_FILTER_INVALID`, 503 |
| S1 Next | E1 | filters + `after=lastCode` | 200, page+1 | 400, 503 |
| S1 Previous | E1 | filters + `before=firstCode` | 200, page−1 | 400, 503 |
| S1 row Save | E4 `PUT /api/v1/transaction-types/{code}` | `{ description }` | 200 → replace row, status "Transaction type {code} updated." | 400, 404, 409 `RECORD_LOCKED`, 422 `NO_CHANGES` |
| S1 row Delete (after confirm) | E5 `DELETE /api/v1/transaction-types/{code}` | — | 204 → status "Transaction type {code} deleted.", reload page 1 (E1, no paging key, filters kept) | 404, 409 `HAS_DEPENDENT_RECORDS` |
| S1 Add / code link | (navigation) | — | route change | — |
| S2 Create (after confirm) | E3 `POST /api/v1/transaction-types` | `{ typeCode, description }` | 201 → navigate S1, status "Transaction type {code} created." | 400, 409 `TYPE_ALREADY_EXISTS` |
| S3 open / Look up | E2 `GET /api/v1/transaction-types/{code}` | — | 200 | 400, 404 → not-found state |
| S3 Save changes (after confirm) | E4 | `{ description }` | 200, status "Changes saved." | 400, 404, 409, 422 |
| S3 Delete (after confirm) | E5 | — | 204 → navigate S1 with status | 404, 409 |
| Any dialog Cancel | none | — | status "Nothing was changed." | — |
| Back to admin menu | none (route `/admin`, MG-02) | — | — | — |

Every endpoint E1–E5 has a UI caller; E6 `/health` is operational only (not a UI action — intended).

## 5. Validation Parity
| Field | UI rule / message | Server rule / message | Status |
|---|---|---|---|
| Type code filter | non-digit → "Type code must be a 2-digit number." | 400 `TYPE_FILTER_INVALID` same text | aligned |
| Type code (S2) empty | "Type code is required." | "Type code is required." | aligned |
| Type code non-numeric | "Type code must be numeric." | same | aligned |
| Type code zero | "Type code must not be zero." | same | aligned |
| Type code 1 digit | padded to 2 on submit | normalised to 2 digits | aligned |
| Description empty | "Description is required." | same | aligned |
| Description characters | "Description can contain only letters, numbers and spaces." | same, regex `^[A-Za-z0-9 ]+$` | aligned |
| Description length | `maxLength=50` (cannot exceed) | "Description can be at most 50 characters." | aligned (server-only message reachable via API only) |
| Description unchanged | Save disabled + hint "No changes to save." | 422 `NO_CHANGES` "No change detected with respect to database values." | aligned (server message shown if reached, e.g. stale page) |
| Description filter length | `maxLength=50` | "Description filter can be at most 50 characters." | aligned |

## 6. Error & Edge-Case Parity
| Backend path | UI presentation |
|---|---|
| 200 `items=[]`, `filtered=true` | S1 error alert "No transaction types match these filters.", filter fields `aria-invalid` |
| 200 `items=[]`, `filtered=false` | S1 empty state "No transaction types yet." + Add button |
| `hasPrevious=false` / `hasNext=false` | Previous / Next disabled |
| 404 `TYPE_NOT_FOUND` on E2 | S3 not-found state "No transaction type with code {code}." + Create it |
| 404 on E4/E5 | error alert with server `message`; S1 reloads current page |
| 409 `TYPE_ALREADY_EXISTS` | S2 field error on Type code + link to S3 |
| 409 `RECORD_LOCKED` | error alert; row stays in edit mode for retry |
| 409 `HAS_DEPENDENT_RECORDS` | error alert "Please delete associated child records first."; row unchanged |
| 422 `NO_CHANGES` | error alert with server `message` |
| 400 `VALIDATION_FAILED` | field errors under each field from `errors`; focus first invalid field |
| 401 | redirect to sign-in (or dev bypass) — "Sign in to continue." |
| 403 | full-page message "You do not have permission to maintain transaction types." |
| 503 `DB_UNAVAILABLE` | error alert "The service is temporarily unavailable. Try again later."; actions stay enabled for retry |
| 500 `INTERNAL_ERROR` | error alert "Something went wrong. Reference: {traceId}." |
| Network failure (no response) | error alert "Cannot reach the service. Check your connection and try again." (UI-only state, no backend counterpart needed) |

No orphans: every backend code has a UI presentation and every UI error state has a trigger.

## 7. Cross-Cutting Alignment
- **OAuth 2.0 / JWT:** the UI API client attaches `Authorization: Bearer <token>` to every `/api/v1` call; token kept in memory. In local dev with `VITE_AUTH_BYPASS=true` the client sends `dummy-dev-token`, accepted by the backend only in Development with `Auth:DevBypass=true`.
- **TLS 1.3:** terminated at Kestrel (non-dev) or the API gateway; the dev proxy uses HTTP on localhost only.
- **OWASP:** server-side validation on every write regardless of UI checks; parameterised queries; ProblemDetails without stack traces; CORS limited to `http://localhost:5173` in dev.
- **GDPR:** no personal data on either side; backend audit log holds only the admin `sub`.
- **WCAG:** backend `code`/`message`/`errors` feed the UI's `role="alert"` and field `aria-describedby` messages.

## 8. Gaps & Resolutions for a Fully Functional Screen
| # | Area | Gap / Inconsistency | Resolution (or Needs From User) |
|---|---|---|---|
| G1 | Navigation | "Back to admin menu" targets MG-02, not built yet | Link to placeholder `/admin` route showing "Admin menu is not available yet." until MG-02 |
| G2 | Auth | No OAuth provider runs locally (`http://localhost:8081`) | Use dev bypass for local build/test; production realm, audience `carddemo-api`, role `carddemo.admin` and scopes need confirmation (Needs From User at Gate 2) |
| G3 | Paging | Page number is client-held; deep links with `page>1` can't rebuild keyset position | Page in URL is informational; opening a URL with filters always starts at page 1 |
| G4 | Upsert | Legacy save inserts when missing; target PUT returns 404 | Accept split (backend D1), or ask user at Gate 2 |
| G5 | Concurrency | No optimistic concurrency (legacy parity) | Accept (backend D2), or add ETag at Gate 2 |
| G6 | Child FK | 409 `HAS_DEPENDENT_RECORDS` cannot occur until `transaction_type_category` exists | Tested with a temporary child table in integration tests |
| G7 | Session timeout | 15-minute idle timeout is a UI assumption | Confirm at Gate 2 |

## 9. Implementation Plan (Rewiring Front End & Back End)
1. **Backend:** create the solution and API project (backend spec §10); `CardDemoDbContext` mapped to `transaction_type`; repository (keyset queries, LIKE escaping, FOR UPDATE on update); service (rules R1–R25, error mapping); validators (§5); controller E1–E5 + `/health`; ProblemDetails middleware; JWT + dev-bypass auth; CORS; rate limiting. Verify with `curl` against the Phase 5 database.
2. **Frontend API layer:** `transactionTypesApi.js` with `list`, `get`, `create`, `update`, `remove`; base `/api/v1`; bearer header; ProblemDetails → `{ status, code, message, errors }`.
3. **Validation module** mirroring §5 messages.
4. **Screens:** S1 list (filters, table, inline edit, paging, delete dialog), S2 add, S3 details/lookup/delete; router `/transaction-types`, `/transaction-types/new`, `/transaction-types/:typeCode`, `/` → `/transaction-types`, `/admin` placeholder.
5. **States:** loading skeletons, empty, no-match, error alert, success status, confirmation dialogs.
6. **Wiring check:** run each Action ↔ Endpoint row (§4) and each error row (§6) end to end through the Vite proxy.
7. **Tests:** backend xUnit (validation, service, API integration on Postgres); frontend build + lint; Phase 8 BDD-based UI tests.

## 10. Build Acceptance Criteria
- [ ] Every field in §3 is bound and displays/saves correctly.
- [ ] Every action in §4 calls the listed endpoint and handles the listed statuses.
- [ ] Every rule in §5 is enforced on both sides with the same message.
- [ ] Every path in §6 shows the listed UI state.
- [ ] Seed data (01–07) lists, filters, edits, adds and deletes end to end through `http://localhost:5173` → `:5000` → Postgres `:5433`.
- [ ] Dev bypass works only in Development; no secrets in the repo.
- [ ] No legacy identifiers visible in the UI.
