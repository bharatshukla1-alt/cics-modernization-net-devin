# MG-03 – Security Review (User Security Administration)

## 1. Security Scorecard
| # | Metric | Measured value | Band | Basis (how it was derived) |
|---|--------|----------------|------|----------------------------|
| 1 | Overall Security Posture Grade | **C** | C – moderate findings, fix before sign-off | 0 Critical and 1 High (SEC-001). Any High caps the grade at C. |
| 2 | OWASP Top 10 (2021) Coverage | **6 / 10 clean** | C | Breached: A02 (SEC-001), A04 (SEC-003), A05 (SEC-005), A06 (SEC-006). Clean: A01, A03, A07, A08, A09, A10. |
| 3 | Authentication & Session Strength | **78 %** (7/9) | 60–89 % – adequate | **Present:** RS256 JWT validated against SignOnService JWKS (iss/aud/exp/nbf), 2-minute clock skew, inbound claim mapping off, anonymous access only to `/health`, every user route guarded, no token in web storage, idle timeout in the shell. **Missing:** token revocation (SEC-002), refresh rotation. |
| 4 | Authorization Coverage | **100 %** (5/5 endpoints) | 100 % | Each endpoint needs role `carddemo.admin` **and** a scope (`users.read` / `users.write`). Read-scope POST returns 403 (API test). User token returns 403 on all 5 (AUTHZ-001…003). |
| 5 | Input Validation & Injection Resistance | **100 %** (5/5 fields); no injection sink | ≥95 % | `UserValidator`: required fields, lengths 8/20/20/8, alphanumeric ID, type A/U. Unknown JSON members are rejected. EF Core parameterised queries; the one raw SQL statement (`FOR UPDATE`) has no input. React escapes output. |
| 6 | Data Protection & Privacy (PCI/GDPR) | **67 %** (2/3) | 60–74 % | ✓ Passwords never returned or logged. ✓ Audit records carry only the user ID. ✗ Plain-text storage (SEC-001). |
| 7 | Transport, Headers & Browser Hardening | **89 %** (8/9) | 75–89 % | TLS 1.3 only outside Development, HSTS, HTTPS redirect, CSP `default-src 'none'`, nosniff, DENY framing, no-referrer, `no-store`, Server header removed. Missing: TLS in the demo (SEC-005). |
| 8 | Secrets & Supply-Chain Hygiene | **0 secrets; 2 moderate advisories** | C | `git grep` finds only test fixture values. The demo `.env` is generated and gitignored. NuGet: no vulnerable packages. npm: 2 moderate React Router advisories (SEC-006). |
| 9 | Vulnerability Density | **6.6 / KLOC; 1.6 / endpoint** | 4–8 – moderate | 8 findings ÷ 1,204 LOC (backend `src` 706 + frontend `src` 498); 8 ÷ 5 endpoints. |

## 2. Review Metadata
| Item | Value |
|---|---|
| Tran Code | MG-03 – User Security Administration (CU00–CU03 / COUSR00C–COUSR03C) |
| Review date | 2026-11-20 |
| Target paths scanned | `Target/MG-03/Backend/UserAdminService` (src, tests, Dockerfile, role SQL); `Target/MG-03/Frontend/src`; `Target/MG-03/demo`; the MG-02 shell wiring (routes, session, Vite proxy) and the SignOnService scope change |
| Context sources | MG-03 UI/Backend/Reconcile specs, architecture, VSAM-Details, DB-Status, Phase 8 results (75/75) |
| Tools run | `dotnet list package --vulnerable --include-transitive`, `npm audit --omit=dev`, `dotnet build` (0 warnings), `git grep` secret scan, live checks from Phase 8 |

## 3. Executive Summary
The service is guarded by role and scope on every endpoint. It validates all input and uses a database login that can touch only `usrsec`. It enforces optimistic concurrency and the self/last-admin rules, and it audits every change without passwords.

