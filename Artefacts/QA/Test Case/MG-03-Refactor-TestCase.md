# MG-03 — Refactor Test Cases

## 1. Document Metadata
- **Tran Group:** MG-03 — User Security Administration
- **Programs / transactions (BSTS):** CU00 / COUSR00C (list), CU01 / COUSR01C (add), CU02 / COUSR02C (update), CU03 / COUSR03C (delete)
- **Sources (only):**
  - `Artefacts/TranGroupData/MG-03-UserSecurityAdministration-BSTS.md`
  - `Artefacts/TranGroupData/MG-03-UserSecurityAdministration-BDD.md`
  - `Artefacts/TranGroupData/MG-03-UserSecurityAdministration-VSAM-Details.md`. This VSAM-only group has no `-DB-Details.md`, so the VSAM-Details file is its data artefact.
- **Target stack under test:**
  - Shell UI: React + Tailwind (:5173)
  - UserAdminService: ASP.NET Core 9 (:5002)
  - SignOnService: ASP.NET Core 9 (:5001), the token issuer
  - Database: PostgreSQL `carddemo`, table `usrsec` (:5433)
  - Auth: OAuth 2.0 bearer JWT (RS256)
- **ID scheme:** `MG03-<category>-NNN`. Categories: FE, HP, AUTHN, AUTHZ, FV, BR, DB, CON, TX, HTTP, UI, PSF, BND.

## 2. Scope & Objective
Under test are the User List, Add User, Update User and Delete User screens and their `usrsec` reads and writes. The objective is functional equivalence with COUSR00C–COUSR03C: the same records, ordering, legacy messages and CRUD effects. The cases also check the fixes for the legacy gaps (BDD Gaps 1, 2, 4, 5, 6, 7) and the authentication, authorisation, HTTP and UI guards.

## 3. Test Data & Preconditions
- **Seed** (VSAM-Details §d): `usrsec` holds ADMIN001–ADMIN005 (type A) and USER0001–USER0005 (type U), all with password `PASSWORD`. That is 10 users.
- **Identities:**
  - admin: a token from signing in as ADMIN001 (role `carddemo.admin`, scope `users.read users.write`)
  - user: a token from signing in as USER0001 (role `carddemo.user`)
  - anonymous: no token
- **Test-created users:** IDs prefixed `ZZ` or `USER0006`. Every case removes them, so the seed is unchanged afterwards.
- **Version token:** each user carries a `version` (ETag). PUT and DELETE need `If-Match: "<version>"`.

## 4. Test Cases

### FE — Functional Equivalence
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-FE-001 | First page in user-ID order | seed, admin | GET list | 10 rows: ADMIN001…ADMIN005, USER0001…USER0005 in ascending order, each with userId, firstName, lastName, userType | BDD a. "First page lists users in user-ID order"; BSTS PROCESS-PAGE-FORWARD |
| MG03-FE-002 | Search positions at ≥ key | seed | search `USER0003` | first row USER0003, then USER0004, USER0005 | BDD a. "Search positions the list at a user ID" |
| MG03-FE-003 | Add message | no USER0006 | add ANN/LEE/USER0006/PASSWORD/U | message `User USER0006 has been added ...` | BDD a. "User is added" |
| MG03-FE-004 | Update message | ZZ user exists | change last name and save | `User <id> has been updated ...` | BDD a. "Changes are saved with PF5" |
| MG03-FE-005 | Delete message | ZZ user exists | delete with confirmation | `User <id> has been deleted ...` | BDD a. "Delete is confirmed with PF5" |
| MG03-FE-006 | Fetched record matches stored | seed | GET USER0002 | firstName, lastName and userType equal the `usrsec` row (trailing blanks trimmed) | BDD a. "Selected user is shown for update"; VSAM-Details record layout |
| MG03-FE-007 | Upper-casing (gap 2 fixed) | — | add `zzlow01` with password `pw`, then sign in as `ZZLOW01` / `PW` | stored `ZZLOW01` / `PW`; sign-in 200 | BDD Gap 2 |

### HP — Happy Path
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-HP-001 | List | admin | GET `/api/v1/users` | 200 | BDD a. List |
| MG03-HP-002 | Add | admin | POST a valid user | 201 with Location `/api/v1/users/<id>` | BDD a. "User is added" |
| MG03-HP-003 | Fetch for update | admin | GET `/api/v1/users/USER0002` | 200 with ETag | BDD a. "Selected user is shown for update" |
| MG03-HP-004 | Update | admin, current version | PUT with a change | 200 with the new version | BDD a. "Changes are saved with PF5" |
| MG03-HP-005 | Delete | admin, current version | DELETE | 200; then GET returns 404 | BDD a. "Delete is confirmed with PF5" |
| MG03-HP-006 | Save & exit (PF3) | UI, admin | Update screen → Save & exit | the change is saved and the Administration menu is shown | BDD a. "PF3 saves and exits" |

