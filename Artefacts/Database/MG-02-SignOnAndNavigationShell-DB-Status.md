# MG-02 – Sign-on and Navigation Shell – DB Status Report

**Target:** PostgreSQL 15, `carddemo` @ 127.0.0.1:5433 · **Agents run:** setup-vsam (`create-then-provision`). setup-db was not run because MG-02 has no Db2 access.
**Config check:** Config.md declares DB2 and VSAM → Postgres. This group's source uses VSAM only. Both Database rows target Postgres, so the dialect guard passed.

## Live verification (independent)
| Table | Expected (VSAM-Details §c) | Live (`--describe-table`) | Rows | Status |
|---|---|---|---|---|
| usrsec | 5 columns, PK sec_usr_id | sec_usr_id CHAR(8) PK; fname CHAR(20); lname CHAR(20); pwd CHAR(8); type CHAR(1); all NOT NULL | 10 (5 'A', 5 'U') | ✅ Match |
| transaction_type (MG-01) | unchanged | present | 7 | ✅ Not touched |

## Open gaps by criticality
| # | Severity | Gap | Owner |
|---|---|---|---|
| 1 | High | Plain-text 8-character passwords stored in `sec_usr_pwd` (source parity). They must not reach production: hash them or delegate to OAuth. | Phase 6 / Gate 2 |
| 2 | Medium | No CHECK on `sec_usr_type IN ('A','U')`; the values are known only from COCOM01Y. | Gate 2 |
| 3 | Medium | The demo database runs as the owner role. A least-privilege runtime role for `usrsec` (as MG-01 has `carddemo_app`) is still needed. | Phase 6 |
| 4 | Low | `READINTEG(UNCOMMITTED)` / `UPDATEMODEL(LOCKING)` / `RECORDFORMAT(V)` have no direct equivalent (see VSAM-Details Gaps). | Info |
| 5 | Low | Versioned migrations (MG-01 CQ-004) are not yet in place; the DDL is applied with `db-apply.mjs`. | Backlog |

No Critical gaps.
