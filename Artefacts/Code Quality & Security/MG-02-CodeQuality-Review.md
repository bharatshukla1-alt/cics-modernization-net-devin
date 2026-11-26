# MG-02 – Code Quality Review (Sign-on and Navigation Shell)

## 1. Code Quality Scorecard

| # | Metric | Measured value | Band | Basis (how it was derived) |
|---|--------|----------------|------|----------------------------|
| 1 | Overall Code Quality Grade | **B** | B – only minor findings, cleanup optional | 0 Critical, 0 High, 5 Medium, 6 Low/Info. No finding affects correctness: 63/63 Phase 8 cases pass. |
| 2 | Maintainability Rating | **B (~82)** ~ | B – minor friction | Small, cohesive units: 11 backend source files and 7 frontend modules. Largest hotspot is `Program.cs` at 156 lines (CQ-003). Estimated; no Sonar run. |
| 3 | Code Duplication | **~11 %** ~ | D – high, active duplication debt | About 110 lines are copied from MG-01 (`ApiErrors.cs`/ProblemDetails, `AuditLog.cs`, `StartupGuards.cs`, the security-header block and the authorization result handler in `Program.cs`) out of 1,020 LOC. Duplication within MG-02 itself is negligible. Estimated by file comparison. |
| 4 | Cyclomatic / Cognitive Complexity | **avg ~2.5 (A)** ~ | 1–10 – simple | Highest are `SignInService.SignInAsync` (~8) and `MenuService.ToOption` (~4). React components are mostly linear. Estimated by reading the code. |
| 5 | Architecture Conformance | **88 %** | 75–89 % – minor deviations | Endpoints → services → store → DbContext layering holds, DTOs are records and no entity leaks (the store returns trimmed copies). Deviations: the frontend imports another move group's source tree (CQ-002), and composition and endpoints are mixed in `Program.cs` (CQ-003). |
| 6 | BFSI Standards Compliance | **83 %** (5/6) | 75–89 % – minor gaps | Met: audit event for sign-in, named constants for error codes and roles, no numeric/money handling (not applicable), constant-time comparison, ProblemDetails envelope. Gap: menu status strings are duplicated as literals in the frontend (CQ-006). |
| 7 | Documentation & API Coverage | **48 %** (16/33) | <50 % – insufficient | Backend: 10/26 public types have XML `<summary>`. Frontend: 6/7 modules have JSDoc. The minimal-API endpoints have no OpenAPI response metadata (CQ-008), and there is no MG-02 demo README (CQ-010). |
| 8 | Technical Debt Ratio | **~7 %** ~ | B – manageable | Estimated remediation of about 2 developer-days (shared library, endpoint split, CI script, docs) against about 30 days of build effort. |
| 9 | Issue Density | **10.8 / KLOC** | 6–15 – acceptable | 11 findings ÷ 1,020 LOC (backend `src` 594 + frontend `src` non-test 426). By severity: 0 C / 0 H / 5 M / 5 L / 1 I. |

MG-02 is small, well tested (35 backend tests at 94% line coverage, 19 frontend tests at 95%) and simple to follow. The main debt is structural: shared cross-cutting code is copied between services instead of shared, and the shell reaches directly into MG-01's source tree. Both will multiply as MG-03 to MG-12 arrive, so they are worth fixing now.

## 2. Review Metadata

| Item | Value |
|---|---|
| Tran Code | MG-02 – Sign-on and Navigation Shell |
| Review date | 2026-11-20 |
| Target paths scanned | `Target/MG-02/Frontend`, `Target/MG-02/Backend/SignOnService` (src + tests), `Target/MG-02/Backend/db`, `Target/MG-02/demo` |
| Review mode | **Standard only.** The user's Phase 9 answer for MG-01 ("No, use standard .NET/React best practices") is reused for MG-02; no external guideline was supplied. |
| Context sources | MG-02 UI, Backend and reconcile specs; `MG-02.Architecture.md`; MG-02 BSTS, BDD and VSAM-Details; `MG-02-Run-Guide.md`; `config/Config.md`; the MG-01 target code (for the duplication comparison) |
| Tools run | `dotnet build -warnaserror` (0 warnings), `dotnet test` with coverage, ESLint (clean), `vitest --coverage`, `vite build` |

