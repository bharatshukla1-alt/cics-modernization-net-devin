# MG-01 — UI Build Spec (Refactor)

## 1. Spec Metadata & Traceability
- **Tran Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
- **Target UI stack** (`config/Config.md`): `target.ui.framework` = React; `target.ui.styling` = Tailwind CSS; `target.ui.motion` = microanimation; `target.ui.responsiveness` = Responsive UI; `target.frontend.port` = 5173; `target.frontend.proxyPath` = /api; `target.backend.port` = 5000.
- **Design tokens:** `config/Style-Sheet.md` (BFSI design system: `--color-*`, `--sp-*`, `--fs-*`, `--radius-*`, `--shadow-*`, `--focus-ring`; classes `.container .card .card-header .card-body .card-footer .btn .btn-primary .btn-secondary .btn-danger .btn-ghost .btn-sm .form-group .form-hint .form-error .input-group .data-table .table-wrap .alert--* .badge--* .visually-hidden .grid-2 .stack .flex-between`).
- **Sources read:** `Artefacts/Discovery/MoveGroup.md`; `Artefacts/Discovery/Screen-metadata.md` (sections COTRTLI, COTRTUP); `Input/bms/COTRTLI.bms`, `Input/bms/COTRTUP.bms`; `Input/cpy-bms/COTRTLI.cpy`, `Input/cpy-bms/COTRTUP.cpy`; `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-BSTS.md`; `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-BDD.md`.
- **Target screen ← legacy mapping (traceability only):**

| Target screen | Route | Derived from |
|---|---|---|
| S1 Transaction Types (list, filter, inline edit, delete) | `/transaction-types` | Mapset COTRTLI / map CTRTLIA (transaction CTLI, program COTRTLIC) |
| S2 Add Transaction Type | `/transaction-types/new` | Mapset COTRTUP / map CTRTUPA create path (CTTU: not-found → F5 → new details) |
| S3 Transaction Type Details (view, edit, delete) | `/transaction-types/:typeCode` | Mapset COTRTUP / map CTRTUPA lookup/update/delete paths |

## 2. Screen Purpose & User Context
The only user is the **administrator**, who maintains the reference list of two-digit transaction-type codes and their descriptions used across card transactions (BSTS a. "Purpose and workflow").
- **S1** is the landing screen: browse types in code order, narrow them by an exact code or a description fragment, correct a description in place, or remove a type that is no longer used (BSTS "CTLI — list, filter, inline update and delete").
- **S2** creates a new type in a single step, entering code and description together (BSTS "CTTU — single-record maintenance", create path).
- **S3** shows one type for review, lets the administrator change its description, or delete it (BSTS "CTTU", update/delete paths).

## 3. Screen Rationalization
**Common to all screens.** The legacy header (transaction id, program name, title lines, date, time — `TRNNAME`, `PGMNAME`, `TITLE01`, `TITLE02`, `CURDATE`, `CURTIME`) is dropped; the app header (`.app-header`) shows the product name and the signed-in user instead. The PF-key legend rows (`BUTNF02/03/07/08/10`, `FKEYS`, `FKEY04/05/06/12`) become labelled buttons. `INFOMSG` becomes an inline status line (`.alert--info`/`.alert--success`), `ERRMSG` becomes an error alert (`.alert--danger`) plus field-level errors.

**S1 — Transaction Types.**
- Card 1 "Find transaction types": `TRTYPE` → *Type code* filter, `TRDESC` → *Description contains* filter, with *Search* and *Clear* buttons. Legacy filters both lived on rows 6/8; they render as one row of two fields.
- Card 2 "Transaction types" table: columns *Code* (`TRTTYP1-7`), *Description* (`TRTYPD1-7`), *Actions* (replaces the one-character selector `TRTSEL1-7` "U"/"D"). Actions per row: **Edit** (ghost button) and **Delete** (ghost danger button).
- Inline edit: Edit turns that row's description into a text input with **Save** and **Cancel** buttons; all other row actions are disabled while a row is in edit mode. This replaces "U + ENTER → edit → F10" and enforces the legacy single-action rule structurally (BDD c. "Row action codes").
- Delete opens a confirmation dialog (replaces "D + ENTER → highlighted row → F10").
- Paging: *Previous* / *Next* buttons and "Page N" indicator (`PAGENO`), 7 rows per page (BSTS; COTRTLIC `WS-MAX-SCREEN-LINES`).
- Primary action **Add transaction type** in the page header (replaces F2=Add). **Back to admin menu** link replaces F3=Exit.
- Dropped: the invalid-action-code and "more than one action" error states (`'Action code selected is invalid'`, `'Please select only 1 action'`) cannot occur because free-text selectors no longer exist; recorded in Section 13.

