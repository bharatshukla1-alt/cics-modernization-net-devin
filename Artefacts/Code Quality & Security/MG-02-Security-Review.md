# MG-02 – Security Review (Sign-on and Navigation Shell)

## 1. Security Scorecard

| # | Metric | Measured value | Band | Basis (how it was derived) |
|---|--------|----------------|------|----------------------------|
| 1 | Overall Security Posture Grade | **C** | C – moderate findings, fix before sign-off | 0 Critical and 2 High (SEC-001, SEC-002); any High caps the grade at C. |
| 2 | OWASP Top 10 (2021) Coverage | **4 / 10 clean** | D – broad exposure | Breached: A02 (SEC-001), A04 (SEC-002), A05 (SEC-006, SEC-011, SEC-012), A06 (SEC-007), A07 (SEC-003, SEC-004, SEC-005), A09 (SEC-008). Clean: A01, A03, A08, A10. |
| 3 | Authentication & Session Strength | **56 %** (5/9) | 40–59 % – weak | Present: JWT signature/iss/aud/exp validation, asymmetric RS256 with JWKS, token held in memory, 15-min expiry and idle timeout, client sign-out. Missing: hashed credentials (SEC-001), lockout (SEC-004), enumeration-safe responses (SEC-003), revocation (SEC-005). |
| 4 | Authorization Coverage | **100 %** (7/7) | 100 % | Both menu endpoints are role-guarded (`RequireAuthorization(Roles.*)`, Program.cs:113-114). All 5 MG-01 endpoints return 403 for a user token (tests AUTHZ-003/004). Discovery, JWKS and `/health` are anonymous by design and return no private data. |
| 5 | Input Validation & Injection Resistance | **100 %** (2/2), no injection sink | ≥95 % | Inbound fields `userId` and `password` are both required and capped at 8 characters (SignInService.cs:23-31). Unknown JSON members are rejected (Program.cs:49) and the body is capped at 1 KB (Program.cs:28). Data access is an EF LINQ primary-key lookup only (SignOnDbContext.cs). |
| 6 | Data Protection & Privacy (PCI/GDPR) | **50 %** (2/4 applicable) | 40–59 % – significant | Applicable: credential protection ✗ (SEC-001), token data minimisation ✗ (SEC-010), no secrets/passwords in logs ✔, encryption in transit outside Development ✔ (StartupGuards + TLS 1.3). No PAN/CVV in MG-02. |
| 7 | Transport, Headers & Browser Hardening | **89 %** (8/9) | 75–89 % – minor omissions | Present: TLS 1.3 outside Development, HSTS, API CSP `default-src 'none'`, CORS allow-list, `nosniff`, `Referrer-Policy`, `frame-ancestors`/`X-Frame-Options`, `Cache-Control: no-store`. Missing: CSP on the SPA (SEC-011). |
| 8 | Secrets & Supply-Chain Hygiene | **0 secrets; 2 moderate advisories** | C – 0 secrets, vulnerable dependencies present | `git grep` found no committed credentials or keys. `demo/.env` is git-ignored and the signing key is ephemeral or a mounted PEM. NuGet: no vulnerable packages. `npm audit`: 2 moderate advisories in `react-router-dom` 6.30.6 (SEC-007). Versions are pinned and lockfiles committed. |
| 9 | Vulnerability Density | **11.8 / KLOC; 2.0 / endpoint** | 9–15 – concerning | 12 findings ÷ 1,020 LOC (backend `src` 594 + frontend `src` non-test 426); 12 ÷ 6 endpoints. By severity: 0 C / 2 H / 6 M / 3 L / 1 I. |

The code-level controls are sound: every protected endpoint is role-checked, input is strictly validated, there are no injection or XSS sinks, and tokens are asymmetric with short lifetimes. The grade is pulled down by two deliberate parity choices (plain-text passwords and distinct error messages) and by a real design weakness: the sign-in rate limit is shared by all clients behind a proxy. The density is high mainly because MG-02 is small (about 1 KLOC) while it owns authentication.

## 2. Review Metadata

