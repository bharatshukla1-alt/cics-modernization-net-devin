# MG-02 — Backend Build Spec (Refactor)

## 1. Spec Metadata & Traceability
| Item | Value |
|---|---|
| Trans Group | MG-02 — Sign-on and Navigation Shell (CC00/COSGN00C, CM00/COMEN01C, CA00/COADM01C) |
| Inputs | MG-02 BSTS, BDD, VSAM-Details, DB-Status; MG-02 UI spec; config/Config.md |
| Target | ASP.NET Core 9 Web API `SignOnService` on port **5001**, PostgreSQL via EF Core/Npgsql (`usrsec` table), stateless JWT |
| User decisions | Refactor path. Legacy plain-text password comparison, **demo only** (Phase 5 Q1) |

## 2. Business Context & Scope
One microservice provides the shell for the whole estate:
- It authenticates users against `usrsec`.
- It issues the access token that every other service (MG-01 today, and later groups) validates.
- It serves the role-specific menus.

The menus are served as data from COMEN02Y and COADM02Y. Each option carries a target route and an availability status. The status replaces the CICS `INQUIRE PROGRAM` and `PGMIDERR` probes.

Out of scope: user maintenance (MG-03) and every function behind the menu options.

## 3. Service & API Specification
| Method & path | Auth | Request | Response | Legacy |
|---|---|---|---|---|
| `POST /api/v1/auth/sign-in` | Anonymous, rate-limited | `{ userId, password }` | 200 `{ accessToken, tokenType:"Bearer", expiresIn, user:{ userId, firstName, lastName, userType } }` | PROCESS-ENTER-KEY, READ-USER-SEC-FILE |
| `GET /api/v1/menus/main` | Role `carddemo.user` | — | 200 `{ title, options:[{ number, name, route, status }] }` | COMEN01C BUILD-MENU-OPTIONS / COMEN02Y |
| `GET /api/v1/menus/admin` | Role `carddemo.admin` | — | 200, same shape | COADM01C / COADM02Y |
| `GET /.well-known/openid-configuration` | Anonymous | — | Issuer and `jwks_uri` | Target-only. Lets MG-01 and later services validate tokens through their existing `Auth:Authority` setting. |
| `GET /.well-known/jwks.json` | Anonymous | — | RSA public key (RS256) | Target-only |
| `GET /health` | Anonymous | — | `{status}` | — |

`status` values:
- `AVAILABLE`: the option's group is migrated and `route` is set.
- `NOT_INSTALLED`: the option's group is not migrated yet.
- `COMING_SOON`: the program name starts with "DUMMY".
- `ADMIN_ONLY`: the option's user type is 'A' and the caller is a regular user.

Token claims:
| Claim | Value |
|---|---|
| `sub` | user ID |
| `name` | first and last name |
| `role` | `carddemo.admin` for type 'A', `carddemo.user` for type 'U' |
| `scope` | admin: `transaction-types.read transaction-types.write`; user: none for now |
| `aud` | `carddemo-api` |
| `iss` | `Auth:Issuer` |

Tokens last 15 minutes, matching the MG-01 idle timeout.

## 4. Business Rules & Functional Equivalence
| # | Legacy rule | Target behaviour | Source |
|---|---|---|---|
| R1 | User ID is required | 400 `USER_ID_REQUIRED` "Please enter User ID ..." | PROCESS-ENTER-KEY |
| R2 | Password is required (checked after the user ID) | 400 `PASSWORD_REQUIRED` "Please enter Password ..." | PROCESS-ENTER-KEY |
| R3 | User ID and password are upper-cased | `ToUpperInvariant()` on both before the lookup and the comparison | FUNCTION UPPER-CASE |
| R4 | Key lookup on USRSEC | `SELECT … WHERE sec_usr_id = @id` with the id padded to CHAR(8) | READ DATASET(USRSEC) RIDFLD |
| R5 | NOTFND | 401 `USER_NOT_FOUND` "User not found. Try again ..." | RESP 13 |
| R6 | Password mismatch | 401 `WRONG_PASSWORD` "Wrong Password. Try again ..." | `SEC-USR-PWD = WS-USER-PWD` |
| R7 | Any other file error | 503 `UNABLE_TO_VERIFY` "Unable to verify the User ..." | WHEN OTHER |
| R8 | Type 'A' routes to the admin menu; anything else to the user menu | `userType` is returned; role `carddemo.admin` for 'A', otherwise `carddemo.user` | `IF CDEMO-USRTYP-ADMIN` |
| R9 | The main menu lists 11 options in table order | Static `MenuCatalog.Main`, cited to COMEN02Y | BUILD-MENU-OPTIONS |
| R10 | The admin menu lists 6 options | `MenuCatalog.Admin`, cited to COADM02Y | BUILD-MENU-OPTIONS |
| R11 | Option outside 1..count is rejected | Enforced in the UI (`QuickOption`). The menu endpoints accept no option input. | PROCESS-ENTER-KEY |
| R12 | Admin-only option chosen by a user | `status = ADMIN_ONLY` | COMEN01C admin-only check |
| R13 | INQUIRE PROGRAM / PGMIDERR | `status = NOT_INSTALLED` unless the option is mapped in `Menu:Routes` config | COMEN01C INQUIRE; COADM01C PGMIDERR |
| R14 | DUMMY program | `status = COMING_SOON` | `PGMNAME(1:5) = 'DUMMY'` |

