# MG-03 — Backend Build Spec (Refactor)

## 1. Spec Metadata & Traceability
| Item | Value |
|---|---|
| Trans Group | MG-03 — User Security Administration (CU00/COUSR00C, CU01/COUSR01C, CU02/COUSR02C, CU03/COUSR03C) |
| Inputs | MG-03 BSTS, BDD, VSAM-Details, DB-Status; MG-03 UI spec; MG-02 Backend spec (token contract); config/Config.md |
| Target | ASP.NET Core 9 Web API `UserAdminService` on port **5002**, PostgreSQL via EF Core/Npgsql (`usrsec`), stateless JWT validated against SignOnService |
| User decisions | Refactor path. Plain-text passwords kept for the demo, as in MG-02 (Q1 = B). New microservice with its own login (Q2 = A). Every legacy gap is fixed (Q3 = A). |

## 2. Business Context & Scope
UserAdminService owns writes to `usrsec`. SignOnService (MG-02) keeps read-only sign-in. Only administrators (role `carddemo.admin`) can call the service. The four CICS programs become one REST resource, `users`.
Out of scope: password hashing (user decision), self-service password change and the admin menu itself (MG-02).

## 3. Service & API Specification
All endpoints require role `carddemo.admin`. Reads need scope `users.read`; writes need scope `users.write`. SignOnService adds both scopes to admin tokens.

| Method & path | Request | Response | Legacy |
|---|---|---|---|
| `GET /api/v1/users?search=&after=&before=` | `search`: start key (≥). `after`: next page after this ID. `before`: previous page before this ID. At most one is set. | 200 `{ users:[{userId, firstName, lastName, userType}], firstUserId, lastUserId, hasPrevious, hasNext, message? }`, 10 per page | COUSR00C PROCESS-PAGE-FORWARD / BACKWARD, PF7 / PF8 |
| `GET /api/v1/users/{userId}` | — | 200 `{ userId, firstName, lastName, userType, version }` plus `ETag: "<version>"`. The password is never returned. | COUSR02C/03C READ-USER-SEC-FILE |
| `POST /api/v1/users` | `{ userId, firstName, lastName, password, userType }` | 201 `{ user, message:"User <id> has been added ..." }` plus `Location` | COUSR01C WRITE-USER-SEC-FILE |
| `PUT /api/v1/users/{userId}` | `If-Match: "<version>"`; `{ firstName, lastName, password?, userType }` | 200 `{ user, message:"User <id> has been updated ..." }` | COUSR02C UPDATE-USER-INFO / REWRITE |
| `DELETE /api/v1/users/{userId}` | `If-Match: "<version>"` | 200 `{ message:"User <id> has been deleted ..." }` | COUSR03C DELETE-USER-SEC-FILE |
| `GET /health` | — | `{status}` | — |

## 4. Business Rules & Functional Equivalence
| # | Legacy rule | Target behaviour | Source |
|---|---|---|---|
| R1 | List in key order, 10 per page | `ORDER BY sec_usr_id LIMIT 11`. The 11th row sets `hasNext`. | PROCESS-PAGE-FORWARD |
| R2 | Search positions at user ID ≥ the value typed | `WHERE sec_usr_id >= upper(search)` | STARTBR RIDFLD |
| R3 | PF8 at the bottom | Not called when `hasNext=false`. An `after` request with no rows returns `message` "You are already at the bottom of the page..." | PROCESS-PF8-KEY |
| R4 | PF7 at the top | Same with `hasPrevious`: "You are already at the top of the page..." | PROCESS-PF7-KEY |
| R5 | Add: fields required in the order FNAME, LNAME, USERID, PASSWD, USRTYPE | 400 with the first failing code and message: `FIRST_NAME_REQUIRED` "First Name can NOT be empty...", `LAST_NAME_REQUIRED`, `USER_ID_REQUIRED`, `PASSWORD_REQUIRED`, `USER_TYPE_REQUIRED` | PROCESS-ENTER-KEY |
| R6 | Duplicate key | 409 `USER_ID_EXISTS` "User ID already exist..." (PostgreSQL 23505) | DUPKEY/DUPREC |
| R7 | Other write failure | 503 `UNABLE_TO_ADD` "Unable to Add User..." | WHEN OTHER |
| R8 | Lookup NOTFND | 404 `USER_NOT_FOUND` "User ID NOT found..." | READ NOTFND |
| R9 | Lookup failure | 503 `UNABLE_TO_LOOKUP` "Unable to lookup User..." | WHEN OTHER |
| R10 | Update: nothing changed | 400 `NO_CHANGES` "Please modify to update ..." | UPDATE-USER-INFO |
| R11 | Rewrite / delete failure | 503 `UNABLE_TO_UPDATE` "Unable to Update User..." (update) or `UNABLE_TO_DELETE` "Unable to Delete User..." (delete; the legacy delete program reused the update text) | UPDATE/DELETE-USER-SEC-FILE |
| R12 | Success messages | "User <id> has been added / updated / deleted ..." | WRITE/REWRITE/DELETE NORMAL |
| G1 (fixed) | Type not validated | Type is upper-cased and must be `A` or `U`, else 400 `INVALID_USER_TYPE` "User Type must be A (Admin) or U (User)..." | BSTS gap 1 |
| G2 (fixed) | No upper-casing | `userId` and `password` are upper-cased before insert, update and compare, matching sign-on R3 | BSTS gap 2 |
| G3 (fixed) | Self / last-admin removal | 409 `CANNOT_DELETE_SELF` "You cannot delete your own user ID...". 409 `CANNOT_DEMOTE_SELF`. 409 `LAST_ADMIN` "At least one administrator must remain..." | BSTS gap 4 |
| G4 (fixed) | No concurrency check | `If-Match` is required: 428 `VERSION_REQUIRED` when missing, 412 `STALE_USER` "This user was changed by someone else. Fetch it again..." when stale | BSTS gap 5 |
| G5 (fixed) | No audit | Structured audit events `user.create`, `user.update`, `user.delete` with actor, target, outcome, client IP and trace ID; never the password | BSTS gap 7 |

