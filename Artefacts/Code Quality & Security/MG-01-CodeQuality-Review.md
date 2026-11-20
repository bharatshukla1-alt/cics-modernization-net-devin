# MG-01 – Code Quality Review (Transaction Type Maintenance)

## 1. Code Quality Scorecard

| # | Metric | Measured value | Band | Basis (how it was derived) |
|---|--------|----------------|------|----------------------------|
| 1 | Overall Code Quality Grade | **B** | B – only minor findings, cleanup optional | 0 Critical, 0 High, 6 Medium, 6 Low. None of them affect correctness: Phase 8 passed 72/72 runnable cases and 46 + 15 automated tests are green. |
| 2 | Maintainability Rating | **B (80)** | B – minor friction, isolated hotspots | Backend: 16 files, 714 non-blank LOC, strict layering, small methods. Frontend hotspots are `TransactionTypeListPage.jsx` (296 lines) and `TransactionTypeDetailPage.jsx` (217 lines) (CQ-001). |
| 3 | Code Duplication | **≈4 %** (estimated) | B – acceptable | Manual comparison: the Add and Details screens repeat the field state, blur validation, submit-guard and confirm-dialog wiring (~70 lines out of 1,736 LOC). The UI-005 fix had to be applied twice (CQ-002). No tool-measured value (see §9). |
| 4 | Cyclomatic / Cognitive Complexity | **avg ≈ 3 (A); max ≈ 14 (B)** | A – simple, low risk | Branch count per method/component. The highest are `TransactionTypeListPage` (inline-edit plus delete plus paging state) and `TransactionTypeValidators.ValidateList` (≈ 9). All backend methods are ≤ 10. |
| 5 | Architecture Conformance | **90 %** (9/10 rules) | 90–100 % – conforms | Rules checked: controller has no data access ✔, service has no EF types ✔, repository-only DB access ✔, DTOs separate from entity ✔, DI everywhere ✔, async/CancellationToken propagated ✔, centralised error mapping ✔, UI API access isolated in `api/` ✔, hooks hold state ✔. Not met: service layer raises HTTP status codes (CQ-009). |
| 6 | BFSI Standards Compliance | **83 %** (5/6 applicable) | 75–89 % – minor gaps | Server-side validation ✔, no internal leakage in errors ✔, traceId correlation ✔, transactional update with row lock ✔, no floating-point money (no money fields; N/A) ✔, structured audit trail ✘ (CQ-007 / SEC-008). |
| 7 | Documentation & API Coverage | **43 %** | <50 % – insufficient | Files with doc comments on their public type: backend 7/16, frontend 5/12 (non-test) = 12/28. No OpenAPI document is generated. The run guide and specs exist outside the code (CQ-003). |
| 8 | Technical Debt Ratio | **≈ 7 % (B)** | 6–10 % – manageable | Estimated remediation effort for all CQ findings (~3 dev-days) ÷ estimated build effort for MG-01 code (~40 dev-days at 45 LOC/day). |
| 9 | Issue Density | **6.9 / KLOC** | 6–15 – acceptable | 12 findings ÷ 1,736 non-blank LOC. By severity: 0 C / 0 H / 6 M / 6 L. |

The code is clean, layered and well tested at the service and API level. The remaining debt is mostly on the frontend: two large page components and duplicated form logic. The other gaps are delivery hygiene: thin inline documentation, no OpenAPI, no migrations, no CI quality gate and no frontend component tests.

## 2. Review Metadata

| Item | Value |
|---|---|
| Tran Code | MG-01 – Transaction Type Maintenance (CTLI/COTRTLIC, CTTU/COTRTUPC) |
| Review date | 2026-11-20 |
| Target paths scanned | `Target/MG-01/Frontend` (src, configs, package.json), `Target/MG-01/Backend/TransactionTypeService` (src, tests, csproj, appsettings*) |
| Review mode | **Standard only.** No guideline document was supplied, so the yardstick is standard .NET 9 / ASP.NET Core and React 18 best practice plus `config/Config.md` and the MG-01 specs. |
| Context sources | `MG-01-UI-Spec.md`, `MG-01-Backend-Spec.md`, `MG-01-reconcile-Spec.md`, `MG-01.Architecture.md`, MG-01 BSTS/BDD/DB-Details, `config/Config.md`, `config/Style-Sheet.md`, `DEVIN.md`, Phase 8 test result |
| Tools run | `dotnet build -warnaserror` (0 warnings), `dotnet test` (46 passed), `eslint src` (0 issues), `vitest run` (15 passed), `vite build` (ok) – all from Phase 8 and still valid on HEAD |

## 3. Executive Summary

MG-01 conforms to the specified stack (ASP.NET Core 9, EF Core with Npgsql, React/Vite/Tailwind, BFSI tokens) and to the controller → service → repository layering. Business rules R1–R25 are traceable in the code. **Findings: 0 Critical, 0 High, 6 Medium, 6 Low.** **Verdict: "remediate then sign off".** The Medium items are worth fixing before MG-02 copies these patterns. None of them blocks functionality.