## 3. Executive Summary

The code is correct, readable and covered by tests. **Findings: 0 Critical, 0 High, 5 Medium, 5 Low, 1 Informational.** The Medium items are:
- cross-service duplication;
- cross-group source coupling in the shell;
- an overloaded `Program.cs`;
- no versioned migrations for `usrsec`, which is owned by Phase 5;
- no CI quality gate for MG-02.

Verdict: **remediate then sign off.** CQ-001, CQ-003 and CQ-005 are quick wins; CQ-002 is a design choice to confirm.

## 4. Findings by Severity

### Critical
None found.

### High
None found.

### Medium
| ID | Area | Finding | Evidence (file:line) | Impact | Recommended Fix |
|---|---|---|---|---|---|
| CQ-001 | 1 | Cross-cutting code is copied from MG-01: ProblemDetails middleware and writer, audit log, startup guards, the security-header middleware and the 401/403 result handler. | `Errors/ApiErrors.cs:1-70`; `Audit/AuditLog.cs`; `Security/StartupGuards.cs`; `Program.cs:89-97,139-156` versus the matching MG-01 files | Every fix must be made in N services; drift is likely. | Extract a `CardDemo.ServiceDefaults` class library (errors, audit, guards, headers, auth result handler) referenced by both services. |
| CQ-002 | 2 | The shell imports MG-01 source files through a Vite alias into `../../MG-01/Frontend/src`. MG-01's own `App.jsx` and OIDC sign-in flow become dead code in the combined deployment. | `Frontend/vite.config.js:9,13-14`; `Frontend/src/App.jsx:3-7`; `Frontend/src/auth/session.js:1` | Build coupling across move groups, and React must be deduplicated by configuration. | Turn the frontends into an npm workspace (`packages/mg01-transaction-types`) that exports its screens, with the shell depending on it. Retire MG-01's standalone sign-in when it is hosted by the shell. |
| CQ-003 | 1, 7 | `Program.cs` (156 lines) mixes service registration, the rate limiter, CORS, header middleware, all endpoint definitions, the discovery document and an authorization handler class. | `Program.cs:1-156` | Harder to read and test, and grows with every endpoint. | Move endpoints to `Endpoints/AuthEndpoints.cs` and `MenuEndpoints.cs` (`MapGroup`), and the result handler to `Security/`. |
| CQ-004 | 8 | `usrsec` has no versioned schema migration; the DDL is applied with `db-apply.mjs`. This is the same as MG-01 CQ-004. | `Artefacts/DDL/MG-02-…-vsam-schema.sql`; `demo/docker-compose.yml` initdb mounts | Schema changes (for example hashed passwords) cannot be rolled forward or back safely. | Phase 5 owner: add versioned migrations (EF Core migrations or Flyway-style SQL) for MG-01 and MG-02 together. Not changed here. |
| CQ-005 | 16 | There is no CI quality gate for MG-02; MG-01 has `ci/quality-gate.sh`, MG-02 has none, and neither is wired into repository CI. | `Target/MG-02/` (no `ci/`) | Regressions are not caught automatically. | Add `Target/MG-02/ci/quality-gate.sh` (build `-warnaserror`, tests with coverage thresholds, ESLint, vitest coverage, audits). Wiring into repo CI needs approval. |