| Item | Value |
|---|---|
| Tran Code | MG-02 – Sign-on and Navigation Shell (CC00/COSGN00C, CM00/COMEN01C, CA00/COADM01C) |
| Review date | 2026-11-20 |
| Target paths scanned | `Target/MG-02/Frontend` (src, vite/eslint config, package.json, lockfile, Dockerfile.demo); `Target/MG-02/Backend/SignOnService` (src, tests, csproj, appsettings*, Dockerfile); `Target/MG-02/Backend/db`; `Target/MG-02/demo` |
| Context sources | `Artefacts/Specs/Refactor/MG-02-{UI,Backend,reconcile}-Spec.md`; `Artefacts/Architecture/MG-02.Architecture.md`; `Artefacts/TranGroupData/MG-02-SignOnAndNavigationShell-{BSTS,BDD,VSAM-Details}.md`; `Artefacts/Review/MG-02-ReviewGate2.md`; `config/Config.md` |
| Tools run | `dotnet list package --vulnerable --include-transitive`, `npm audit`, `dotnet build -warnaserror`, `git grep` secret scan, Phase 8 live results (HTTP-006 rate limit, DB-002 least privilege) |

## 3. Executive Summary

MG-02 is the authentication front door for the whole estate. **Findings: 0 Critical, 2 High, 6 Medium, 3 Low, 1 Informational.** The High items are:
- plain-text, 8-character passwords (a user decision for legacy parity, marked demo-only and blocked outside Development by `StartupGuards`);
- the sign-in rate limiter keying on the connection IP. Behind the Vite proxy or any gateway, that IP is the proxy's, so one attacker can lock every user out of sign-in with 5 requests a minute.

Verdict: **proceed with fixes.** SEC-002 is the one to fix before any shared demo or deployment. SEC-001 and SEC-003 are accepted parity risks to resolve before production.

## 4. Findings by Severity

### Critical
None found.

### High
| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-001 | 1, 8 (A02, A07; PCI-DSS 8.3.2) | Passwords are stored and compared in plain text, at most 8 characters and upper-cased, as in USRSEC. This is an accepted user decision for the demo. | `Auth/SignInService.cs:63-64`; `Artefacts/DDL/MG-02-…-vsam-schema.sql` (`sec_usr_pwd CHAR(8)`); `Security/StartupGuards.cs:15-17` | A database read exposes every credential. Low entropy invites brute force. | Before production, hash with PBKDF2 or bcrypt (one-way migration of `usrsec`), or delegate sign-in to the enterprise IdP. Keep the guard until then. |
| SEC-002 | 12 (A04, A07) | The sign-in rate limit is partitioned by `Connection.RemoteIpAddress`. Behind the Vite proxy (demo) or a gateway, every request has the proxy's IP, so all users share one bucket of 5 per minute. | `Program.cs:74-75`; `demo/docker-compose.yml` (UI proxies to `signon:5001`); Phase 8 HTTP-006 (6th request → 429) | Denial of service: anyone can block sign-in for everyone. A legitimate busy period also locks users out. | Use `UseForwardedHeaders` with configured `KnownProxies`/`KnownNetworks`, and partition on client IP plus normalised user ID, so the limit applies per account per source. |

### Medium
| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-003 | 9 (A07) | Distinct 401 codes and messages for an unknown user and a wrong password allow user-ID enumeration. Kept for legacy parity (Gate 2 GH-2). | `Auth/SignInService.cs:45-53` | Attackers can harvest valid user IDs. | In production, return one `INVALID_CREDENTIALS` response and keep the distinction only in the audit log. |
| SEC-004 | 1 (A07) | There is no per-account lockout or failed-attempt counter. The legacy had none either. | `Auth/SignInService.cs` (no attempt tracking) | Online guessing against a known ID is limited only by the IP rate limit. | Add a sliding-window failure counter per user ID (memory or Redis) with a temporary lockout. |
| SEC-005 | 1 (A07) | Sign-out only discards the token in the browser. The bearer token stays valid for up to 15 minutes. | `Frontend/src/auth/session.js:15-18`; `Auth/TokenIssuer.cs` (no `jti`/deny-list) | A stolen token survives sign-out. | Add `jti` and a short deny-list checked in each API, or shorten the lifetime and add refresh with revocation. |
| SEC-006 | 3, 15 (A05) | The demo compose runs SignOnService and the MG-01 API with `ASPNETCORE_ENVIRONMENT=Development`: HTTP issuer, no TLS, ephemeral key, `AllowedHosts` from compose only. | `demo/docker-compose.yml:30,45`; `appsettings.Development.json:2-3` | If the demo stack is exposed beyond localhost, credentials and tokens travel in clear text. | Keep the demo on localhost (documented). Add a production compose profile with a TLS cert, PEM key and https issuer. |
| SEC-007 | 13 (A06) | `react-router-dom` 6.30.6 has 2 moderate advisories that are fixed only in v7 (the same residual as MG-01). | `Frontend/package.json` | Moderate client-side risk. | Plan the React Router v7 upgrade across all move groups. |
| SEC-008 | 11 (A09) | Failed sign-in audit events record the raw typed user ID (users sometimes type a password into that field) and do not record the source IP. | `Audit/AuditLog.cs:13-16`; `Auth/SignInService.cs:41,47,52` | Possible secret leakage into logs; weaker forensics. | For `USER_NOT_FOUND`, log a masked or hashed user ID. Add the client IP (after the forwarded-headers fix) to `AuditEvent`. |