The one High finding (SEC-001) is the plain-text password storage the user accepted for the demo. The service refuses to start that way outside Development unless explicitly acknowledged.

The Medium findings are:
- a deleted or demoted user's token stays valid for up to 15 minutes (SEC-002)
- the admin write endpoints have no rate limit (SEC-003)
- the audit trail exists only in container logs (SEC-004)
- the demo runs without TLS (SEC-005)

## 4. Findings by Severity
### Critical
None.

### High
| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-001 | A02, A07; PCI-DSS 8.3.2 | Passwords are written in plain text (upper-cased, 8 characters) to `usrsec.sec_usr_pwd`. This was the user's Phase 6 decision. | `Users/UserService.cs` CreateAsync/UpdateAsync; `Security/StartupGuards.cs` (requires `Auth:AllowLegacyPlainTextPasswords` outside Development) | Anyone who reads the database or a backup sees every credential. | Hash with bcrypt or Argon2 in a wider column, together with SignOnService (MG-02 SEC-001). Or federate to the enterprise IdP. |

### Medium
| ID | Area | Finding | Evidence | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-002 | A01, A07 | A token issued before a user is deleted or demoted stays valid until it expires (15 minutes). The services trust the token's `role` claim. | Program.cs JWT setup; SignOnService token lifetime | A demoted admin can keep using admin functions for up to 15 minutes. | Shorten the admin token lifetime, or check the current user type on write. Add revocation (a `jti` deny-list in Redis). |
| SEC-003 | A04 | No rate limit on `POST/PUT/DELETE /api/v1/users`. | Program.cs (no `AddRateLimiter`) | A stolen admin token can be used to bulk-create or bulk-delete users quickly. | Add a per-user fixed-window limit on write endpoints, like the SignOnService limiter. |
| SEC-004 | A09 | Audit events go only to the container's stdout logger. Nothing retains or protects them from tampering. | `Audit/AuditLog.cs` | User maintenance cannot be reliably reconstructed after a container restart. | Ship the logs to a SIEM or append-only store (Kafka/Azure Service Bus topic), as Gate 2 suggested. |
| SEC-005 | A05 | The demo compose runs `ASPNETCORE_ENVIRONMENT=Development` over HTTP. | `Target/MG-03/demo/docker-compose.yml` | Fine for local demos only. In production the guards force HTTPS and TLS 1.3. | Document it as local-only (done in the run guide). Use the production profile behind TLS for shared demos. |

### Low
| ID | Area | Finding | Evidence | Recommended Fix |
|---|---|---|---|---|
| SEC-006 | A06 | 2 moderate React Router advisories, fixed only in v7 (same as MG-01/MG-02). | `npm audit` | Upgrade all groups to React Router v7 together. |
| SEC-007 | A04 | The "Password" input on Update takes any 8 characters. There is no complexity rule (legacy parity). | `UserValidator.cs` | Add a complexity policy when hashing is introduced. |
| SEC-008 | A09 | When an ID fails validation, the audit trail has no record of the attempt, because validation runs before auditing. | `UserService.CreateAsync` | Optional: audit validation rejections too, with the raw ID truncated to 8 characters. |

## 5. Positive Controls Verified
- Role **and** scope checks on every endpoint (AUTHZ-001…003; a read-scope token cannot write).
- Least-privilege `carddemo_useradmin` login: CRUD on `usrsec` only. Live checks show permission denied on `transaction_type` and on DDL.
- `If-Match` concurrency: 428 when missing, 412 when stale (CON-001/002/004).
- Self and last-admin protection, with admin rows locked `FOR UPDATE` (BR-004/005).
- No password in responses or logs (FE-006, audit log check).

## 6. Verdict
**Proceed with fixes.** No Critical findings. SEC-001 is an accepted demo risk. SEC-002, SEC-003 and SEC-004 can be fixed within `Target/MG-03` (SEC-002 partly needs SignOnService).
