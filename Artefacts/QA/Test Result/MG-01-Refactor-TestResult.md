# MG-01 — Refactor Test Result (2026-11-18)

## 1. Run Metadata
| Item | Value |
|---|---|
| Tran Group | `MG-01` — Transaction type maintenance (CTLI/COTRTLIC, CTTU/COTRTUPC) |
| Target under test | ASP.NET Core Web API (.NET 9.0.321) `Target/MG-01/Backend/TransactionTypeService/src/TransactionTypeService.Api` · React/Vite SPA `Target/MG-01/Frontend` |
| Database | PostgreSQL 16 (docker), database `carddemo`, table `transaction_type`, 7 seed rows from `Input/ctl/DB2LTTYP.ctl` |
| Backend URL | `http://localhost:5080`, REST base `/api/v1/transaction-types`, health `/health` |
| Context | `Artefacts/Specs/Refactor/MG-01-*-Spec.md`, `Artefacts/Architecture/MG-01.Architecture.md`, `Artefacts/QA/Test Case/MG-01-Refactor-TestCase.md` |

The previous version of this file described a Java 21 / Spring Boot target that is not in this repository; it was replaced by this run (see git history).

## 2. Result Summary
| # | Scenario | Cases | Passed | Failed | Blocked |
|---|---|---|---|---|---|
| 1 | Backend unit tests (xUnit, `Tests/TransactionTypeService.Tests.csproj`) | 15 | 15 | 0 | 0 |
| 2 | API functional equivalence vs Postgres (curl, auth disabled run) | 14 | 14 | 0 | 0 |
| 3 | Authentication guard (auth enabled run) | 4 | 4 | 0 | 0 |
| 4 | Frontend build + lint (`vite build`, `eslint .`) | 2 | 2 | 0 | 0 |
| 5 | Screen-level E2E (Playwright) | — | — | — | All |

## 3. API Functional Equivalence (Postgres)
| Case | Request | Expected (legacy behaviour) | Actual |
|---|---|---|---|
| E6 health | GET /health | DB reachable | 200 Healthy |
| E1 first page | GET list | 7 rows ascending from 01, no previous page (CA-FIRST-PAGE) | 200, 01–07 |
| E1 description filter | `?description=pay` | case-insensitive LIKE | 200, 02 PAYMENT |
| E1 invalid filter | `?typeCode=A1` | "Type code must be a 2-digit number" | 400 INVALID_TYPE_CODE_FILTER |
| E2 read | GET /03 | CREDIT | 200 |
| E2 not found | GET /55 | SQLCODE +100 message | 404 TXN_TYPE_NOT_FOUND |
| E3 insert | POST 08 FEE | row added | 201 |
| E3 duplicate | POST 08 again | SQLCODE -803 | 409 TXN_TYPE_ALREADY_EXISTS |
| E1 forward paging | `?cursor=07&direction=forward` | next page with previous enabled | 200, 08, hasPrevious=true |
| E4 no change | PUT /08 "fee" | "No change detected" (case-insensitive compare) | 200 TXN_TYPE_NO_CHANGE |
| E4 update | PUT /08 "SERVICE FEE" | row updated | 200 changed=true |
| E4 concurrent delete | PUT /77 | record removed by someone else | 409 TXN_TYPE_CONCURRENTLY_DELETED |
| E5 delete | DELETE /08 | row deleted | 204 |
| E5 delete again | DELETE /08 | not found | 404 |

Final row count after the run: 7 (fixtures cleaned up).

## 4. Authentication Guard
| Case | Expected | Actual |
|---|---|---|
| GET list without token | 401 | 401 |
| DELETE without token | 401 | 401 |
| GET list with malformed bearer token | 401 | 401 |
| GET /health anonymous | 200 | 200 |

## 5. Bugs Found and Fixed
| ID | Defect | Fix |
|---|---|---|
| BUG-01 | API used EF in-memory store; Postgres never used | `Program.cs` uses Npgsql when `ConnectionStrings:DefaultConnection` is set |
| BUG-02 | EF mapping `TRANSACTION_TYPE`/`TR_TYPE` quoted upper-case, does not match Postgres lower-case DDL | Mapped to `transaction_type`, `tr_type`, `tr_description` |
| BUG-03 | `Database=CARDDEMO` in appsettings (Postgres database is `carddemo`) | Corrected |
| BUG-04 | Root `TransactionTypeService.csproj`/`Program.cs` was the weather-forecast template and globbed `src/` | Removed |
| BUG-05 | `Tests/TransactionTypeServiceTests.cs` had no project and only placeholder asserts | Added xUnit project with 15 service tests |

## 6. Blocked / Not Run
- Screen-level Playwright cases (UI-Spec §3–§10): no Keycloak realm/client definition is in the repo (`carddemo` realm, `mg01-frontend` client), so an authenticated browser session could not be created. Authenticated API calls with `TXN_TYPE_ADMIN`/`TXN_TYPE_READ` were likewise not exercised.