### Low / Informational
| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-009 | 1 | In Development the RSA signing key is ephemeral, so a restart invalidates sessions. MG-01 caches the old JWKS until it refreshes. | `Auth/AuthOptions.cs:18-27` | Availability nuisance in demos (seen in Phase 8). | Mount a PEM in the demo via `Auth__SigningKeyPath` (Gate 2 GH-4). |
| SEC-010 | 8 (GDPR data minimisation) | The access token carries the user's full name in the `name` claim, and it is decodable by anyone holding the token. | `Auth/TokenIssuer.cs` (claims dictionary) | Minor personal-data exposure. | Drop `name` from the token. The UI already receives the name in the sign-in response. |
| SEC-011 | 5, 15 (A05) | The shell SPA is served by the Vite dev server in the demo image, with no CSP or security headers on the HTML. | `Frontend/Dockerfile.demo`; `Frontend/index.html` | Reduced XSS defence-in-depth (no XSS sink found in code). | Serve a production build behind nginx or a gateway with a CSP and the same headers as the APIs. |
| SEC-012 | 15 (A05) – Informational | OpenAPI is mapped anonymously, in Development only. | `Program.cs:122-125` | None in production. | No action. |

## 5. Area Coverage Matrix

| # | Area | Status | Finding IDs |
|---|---|---|---|
| 1 | Authentication & session | Findings | SEC-001, SEC-004, SEC-005, SEC-009 |
| 2 | Authorization | Clean: menus role-guarded; MG-01 enforces admin role and scope for tokens from this issuer | — |
| 3 | Transport & network | Findings | SEC-006 |
| 4 | Input validation & injection | Clean: 2/2 fields validated, unknown members rejected, 1 KB body cap, EF LINQ only | — |
| 5 | Output encoding & browser-side risk | Findings (no XSS sink; React escaping) | SEC-011 |
| 6 | CSRF & state-changing requests | Clean: bearer header auth, no cookies; the only POST is sign-in | — |
| 7 | Secrets & configuration | Clean: no committed secrets; DB passwords from git-ignored `.env`; signing key from PEM path; seed passwords are synthetic legacy values | — |
| 8 | Data protection & privacy | Findings | SEC-001, SEC-010 |
| 9 | Error handling & information disclosure | Findings: ProblemDetails with no stack traces, but enumeration is possible | SEC-003 |
| 10 | Database & persistence | Clean: `carddemo_signon` has SELECT on `usrsec` only (Phase 8 DB-002); no schema mutation | — |
| 11 | Logging, audit & monitoring | Findings | SEC-008 |
| 12 | Availability & abuse resistance | Findings | SEC-002 |
| 13 | Dependency & supply chain | Findings | SEC-007 |
| 14 | API & HTTP hygiene | Clean: correct status codes, `problem+json`, `no-store`, versioned `/api/v1` | — |
| 15 | Build & deployment posture | Findings: images run as non-root; Development settings in the demo | SEC-006, SEC-011, SEC-012 |

## 6. Compliance Posture

| Mandate | Status |
|---|---|
| OAuth 2.0 + JWT | **Met for token issuance and validation:** RS256, JWKS, OIDC discovery, `aud`/`iss`/`exp`, role and scope claims. **Partial for credentials:** SEC-001, SEC-003, SEC-004 and SEC-005. SignOnService is the interim IdP (Gate 2 A-1). |
| HTTPS / TLS 1.3 | **Met in code:** a TLS 1.3 Kestrel setting and `StartupGuards` enforce an https issuer outside Development. **Not met in the demo** (SEC-006). |
| OWASP Top 10 | 4/10 categories clean. The breaches come from parity decisions, rate-limit design, configuration and dependencies. There are no injection or access-control flaws. |
| PCI-DSS | No cardholder data. Requirement 8 (strong authentication) is not met because of SEC-001 and SEC-004. Requirement 10 (audit) is partial: events exist, but there is no source IP (SEC-008). |
| GDPR | Names are processed only for display; the token copy is SEC-010. Passwords are never logged. |

