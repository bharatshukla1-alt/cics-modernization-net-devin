# MG-01 – Security Review (Transaction Type Maintenance)

## 1. Security Scorecard

| # | Metric | Measured value | Band | Basis (how it was derived) |
|---|--------|----------------|------|----------------------------|
| 1 | Overall Security Posture Grade | **C** | C – moderate findings, fix before sign-off | 0 Critical, 2 High (SEC-001, SEC-002) → capped at C by rule. |
| 2 | OWASP Top 10 (2021) Coverage | **5 / 10 clean** | D – broad exposure | Breached: A02 (SEC-004), A05 (SEC-002, SEC-005, SEC-007, SEC-010), A06 (SEC-001, SEC-006), A07 (SEC-003), A09 (SEC-008, SEC-009). Clean: A01, A03, A04, A08, A10. |
| 3 | Authentication & Session Strength | **83 %** (5/6) | 75–89 % – minor gaps | Expected: JWT validation (issuer/audience/lifetime via Authority), in-memory token storage, idle timeout, sign-out clears session, dev-bypass environment guard, interactive OAuth login. Missing: interactive login (SEC-003). |
| 4 | Authorization Coverage | **100 %** (5/5) | 100 % | All 5 `/api/v1/transaction-types` actions carry `[Authorize(Policy=…)]`; policies require role `carddemo.admin` + scope. `/health` is anonymous by design and returns no data. No object-level ownership applies (reference data). |
| 5 | Input Validation & Injection Resistance | **100 %** (8/8), no injection sink | ≥95 % | Inbound fields: path `typeCode`, body `typeCode`, body `description`, query `typeCode`, `description`, `after`, `before`, `pageSize` – all validated in `TransactionTypeValidators`. Only raw SQL is `FromSql($"…{typeCode}…")` (parameterised interpolation); LIKE wildcards escaped; unknown JSON members rejected; 4 KB body limit. |
| 6 | Data Protection & Privacy (PCI/GDPR) | **75 %** (3/4 applicable) | 75–89 % – minor gaps | MG-01 holds no PAN/CVV/PII (only 2-char code + description), so PAN masking / CVV / erasure are N/A. Applicable: data minimisation ✔, no sensitive data in logs ✔, no SQL/stack leakage in errors ✔, encryption in transit ✘ (SEC-004). |
| 7 | Transport, Headers & Browser Hardening | **86 %** (6/7) | 75–89 % – minor gaps | Present: HSTS (non-Dev), CSP `default-src 'none'`, CORS allow-list, `nosniff`, `Referrer-Policy`, `frame-ancestors`/`X-Frame-Options`. Missing: an actual TLS 1.3 listener (SEC-004). Cookie flags N/A (no cookies). |
| 8 | Secrets & Supply-Chain Hygiene | **0 secrets; 1 runtime + 10 dev-tool advisories** | C – 0 secrets, vulnerable dependencies present | `git grep` shows no committed credentials (DB password via `ConnectionStrings__CardDemo`; `dummy-dev-token` is a dev-only constant, not a secret). Exact version pins + `package-lock.json` present. `npm audit`: react-router-dom 6.28.0 (High, runtime); vite, vitest, tinypool, postcss, esbuild, eslint (dev-only). `dotnet list package --vulnerable --include-transitive`: none. |
| 9 | Vulnerability Density | **6.3 / KLOC; 1.8 / endpoint** | 4–8 – acceptable | 11 findings ÷ 1,736 non-blank LOC (backend 714 + frontend 1,022) ; 11 ÷ 6 endpoints. By severity: 0 C / 2 H / 5 M / 3 L / 1 I. |

The code-level controls are strong: every endpoint is authorised, all inputs are validated server-side, there are no injection sinks and errors do not leak internals. The grade is pulled down by environment and supply-chain issues: a vulnerable runtime router package, a superuser database login, no interactive OAuth login in the SPA and no HTTPS listener actually configured. All of these can be fixed without touching the business logic.

## 2. Review Metadata

| Item | Value |
|---|---|
| Tran Code | MG-01 – Transaction Type Maintenance (CTLI/COTRTLIC, CTTU/COTRTUPC) |
| Review date | 2026-11-20 |
| Target paths scanned | `Target/MG-01/Frontend` (src, vite/tailwind/eslint config, package.json, package-lock.json), `Target/MG-01/Backend/TransactionTypeService` (src + tests, csproj, appsettings*, launchSettings) |
| Context sources | `Artefacts/Specs/Refactor/MG-01-UI-Spec.md`, `MG-01-Backend-Spec.md`, `MG-01-reconcile-Spec.md`; `Artefacts/Architecture/MG-01.Architecture.md`; `Artefacts/TranGroupData/MG-01-TransactionTypeMaintenance-{BSTS,BDD,DB-Details}.md`; `config/Config.md`; `DEVIN.md` |
| Tools run | `npm audit`, `dotnet list package --vulnerable --include-transitive`, `git ls-files`, live `pg_roles` query on the Phase 5 database |