### Low / Informational
| ID | Area | Finding | Evidence (file:line) | Impact | Recommended Fix |
|---|---|---|---|---|---|
| CQ-006 | 3 | Menu status codes are string literals repeated across the backend and two frontend files. | `Menus/MenuService.cs:9-15`; `Frontend/src/pages/MenuPage.jsx:8-12`; `Frontend/src/components/MenuTile.jsx:1-5` | Typos and drift. | Add `Frontend/src/api/menuStatus.js` constants used by both components. |
| CQ-007 | 1 | `SignInService` turns every non-cancellation exception from the store into `UNABLE_TO_VERIFY`. | `Auth/SignInService.cs:38` | Programming errors are reported as "unable to verify" (they are still logged). | Narrow the catch to `DbException`, `TimeoutException` and `InvalidOperationException`. |
| CQ-008 | 7, 10 | Minimal-API endpoints have no `.WithName`, `.WithSummary` or `.Produces` metadata, so OpenAPI lacks response schemas and error codes. | `Program.cs:107-130` | API consumers rely on the specs instead of the live contract. | Add endpoint metadata and `ProducesProblem(400/401/403/429/503)`. |
| CQ-009 | 14 | User-facing strings are hardcoded in the components (single locale, consistent with MG-01). | `Frontend/src/pages/*.jsx` | Localisation would need a sweep. | Add a message catalogue when a second locale is required. |
| CQ-010 | 10 | `Target/MG-02/demo` has no README (MG-01 has one), so the demo instructions are only in the run guide and `start-demo.sh`. | `Target/MG-02/demo/` | Onboarding friction for presenters. | Add `demo/README.md` with the steps, credentials and a 5-minute demo script. |
| CQ-011 | 1 – Info | The header's home link uses an inline handler to prevent default navigation. | `Frontend/src/App.jsx:64-66` | Style only. | Use `<Link to={homeFor(user)}>`. |

## 5. Area Coverage Matrix

| # | Area | Status | Finding IDs |
|---|---|---|---|
| 1 | Readability & maintainability | Findings | CQ-001, CQ-003, CQ-007, CQ-011 |
| 2 | Architecture & layering | Findings | CQ-002 |
| 3 | BFSI coding standards | Findings: no money arithmetic in the group; codes are mostly named constants | CQ-006 |
| 4 | Transactions & data integrity | Not applicable: the service is read-only (USRSEC read) | — |
| 5 | Concurrency & thread-safety | Clean: stateless services, immutable singletons; CON-001 passed | — |
| 6 | Performance | Clean: single primary-key lookup with `AsNoTracking`; menus served from static data | — |
| 7 | API design & contract | Findings | CQ-003, CQ-008 |
| 8 | Database & schema design | Findings | CQ-004 |
| 9 | Logging & observability | Clean: structured audit events with traceId; no console logging | — |
| 10 | Documentation | Findings | CQ-008, CQ-010 |
| 11 | Configuration hygiene | Clean: issuer, audience, routes, rate limit and CORS are externalised; ports come from env | — |
| 12 | Dependency & framework currency | Clean: pinned versions and lockfiles, same stack versions as MG-01 | — |
| 13 | Accessibility (WCAG 2.2 AA) | Clean: labelled inputs, `aria-invalid`/`describedby`, focus moved to the invalid field, `role="alert"`, tiles as buttons, header contrast fixed | — |
| 14 | i18n / l10n | Findings | CQ-009 |
| 15 | Maker-checker | Not applicable: the legacy sign-on and menus have no approval or dual-control step (BSTS) | — |
| 16 | CI/CD quality gate | Findings | CQ-005 |

## 6. Guideline Conformance
Not applicable: no external guideline was supplied.

## 7. Standards & Stack Conformance

| Config / spec item | Status |
|---|---|
| .NET 9 / ASP.NET Core Web API | Met |
| React + Vite + Tailwind with BFSI tokens | Met; the shell reuses `bfsi-theme.css` |
| PostgreSQL via EF Core / Npgsql | Met; read-only with a least-privilege role |
| OAuth 2.0 / JWT | Met (RS256 + JWKS; MG-01 trusts it) |
| Microservice architecture | Met (SignOnService is separate from TransactionTypeService). Partial for the frontend because of the shared-source coupling (CQ-002). |
| Stateless REST | Met; no server session |
| Spec conformance (UI, Backend, reconcile) | Met; every reconcile row is implemented and covered by Phase 8 |