**S2 — Add Transaction Type.** One card "New transaction type" with *Type code* (`TRTYPCD`) and *Description* (`TRTYDSC`) both editable at once; buttons **Create** (primary), **Cancel** (secondary). No preliminary search (agent mandate). Legacy flow "enter code → not found → F5 → enter description → ENTER → F5" collapses into: fill both fields → Create → confirm dialog → saved.

**S3 — Transaction Type Details.** Reached from S1 (row code link) or by direct URL. Card "Transaction type 05": *Type code* read-only, *Description* editable. Buttons **Save changes** (primary, enabled only when the description differs from the stored value), **Delete** (danger), **Back to list** (ghost). Save and Delete each go through a confirmation dialog (legacy two-step: "Changes validated. Press F5 to save" / "Delete this record ? Press F4 to confirm"). A *Look up another code* field at the top (`TRTYPCD` lookup path) navigates to `/transaction-types/:code`; when the code doesn't exist it shows "No transaction type with code 12." and offers **Create it** → S2 with the code pre-filled (legacy "No record found for this key in database" + "Press F05 to add").

## 4. Layout & Responsive Behaviour
- Page shell: `.container` (max `--container-max` 1200px), vertical rhythm `.stack` with `--sp-5` between cards; cards `.card` with `--shadow-sm`, `--radius-lg`.
- Grid: 12-column grid, gutter `--sp-4`. Filters card: two fields in `.grid-2` (each 6 columns) from `md` (≥768px); single column below. Detail/Add card: single column of fields, max width 640px, fields span full card width.
- Alignment convention: **labels top-aligned** above inputs in every form; all inputs in a card share one left edge; card headings (`.card-header`, `--fs-md`, `--fw-semibold`) align to the same left edge as the fields; buttons right-aligned in `.card-footer` (primary rightmost), filter buttons aligned to the input baseline row.
- Table: `.table-wrap` gives horizontal scroll below `sm`; Code column fixed width (6ch, `.numeric`, monospace `--font-mono`), Description fluid, Actions right-aligned fixed width. Below 480px the Actions collapse to icon-only buttons with `aria-label`.
- Motion (`target.ui.motion` = microanimation): 150ms (`--transition`) fade/slide for alerts, inline-edit row expansion and dialog open; respect `prefers-reduced-motion`.

## 5. Component Inventory
| Label (screen) | Control | Legacy source | Order / grouping | State | Token / class |
|---|---|---|---|---|---|
| Page title "Transaction types" (S1) | h1 | title literal "Maintain Transaction Type" (row 4) | header | read-only | `--fs-xl`, `--color-primary-900` |
| Add transaction type (S1) | button | `BUTNF02` "F2=Add" | header right | action | `.btn .btn-primary` |
| Back to admin menu (S1, S3) | link | `BUTNF03` "F3=Exit" / `FKEYS` "F3=Exit" | header left | action | `.btn .btn-ghost` |
| Type code (filter, S1) | text input, 2 chars | `TRTYPE` (len 2, UNPROT, IC) | filters row, 1st | editable | `.form-group` |
| Description contains (filter, S1) | text input, 50 | `TRDESC` (len 50, UNPROT) | filters row, 2nd | editable | `.form-group` |
| Search / Clear (S1) | buttons | ENTER key | filters row end | action | `.btn-primary` / `.btn-secondary .btn-sm` |
| Transaction types table (S1) | table | rows 12-18, `TRTSEL/TRTTYP/TRTYPD 1-7` | card 2 | read-only | `.data-table .table-wrap` |
| Code (S1 column) | cell + link to S3 | `TRTTYPn` (len 2, PROT) | col 1 | read-only | `.numeric`, `--font-mono` |
| Description (S1 column) | cell / inline input | `TRTYPDn` (len 50) | col 2 | read-only; editable in edit mode | `.form-group` in edit mode |
| Edit / Delete (S1 row) | buttons | `TRTSELn` "U"/"D" | col 3 | action | `.btn-ghost .btn-sm`, delete text `--color-danger-700` |
| Save / Cancel (S1 row, edit mode) | buttons | F10=Save (`BUTNF10`), ENTER without change | col 3 | action | `.btn-primary .btn-sm` / `.btn-secondary .btn-sm` |
| Previous / Next, "Page N" (S1) | buttons + text | `BUTNF07`, `BUTNF08`, `PAGENO` | table footer | action / read-only | `.btn-secondary .btn-sm`, `.text-muted` |
| Status line (all) | live region | `INFOMSG` (len 45) | above table / in card | read-only | `.alert--info` / `.alert--success` |
| Error alert (all) | alert | `ERRMSG` (len 78, BRT, red) | top of card | read-only | `.alert--danger` |
| Type code (S2) | text input, 2 digits | `TRTYPCD` (len 2, UNPROT, IC) | field 1 | editable, required | `.form-group` |
| Description (S2, S3) | text input, 50 | `TRTYDSC` (len 50, UNPROT) | field 2 | editable, required | `.form-group`, `.form-hint` counter |
| Type code (S3) | text (read-only) | `TRTYPCD` after successful lookup (protected) | field 1 | read-only | `--font-mono` |
| Look up another code (S3) | input + button | `TRTYPCD` lookup | top | editable | `.input-group` |
| Create / Save changes (S2, S3) | button | `FKEY05` "F5=Save" | footer right | action | `.btn .btn-primary` |
| Delete (S3) | button | `FKEY04` "F4=Delete" | footer left | action | `.btn .btn-danger` |
| Cancel / Back to list (S2, S3) | button | `FKEY12` "F12=Cancel", F3 | footer | action | `.btn .btn-secondary` / `.btn-ghost` |
| Confirm dialog (S1, S2, S3) | modal dialog | two-step confirm (F10 / F5 / F4 second press) | overlay | action | `.card`, `--shadow-lg` |