## 3. Executive Summary

MG-01 is a small, well-guarded reference-data service. **Findings: 0 Critical, 2 High, 5 Medium, 3 Low, 1 Informational.** The two High items are a known XSS/open-redirect advisory in the runtime `react-router-dom` version and the service connecting to PostgreSQL as a superuser that also owns the table. Neither is in MG-01's business logic. **Verdict: "proceed with fixes".**

## 4. Findings by Severity

### Critical
None.

### High

| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-001 | 12 Dependencies · A06 | Runtime dependency `react-router-dom` 6.28.0 (and `@remix-run/router`) is covered by GHSA-2w69-qvjg-hvjx (XSS via open redirect). | `Frontend/package.json:15` | Attacker-crafted redirect paths could execute script in an admin session. | Upgrade to `react-router-dom` ≥ 6.30.3 (same major version) and refresh the lockfile. |
| SEC-002 | 10 Database · A05 / PCI 7.2 | The service connects as `carddemo`, which is a PostgreSQL **superuser** (`rolsuper = t`) and owns `transaction_type`. | live `pg_roles`; `appsettings.json` (connection string supplied via env); `Artefacts/MG-01-Run-Guide.md` | If the API is compromised, the attacker gets full control of the cluster (DDL, other databases, `COPY … PROGRAM`). | Create a `carddemo_app` login with only `SELECT, INSERT, UPDATE, DELETE` on `transaction_type`; keep the owner/superuser for migrations only; document it in the run guide. |

### Medium

| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-003 | 1 AuthN · A07 | The SPA has no OAuth 2.0 Authorization Code + PKCE login. `setAccessToken` is never called, so outside dev bypass every call is anonymous (it fails closed with a 401). | `Frontend/src/api/transactionTypesApi.js:8,36`; no caller in `src/` | The mandated OAuth flow is not implemented end to end; teams may be tempted to keep the bypass. | Add an OIDC client (e.g. `oidc-client-ts`) with PKCE, keep the token in memory, add silent renew/sign-out, and drive the `Auth:*` settings from config. |
| SEC-004 | 3 Transport · A02 | No HTTPS endpoint is declared. `ConfigureHttpsDefaults(Tls13)` only applies if an HTTPS endpoint is added, and `launchSettings` is HTTP-only. | `Program.cs:27-35`; `Properties/launchSettings.json:8` | TLS 1.3 (Config mandate) is not enforced by the service itself; bearer tokens could travel in plaintext if TLS is not terminated upstream. | Declare a Kestrel HTTPS endpoint (certificate from config/secret store) with `SslProtocols.Tls13`, or document and enforce TLS termination at the gateway with a startup check. |
| SEC-005 | 15 Build posture · A05 | Base `appsettings.json` uses an `http://` Authority and `AllowedHosts: "*"`. In non-Development environments `RequireHttpsMetadata=true`, so it would fail at first token use rather than at startup. | `appsettings.json:9-13` | Misconfiguration is found late, and Host-header values are not restricted. | Validate at startup that `Auth:Authority` is https outside Development; set `AllowedHosts` to the real hostnames. |
| SEC-006 | 12 Dependencies · A06 | Dev toolchain packages have advisories: vite 5.4.11 (High), vitest 2.1.5 / tinypool (Critical), postcss, esbuild (Moderate), eslint (Low). | `Frontend/package.json:18-28`; `npm audit` | Dev-server file-disclosure/SSRF style issues on developer machines; not shipped in the production bundle. | Upgrade vite to the patched 5.4.x/6.x line, vitest ≥ the patched release, postcss ≥ 8.5.x; run `npm audit` in CI. |
| SEC-008 | 14 Logging · A09 / PCI 10 | Audit events are plain `ILogger` Information lines (who/what code) with no before/after value, no outcome on failure and no durable, tamper-evident sink. | `Services/TransactionTypeService.cs:70,92,104` | Changes to financial reference data cannot be reliably reconstructed. | Emit structured audit events (user, action, code, old/new description, traceId, outcome) to an append-only store or SIEM. |

### Low / Informational

