# MG-03 — Review Gate 2 (User Security Administration)

## 1. Review Metadata
| Item | Value |
|---|---|
| Group | MG-03: CU00/COUSR00C (list), CU01/COUSR01C (add), CU02/COUSR02C (update), CU03/COUSR03C (delete) |
| Path | Refactor |
| Reviewed | BDD, BSTS, VSAM-Details, DB-Status, UI/Backend/Reconcile specs, architecture, run guide, `Target/MG-03`, MG-02 shell and SignOnService changes |
| Evidence | UserAdminService: build clean (0 warnings), 29/29 tests (unit and API against PostgreSQL). Shell UI: 29/29 tests (10 new MG-03 screen tests), 96.6% line coverage, build OK. SignOnService: 38/38 tests after the scope and route change. `run_pipeline.py --phase 6 --group MG-03` completed. The Docker demo (`Target/MG-03/demo`) is up with all 3 APIs healthy. |

## 2. Verdict Summary
**Ready to test, with recorded decisions.** There are 0 Critical gaps, 5 Good-to-Have findings, 3 architecture recommendations and 2 advanced suggestions.

Live smoke checks through the UI proxy (:5173) all passed:
| Check | Result |
|---|---|
| Admin token, list users | 200; 10 rows in user-ID order |
| User token, list users | 403 |
| Add `user0006`, password `pw` | 201; "User USER0006 has been added ..." (stored upper-case) |
| Sign in as USER0006 / `pw` | 200 (gap G2 fixed) |
| Update with `If-Match` | 200; "User USER0006 has been updated ..." |
| Delete with `If-Match` | 200; "User USER0006 has been deleted ..." |
| ADMIN001 deletes itself (UI) | Blocked: "You cannot delete your own user ID..." |
| Admin menu options 1–4 | AVAILABLE, routed to `/admin/users*` |
| Audit events | 3 written (create, update, delete); no passwords |
| Data after the checks | 10 users (5 admins), the same as before |

## 3. Critical Gaps
None.

## 4. Good to Have
| ID | Finding | Recommendation |
|---|---|---|
| GH-1 | Passwords are stored in plain text, upper-cased, max 8 characters (user decision, demo-only; same as MG-02 GH-1). `StartupGuards` refuses this mode outside Development unless `Auth:AllowLegacyPlainTextPasswords` is set. | Hash passwords (for example bcrypt) in a wider column, in MG-02 and MG-03 together, or federate to the enterprise IdP. |
| GH-2 | The version token is PostgreSQL `xmin`. It is reliable for concurrency but changes after a VACUUM FULL or restore, which forces users to re-fetch. | Acceptable. Add an explicit `row_version` column with the planned versioned migrations (MG-01 CQ-004). |
| GH-3 | The legacy delete failure message reused "Unable to Update User...". The target says "Unable to Delete User..." (deliberate fix). | Confirm. |
| GH-4 | The legacy PF7/PF8 page counter could drift at the ends of the file. The target uses keyset paging with Previous/Next enabled only when rows exist. The legacy "You are already at the top/bottom of the page..." messages are kept for edge requests. | Confirm. |
| GH-5 | The update screen leaves Password blank and keeps the current one unless a new value is typed (the legacy shows and rewrites the stored password). The API never returns passwords. | Confirm. |

## 5. Architecture Recommendations
| ID | Recommendation |
|---|---|
| A-1 | UserAdminService is its own microservice (port 5002) with its own database login, `carddemo_useradmin`, which can only CRUD `usrsec`. SignOnService stays read-only. Both share the `usrsec` table as the single source of truth. |
| A-2 | Admin tokens now carry the `users.read users.write` scopes. Endpoints require the `carddemo.admin` role **and** the scope, so a read-only admin token cannot write. |
| A-3 | The last-admin rule locks admin rows (`SELECT … FOR UPDATE`) inside the transaction, so two simultaneous demotions cannot leave zero admins. |

## 6. Advanced Suggestions
- Publish audit events to a message bus (Kafka/RabbitMQ/Azure Service Bus) for SIEM ingestion, instead of only the structured log.
- Force a password change at first sign-on for users created by an admin.

## 7. Traceability Notes
- **List (COUSR00C):** BDD scenarios for list, search, paging and row action map to `ApiTests.List_*`, `Search_*` and `UserScreens.test` (UserListPage).
- **Add (COUSR01C):** BDD scenarios for the required-field order, duplicate and success map to `UserValidatorTests`, `ApiTests.Create_*` and `UserScreens.test` (UserAddPage).
- **Update (COUSR02C):** BDD scenarios for fetch, not found, no change, save, and save-and-exit map to `ApiTests.Create_update_delete_round_trip…` and `UserScreens.test` (UserUpdatePage).
- **Delete (COUSR03C):** BDD scenarios for fetch, confirm and delete map to the API round trip and `UserScreens.test` (UserDeletePage).
- **Fixed legacy gaps:**
  - G1, type A/U: `INVALID_USER_TYPE` tests
  - G2, upper-casing: validator test and the live sign-on check
  - G3, self and last admin: `Admin_cannot_delete_or_demote_self` and `AdminGuard` tests
  - G4, `If-Match`: the 428 and 412 assertions
  - G5, audit: live log check

## 8. Sign-off
- **Gate 2: APPROVED** by the user ("ok proceed", 2026-11-20 16:46 IST).
- The current build choices are kept:
  - GH-3: "Unable to Delete User..." for failed deletes.
  - GH-4: keyset Previous/Next paging.
  - GH-5: password left blank on Update means keep the current one.
- No Good-to-Have findings were applied at this gate.
