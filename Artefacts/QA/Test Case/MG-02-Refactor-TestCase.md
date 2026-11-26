# MG-02 — Refactor Test Cases

## 1. Document Metadata
- **Tran Group:** MG-02 — Sign-on and Navigation Shell
- **Programs / transactions (BSTS):** CC00 / COSGN00C (sign-on), CM00 / COMEN01C (main menu), CA00 / COADM01C (admin menu)
- **Sources (only):** `Artefacts/TranGroupData/MG-02-SignOnAndNavigationShell-BSTS.md`, `-BDD.md`, `-VSAM-Details.md` (data artefact for a VSAM-only group)
- **Target stack under test:**
  - Shell UI: React + Tailwind (:5173)
  - SignOnService: ASP.NET Core 9 (:5001)
  - TransactionTypeService (MG-01): ASP.NET Core 9 (:5000), validating SignOnService tokens
  - Database: PostgreSQL `carddemo` (:5433)
  - Auth: OAuth 2.0 bearer JWT (RS256)
- **ID scheme:** `MG02-<category>-NNN`. Categories: FE, HP, AUTHN, AUTHZ, FV, BR, DB, CON, TX, HTTP, UI, PSF, BND.

## 2. Scope & Objective
Under test:
- the sign-in screen and its USRSEC lookup;
- the customer services (main) menu;
- the administration menu;
- navigation from the admin menu into the migrated MG-01 screens;
- sign-out.

The objective is functional equivalence with COSGN00C, COMEN01C and COADM01C, plus authentication, authorisation, validation, HTTP and UI quality guards.

## 3. Test Data & Preconditions
- **Seed** (VSAM-Details §d): `usrsec` holds ADMIN001–ADMIN005 (type A) and USER0001–USER0005 (type U), all with password `PASSWORD`.
- **Identities:**
  - admin: a token from signing in as ADMIN001
  - user: a token from signing in as USER0001
  - anonymous: no token
- **Rate limit:** the sign-in limit is set to 1000 per minute for the functional run. It is restored to the default of 5 for HTTP-006.
- **Menu routes:** COTRTLIC → `/transaction-types` and COTRTUPC → `/transaction-types/new`. All other options are NOT_INSTALLED.

## 4. Test Cases

### FE — Functional Equivalence
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-FE-001 | Credentials upper-cased | seed | sign in `user0001` / `password` | 200; user.userId `USER0001` | BDD a. "User id and password are not case-sensitive"; BSTS PROCESS-ENTER-KEY UPPER-CASE |
| MG02-FE-002 | Type A routes to admin | seed | sign in ADMIN001 | userType `A`; token role `carddemo.admin` | BDD a. "Administrator signs on…"; BSTS READ-USER-SEC-FILE XCTL COADM01C |
| MG02-FE-003 | Type U routes to main menu | seed | sign in USER0001 | userType `U`; role `carddemo.user`; no scope | BDD a. "Regular user signs on…" |
| MG02-FE-004 | Main menu options and order | user token | GET menus/main | 11 options, 1 "Account View" … 11 "Pending Authorization View" | BDD a. "Main menu lists the eleven user options"; COMEN02Y |
| MG02-FE-005 | Admin menu options and order | admin token | GET menus/admin | 6 options, 1 "User List" … 6 "Transaction Type Maintenance" | BDD a. "Admin menu lists the six admin options"; COADM02Y |
| MG02-FE-006 | Legacy message text kept | seed | unknown user; wrong password | API messages "User not found. Try again ..." and "Wrong Password. Try again ..." | BDD d. |
| MG02-FE-007 | Validation order | — | both fields blank | `USER_ID_REQUIRED` (not the password error) | BDD c. "User id is required" / BSTS EVALUATE order |
| MG02-FE-008 | Trailing spaces ignored (PIC X(8) padding) | seed | password `PASSWORD ` | 200 | VSAM-Details §b X(08); BSTS SEC-USR-PWD = WS-USER-PWD |

### HP — Happy Path
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-HP-001 | Admin signs in (UI) | seed | UI sign in ADMIN001 | lands on `/admin` "Administration" | BDD a. Administrator signs on |
| MG02-HP-002 | User signs in (UI) | seed | UI sign in USER0001 | lands on `/menu` "Customer services" | BDD a. Regular user signs on |
| MG02-HP-003 | Admin option 5 → MG-01 list | admin in UI | click "Transaction Type List/Update" | `/transaction-types`, 7 seed rows | BDD a. Valid admin option (5 → COTRTLIC) |
| MG02-HP-004 | Admin option 6 → MG-01 add | admin in UI | click "Transaction Type Maintenance" | `/transaction-types/new` form | BDD a. Valid admin option (6 → COTRTUPC) |
| MG02-HP-005 | Sign out | signed in | click Sign out | "Thank you for using CardDemo." | BDD a. Sign-off; BDD "Either menu returns to sign-on with PF3" |
| MG02-HP-006 | Quick option on admin menu | admin in UI | Go to option `5` | navigates to `/transaction-types` | BDD a. Valid admin option |

