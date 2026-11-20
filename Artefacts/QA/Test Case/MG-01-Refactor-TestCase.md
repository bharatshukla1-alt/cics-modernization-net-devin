# MG-01 — Refactor Test Cases

## 1. Document Metadata
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Programs / transactions (BSTS):** CTLI / COTRTLIC (list, filter, inline update, delete); CTTU / COTRTUPC (lookup, add, update, delete)
- **Sources (only):** `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-BSTS.md`, `-BDD.md`, `-DB-Details.md`
- **Target stack under test:** React + Tailwind UI (:5173) → ASP.NET Core Web API .NET 9 (:5000) → PostgreSQL `carddemo` (:5433); OAuth 2.0 bearer tokens
- **ID scheme:** `MG01-<category>-NNN`; categories FE, HP, AUTHN, AUTHZ, FV, BR, DB, CON, TX, HTTP, UI, PSF, BND.

## 2. Scope & Objective
Screens under test: the transaction-type list (filter, page, inline update, delete), and single-record maintenance (look up, add, update, delete). The objective is functional equivalence with COTRTLIC/COTRTUPC behaviour on table `TRANSACTION_TYPE`, plus authentication, authorisation, validation, HTTP and UI quality guards.

## 3. Test Data & Preconditions
- Seed (DB-Details §c): `01 PURCHASE, 02 PAYMENT, 03 CREDIT, 04 AUTHORIZATION, 05 REFUND, 06 REVERAL, 07 ADJUSTMENT`.
- Test rows use codes 90–99 and are removed after each case; seed rows are restored if changed.
- Identities: **admin** (authorised administrator; local dev token), **anonymous** (no token), **non-admin** (authenticated without admin role) — BSTS a. "The only actor is the CardDemo administrator".
- Table `TRANSACTION_TYPE(TR_TYPE CHAR(2) PK, TR_DESCRIPTION VARCHAR(50) NOT NULL)` (DB-Details §b).

## 4. Test Cases

### FE — Functional Equivalence
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-FE-001 | List order and page size | seed only | open list | rows 01..07 in ascending code order, max 7 per page | BDD a. "Administrator opens the transaction type list"; BSTS CTLI |
| MG01-FE-002 | Single-digit code normalised | seed | look up code `5` | record `05 REFUND` shown | BDD c. "Type code validation" (5 → 05) |
| MG01-FE-003 | Blank/zero type filter = no filter | seed | filter type `00` | all 7 seed rows | BDD c. "Type filter values" (00) |
| MG01-FE-004 | Description filter is contains-match | seed | filter description `PAY` | only `02 PAYMENT` | BDD a. "Filter by partial description"; BSTS LIKE '%text%' |
| MG01-FE-005 | Description filter is case-sensitive | seed | filter description `pay` | no rows; no-match error | BSTS LIKE (Db2 LIKE case-sensitive) — assumption A1 |
| MG01-FE-006 | Change check ignores case/spaces | 05 REFUND | update description to ` refund ` | rejected as "no change detected" | BDD c. "Description validation" (same text different case) |
| MG01-FE-007 | Description stored trimmed | none | create `95` with `  Fee reversal ` | DB `tr_description = 'Fee reversal'` | BSTS COTRTUPC 9600 TRIM; DB-Details VARCHAR(50) |
| MG01-FE-008 | Delete returns list to page 1 | ≥8 rows, on page 2 | delete a row | list shows page 1 | BDD b. "Delete of a row (D)" (paging reset) |