## 8. Remediation Plan

| Bucket | Order | Finding | Files touched | Effort |
|---|---|---|---|---|
| Fix before sign-off | 1 | CQ-005 | new `Target/MG-02/ci/quality-gate.sh` | XS |
| | 2 | CQ-003, CQ-008 | `Program.cs`, new `Endpoints/*.cs` | S |
| | 3 | CQ-006, CQ-007, CQ-010, CQ-011 | `MenuPage.jsx`, `MenuTile.jsx`, new `menuStatus.js`, `SignInService.cs`, new `demo/README.md`, `App.jsx` | XS |
| Fix in next iteration | 4 | CQ-001 | new shared library used by MG-01 and MG-02 (edits outside `Target/MG-02`) | M |
| | 5 | CQ-002 | frontend workspace restructure (MG-01 and MG-02) | M |
| | 6 | CQ-004 | Phase 5 migrations | M |
| Optional cleanup | 7 | CQ-009 | message catalogue | S |

## 9. Open Items & Assumptions

- The duplication, maintainability, complexity and debt figures are estimates (`~`) from reading the code; no Sonar or jscpd run was available.
- CQ-001 and CQ-002 need edits outside `Target/MG-02/`, which the fix agent may not make without approval.
- Security observations are handed to the Security Review: rate-limit partitioning behind the proxy (SEC-002) and the audit-log user-ID masking (SEC-008).

## Fixes Applied
The user asked to fix the recommended set. Every change is under `Target/MG-02/`.

| ID | Status | Change | Files |
|---|---|---|---|
| CQ-003 | Fixed | `Program.cs` now holds only composition (down from 156 to ~115 lines). Endpoints moved to `Endpoints/AuthEndpoints.cs`, `MenuEndpoints.cs` and `WellKnownEndpoints.cs` (route groups). The 401/403 handler moved to `Security/ProblemAuthorizationResultHandler.cs`. | `Program.cs`, `Endpoints/*.cs`, `Security/ProblemAuthorizationResultHandler.cs` |
| CQ-005 | Fixed | Added `Target/MG-02/ci/quality-gate.sh`: build with warnings as errors, tests with an 80% backend coverage floor, NuGet vulnerability check, ESLint, vitest coverage thresholds, Vite build and `npm audit` (High). It passes. | `ci/quality-gate.sh` |
| CQ-006 | Fixed | Added `MENU_STATUS` constants, used by `MenuPage` and `MenuTile`. | `Frontend/src/api/menuStatus.js`, `pages/MenuPage.jsx`, `components/MenuTile.jsx` |
| CQ-007 | Fixed | The store-failure catch is narrowed to `DbException`, `TimeoutException` and `InvalidOperationException`. Other exceptions surface as 500 and are logged. New unit test. | `Auth/SignInService.cs`, `tests/SignInServiceTests.cs` |
| CQ-008 | Fixed | Endpoints have names, summaries, tags, `Produces<T>` and `ProducesProblem` metadata, so the OpenAPI document lists the response types. | `Endpoints/*.cs` |
| CQ-010 | Fixed | Added `demo/README.md` with run steps, credentials and a 5-minute demo script. | `demo/README.md` |
| CQ-011 | Fixed | The header home link is now a router `<Link>`. | `Frontend/src/App.jsx` |
| CQ-001, CQ-002 | Not applied | These need edits outside `Target/MG-02` (a shared library, an npm workspace) and an approval. | — |
| CQ-004 | Not applied | Schema migrations belong to the Phase 5 owner. | — |
| CQ-009 | Not applied | Optional; single locale. | — |
