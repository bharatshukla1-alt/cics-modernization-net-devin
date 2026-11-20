# MG-03 – Code Quality Review (User Security Administration)

## 1. Quality Scorecard
| # | Metric | Measured value | Band | Basis |
|---|--------|----------------|------|-------|
| 1 | Overall Code Quality Grade | **B** | B – good, minor remediation | 0 Critical/High, 5 Medium, 5 Low |
| 2 | Build health | 0 errors, 0 warnings | A | `dotnet build`; `vite build` OK |
| 3 | Backend tests / coverage | 29/29; **86.8 % line, 77.9 % branch** | B (≥80 % line) | `dotnet test --collect:"XPlat Code Coverage"` against PostgreSQL |
| 4 | Frontend tests / coverage | 29/29 (10 MG-03); 96.6 % line overall | A | `vitest run --coverage` in the shell |
| 5 | Functional test pass rate | 75/75 (100 %) | A | Phase 8 result |
| 6 | Size | Backend `src` 706 LOC; frontend 498 LOC | — | `wc -l` |
| 7 | Duplication | Audit, error and startup-guard code is copied in 3 services | C | CQ-001 |

## 2. Review Metadata
| Item | Value |
|---|---|
| Tran Code | MG-03 – CU00–CU03 |
| Review date | 2026-11-20 |
| Scope | `Target/MG-03/**`, MG-02 shell wiring for MG-03 |

## 3. Findings
### Critical / High
None.

### Medium
| ID | Finding | Evidence | Recommendation |
|---|---|---|---|
| CQ-001 | `ApiErrors`, `AuditLog`, `StartupGuards` and `ProblemAuthorizationResultHandler` are copied from MG-02, which copied them from MG-01. | `Target/MG-0{1,2,3}/Backend/**/Errors`, `Audit`, `Security` | Extract a shared `CardDemo.ServiceDefaults` library (same as MG-02 CQ-001). |
| CQ-002 | `UserService.UpdateAsync` and `DeleteAsync` repeat the same 4-branch try/catch audit pattern. The file is 230 lines. | `Users/UserService.cs` | Move the audit and error mapping into one helper (for example `RunAudited(action, target, func)`). Split listing into `UserQueries`. |
| CQ-003 | Keyset ordering uses the PostgreSQL column collation. The legacy VSAM key order is EBCDIC, where letters sort before digits. They give the same result for the seed IDs (all start with a letter) but would differ for IDs starting with a digit. | `UserService.ListAsync` (`string.Compare` → SQL `<`/`>`) | Use `COLLATE "C"` explicitly and document the remaining EBCDIC difference in the reconcile spec. |
| CQ-004 | No versioned database migrations. The schema and role come from SQL scripts. | `Target/MG-03/Backend/db` | Belongs to the Phase 5 database agent (same as MG-01 CQ-004). |
| CQ-005 | There is no `ci/quality-gate.sh` for MG-03, though MG-01 and MG-02 each have one. | `Target/MG-01/ci`, `Target/MG-02/ci` | Add `Target/MG-03/ci/quality-gate.sh`: build, tests, coverage threshold, vulnerable-package and npm audit checks. |

### Low
| ID | Finding | Evidence | Recommendation |
|---|---|---|---|
| CQ-006 | The MG-03 screen tests live in the shell (`MG-02/Frontend/src/mg03`), because MG-03 has no `node_modules`. | `UserScreens.test.jsx` | Acceptable with one shell. Move them if MG-03 gets its own package. |
| CQ-007 | `UserFields.Field` duplicates most of MG-01's `TextField`. It exists to support `type=password`. | `components/UserFields.jsx` | Add a `type` prop to the shared `TextField` and reuse it. |
| CQ-008 | The routes pass a constant `key` ("update"/"delete") that has no effect. | `MG-02/Frontend/src/App.jsx` | Remove it. |
| CQ-009 | `IsDataError` treats every `InvalidOperationException` that wraps an `NpgsqlException` as a data error. This is correct but undocumented. | `UserService.cs` | Add a one-line comment, or narrow it to the Npgsql transient types. |
| CQ-010 | The API tests share the live demo database and rely on the `ZZ` prefix for clean-up. | `ApiTests.cs` | Use a Testcontainers PostgreSQL instance in CI. |

## 4. Positive Observations
- One thin endpoint layer, a service holding the business rules, and a pure validator. The legacy paragraph names are cited in the doc comments.
- The legacy messages are kept as one-line constants and covered by tests.
- The frontend reuses the shell's `Alert`/`ConfirmDialog` and the shared theme. It has accessible labels, alerts and focus management (UI-004).

## 5. Verdict
**Remediate, then sign off.** CQ-002, CQ-003, CQ-005 and CQ-007…CQ-009 stay inside MG-03. CQ-001, CQ-004 and CQ-006 affect other move groups.
