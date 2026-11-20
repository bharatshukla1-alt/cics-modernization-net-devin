# MG-02 — Reconciliation Spec (Refactor)

## 1. Reconciliation Metadata
Inputs: MG-02-UI-Spec.md, MG-02-Backend-Spec.md. Group: MG-02 (CC00 → CM00 → CA00).

## 2. Reconciled Screen Overview
| Screen | Route | Endpoints |
|---|---|---|
| Sign in | `/signin` | `POST /api/v1/auth/sign-in` |
| Customer services menu | `/menu` | `GET /api/v1/menus/main` |
| Administration menu | `/admin` | `GET /api/v1/menus/admin` |
| Signed out | `/signed-out` | — |

## 3. Field Binding Map
| UI field | API attribute | DB column | Legacy |
|---|---|---|---|
| User ID | `userId` | `usrsec.sec_usr_id` | USERID / SEC-USR-ID |
| Password | `password` | `usrsec.sec_usr_pwd` | PASSWD / SEC-USR-PWD |
| Header user name | `user.firstName` + `user.lastName` | `sec_usr_fname`, `sec_usr_lname` | (new: not shown on the legacy screen) |
| Role badge and routing | `user.userType` / `role` claim | `sec_usr_type` | CDEMO-USER-TYPE |
| Tile number and name | `options[].number`, `options[].name` | COMEN02Y / COADM02Y | OPTN001–012 |
| Tile action | `options[].route`, `options[].status` | config `Menu:Routes` | CDEMO-*-OPT-PGMNAME |
| Go to option | client-side index into `options` | — | OPTION |

## 4. Action ↔ Endpoint Map
| UI action | Endpoint | Success | Failure |
|---|---|---|---|
| Sign in | POST sign-in | 200: store the token, route by `userType` | 400 / 401 / 503 → alert |
| Open menu | GET menus/{main\|admin} | Render tiles | 401 → `/signin`; 403 → own menu |
| Tile click / Go | none (client routing) | Navigate to `route` | Status message |
| Sign out | none (token discarded) | `/signed-out` | — |

## 5. Validation Parity
| Rule | UI | API |
|---|---|---|
| User ID required | ✔ | ✔ `USER_ID_REQUIRED` |
| Password required | ✔ (after user ID) | ✔ `PASSWORD_REQUIRED` |
| Length at most 8 | `maxLength=8` | ✔ `FIELD_TOO_LONG` |
| Upper-case | UI upper-cases the user ID | API upper-cases both |
| Option 1..count | ✔ QuickOption | not applicable |

## 6. Error & Edge-Case Parity
| Code | HTTP | UI message |
|---|---|---|
| USER_NOT_FOUND | 401 | "User not found. Try again." (focus on User ID) |
| WRONG_PASSWORD | 401 | "Wrong password. Try again." (focus on Password, field cleared) |
| UNABLE_TO_VERIFY | 503 | "Unable to verify the user. Try again later." |
| NOT_INSTALLED / COMING_SOON / ADMIN_ONLY | 200 (status field) | Info message on the menu |
| Expired or missing token | 401 | Redirect to `/signin` |

## 7. Cross-Cutting Alignment
- Token: RS256, `aud=carddemo-api`, `role` claim. MG-01 validates it with `Auth:Authority=http://signon:5001` in Development.
- Both services use 15-minute expiry/idle time, the same ProblemDetails shape, audit events and TLS rules.
- The MG-01 SPA drops its own sign-in button when it runs inside the shell and receives the token from the shell session (single SPA, see §9).

## 8. Gaps & Resolutions for a Fully Functional Screen
| Gap | Resolution |
|---|---|
| MG-01 and MG-02 UIs are separate apps | Build **one shell SPA** in `Target/MG-02/Frontend` that hosts the MG-02 pages and imports the MG-01 pages/components as routes (`/transaction-types/*`), so the admin menu tiles open them with the same token |
| MG-01 dev bypass | Switch it off in the combined demo; MG-01 trusts the SignOnService JWKS |
| Unmigrated options | `NOT_INSTALLED` until their groups are delivered |
| Plain-text passwords | Accepted for the demo (user decision); recorded at Gate 2 |

## 9. Implementation Plan (Rewiring Front End & Back End)
1. Build SignOnService (API, EF Core, token issuer, JWKS, menus) and the least-privilege role.
2. Build the shell SPA: sign-in, menus, session and guards. Mount the MG-01 pages under `/transaction-types`.
3. Configure the MG-01 API: `Auth:DevBypass=false`, `Auth:Authority` = SignOnService, `Auth:Audience=carddemo-api`.
4. Extend the demo docker-compose: db, signon (5001), mg01-api (5000), shell UI (5173).
5. Tests: unit and API integration tests for SignOnService, and Vitest for the shell pages.

## 10. Build Acceptance Criteria
- Every row in §3–§6 is implemented.
- ADMIN001 can sign in, open "Transaction Type List/Update" and see the 7 rows through MG-01 using the SignOnService token.
- USER0001 sees 11 tiles, and a direct call to MG-01 returns 403.