## 5. Server-Side Validation
| Field | Rule |
|---|---|
| `userId` | Required, trimmed, at most 8 characters, letters and digits only |
| `firstName`, `lastName` | Required, at most 20 characters |
| `password` | Required on create, at most 8 characters. On update it is optional; blank keeps the current password. |
| `userType` | 1 character, A or U |

Fields longer than allowed return 400 `FIELD_TOO_LONG`. Unknown JSON members are rejected. The request body is limited to 2 KB.

## 6. Data Model & Data Access
- EF Core entity `SecurityUser` maps to `usrsec`. CHAR padding is trimmed on read.
- `Version` maps to PostgreSQL system column `xmin` as a concurrency token, so no schema change is needed.
- Paging uses keyset queries on the primary key.
- The last-admin check runs in a transaction: `SELECT sec_usr_id FROM usrsec WHERE sec_usr_type='A' FOR UPDATE`, then a count, before demote or delete.
- Runtime login is `carddemo_useradmin`, with SELECT, INSERT, UPDATE and DELETE on `usrsec` only.

## 7. Error Handling & Edge Cases
All errors are RFC 7807 ProblemDetails with `code`, `message` and `traceId`, the same as MG-01 and MG-02. Database exceptions return 503 with the legacy message. Details are logged without the password. An unknown `userId` in the path returns 404.

## 8. Security Specification (mandatory build instruction)
- **Tokens:** JWT bearer validated through `Auth:Authority` (the SignOnService issuer and JWKS), with audience `carddemo-api` and RS256.
- **Authorization:** policies `users.read` and `users.write` require role `carddemo.admin` plus the scope.
- **Plain-text passwords:** `StartupGuards` refuses to start outside Development unless `Auth:AllowLegacyPlainTextPasswords=true`, mirroring MG-02. Passwords are never logged or returned.
- **Defaults copied from MG-01/MG-02:** HTTPS/TLS 1.3 outside Development, HSTS, security headers, CORS allow-list, `AllowedHosts`, forwarded headers from known proxies only, and no server header.

## 9. Non-Functional Requirements
The service is stateless. p95 is under 200 ms for list and single-row operations on local PostgreSQL. Docker Compose checks `/health`.

## 10. Backend Folder Structure
```
Target/MG-03/Backend/UserAdminService/
  src/UserAdminService.Api/  Program.cs  Users/(UserService, UserValidator, UserDtos)
                             Data/(UserAdminDbContext, SecurityUser)  Endpoints/  Errors/  Audit/  Security/
  tests/UserAdminService.Tests/ (unit + API integration against PostgreSQL)
Target/MG-03/Backend/db/least-privilege-useradmin-role.sql
```

## 11. Acceptance Criteria & Deviations
**Acceptance criteria:**
- R1–R12 and G1–G5 each have a passing test.
- An admin token from SignOnService works.
- A regular-user token gets 403, and no token gets 401.
- `carddemo_useradmin` cannot touch `transaction_type`.

**Deviations:**
- DV1: one REST resource replaces 4 programs and the COMMAREA selection hand-off.
- DV2: on update the password is optional (blank keeps it), and the API never returns the stored password; the legacy screen redisplayed it in a dark field.
- DV3: G1–G5 are fixed by user decision, so the target is intentionally stricter than the legacy.
- DV4: passwords stay plain text for the demo, by user decision.