## 4. Findings by Severity

### Critical
None.

### High
None.

### Medium

| ID | Area | Finding | Evidence (file:line) | Impact | Recommended Fix |
|---|---|---|---|---|---|
| CQ-001 | 2 Modularity / SRP | The list page mixes data fetching, filter form, inline edit, delete confirmation, alerts and paging in one 296-line component. The Details page (217 lines) is similar. | `Frontend/src/screens/TransactionTypeListPage.jsx`, `TransactionTypeDetailPage.jsx` | Harder to test and change, and MG-02+ screens will copy the pattern. | Extract `FilterBar`, `TransactionTypeTable`/`EditableRow` and `Pager` components plus a `useInlineEdit` hook. |
| CQ-002 | 3 Duplication | The Add and Details forms duplicate field state, blur validation (including the UI-005 `relatedTarget` guard), submit validation and confirm-dialog flow. | `TransactionTypeAddPage.jsx`, `TransactionTypeDetailPage.jsx` (onBlur handlers, submit) | Fixes must be made twice (as UI-005 showed). | Add a shared `useTransactionTypeForm` hook or a `TransactionTypeForm` component. |
| CQ-003 | 10 Documentation | 57 % of source files have no doc comments on public types or members (controller actions, DTOs, `ErrorCodes`, `ITransactionType*`, React screens/components). No OpenAPI/Swagger document. | e.g. `Controllers/TransactionTypesController.cs`, `Dtos/TransactionTypeDto.cs` | Consumers and the next team have to read specs instead of the contract. | Add XML docs (`GenerateDocumentationFile`), `Microsoft.AspNetCore.OpenApi` (`MapOpenApi`) with response types, and JSDoc on shared components and hooks. |
| CQ-004 | 8 Schema / migrations | The service has no versioned schema migrations. The schema lives only in `Artefacts/DDL/…schema.sql`, applied by hand. | `Data/CardDemoDbContext.cs`; no `Migrations/` | Schema drift between environments; no reversible change path. | Add EF Core migrations (baseline from the Phase 5 DDL) or a Flyway/DbUp script set under `Target/MG-01/Backend`. |
| CQ-005 | 16 CI/CD quality gate | No pipeline runs the build, tests, lint, coverage threshold or static analysis. No Dockerfile. | `Target/MG-01/` tree | Quality relies on manual runs, and the regressions Phase 8 caught would not be caught automatically. | Add a CI workflow: `dotnet build -warnaserror`, `dotnet test --collect:"XPlat Code Coverage"` with a threshold, `npm ci && npm run lint && npm test && npm run build`, and an analyzer gate (e.g. Sonar). |
| CQ-006 | 7 Testability / tests | Frontend tests cover only the validation module (15 tests). No component/interaction tests for the screens, dialog focus trap or idle timeout. No coverage measurement on either side. | `Frontend/src/validation/transactionType.test.js` (only test file) | UI behaviour is only verified by manual/browser runs. | Add React Testing Library tests for the List, Add and Details flows and `ConfirmDialog`; enable vitest and coverlet coverage. |

### Low / Informational

| ID | Area | Finding | Evidence (file:line) | Impact | Recommended Fix |
|---|---|---|---|---|---|
| CQ-007 | 9 BFSI conventions | Audit is unstructured log text with no old/new values (same root cause as SEC-008). | `Services/TransactionTypeService.cs:70,92,104` | Weak change history for reference data. | Add a structured audit event or an audit table. |
| CQ-008 | 1 Readability | `Program.cs` (146 lines) holds inline header middleware, auth wiring and a nested `ProblemAuthorizationResultHandler` class. | `Program.cs:27-144` | Startup is hard to scan. | Move these into `AddTransactionTypeSecurity()` and `UseSecurityHeaders()` extension methods and a separate handler file. |
| CQ-009 | 5 Layering | The service layer throws `ApiException` with HTTP status codes (`StatusCodes.Status404NotFound` etc.). | `Services/TransactionTypeService.cs:51,66,85,120` | Couples domain rules to HTTP and limits reuse (e.g. from a message consumer in later MGs). | Throw domain exceptions (`NotFound`, `Conflict`, `NoChanges`) and map them to HTTP in `ProblemDetailsMiddleware`. |
| CQ-010 | 4 Consistency | The page size 7 and the 50-char description limit are hard-coded in both the frontend and the backend. | `transactionTypesApi.js:76`; `validation/transactionType.js`; `TransactionTypeValidators.cs:10-11` | Front and back can drift apart. | Keep the limits in one place, either exposed through the API or kept in a shared constants module with a contract test. |
| CQ-011 | 6 Type safety | The frontend is plain JavaScript, so there is no static typing of the API contract or props. | `Frontend/src/**/*.js(x)` | Contract errors only show up at runtime. | Adopt TypeScript (or JSDoc `@typedef` with `checkJs`) and generate the types from OpenAPI (ties to CQ-003). |
| CQ-012 | 12 Dependency currency | Several packages trail their patched releases (react-router-dom, vite, vitest, postcss); details are in SEC-001 and SEC-006. | `Frontend/package.json` | Upgrade cost grows over time. | Upgrade together with SEC-001/SEC-006 and add Renovate/Dependabot. |