## 5. Server-Side Validation
- `userId`: required, trimmed, at most 8 characters.
- `password`: required, at most 8 characters.
- Values longer than 8 characters return 400 `FIELD_TOO_LONG`, because the legacy fields are PIC X(8).
- The request body must not exceed 1 KB.

## 6. Data Model & Data Access
- EF Core entity `SecurityUser` maps to `usrsec`: `sec_usr_id` (key), `sec_usr_fname`, `sec_usr_lname`, `sec_usr_pwd`, `sec_usr_type`, all CHAR.
- CHAR padding is trimmed on read.
- Access is read-only: `AsNoTracking`, single-row lookup by primary key.
- Runtime login is the least-privilege role `carddemo_signon`, with SELECT on `usrsec` only.
- PostgreSQL READ COMMITTED replaces `READINTEG(UNCOMMITTED)`.

## 7. Error Handling & Edge Cases
- All errors are RFC 7807 ProblemDetails with `code`, `message` and `traceId`.
- A database exception returns 503 `UNABLE_TO_VERIFY` and the details are logged without the password.
- The 401 codes keep the legacy distinction between unknown user and wrong password, for demo fidelity. Production would merge them (see Deviations).

## 8. Security Specification (mandatory build instruction)
- Signing key: RS256 with an RSA 2048 key. Development generates an ephemeral key at startup. Production requires `Auth:SigningKeyPath`, enforced by `StartupGuards`.
- Plain-text comparison uses `CryptographicOperations.FixedTimeEquals`. `StartupGuards` refuses to start outside Development unless `Auth:AllowLegacyPlainTextPasswords=true` is set explicitly. The demo banner states the limitation.
- Sign-in is rate-limited to 5 requests per minute per IP. Passwords are never logged.
- Every sign-in writes a structured audit event (`auth.sign-in`, outcome, user, trace ID), reusing MG-01's `AuditLog` pattern.
- HTTPS/TLS 1.3, CORS, security headers and `AllowedHosts` follow MG-01.

## 9. Non-Functional Requirements
- Stateless: no server session; the token carries identity.
- p95 sign-in under 200 ms against local PostgreSQL.
- `/health` is checked by Docker Compose.

## 10. Backend Folder Structure
```
Target/MG-02/Backend/SignOnService/
  src/SignOnService.Api/  Program.cs  Auth/(TokenIssuer, SigningKeys, SignInService, PasswordVerifier)
                          Menus/(MenuCatalog, MenuService)  Data/(SignOnDbContext, SecurityUser)
                          Endpoints/  Errors/  Audit/  Security/(StartupGuards)
  tests/SignOnService.Tests/ (unit + API integration against PostgreSQL)
Target/MG-02/Backend/db/least-privilege-signon-role.sql
```

## 11. Acceptance Criteria & Deviations
**Acceptance criteria**
- R1–R14 each have a passing test.
- An admin token from this service is accepted by the MG-01 API (`GET /api/v1/transaction-types` returns 200) when MG-01 has `Auth:Authority` pointing at this service.
- A user token gets 403 from MG-01.

**Deviations**
- DV1: the service issues JWTs (OAuth 2.0 bearer) instead of passing a COMMAREA.
- DV2: plain-text passwords are kept by user decision. Production must replace them with hashing or an external IdP.
- DV3: separate unknown-user and wrong-password messages reveal whether a user ID exists. They are kept for parity in the demo.
- DV4: menu availability is configuration-driven, not a runtime program probe.
- DV5: the menus enforce the caller's role (the legacy code did not).
