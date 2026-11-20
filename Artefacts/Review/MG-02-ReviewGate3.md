# MG-02 – Human Review Gate 3 (Final Sign-Off)

**Move group:** MG-02 – Sign-on and Navigation Shell (CC00 → COSGN00C, CM00 → COMEN01C, CA00 → COADM01C)
**Date:** 2026-11-20 · **Path:** Refactor · **Branch:** `devin/1795154209-cics-modernization-mg01` ([PR #17](https://github.com/TCS-Devin-Sandbox/ts-cobol-carddemo/pull/17))
**Recommendation:** **Ready for sign-off.** The Gate 3 decisions are in §6.

## 1. Pipeline status for MG-02

| Phase | Output | Status |
|---|---|---|
| 4 BDD / BSTS | `Artefacts/TranGroupData/MG-02-SignOnAndNavigationShell-{BDD,BSTS}.md` | Done |
| 5 Database (VSAM → PostgreSQL) | `…-VSAM-Details.md`, `Artefacts/DDL/MG-02-…-vsam-schema.sql`, `Artefacts/Database/MG-02-…-DB-Status.md` | Done. `usrsec` provisioned with 10 seed users; 0 Critical gaps |
| 6 Specs + build | `Artefacts/Specs/Refactor/MG-02-{UI,Backend,reconcile}-Spec.md`, `Architecture/MG-02.Architecture.md`, `MG-02-Run-Guide.md`, `Target/MG-02/` | Done |
| 7 Gate 2 | `Artefacts/Review/MG-02-ReviewGate2.md` | Approved by the user |
| 8 Testing | `Artefacts/QA/Test Case/MG-02-Refactor-TestCase.md`, `Artefacts/QA/Test Result/MG-02-Refactor-TestResult.md` | 63/63 passed after the user-approved BND-002 input correction |
| 9 Quality & security | `Artefacts/Code Quality & Security/MG-02-Security-Review.md`, `MG-02-CodeQuality-Review.md` | Reviewed; the recommended fixes were applied (§3) |

## 2. Review results (before fixes)

| Review | Grade | Critical | High | Medium | Low/Info |
|---|---|---|---|---|---|
| Security | C | 0 | 2 (SEC-001 plain-text passwords, accepted parity; SEC-002 shared rate-limit bucket) | 6 | 4 |
| Code quality | B | 0 | 0 | 5 | 6 |

## 3. Fix log

### Security (4 in scope, all fixed)
| ID | Fix |
|---|---|
| SEC-002 | Forwarded headers are accepted only from configured proxies, and the sign-in limit now applies per real client. In the demo the UI proxy is pinned to 172.28.0.10. Covered by a new `RateLimitTests`. |
| SEC-008 | Audit events now carry the client IP. Unknown user IDs are logged only as a hash (`unknown#…`). |
| SEC-009 | The demo uses a persistent signing key (`demo/keys/signon.pem`, git-ignored). |
| SEC-010 | The `name` claim is removed from tokens. |

### Code quality (7 in scope, all fixed)
| ID | Fix |
|---|---|
| CQ-003 | Endpoints moved into `Endpoints/*`, and the 401/403 handler into `Security/`. `Program.cs` now holds only composition. |
| CQ-005 | Added `Target/MG-02/ci/quality-gate.sh`. |
| CQ-006 | Added `MENU_STATUS` constants on the frontend. |
| CQ-007 | The store-failure catch is narrowed. |
| CQ-008 | Endpoints have OpenAPI names, summaries and response metadata. |
| CQ-010 | Added `demo/README.md` with a 5-minute demo script. |
| CQ-011 | The header link is now a router `Link`. |

## 4. Verification after fixes

| Check | Result |
|---|---|
| `Target/MG-02/ci/quality-gate.sh` | **Passed** |
| Backend | `dotnet build -warnaserror` clean; **38/38** tests; **94.7%** line coverage; no vulnerable NuGet packages |
| Frontend | ESLint clean; **19/19** tests; **96.5%** line coverage; Vite build OK; `npm audit` shows no High or Critical (2 Moderate React Router) |
| Phase 8 suites re-run on the rebuilt stack | API **40/40**, UI **20/20** (BR-005 unit and HTTP-006/007 were run separately: pass) |
| SEC-002 live | Direct calls with a spoofed `X-Forwarded-For` header are still limited (5 × 400, then 429). Spoofing is ignored. |
| SEC-008 live | Audit shows `ClientIp=172.28.0.1` and `User=unknown#854439e150fb` for an unknown user |
| SEC-009 live | After a SignOnService restart, the MG-01 list still returns 200 with a new token. No MG-01 restart is needed. |
| SEC-010 live | Token claims: `aud, exp, iat, iss, nbf, role, scope, sub, user_type`. There is no `name` claim. |
| OpenAPI | sign-in documents 200/400/401/429/503; the menus document 200/401/403 |

## 5. Residual risks and open items

| Item | Owner / decision |
|---|---|
| SEC-001 plain-text 8-character passwords (legacy parity, demo only, blocked outside Development) | Product: hash passwords or move to the enterprise IdP before production |
| SEC-003 user enumeration through distinct messages; SEC-004 no lockout; SEC-005 no token revocation | Product / security, together with SEC-001 |
| SEC-007 React Router v6 Moderate advisories | Cross-group upgrade to v7 (also open for MG-01) |
| CQ-001 duplicated cross-cutting code; CQ-002 shell imports MG-01 source | Platform: shared library and frontend workspace (edits outside `Target/MG-02`) |
| CQ-004 no versioned migrations (MG-01 and MG-02) | Phase 5 owner |
| CI wiring of the MG-01 and MG-02 gate scripts | Repository owner |
| Demo runs in Development (HTTP) | Documented; production is enforced by `StartupGuards` |

## 6. Decisions for Gate 3 sign-off
1. Accept MG-02 with the residual risks in §5. SEC-001, SEC-003, SEC-004 and SEC-005 are tracked as one "credential hardening" item before production.
2. Confirm that MG-03 (user security administration, CU00–CU03) is next. It writes the same `usrsec` table, so it is the natural place to introduce hashed passwords.

## 7. Sign-off
- [x] **Gate 3: APPROVED** by the user ("ok proceed", 2026-11-20 16:15 IST). MG-03 is next.