## 5. Area Coverage Matrix

| # | Area | Status | Finding IDs |
|---|---|---|---|
| 1 | Readability & naming | Findings (minor) – naming is clear and intention-revealing | CQ-008 |
| 2 | Modularity / single responsibility | Findings | CQ-001 |
| 3 | Duplication | Findings | CQ-002 |
| 4 | Complexity & consistency | Findings (minor) | CQ-010 |
| 5 | Architecture & layering | Findings (minor) | CQ-009 |
| 6 | Type safety & language idioms | Findings (minor) – C# nullable enabled, records for DTOs, primary constructors | CQ-011 |
| 7 | Testability & test quality | Findings | CQ-006 |
| 8 | Database & schema design | Findings – schema, PK and index match the DB-Details doc; keyset queries use the PK | CQ-004 |
| 9 | BFSI coding conventions | Findings (minor) – no money fields; transactions and row lock used | CQ-007 |
| 10 | Documentation | Findings | CQ-003 |
| 11 | Error handling & resilience | Clean – centralised ProblemDetails, SQLSTATE mapping, CancellationToken propagation | — |
| 12 | Dependency management | Findings (minor) – exact pins and lockfile present | CQ-012 |
| 13 | Accessibility (WCAG 2.2 AA) | Clean – labelled inputs, `aria-invalid`/`aria-describedby`, live-region alerts, focus-trapped dialog, keyboard operable (verified in Phase 8 UI cases) | — |
| 14 | UI consistency / design system | Clean – BFSI tokens via Tailwind theme and `.btn`/`.card`/`.data-table` classes from `Style-Sheet.md` | — |
| 15 | Performance | Clean – keyset paging (no OFFSET), `AsNoTracking`, page size capped at 50, 4 KB body cap | — |
| 16 | CI/CD build & quality gate | Findings | CQ-005 |

## 6. Guideline Conformance

Not applicable. No guideline document was supplied, so the review uses standard practice only.

## 7. Standards & Stack Conformance

| Config key / spec | Expected | Status |
|---|---|---|
| `target.ui.*` | React, Tailwind CSS, micro-animation, responsive | **Met.** React 18 + Vite, Tailwind mapped to BFSI tokens, responsive grid, transitions on alerts and dialogs. |
| `target.backend.*` | .NET 9 / C#, ASP.NET Core Web API, REST | **Met.** `net9.0`, controllers, versioned `/api/v1` routes, ProblemDetails. |
| `target.database.engine` | PostgreSQL | **Met.** EF Core 9 + Npgsql 9.0.4; migrations are missing (CQ-004). |
| `target.security.*` | OAuth 2.0, HTTPS/TLS 1.3 | **Partial.** See SEC-003 and SEC-004 in the security review. |
| `target.state` | Stateless REST + client state | **Met.** No server session; filters and paging live in React and the URL. |
| `target.cache` / `target.messaging` | Redis, Kafka/RabbitMQ | **Not used, as designed.** The Architecture doc states MG-01 needs neither. |
| Ports / proxy | 5000 / 5173, `/api` proxy | **Met.** |
| Specs (UI/Backend/Reconcile) | E1–E6, S1–S3, R1–R25 | **Met.** The deviations (POST/PUT split, no optimistic concurrency, filters kept after delete) were approved at Gate 2. |

## 8. Remediation Plan

| Bucket | Order | Findings | Files touched | Effort |
|---|---|---|---|---|
| Fix before sign-off | 1 | CQ-002, CQ-001 | `Frontend/src/screens/*`, new `components/`, new `hooks/useTransactionTypeForm.js` | M |
| | 2 | CQ-003 | controller, DTOs, csproj (`GenerateDocumentationFile`, OpenAPI), JSDoc on components | S |
| | 3 | CQ-006 | new `*.test.jsx`, vitest config, coverlet | M |
| Fix in next iteration | 4 | CQ-004, CQ-005 | `Backend/.../Migrations`, CI workflow, Dockerfiles | M |
| | 5 | CQ-009, CQ-007 | `Services/`, `Errors/` | S |
| Optional cleanup | 6 | CQ-008, CQ-010, CQ-011, CQ-012 | `Program.cs`, constants, TS config, `package.json` | S–M |

## 9. Open Items & Assumptions

- Duplication, complexity and technical debt figures are manual estimates; no Sonar or other analyzer was run. Wiring one in is part of CQ-005.
- Coverage is not measured; the 46 backend and 15 frontend tests pass, but the percentage is unknown.
- Whether MG-02+ will reuse a shared frontend shell (auth, layout, `api/` client) affects how CQ-001/002/011 should be fixed. Worth deciding before MG-02 starts.