## 6. Field-Level Specification & Presentation-Layer Validation
| Field | Type / format | Length | Required | Rule → message (when) | Origin |
|---|---|---|---|---|---|
| Type code filter (S1) | digits | 1–2 | No | non-digit → "Type code must be a 2-digit number." (on Search) | BDD c. "Type filter values" (`TYPE CODE FILTER,IF SUPPLIED MUST BE A 2 DIGIT NUMBER`); BMS len 2 |
| Type code filter (S1) | normalisation | — | — | blank or "00" → no type filter; "5" → sent as "05" | BDD c. (zeros treated as blank); COTRTUPC NUMVAL normalisation |
| Description contains (S1) | text | ≤ 50 | No | trimmed; no client rule (server applies contains-match) | BMS `TRDESC` len 50; BSTS LIKE '%text%' |
| Type code (S2) | digits | 1–2 | **Yes** | empty → "Type code is required." (blur + submit); non-digit → "Type code must be numeric."; value 0/00 → "Type code must not be zero."; 1 digit is zero-padded to 2 on submit | BDD c. "Type code validation"; agent mandate (PK required on Add) |
| Description (S2, S3, S1 inline) | letters, digits, spaces | 1–50 | **Yes** | empty/blank → "Description is required."; any other character → "Description can contain only letters, numbers and spaces."; > 50 prevented by `maxLength=50` (blur + submit) | BDD c. "Description validation" |
| Description (S3, S1 inline) | change check | — | — | equal to stored value ignoring case and surrounding spaces → Save disabled, hint "No changes to save." | BDD c. "same text, different case" (`No change detected …`) |

Client rules are a convenience only; the server re-validates every field (backend spec Section 5).