## 7. Remediation Plan

| Bucket | Order | Finding | Files touched | Effort |
|---|---|---|---|---|
| Fix before deployment | 1 | SEC-002 | `Program.cs`, `appsettings.json` (KnownProxies) | S |
| | 2 | SEC-008 | `Audit/AuditLog.cs`, `Auth/SignInService.cs` | XS |
| | 3 | SEC-010 | `Auth/TokenIssuer.cs` | XS |
| Fix in next iteration (needs product decision) | 4 | SEC-001, SEC-003, SEC-004 | `Auth/SignInService.cs`, `usrsec` migration (Phase 5 owner), UI messages | M |
| | 5 | SEC-005 | `Auth/TokenIssuer.cs`, MG-01 and MG-02 token validation | M |
| | 6 | SEC-007 | `Frontend/package.json` (React Router v7, every group) | M |
| Optional hardening | 7 | SEC-006, SEC-009, SEC-011 | `demo/docker-compose.yml`, `Frontend/Dockerfile.demo` | S |

## 8. Open Items & Assumptions

- The production topology (gateway or ingress) is not specified. SEC-002's exact fix depends on which proxies are trusted.
- Whether SignOnService remains the IdP or is replaced by the enterprise IdP decides how SEC-001, SEC-003, SEC-004 and SEC-005 are resolved.
- RSA signing runs on a singleton `RSA` instance. This is believed thread-safe on .NET 9 / OpenSSL and the Phase 8 CON-001 test (10 parallel sign-ins) passed; a load test would confirm it.
- CVE status comes from `npm audit` and NuGet advisory data on the review date. Re-run before release.
- LOC counts only non-blank `.cs`, `.js` and `.jsx` under `src`, excluding tests.

## Fixes Applied
The user asked to fix the recommended set. Every change is under `Target/MG-02/`.

| ID | Status | Change | Files |
|---|---|---|---|
| SEC-002 | Fixed | `UseForwardedHeaders` trusts only the configured proxies (`ForwardedHeaders:KnownProxies`). The sign-in limiter now partitions on the real client IP. The shell's Vite proxy sends `X-Forwarded-For` (`xfwd`). The demo pins the UI container to 172.28.0.10 and trusts only that address, so direct callers cannot spoof the header. New test `RateLimitTests`. | `Program.cs`, `appsettings.json`, `Frontend/vite.config.js`, `demo/docker-compose.yml`, `tests/RateLimitTests.cs` |
| SEC-008 | Fixed | Audit events now carry `ClientIp`. For unknown users only a hash prefix is logged (`unknown#<sha256-12>`), never the typed text. | `Audit/AuditLog.cs`, `Auth/SignInService.cs`, `tests/SignInServiceTests.cs` |
| SEC-009 | Fixed | The demo uses a persistent signing key: `start-demo.sh` generates `demo/keys/signon.pem` (git-ignored), mounted through `Auth__SigningKeyPath`. Restarting SignOnService no longer invalidates sessions or MG-01's cached JWKS. | `demo/start-demo.sh`, `demo/docker-compose.yml`, `demo/.gitignore` |
| SEC-010 | Fixed | The `name` claim is removed from the access token. The UI still gets the display name from the sign-in response. | `Auth/TokenIssuer.cs`, `tests/SignInServiceTests.cs` |
| SEC-001, SEC-003, SEC-004, SEC-005, SEC-007 | Not applied | These need a product decision (plain-text passwords, distinct messages, lockout, revocation) or a cross-group upgrade (React Router v7). | — |
| SEC-006, SEC-011, SEC-012 | Not applied | Demo topology only; documented in `demo/README.md`. | — |

Re-verification:
- `ci/quality-gate.sh` passes: 38/38 backend tests, 94.7% line coverage, no vulnerable NuGet packages, ESLint clean, 19/19 frontend tests, build OK, `npm audit` shows no High or Critical.
- The Phase 8 API and UI suites were re-run against the rebuilt stack.
