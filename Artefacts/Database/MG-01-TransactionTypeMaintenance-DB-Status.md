# MG-01 — Transaction Type Maintenance: DB Status (2026-11-18)

## a. Target Database Reachability
Dialect `postgresql` (`config/Config.md`). Host `localhost:5432`, database `carddemo`, reached via `tools/db-apply.mjs` — **Reachable**.

## b. DB2 Table Status
| Target Table | Source DB2 Table | Status | Detail |
|---|---|---|---|
| transaction_type | CARDDEMO.TRANSACTION_TYPE | Present | `tr_type character NOT NULL PK`, `tr_description character varying NOT NULL`; 7 seed rows from `Input/ctl/DB2LTTYP.ctl` |

## c. VSAM-derived Table Status
None — MG-01 accesses no VSAM files.

## d. Gaps by Criticality
### Critical
None
### High
None
### Medium
- `GRANT ... ON CARDDEMO.TRANSACTION_TYPE TO PUBLIC` (Input/ctl/DB2CREAT.ctl) not applied; DBA to define role-based grants.
### Low
- Seed value `'06','REVERAL'` carried verbatim from source (likely a typo of "REVERSAL"); confirm with business before changing.
- `CREATE DATABASE CARDDEMO` replaced by the target database `carddemo` (Postgres folds unquoted names to lower case).