### HP — Happy Path
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-HP-001 | Filter by exact code | seed | filter `01` | only 01 | BDD a. "Filter by exact type code" |
| MG01-HP-002 | Inline update | seed | Edit row 01 → "Purchases" → confirm save | row shows Purchases; success message | BDD a. "Update a description from the list" |
| MG01-HP-003 | Inline delete | row 90 exists | Delete row 90 → confirm | row removed; success message | BDD a. "Delete a transaction type from the list" |
| MG01-HP-004 | Add new type | 92 absent | add 92 "Fee reversal" → confirm | created; "committed" success message | BDD a. "Add a new transaction type" |
| MG01-HP-005 | Update on maintenance screen | 91 "OLD" | open 91 → "Retail purchase" → confirm | saved; success message | BDD a. "Update description on the maintenance screen" |
| MG01-HP-006 | Delete on maintenance screen | 93 exists | open 93 → Delete → confirm | deleted; success message | BDD a. "Delete on the maintenance screen" |
| MG01-HP-007 | Not found offers create | 12 absent | open code 12 | not-found message + create option | BDD a. Add scenario ("No record found…", "Press F05 to add") |
| MG01-HP-008 | Exit returns to caller/admin menu | on list | choose Back/Exit | navigates away to admin menu | BDD b. "F3 returns to the caller" / "F3 with no caller" |

### AUTHN — Authentication Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-AUTHN-001 | List without token | anonymous | GET list | 401 | BSTS a. (admin-only function) |
| MG01-AUTHN-002 | Create without token | anonymous | POST type | 401, no row written | BSTS a. |
| MG01-AUTHN-003 | Delete without token | anonymous | DELETE 05 | 401, 05 still present | BSTS a. |

### AUTHZ — Authorization Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-AUTHZ-001 | Non-admin read | non-admin token | GET list | 403 | BSTS a. (sets user type admin) |
| MG01-AUTHZ-002 | Non-admin write | non-admin token | PUT 05 | 403, unchanged | BSTS a. |

### FV — Field-Level Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-FV-001 | Type filter non-numeric | seed | filter `A1` | 400; "Type code must be a 2-digit number" (intent) | BDD c. "Type filter values" |
| MG01-FV-002 | Type code blank (add) | — | add with code blank | 400; field typeCode "must be supplied" | BDD c. "Type code validation" (blank) |
| MG01-FV-003 | Type code non-numeric | — | add code `AB` | 400; "must be numeric" | BDD c. (AB) |
| MG01-FV-004 | Type code zero | — | add code `00` | 400; "must not be zero" | BDD c. (00) |
| MG01-FV-005 | Description blank | — | add 96, description blank | 400; "Transaction Desc must be supplied" (intent) | BDD c. "Description validation" |
| MG01-FV-006 | Description punctuation | — | add 96 "Purchase-Online" | 400; "numbers or alphabets only" (intent) | BDD c. "Description validation" |
| MG01-FV-007 | Description letters/digits/spaces | — | add 96 "Purchase Online 2" | 201 | BDD c. "Description validation" |
| MG01-FV-008 | Duplicate code on add | 05 exists | add 05 "X" | 409; already exists | DB-Details PK TR_TYPE |
| MG01-FV-009 | Inline description blank | seed | inline edit 01 → blank → save | rejected "must be supplied" | BDD c. (list row TRTYPD) |

### BR — Cross-Field / Business-Rule Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-BR-001 | Filters with no match | seed | filter `77` | no rows; "No Records found for these filter conditions" (intent); filter flagged | BDD c. "No rows match the filters" |
| MG01-BR-002 | Only one row action at a time | seed | start edit on row 01, try action on row 02 | second action not possible | BDD c. "Row action codes" (D on two rows) |
| MG01-BR-003 | Confirmation required before delete | seed | Delete 05 → cancel | nothing deleted; "cancelled" message | BDD d. "F12 cancels a pending action" |
| MG01-BR-004 | Confirmation required before save | 91 | change → cancel confirm | nothing saved | BDD d. "F12 cancels…" (changes validated) |
| MG01-BR-005 | Filter change clears pending row action | row in edit | change filter and search | edit discarded; list re-read | BDD c. "Selections are cleared when a filter changes" |
| MG01-BR-006 | Unchanged description cannot be saved (API) | 05 REFUND | PUT 05 "REFUND" | 422 no-change | BDD c. (same text) |

