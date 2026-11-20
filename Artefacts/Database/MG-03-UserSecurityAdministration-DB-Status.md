# MG-03 – User Security Administration – DB Status Report

Database: PostgreSQL 16 `carddemo` (demo container `carddemo-mg02-db-1`, port 5433). Table `usrsec` is shared with MG-02.

## Live verification (independent)
| Check | Result |
|---|---|
| `usrsec` columns vs CSUSR01Y | sec_usr_id char(8), sec_usr_fname char(20), sec_usr_lname char(20), sec_usr_pwd char(8), sec_usr_type char(1). Match. |
| Primary key | `usrsec_pk (sec_usr_id)`. Match with KSDS KEYS(8,0). |
| Rows | 10 (5 type A, 5 type U), unchanged from MG-02 |
| `carddemo_useradmin` created | LOGIN, not superuser, cannot create databases or roles |
| `carddemo_useradmin` grants | SELECT, INSERT, UPDATE, DELETE on `usrsec` only |
| INSERT / UPDATE / DELETE as `carddemo_useradmin` | Succeeded inside a rolled-back transaction |
| `carddemo_useradmin` on `transaction_type` | permission denied (expected) |
| `carddemo_useradmin` CREATE TABLE | permission denied (expected) |
| `carddemo_signon` DELETE on `usrsec` | permission denied (MG-02 stays read-only) |

| `tools/db-apply.mjs --dialect postgresql --list-tables` | Connected; lists `transaction_type` and `usrsec` |
| `tools/db-apply.mjs --describe-table usrsec` | 5 columns, `character` types, as above |

DB2 tables: not applicable, because MG-03 has no Db2 footprint, so no `-DB-Details.md` is produced. `run_pipeline.py --phase 5` expects `{group}-*-DB-Details.md` for every group. For VSAM-only groups (MG-02, MG-03) that check is a known limitation of the runner, so this report and `-VSAM-Details.md` are the Phase 5 outputs.

## Open gaps by criticality
- **Critical:** none.
- **High:** passwords are plain text in `sec_usr_pwd CHAR(8)`; hashing needs a wider column and a sign-on change. Decide in Phase 6.
- **Medium:** no CHECK constraint on `sec_usr_type`; no concurrency column (value comparison or `xmin` will be used).
- **Low:** no audit table, so structured audit log events are used instead.