### AUTHN — Authentication Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-AUTHN-001 | No token: list | anonymous | GET list | 401 | BDD b. "No COMMAREA goes to sign-on" |
| MG03-AUTHN-002 | No token: write | anonymous | POST user | 401; nothing written | BDD b. "No COMMAREA goes to sign-on" |
| MG03-AUTHN-003 | Tampered token | admin token with a corrupted signature | GET list | 401 | BDD b. "No COMMAREA goes to sign-on" |
| MG03-AUTHN-004 | UI route without session | browser, signed out | open `/admin/users` | redirected to `/signin` | BDD b. "No COMMAREA goes to sign-on" |

### AUTHZ — Authorization Guard
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-AUTHZ-001 | Regular user cannot list | user token | GET list | 403 | BSTS: reached only from the admin menu (COADM01C) |
| MG03-AUTHZ-002 | Regular user cannot add | user token | POST | 403; nothing written | BSTS admin-only |
| MG03-AUTHZ-003 | Regular user cannot update/delete | user token | PUT and DELETE USER0002 | 403 for both; row unchanged | BSTS admin-only |
| MG03-AUTHZ-004 | Regular user UI | USER0001 signed in | open `/admin/users` | User List is not shown (redirected to the main menu) | BSTS admin-only |

### FV — Field-Level Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-FV-001 | First Name blank | — | add with all fields blank | 400 `First Name can NOT be empty...` | BDD c. "Add requires every field" |
| MG03-FV-002 | Last Name blank | first name filled | add | 400 `Last Name can NOT be empty...` | BDD c. |
| MG03-FV-003 | User ID blank | names filled | add | 400 `User ID can NOT be empty...` | BDD c. |
| MG03-FV-004 | Password blank | names and ID filled | add | 400 `Password can NOT be empty...` | BDD c. |
| MG03-FV-005 | User Type blank | everything else filled | add | 400 `User Type can NOT be empty...` | BDD c. |
| MG03-FV-006 | User Type not A/U (gap 1 fixed) | — | add with type `X` | 400 `INVALID_USER_TYPE` | BDD Gap 1 |
| MG03-FV-007 | Duplicate user ID | USER0001 exists | add USER0001 | 409 `User ID already exist...` | BDD d. "Duplicate user ID on add" |
| MG03-FV-008 | Fetch with blank ID (UI) | Update and Delete screens | Fetch with Enter User ID blank | `User ID can NOT be empty...` on both screens | BDD c. "Update and delete need a user ID to fetch" |
| MG03-FV-009 | Update with a blank name | ZZ user | PUT with blank lastName | 400 `Last Name can NOT be empty...` | BDD c. "Update requires every field" |
| MG03-FV-010 | Non-alphanumeric user ID | — | add `ZZ-01` | 400 `INVALID_USER_ID` | VSAM-Details SEC-USR-ID X(8) key |

### BR — Cross-Field / Business-Rule Validation
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-BR-001 | No change → no update | ZZ user | PUT with identical values | 400 `Please modify to update ...`; version unchanged | BDD c. "Update requires every field and a change" |
| MG03-BR-002 | Unknown user on update | — | GET and PUT NOUSER01 | 404 `User ID NOT found...` | BDD d. "Fetch of an unknown user" (Update) |
| MG03-BR-003 | Unknown user on delete | — | DELETE NOUSER01 with a version | 404 `User ID NOT found...` | BDD d. "Fetch of an unknown user" (Delete) |
| MG03-BR-004 | Cannot delete self (gap 4 fixed) | ADMIN001 token | DELETE ADMIN001 | 409 `CANNOT_DELETE_SELF`; row present | BDD Gap 4 |
| MG03-BR-005 | Cannot demote self (gap 4 fixed) | ADMIN001 token | PUT ADMIN001 type U | 409 `CANNOT_DEMOTE_SELF` | BDD Gap 4 |
| MG03-BR-006 | Another admin can be demoted | ZZ admin + seed admins | PUT ZZ admin type U | 200, type U | BDD Gap 4 (the rule applies only to self and the last admin) |
| MG03-BR-007 | Blank password keeps current | ZZ user, password P1 | PUT with password blank and a name change | 200; stored password still P1 | BDD a. update; VSAM-Details SEC-USR-PWD |
| MG03-BR-008 | New password replaces and is upper-cased | ZZ user | PUT with password `new1` | stored `NEW1` | BDD Gap 2 |