### DB — DB Side Effects
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-DB-001 | Create persists row | 92 absent | POST 92 "Fee reversal" | `SELECT * WHERE tr_type='92'` → ('92','Fee reversal') | DB-Details CRUD C (COTRTUPC) |
| MG01-DB-002 | Update persists description only | 91 OLD | PUT 91 "New" | row ('91','New'); key unchanged | DB-Details CRUD U |
| MG01-DB-003 | Delete removes row | 93 | DELETE 93 | no row 93 | DB-Details CRUD D |
| MG01-DB-004 | Rejected write leaves DB unchanged | seed | POST 96 "Bad-Desc" | no row 96 | BDD c. + DB-Details |

### CON — Concurrency / Idempotency
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-CON-001 | Double-submit create | 97 absent | POST 97 twice | first 201, second 409; one row | DB-Details PK |
| MG01-CON-002 | Update of concurrently deleted row | 94 exists, then deleted by other session | PUT 94 | 404 "Deleted by others?" (intent) | BDD e. "Concurrent delete before update on the list"; BDD d. +100 |
| MG01-CON-003 | Update while row locked | 94 locked by other tx | PUT 94 | 409 lock conflict (intent "someone else updating") | BDD d. -911 scenarios |
| MG01-CON-004 | Delete twice | 93 exists | DELETE 93 twice | 204 then 404 | DB-Details CRUD D |

### TX — Multi-Step Transaction Integrity
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-TX-001 | Delete blocked by dependent rows | 98 with child row in a table referencing TR_TYPE (ON DELETE RESTRICT) | DELETE 98 | 409 "delete associated child records first"; 98 and child remain | BDD e. "Delete of a referenced type"; DB-Details Other notes (FK) |
| MG01-TX-002 | Failed update leaves original | 94 locked | PUT 94 | original description unchanged | BDD d. -911 |

### HTTP — HTTP Semantics
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-HTTP-001 | Read existing | seed | GET 05 | 200 | BSTS CTTU read |
| MG01-HTTP-002 | Read missing | — | GET 12 | 404 | BDD a. not found |
| MG01-HTTP-003 | Create | — | POST 92 | 201 + Location | BSTS CTTU add |
| MG01-HTTP-004 | Update | 91 | PUT 91 | 200 | BSTS update |
| MG01-HTTP-005 | Delete | 93 | DELETE 93 | 204 | BSTS delete |
| MG01-HTTP-006 | Validation | — | POST code AB | 400 | BDD c. |
| MG01-HTTP-007 | Conflict | 05 | POST 05 | 409 | DB-Details PK |
| MG01-HTTP-008 | Database unavailable | DB stopped | GET list | 503 service unavailable (intent "Db2 access failure") | BDD d. "Db2 unavailable" |

### UI — UI Rendering
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-UI-001 | List screen renders | seed | open list | filters (type, description), table, paging, add action, page number | BDD a./b. CTLI screen |
| MG01-UI-002 | Invalid filter highlighted | seed | filter A1 | field marked invalid with message | BDD c. (TRTYPE turns red) |
| MG01-UI-003 | Inline edit shows editable description | seed | Edit row 05 | description becomes input with Save/Cancel | BDD a. update scenario |
| MG01-UI-004 | Add form shows both fields | — | open add | code and description inputs, Create/Cancel | BDD a. add scenario |
| MG01-UI-005 | Field errors shown on add | — | Create with empty fields | messages under both fields | BDD c. |
| MG01-UI-006 | Not-found state | — | open 12 | message + create option | BDD a. add scenario |
| MG01-UI-007 | Empty table state | table empty | open list | "no records" empty state | BDD e. "Empty table" |
| MG01-UI-008 | No legacy identifiers on screen | — | inspect all screens | no program/transaction/map/PF-key ids | BSTS (target-relevant notes) — assumption A2 |