| ID | Area | Finding | Evidence (file:line) | Risk / Impact | Recommended Fix |
|---|---|---|---|---|---|
| SEC-007 | 3 Transport · A05 | No production hosting config for the SPA (CSP, HSTS and frame-ancestors are set only on API responses). | `Frontend/index.html`; `vite.config.js` | The SPA's browser hardening depends on an undefined host. | Add an nginx or gateway config for static hosting with CSP, HSTS and `nosniff`. |
| SEC-009 | 14 Logging · A09 | 401/403 rejections are returned but not logged. | `Program.cs:130-143` | Brute-force or privilege probing goes unseen. | Log authentication and authorization failures (no token contents) at Warning. |
| SEC-010 | 15 Build posture · A05 | No Dockerfile or CI pipeline, so there is no non-root image or automated SCA/SAST gate. | `Target/MG-01/` tree | Supply-chain checks depend on someone remembering to run them. | Add a CI pipeline with `npm audit`, `dotnet list package --vulnerable`, SAST and a non-root container image. |
| SEC-011 | 1 AuthN | Informational: the dev bypass is well contained. It throws at startup if enabled outside Development, accepts only one exact token, and the SPA only sends it when `import.meta.env.DEV` and `VITE_AUTH_BYPASS=true`. | `Program.cs:18-22`; `transactionTypesApi.js:2` | — | Keep it; add a test that a non-Development host refuses to start with bypass enabled. |

## 5. Area Coverage Matrix

| # | Area | Status | Finding IDs |
|---|---|---|---|
| 1 | Authentication | Findings | SEC-003, SEC-011 |
| 2 | Authorization | Clean – all endpoints policy-guarded (role + scope) | — |
| 3 | Transport & network | Findings | SEC-004, SEC-007 |
| 4 | Input validation | Clean – 8/8 inbound fields validated, unknown members rejected, 4 KB body cap | — |
| 5 | Injection | Clean – EF LINQ + parameterised `FromSql`; LIKE escape | — |
| 6 | Output encoding / XSS | Clean in own code – React escaping, no `dangerouslySetInnerHTML` (library risk in SEC-001) | — |
| 7 | Session management | Clean – in-memory token, 15-min idle timeout with warning, sign-out clears | — |
| 8 | Sensitive data handling (PCI/GDPR) | Not applicable – no PAN/PII in MG-01 | — |
| 9 | Error handling | Clean – ProblemDetails with machine code and traceId; no SQL/stack text | — |
| 10 | Database & persistence | Findings | SEC-002 |
| 11 | Secrets management | Clean – no secrets committed; connection string from env | — |
| 12 | Dependencies & supply chain | Findings | SEC-001, SEC-006 |
| 13 | Rate limiting / DoS | Clean – 100 req/min per user/IP fixed window; body size limit | — |
| 14 | Logging, audit & monitoring | Findings | SEC-008, SEC-009 |
| 15 | Build & deployment posture | Findings | SEC-005, SEC-010 |

## 6. Compliance Posture

| Mandate | Status |
|---|---|
| OAuth 2.0 + JWT | **Partial.** The API validates JWTs with role and scope policies, but the SPA has no login flow (SEC-003) and the realm/role names are still assumptions (Gate 2 GH-5). |
| HTTPS / TLS 1.3 | **Partial.** HSTS and HTTPS redirection exist outside Development, but no TLS 1.3 listener is configured (SEC-004). |
| OWASP Top 10 | 5/10 categories clean. All breaches are configuration, supply-chain or logging, not logic flaws. |
| PCI-DSS | MG-01 stores no cardholder data. Req 7 least privilege (SEC-002) and Req 10 audit trail (SEC-008) are gaps. |
| GDPR | No personal data apart from the authenticated subject id in logs. Compliant. |

## 7. Remediation Plan

| Bucket | Order | Finding | Files touched | Effort |
|---|---|---|---|---|
| Fix before deployment | 1 | SEC-001 | `Frontend/package.json`, `package-lock.json` | XS |
| | 2 | SEC-002 | new `Artefacts/DDL` grant script (or Target/MG-01 db script), run guide, connection string | S |
| | 3 | SEC-004 | `Program.cs`, `appsettings.json` | S |
| | 4 | SEC-003 | `Frontend/src/api/*`, new `auth/` module, `App.jsx` | M |
| Fix in next iteration | 5 | SEC-005, SEC-006, SEC-009 | `Program.cs`, `appsettings.json`, `package.json` | S |
| | 6 | SEC-008 | `Services/TransactionTypeService.cs`, new audit sink | M |
| Optional hardening | 7 | SEC-007, SEC-010, SEC-011 | new hosting/CI files, tests | M |

## 8. Open Items & Assumptions

- The real OAuth realm, audience, role and scope names are not confirmed, so AUTHZ-001/002 are still blocked (see the Phase 8 results).
- TLS may be terminated at an API gateway that isn't specified yet (Gate 2 A-4). If it is, SEC-004 drops to Low.
- Production database credentials and role layout are supplied at deploy time. SEC-002 reflects the Phase 5 local provisioning.
- CVE status was taken from `npm audit` and NuGet advisory data on the review date. Re-run before release.
- The LOC count used in metric 9 is non-blank lines of `.cs/.js/.jsx` under `src` (tests excluded on the backend and included on the frontend).