### DB — DB Side Effects
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-DB-001 | Add writes the row | — | add ZZDB0001 / Ann / Lee / pw / u | `usrsec` row (`ZZDB0001`, `Ann`, `Lee`, `PW`, `U`) | VSAM-Details CRUD: COUSR01C WRITE |
| MG03-DB-002 | Update rewrites only changed columns | ZZDB0001 | change the last name to Wong | lname `Wong`; fname, pwd and type unchanged | VSAM-Details CRUD: COUSR02C REWRITE |
| MG03-DB-003 | Delete removes the row | ZZDB0001 | delete | row count for ZZDB0001 = 0; total back to 10 | VSAM-Details CRUD: COUSR03C DELETE |
| MG03-DB-004 | Least-privilege login | — | as `carddemo_useradmin`: SELECT `transaction_type`; as `carddemo_signon`: DELETE `usrsec` | both: permission denied | VSAM-Details §access |

### CON — Concurrency / Idempotency
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-CON-001 | Stale update rejected (gap 5 fixed) | ZZ user at version v | PUT (v→v2), then a second PUT with v | 2nd: 412 `STALE_USER`; first change kept | BDD Gap 5 |
| MG03-CON-002 | Missing If-Match | ZZ user | PUT and DELETE without If-Match | 428 `VERSION_REQUIRED` for both | BDD Gap 5 |
| MG03-CON-003 | Double-submit add | — | two parallel POSTs of ZZCON01 | one 201 and one 409; exactly 1 row | BDD d. "Duplicate user ID on add"; VSAM-Details key |
| MG03-CON-004 | Stale delete rejected | ZZ user updated after fetch | DELETE with the old version | 412; row present | BDD Gap 5 |

### TX — Multi-Step Transaction Integrity
Not applicable for this group: each action is one write to one record (VSAM-Details CRUD). The last-admin check and write share one database transaction, and BR-004/BR-005 cover it.

### HTTP — HTTP Semantics
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-HTTP-001 | Created | — | valid POST | 201 + Location | BDD a. add |
| MG03-HTTP-002 | Validation | — | blank POST | 400 problem+json with `code`, `message` | BDD c. |
| MG03-HTTP-003 | Not found | — | GET NOUSER01 | 404 | BDD d. |
| MG03-HTTP-004 | Conflict | — | duplicate POST | 409 | BDD d. |
| MG03-HTTP-005 | Precondition | — | stale If-Match | 412 | BDD Gap 5 |
| MG03-HTTP-006 | Unknown JSON field | — | POST with an extra property | 400 `VALIDATION_FAILED` | BSTS input fields |
| MG03-HTTP-007 | Health and headers | — | GET `/health`; any API response | 200 Healthy anonymously; `X-Content-Type-Options: nosniff`, `Cache-Control: no-store` | technical guard |
| MG03-HTTP-008 | Conflicting list query | — | `search` + `after` together | 400 `INVALID_QUERY` | BDD a. list |

### UI — UI Rendering
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-UI-001 | Admin menu opens User List | ADMIN001 signed in | Administration → option 1 | User List table with 10 rows, columns User ID, First name, Last name, Type, Actions; "Page 1" | BDD a. List; COUSR00.bms |
| MG03-UI-002 | Row Update action | User List | Update on USER0002 | Update screen pre-filled for USER0002, with "Press Save to save your updates ..." | BDD a. "Row action transfers to maintenance" (U) |
| MG03-UI-003 | Row Delete action | User List | Delete on USER0002 | Delete screen showing USER0002 read-only, with "Press Delete to delete this user ..." | BDD a. "Row action transfers to maintenance" (D) |
| MG03-UI-004 | Add errors focus the field | Add screen | Add with all blank | alert `First Name can NOT be empty...`; focus on First name, `aria-invalid=true` | BDD c. "cursor is on that field" |
| MG03-UI-005 | Add success clears the fields | Add screen | valid add | green `User … has been added ...`; fields empty | BDD a. "User is added" |
| MG03-UI-006 | Password masked | Add and Update screens | inspect the password inputs | `type=password`, maxlength 8; Update shows an empty password field | BDD c. "Password is not displayed" |
| MG03-UI-007 | Delete confirm dialog | Delete screen | Delete → dialog → Delete | dialog shown; then `User … has been deleted ...` | BDD a. "Delete is confirmed with PF5" |
| MG03-UI-008 | Clear (PF4) | Add screen with values | Clear | all fields empty, no message | BDD e. "PF4 clears the screen" |
| MG03-UI-009 | Leave without saving (PF12) | Update screen with an edit | Back to Administration | menu shown; row unchanged | BDD e. "PF12 leaves without saving" |

