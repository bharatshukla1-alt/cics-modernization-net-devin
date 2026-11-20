# MG-02 — Review Gate 2 (Sign-on and Navigation Shell)

## 1. Review Metadata
| Item | Value |
|---|---|
| Group | MG-02: CC00/COSGN00C → CM00/COMEN01C → CA00/COADM01C |
| Path | Refactor |
| Reviewed | BDD, BSTS, VSAM-Details, DB-Status, UI/Backend/Reconcile specs, architecture, run guide, `Target/MG-02` |
| Evidence | SignOnService: build `-warnaserror` clean, 35/35 tests, 93.9% line coverage. Shell UI: lint clean, 19/19 tests, 95% line coverage, build OK. Docker demo stack is up and healthy. |

## 2. Verdict Summary
**Ready to build/test, with recorded decisions.** There are 0 Critical gaps, 5 Good-to-Have findings, 3 architecture recommendations and 2 advanced suggestions.

Live smoke checks through the UI proxy all passed:
| Check | Result |
|---|---|
| Sign in as admin, in lower case | 200 |
| Wrong password | `WRONG_PASSWORD` |
| Unknown user | `USER_NOT_FOUND` |
| Admin menu | 6 options; options 5 and 6 are AVAILABLE |
| User menu | 11 options |
| User token on the admin menu | 403 |
| MG-01 list with admin token | 200, 7 rows |
| MG-01 list with user token | 403 |
| Old dev token | 401 (bypass is off) |
| Audit events | Written |

## 3. Critical Gaps
None.

## 4. Good to Have
| ID | Finding | Recommendation |
|---|---|---|
| GH-1 | Passwords are compared in plain text, as in USRSEC. This is a user decision and demo-only. | Before production, hash passwords (for example bcrypt) or federate to the enterprise IdP. `StartupGuards` already blocks this mode outside Development unless it is acknowledged explicitly. |
| GH-2 | Different 401 codes for unknown user and wrong password reveal which user IDs exist (kept for legacy parity). | Merge them into one message for production. |
| GH-3 | No lockout or attempt counter (the legacy has none). The rate limit is 5 sign-ins per minute per IP. | Add per-user lockout when hashing is introduced. |
| GH-4 | The ephemeral Development signing key changes on every SignOnService restart, which invalidates open sessions. | Mount a PEM file through `Auth:SigningKeyPath` for persistent demos. |
| GH-5 | Option names drop the legacy "(Security)" and "(Db2)" suffixes. | Confirm the business names. |

## 5. Architecture Recommendations
| ID | Recommendation |
|---|---|
| A-1 | SignOnService acts as the identity provider for every group (OIDC discovery + JWKS). MG-01 trusts it with configuration only, no code change. Confirm this as the interim IdP until the enterprise realm is known. |
| A-2 | A single shell SPA hosts the MG-01 screens through a Vite alias. Later groups plug in the same way. Alternative: micro-frontends. |
| A-3 | Menu availability is configuration-driven (`Menu:Routes`). Update it as each group goes live. |

## 6. Advanced Suggestions
- Add refresh tokens or silent renewal if 15-minute sessions are too short for users.
- Publish menu definitions from a database table, so the business can change options without a deploy.

## 7. Traceability Notes
- Every BDD scenario maps to a test:
  - sign-on validation, not found, wrong password and unable to verify: `SignInServiceTests`, `ApiTests`, `SignInPage.test`
  - menu options, admin-only and DUMMY handling, invalid option: `MenuServiceTests`, `MenuPage.test`
  - EIBCALEN=0 → sign-on: the `App.test` guard
  - PF3 → sign-out: `App.test`
- Legacy defect (BDD Gap 4, the invalid-option fall-through) is not reproduced. Menus now enforce role (BDD Gap 6).

## 8. Sign-off
- **Gate 2: APPROVED** by the user ("ok proceed", 2026-11-20 15:25 IST).
- The current build choices are kept:
  - GH-2: separate unknown-user and wrong-password messages, kept for legacy parity.
  - GH-5: shortened menu names.
  - A-1: SignOnService is the interim identity provider.
  - A-2: one shell SPA hosts every group's screens.
- No Good-to-Have findings were applied at this gate.