### AUTHN — Authentication Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-AUTHN-001 | Main menu without token | anonymous | GET menus/main | 401 `UNAUTHENTICATED` | BDD b. "Menu started without a COMMAREA goes back to sign-on" |
| MG02-AUTHN-002 | Admin menu without token | anonymous | GET menus/admin | 401 | same |
| MG02-AUTHN-003 | UI guard | no session | open `/admin` | redirected to sign-in | same |
| MG02-AUTHN-004 | Tampered token | admin token with altered signature | GET menus/admin | 401 | BSTS (COMMAREA integrity) → target token validation |
| MG02-AUTHN-005 | Dev bypass removed in MG-01 | — | MG-01 GET list with `dummy-dev-token` | 401 | Architecture (MG-01 trusts SignOnService only) |

### AUTHZ — Authorization Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-AUTHZ-001 | User on admin menu | user token | GET menus/admin | 403 `FORBIDDEN` | BDD Gap 6 (role enforced on entry) |
| MG02-AUTHZ-002 | Admin on main menu | admin token | GET menus/main | 403 | BSTS routing by type |
| MG02-AUTHZ-003 | User reads MG-01 | user token | MG-01 GET list | 403 | BSTS admin-only functions (COADM02Y) |
| MG02-AUTHZ-004 | User writes MG-01 | user token | MG-01 PUT `05` | 403; 05 unchanged | same |
| MG02-AUTHZ-005 | UI guard by role | user in UI | open `/transaction-types` | redirected to `/menu` | same |

### FV — Field-Level Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-FV-001 | User ID blank | — | `""` / `PASSWORD` | 400 `USER_ID_REQUIRED` "Please enter User ID ..." | BDD c. User id is required |
| MG02-FV-002 | Password blank | — | `USER0001` / `""` | 400 `PASSWORD_REQUIRED` | BDD c. Password is required |
| MG02-FV-003 | Whitespace-only user ID | — | `"   "` | 400 `USER_ID_REQUIRED` | BDD c. (SPACES) |
| MG02-FV-004 | User ID over 8 | — | `USER00011` | 400 `FIELD_TOO_LONG` | VSAM-Details SEC-USR-ID X(08) |
| MG02-FV-005 | Password over 8 | — | `PASSWORD1` | 400 `FIELD_TOO_LONG` | SEC-USR-PWD X(08) |
| MG02-FV-006 | UI user ID required | UI | Sign in with empty form | "Enter your user ID."; focus on User ID | BDD c. |
| MG02-FV-007 | UI password required | UI | user ID only | "Enter your password."; focus on Password | BDD c. |
| MG02-FV-008 | UI option range | customer menu | Go with `0`, `12`, blank | "Enter a valid option number." | BDD c. "Menu option must be a listed number" |

### BR — Cross-Field / Business-Rule Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-BR-001 | Unknown user | seed | `NOUSER01` | 401 `USER_NOT_FOUND` | BDD d. Unknown user id (RESP 13) |
| MG02-BR-002 | Wrong password | seed | `USER0001` / `WRONG` | 401 `WRONG_PASSWORD` | BDD d. Wrong password |
| MG02-BR-003 | Password case-insensitive on input | seed | `PassWord` | 200 | BDD a. not case-sensitive |
| MG02-BR-004 | Unmigrated option | customer menu | click Account View | "This option Account View is not installed yet." | BDD d. option not installed (INQUIRE/PGMIDERR) |
| MG02-BR-005 | Admin-only and DUMMY options | unit level | MenuService tests | ADMIN_ONLY before COMING_SOON | BDD e. edge cases (unreachable with shipped data) |
| MG02-BR-006 | Wrong password clears field | UI | wrong password | message shown; password emptied; focus on Password | BDD d. cursor on PASSWD |

### DB — DB Side Effects
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-DB-001 | Sign-in is read-only | seed | 5 sign-ins (success + failures) | `usrsec` checksum unchanged | VSAM-Details §a (Read only) |
| MG02-DB-002 | Runtime login cannot write | — | INSERT/UPDATE as `carddemo_signon` | permission denied | DB-Status gap 3 (least privilege) |
| MG02-DB-003 | Seed integrity | — | query usrsec | 10 rows: 5 A, 5 U | VSAM-Details §d |

### CON — Concurrency / Idempotency
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-CON-001 | Parallel sign-ins | seed | 10 concurrent admin sign-ins | all 200 | BSTS stateless pseudo-conversation |
| MG02-CON-002 | Double submit | seed | same sign-in twice | both 200 with valid tokens; no server state | same |

### TX — Multi-Step Transaction Integrity
Not applicable for this group: MG-02 performs no writes (USRSEC is read-only, VSAM-Details §a).