### PSF — Pagination / Sort / Filter
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-PSF-001 | Exactly 10 users: no next page | seed (10) | GET list | hasNext false; Next disabled in the UI | BDD e. "Exactly 10 users" |
| MG03-PSF-002 | Top of list | seed | `before=ADMIN001` | `You are already at the top of the page...` | BDD e. "PF7 on the first page" |
| MG03-PSF-003 | Bottom of list | seed | `after=USER0005` | `You are already at the bottom of the page...` | BDD e. "PF8 on the last page" |
| MG03-PSF-004 | 11 users: page forward and back | add ZZPSF001 | page 1 → Next → Previous | page 2 = [ZZPSF001], hasPrevious true; Previous returns ADMIN001…USER0005 | BDD b. "Paging state is kept"; PROCESS-PAGE-FORWARD/BACKWARD |
| MG03-PSF-005 | Search past the last key | seed | search `ZZZZZZZZ` | empty page, hasNext false | BDD a. Search |

### BND — Boundary & Numeric
| Test Case ID | Scenario / Title | Preconditions | Input / Action | Expected Result | Source (artefact citation) |
|---|---|---|---|---|---|
| MG03-BND-001 | 8-character user ID | — | add `ZZBND001` | 201 | VSAM-Details SEC-USR-ID X(8) |
| MG03-BND-002 | 9-character user ID | — | add `ZZBND0019` | 400 `FIELD_TOO_LONG` | VSAM-Details X(8) |
| MG03-BND-003 | 20/21-character first name | — | add with 20 chars, then with 21 | 201, then 400 `FIELD_TOO_LONG` | VSAM-Details SEC-USR-FNAME X(20) |
| MG03-BND-004 | 8/9-character password | — | add with password 8 chars, then 9 | 201, then 400 `FIELD_TOO_LONG` | VSAM-Details SEC-USR-PWD X(8) |
| MG03-BND-005 | Whitespace-only fields | — | add with first name `"   "` | 400 `First Name can NOT be empty...` | BDD c. |
| MG03-BND-006 | Lower-case type accepted | — | add with type `a` | 201; stored `A` | BDD Gap 1/2 |

## 5. Traceability Matrix
| BDD scenario / BSTS rule | Test Case ID(s) |
|---|---|
| First page lists users in user-ID order | FE-001, HP-001, UI-001 |
| Search positions the list at a user ID | FE-002, PSF-005 |
| Row action transfers to maintenance (U/u/D/d) | UI-002, UI-003 |
| User is added | FE-003, HP-002, DB-001, UI-005 |
| Selected user is shown for update | FE-006, HP-003, UI-002 |
| Changes are saved with PF5 | FE-004, HP-004, DB-002 |
| PF3 saves and exits | HP-006 |
| Selected user is shown for delete confirmation | UI-003 |
| Delete is confirmed with PF5 | FE-005, HP-005, DB-003, UI-007 |
| No COMMAREA goes to sign-on | AUTHN-001…004 |
| Selected user is carried in the COMMAREA | UI-002, UI-003 (route parameter) |
| Paging state is kept in the COMMAREA | PSF-004 |
| Update and delete use READ UPDATE before writing | CON-001, CON-004 |
| Add requires every field, in screen order | FV-001…005, UI-004 |
| Update and delete need a user ID to fetch | FV-008 |
| Update requires every field and a change | FV-009, BR-001 |
| Password is not displayed | UI-006 |
| Invalid row action | Gap: not reproducible. The target offers only Update and Delete buttons. |
| Duplicate user ID on add | FV-007, CON-003, HTTP-004 |
| Add fails for another reason / Lookup fails / Rewrite or delete fails | Gap: needs a database failure; not run against the shared demo database |
| Fetch of an unknown user (Update, Delete) | BR-002, BR-003, HTTP-003 |
| Unsupported key | Gap: no function keys in the web UI |
| PF7 on the first page / PF8 on the last page | PSF-002, PSF-003 |
| Exactly 10 users | PSF-001 |
| PF4 clears; PF12 leaves without saving | UI-008, UI-009 |
| BSTS admin-only access | AUTHZ-001…004 |
| Gaps 1, 2, 4, 5, 6, 7 (fixes) | FV-006, FE-007, BR-004…006, BR-008, CON-001/002/004. Gap 6 is not run (needs a DB failure). Gap 7, audit, is checked in the run metadata. |

## 6. Coverage & Gaps
- **Not applicable:**
  - TX: each action is a single-record write.
- **Not covered:**
  - "Invalid row action" and "Unsupported key" have no web equivalent. The target uses buttons and has no PF keys.
  - The "Unable to Add/lookup/Update/Delete User..." paths need an induced database failure. They are covered by unit-level error mapping, not this run.
- **Assumptions:**
  - Gap fixes follow the user's Phase 6 decisions: type A/U, upper-casing, self and last-admin protection, `If-Match`, audit.
  - PF3 is "Save & exit", PF4 is "Clear" and PF12 is "Back to Administration".
- **Missing source:** the plugin expects `MG-03-*-DB-Details.md`, but the group has no Db2 tables. `-VSAM-Details.md` was used instead.