## 7. Interaction & Navigation
| User action | Target behaviour | Replaces (traceability) |
|---|---|---|
| Open S1 | GET list page 1, no filters; status "Select Edit or Delete on a row to change it." | CTLI first entry; info `Type U to update, D to delete any record` |
| Search (S1) | validate filters → GET list with filters, page 1; zero results with filters → error "No transaction types match these filters." and filter fields marked invalid | ENTER with filters; `No Records found for these filter conditions` |
| Clear (S1) | reset filters, GET page 1 | blank filters |
| Next / Previous (S1) | GET next/previous keyset page; Previous disabled on page 1; Next disabled when no next page | F8 / F7; `No previous pages to display`, `No more pages to display` |
| Edit (S1 row) | row enters edit mode, focus on description input; other actions disabled | `U` + ENTER; `Update HIGHLIGHTED row. Press F10 to save` |
| Save (S1 row) | validate → PUT; success: row updated, status "Transaction type 05 updated." | F10 update; `HIGHLIGHTED row was updated` |
| Cancel (S1 row) | leave edit mode, restore value | ENTER with no change |
| Delete (S1 row) | confirm dialog "Delete transaction type 05 – Refund?" → DELETE; success: status "Transaction type 05 deleted.", reload page 1 | `D` + ENTER + F10; `HIGHLIGHTED row deleted.Hit Enter to continue`; reset to first page |
| Code link (S1 row) | navigate to `/transaction-types/05` | — (new convenience path into S3) |
| Add transaction type (S1) | navigate to `/transaction-types/new` | F2 → XCTL COTRTUPC |
| Create (S2) | validate → confirm dialog "Create transaction type 12 – Fee reversal?" → POST; success: navigate to S1 with status "Transaction type 12 created."; code already exists → field error "Transaction type 12 already exists." with link to S3 | CTTU create path (F5 / F5); `Changes committed to database` |
| Cancel (S2) | navigate back to S1, no save | F12 / F3 |
| Open S3 | GET by code; 404 → not-found state with **Create it** (→ S2 pre-filled) | CTTU lookup; `No record found for this key in database`, `Press F05 to add. F12 to cancel` |
| Save changes (S3) | validate → confirm dialog → PUT; success status "Changes saved." | ENTER → `Changes validated.Press F5 to save` → F5 → `Changes committed to database` |
| Delete (S3) | confirm dialog → DELETE; success: navigate to S1 with status "Transaction type 05 deleted." | F4 → `Delete this record ? Press F4 to confirm` → F4 → `Delete successful.` |
| Cancel in any confirm dialog | close dialog, nothing saved; status "Nothing was changed." | F12 → `Update was cancelled` / `Delete was cancelled` |
| Back to admin menu | navigate to the admin menu route `/admin` (out of MG-01; placeholder link) | F3 → XCTL COADM01C |