### PSF — Pagination / Sort / Filter
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-PSF-001 | Next page | seed + 90,91,92 | Next from page 1 | page 2 = 90,91,92; Next disabled | BDD e. "F8 reaching the end" |
| MG01-PSF-002 | Previous page | on page 2 | Previous | page 1 = 01..07 | BDD b. backward read |
| MG01-PSF-003 | Previous on first page | page 1 | Previous | not possible / stays page 1 ("No previous pages") | BDD e. "F7 on the first page" |
| MG01-PSF-004 | Exactly 7 rows | seed only | open list | Next disabled | BDD e. "Exactly 7 rows on a page" |
| MG01-PSF-005 | Combined filters | seed | type 02 + description PAY | only 02 | BDD a. filter rules |
| MG01-PSF-006 | Filter + paging resets to page 1 | on page 2 | search new filter | page 1 of filtered results | BSTS CTLI (filter change resets paging) |

### BND — Boundary & Numeric
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG01-BND-001 | Description 50 chars | — | add 96 with 50 letters | 201 | DB-Details VARCHAR(50) |
| MG01-BND-002 | Description 51 chars (API) | — | POST 96 with 51 letters | 400 | DB-Details VARCHAR(50) |
| MG01-BND-003 | Whitespace-only description | — | add 96 "   " | 400 "must be supplied" | BDD c. |
| MG01-BND-004 | Code 99 (max) | — | add 99 "Max" | 201 | DB-Details CHAR(2) |
| MG01-BND-005 | Code 3 digits | — | add `123` | 400 numeric/length | DB-Details CHAR(2) |
| MG01-BND-006 | Wildcard in description filter | seed | filter `%` | no rows (literal match) | BSTS LIKE — assumption A3 |

## 5. Traceability Matrix
| Source scenario / rule | Covered by |
|---|---|
| BDD a. opens list | FE-001, UI-001 |
| BDD a. filter exact code / partial description | HP-001, FE-004, PSF-005 |
| BDD a. update from list | HP-002, UI-003 |
| BDD a. delete from list | HP-003 |
| BDD a. add new type | HP-004, HP-007, DB-001 |
| BDD a. update / delete on maintenance screen | HP-005, HP-006 |
| BDD b. first entry / re-entry / priming query | UI-001, HTTP-008 |
| BDD b. PF keys CTLI / CTTU, F3 returns | HP-008, PSF-001..003 |
| BDD b. Db2 operations (R/U/D, C) | DB-001..003 |
| BDD c. type filter values | FV-001, FE-003, UI-002 |
| BDD c. no rows match filters | BR-001 |
| BDD c. row action codes / selections cleared | BR-002, BR-005 |
| BDD c. description validation | FV-005..007, FE-006, BND-003 |
| BDD c. type code validation | FV-002..004, FE-002 |
| BDD d. Db2 unavailable | HTTP-008 |
| BDD d. update failures (+100, -911, other) | CON-002, CON-003, TX-002 |
| BDD d. delete failures (-532, other) | TX-001 |
| BDD d. CTTU read error / lock / save failure | CON-003 (read/insert failures not reproducible — gap G2) |
| BDD d. invalid key | Not testable (buttons only) — gap G3 |
| BDD d. unexpected state abend | Not testable — gap G3 |
| BDD d. F12 cancel | BR-003, BR-004 |
| BDD e. empty table, F7 first page, F8 last page, exactly 7 rows | UI-007, PSF-003, PSF-001, PSF-004 |
| BDD e. concurrent delete, referenced type | CON-002, TX-001 |
| BDD e. re-entering same not-found key | HP-007 |
| BDD e. return from CTTU resets list | FE-008 / HP-008 |
| DB-Details PK, VARCHAR(50), CHAR(2) | FV-008, CON-001, BND-001..005 |

## 6. Coverage & Gaps
- **A1:** case sensitivity of the description filter is inferred from Db2 `LIKE` semantics.
- **A2:** "no legacy identifiers" comes from the modernization goal, not the legacy behaviour.
- **A3:** literal `%` matching is inferred; legacy passes user text into `LIKE` unescaped.
- **G1:** AUTHZ needs a non-admin token; with only the local dev token, AUTHZ cases may be Blocked.
- **G2:** CTTU Db2 read errors and insert failures other than duplicates cannot be triggered without fault injection.
- **G3:** legacy invalid-key and abend paths have no counterpart in a button-driven UI.
- **HTTP-008** requires stopping the database temporarily.