### HTTP — HTTP Semantics
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-HTTP-001 | Sign-in success | seed | POST sign-in | 200 `{accessToken, tokenType:"Bearer", expiresIn:900, user}` | Backend spec §3 |
| MG02-HTTP-002 | Malformed JSON | — | body `{` | 400 `VALIDATION_FAILED` | Backend spec §7 |
| MG02-HTTP-003 | Unknown field | — | extra `role` | 400 | Backend spec §5 |
| MG02-HTTP-004 | 401 shape | anonymous | GET menus/main | `application/problem+json`; `WWW-Authenticate: Bearer` | Backend spec §7 |
| MG02-HTTP-005 | 403 shape | user token | GET menus/admin | problem+json `FORBIDDEN` | same |
| MG02-HTTP-006 | Rate limit | default limit 5/min | 6 sign-ins in a minute | 6th → 429 `TOO_MANY_REQUESTS` | Backend spec §8 |
| MG02-HTTP-007 | Database down | stop db | sign in | 503 `UNABLE_TO_VERIFY` | BDD d. "USRSEC read fails for another reason" |
| MG02-HTTP-008 | Security headers | — | POST sign-in | nosniff, DENY, `Cache-Control: no-store` | Backend spec §8 |
| MG02-HTTP-009 | Discovery and JWKS | — | GET well-known docs | 200; issuer set; JWKS has no private parts | Architecture |

### UI — UI Rendering
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-UI-001 | Sign-in page | — | open `/signin` | labels; masked password; demo notice; no legacy IDs | UI spec §3, §6 |
| MG02-UI-002 | Error banner | — | wrong password | `role="alert"` banner | UI spec §9 |
| MG02-UI-003 | Customer menu | user | open menu | 11 tiles; header shows "LAWRENCE THOMAS" | BDD a. main menu |
| MG02-UI-004 | Admin menu chips | admin | open menu | options 1–4 "Not available yet"; 5–6 without chip | BDD a. admin menu |
| MG02-UI-005 | No legacy identifiers | menus | page text | no `COSGN00C`, `COMEN01C`, `COADM01C`, `CC00`, `CM00`, `CA00`, `PF3` | UI spec §3 |
| MG02-UI-006 | Thank-you page | signed out | — | "Thank you for using CardDemo." and "Sign in again" | BDD a. Sign-off |

### PSF — Pagination / Sort / Filter
Not applicable for this group: MG-02 has no list or inquiry surface. The MG-01 list is covered by MG-01's suite.

### BND — Boundary & Numeric Edge Cases
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG02-BND-001 | 8-character user ID | seed | `ADMIN001` | 200 | X(08) |
| MG02-BND-002 | Leading space in password | seed | ` PASSWOR` (8 characters, the field maximum) | 401 `WRONG_PASSWORD` | X(08) compare (leading space significant) |
| MG02-BND-003 | Lower case with trailing spaces | seed | `admin001  ` | 200 | UPPER-CASE + padding |
| MG02-BND-004 | Option with surrounding space | admin menu | Go ` 5` | navigates to `/transaction-types` | BDD b. "Option input is right-trimmed and zero-filled" |
| MG02-BND-005 | Oversized body | — | body > 1 KB | 413 or 400 | Backend spec §5 |

## 5. Traceability Matrix
| BDD scenario / BSTS rule | Test Case IDs |
|---|---|
| a. Administrator signs on → admin menu | FE-002, HP-001 |
| a. Regular user → main menu | FE-003, HP-002 |
| a. Not case-sensitive | FE-001, BR-003, BND-003 |
| a. PF3 sign-off | HP-005, UI-006 |
| a. Main menu 11 options / valid option transfers | FE-004, UI-003, BR-004 |
| a. Pending Authorization installed check | BR-004 (status mechanism) |
| a. Admin menu 6 options / valid admin option | FE-005, HP-003, HP-004, HP-006, UI-004 |
| a. Either menu PF3 → sign-on | HP-005 |
| b. First entry / no COMMAREA → sign-on | AUTHN-001..003 |
| b. Re-entry receives map | HP-001..006 (stateless token replaces context) |
| b. COMMAREA built before transfer | FE-002, FE-003 (token claims) |
| b. Option right-trimmed / zero-filled | BND-004 |
| c. User id required / password required | FV-001..003, FV-006, FV-007, FE-007 |
| c. Password not displayed | UI-001 |
| c. Menu option must be listed | FV-008 |
| d. Unknown user / wrong password / read fails | BR-001, BR-002, HTTP-007, FE-006 |
| d. Unsupported key | Not testable (no function keys); see gap G2 |
| d. Option not installed (COMEN01C / COADM01C) | BR-004, UI-004 |
| e. DUMMY / admin-only | BR-005 (unit) |
| e. Return target defaults to sign-on | HP-005 |
| VSAM read-only, X(08) keys | DB-001..003, FV-004, FV-005, BND-001, BND-002 |

## 6. Coverage & Gaps
- **N/A:** TX (no writes) and PSF (no list).
- **A1:** "only trailing spaces are insignificant" is inferred from PIC X(8) padded comparison.
- **A2:** the identifier upper-casing on the UI side is a convenience; the API is authoritative.
- **G1:** the DUMMY and admin-only paths are unreachable with shipped data and are covered at unit level only.
- **G2:** the legacy invalid-key path has no counterpart in a button-driven UI.
- **G3:** HTTP-007 requires stopping the database temporarily. HTTP-006 requires the default rate limit.