- **State model:** S1 `browsing → editing(row) → saving → browsing`; `browsing → confirmDelete(row) → deleting → browsing(page 1)`. S2 `editing → confirmCreate → creating → S1`. S3 `loading → viewing | notFound`; `viewing → confirmSave → saving → viewing`; `viewing → confirmDelete → deleting → S1`.
- **State held client-side:** filters, current page's first/last code, page number (React state + URL query `?typeCode=&q=&page=`); no server session (`target.state.management` = stateless REST + client-held state).
- **Dev setup:** `vite.config.js` proxies `/api` → `http://localhost:5000` (`target.frontend.proxyPath`, `target.backend.port`); dev server on port 5173. When the OAuth provider (`target.auth.provider.url` http://localhost:8081) is unavailable in local dev, the API client sends `Authorization: Bearer dummy-dev-token` (enabled only when `import.meta.env.DEV` and `VITE_AUTH_BYPASS=true`).

## 8. Component States & Content
- **Buttons:** default / hover (`.btn-primary:hover`) / focus (`--focus-ring`) / active / disabled (`.btn:disabled`, `aria-disabled`) / loading (spinner + label "Saving…", "Deleting…", button disabled).
- **Inputs:** default border `--color-border`; focus `--focus-ring`; invalid border `--color-danger-600` + `.form-error` text below + `aria-invalid="true"`; read-only background `--color-surface-alt`.
- **Table:** loading = 7 skeleton rows; empty (no filters) = "No transaction types yet." + Add button; empty (with filters) = error alert above; zebra rows `--color-surface-alt`; row in edit mode outlined `--color-primary-300`.
- **Alerts:** success `.alert--success` auto-dismiss after 6s (stays focusable via live region); error `.alert--danger` persists until next action; server error copy shown as returned by the API `message` field; technical detail is never shown.
- **Copy:** labels "Type code", "Description", "Description contains", "Transaction types", "New transaction type", "Transaction type {code}". No legacy identifiers in any copy.

## 9. Accessibility (WCAG 2.2 AA)
- Every input has a visible `<label htmlFor>`; hints/errors linked with `aria-describedby`; required fields marked with text "(required)" not colour alone.
- Focus order follows visual order: filters → Search → Clear → table rows (Code link, Edit, Delete) → paging → Add. Entering edit mode moves focus to the description input; Save/Cancel return focus to the row's Edit button.
- Dialogs: `role="dialog"`, `aria-modal="true"`, labelled by heading, focus trapped, Escape = Cancel, focus returns to the triggering button.
- Status line `aria-live="polite"`, error alert `role="alert"`; on submit with errors, focus moves to the first invalid field.
- Table uses `<th scope="col">`, caption "Transaction types, page N" (`.visually-hidden`).
- Contrast: text `--color-text` on `--color-surface` and buttons per style sheet meet AA; focus ring `--focus-ring` visible on all controls; target size ≥ 24×24px (WCAG 2.5.8).
- Full keyboard operation; no hover-only actions.

## 10. Data Privacy & BFSI Compliance
- **Personal data:** none. Screens show only reference codes and descriptions (`TRANSACTION_TYPE`), no customer, account or card data, so no masking or consent touchpoints apply.
- **Formatting:** codes always shown as 2 digits with leading zero; no monetary values on these screens.
- **State-changing actions:** create, update and delete each require explicit confirmation (Section 7).
- **Session:** inactivity timeout handled by the app shell: after 15 minutes idle show a warning dialog, then sign out and clear client state (timeout value is an assumption, Section 13).
- **Hygiene:** bearer token kept in memory (not localStorage); no tokens or descriptions in URLs other than the type code and filter terms; no console logging of API payloads in production builds.

## 11. Frontend Folder Structure
```text
Target/MG-01/Frontend/
├── index.html
├── package.json
├── vite.config.js            # /api proxy → http://localhost:5000
├── tailwind.config.js        # theme.extend maps Style-Sheet tokens
├── postcss.config.js
├── .env.example              # VITE_AUTH_BYPASS, VITE_API_BASE=/api/v1
└── src/
    ├── main.jsx
    ├── App.jsx               # router: /transaction-types, /new, /:typeCode
    ├── styles/
    │   └── bfsi-theme.css    # copied from config/Style-Sheet.md
    ├── api/
    │   └── transactionTypesApi.js
    ├── hooks/
    │   └── useTransactionTypes.js
    ├── validation/
    │   └── transactionType.js
    ├── components/
    │   ├── ConfirmDialog.jsx
    │   ├── Alert.jsx
    │   └── TextField.jsx
    └── screens/
        ├── TransactionTypeListPage.jsx
        ├── TransactionTypeAddPage.jsx
        └── TransactionTypeDetailPage.jsx
```

## 12. Acceptance Criteria
- [ ] S1 loads page 1 ordered by code, 7 rows per page; Previous disabled on page 1; Next disabled on the last page.
- [ ] Filter by code "05" shows only 05; filter "AB" shows "Type code must be a 2-digit number."; description filter "PAY" matches "PAYMENT" (contains match, case-sensitive like the legacy LIKE).
- [ ] Filters with no match show "No transaction types match these filters." and mark the filter fields invalid.
- [ ] Only one row can be in edit mode; Save is disabled until the description changes (case/space-insensitive).
- [ ] Description blank / with punctuation is rejected with the Section 6 messages on both S1 inline edit, S2 and S3.
- [ ] S2 requires both fields; type code empty, non-numeric or zero is rejected; "5" is created as "05"; duplicate code shows the exists error.
- [ ] Create, save and delete each require a confirmation dialog; Cancel changes nothing and shows "Nothing was changed."
- [ ] S3 for an unknown code shows the not-found state with **Create it** pre-filling S2.
- [ ] Server error messages (e.g. delete blocked by dependent records) appear in the error alert.
- [ ] No program, transaction, map or PF-key identifiers appear anywhere on screen.
- [ ] Layout follows Section 4: top-aligned labels, shared input left edge, headings aligned with content, right-aligned footers; no ragged fields at 375px, 768px and 1280px widths.
- [ ] Keyboard-only run of every flow succeeds; axe scan reports no WCAG 2.2 AA violations.
- [ ] Dev proxy sends `/api` to port 5000; `dummy-dev-token` is used only when `VITE_AUTH_BYPASS=true` in dev.

## 13. Deviations, Assumptions & Gaps
1. **Deviation — separate Add screen:** legacy reaches create only via a failed lookup in CTTU; the target has a direct Add route with code + description entered together (agent mandate).
2. **Deviation — free-text row selector removed:** "U"/"D" selector codes become buttons, so legacy errors `Action code selected is invalid` and `Please select only 1 action` have no UI trigger; single-action behaviour is enforced by disabling other rows.
3. **Deviation — confirmation dialogs** replace the legacy second key press (F10/F5/F4); the explicit two-step confirmation is preserved.
4. **Equivalence — description filter case:** legacy `LIKE '%text%'` is case-sensitive; the target keeps that (backend spec). The UI passes the trimmed text unchanged and shows the hint "Matches exact letter case." under the field.
5. **Assumption — session timeout 15 minutes**; no timeout is defined in the legacy source.
6. **Gap — admin menu target:** F3 returns to COADM01C (MG-02). Until MG-02 is built, "Back to admin menu" points to a placeholder route `/admin`.
7. **Gap — Screen-metadata attributes:** `TRTSELn` is PROT in BMS and unprotected at run time by COTRTLIC; the target uses buttons, so this has no effect.
8. **Not carried over:** legacy page number is shown; legacy date/time header fields are dropped as non-functional.
