# MG-03 – User Security Administration – VSAM Details

Sources: `COUSR00C.cbl`, `COUSR01C.cbl`, `COUSR02C.cbl` and `COUSR03C.cbl`; copybook `CSUSR01Y`; `DUSRSECJ.jcl`; the CSD `DEFINE FILE(USRSEC)`.
MG-03 uses the same VSAM file as MG-02, so the target table is the one MG-02 provisioned. This phase adds the write access MG-03 needs.

## a. VSAM Files & CRUD Footprint
| File (DD) | Type | Key | Record | Program | Operations | CRUD |
|---|---|---|---|---|---|---|
| USRSEC | KSDS | SEC-USR-ID X(8), offset 0 | 80 fixed | COUSR00C | STARTBR, READNEXT, READPREV, ENDBR | R (browse) |
| USRSEC | KSDS | " | " | COUSR01C | WRITE | C |
| USRSEC | KSDS | " | " | COUSR02C | READ UPDATE, REWRITE | R, U |
| USRSEC | KSDS | " | " | COUSR03C | READ UPDATE, DELETE | R, D |
| USRSEC | KSDS | " | " | COSGN00C (MG-02) | READ | R |

## b. Record Layout Analysis (CSUSR01Y)
| Field | PIC | Offset | Length | Column |
|---|---|---|---|---|
| SEC-USR-ID | X(08) | 0 | 8 | sec_usr_id CHAR(8) PK |
| SEC-USR-FNAME | X(20) | 8 | 20 | sec_usr_fname CHAR(20) |
| SEC-USR-LNAME | X(20) | 28 | 20 | sec_usr_lname CHAR(20) |
| SEC-USR-PWD | X(08) | 48 | 8 | sec_usr_pwd CHAR(8) |
| SEC-USR-TYPE | X(01) | 56 | 1 | sec_usr_type CHAR(1) |
| SEC-USR-FILLER | X(23) | 57 | 23 | no column |

## c. Target Schema Definition
- **Table:** `usrsec`, defined in `Artefacts/DDL/MG-03-UserSecurityAdministration-vsam-schema.sql`. The script is idempotent and identical to MG-02's table definition.
- **Browse order:** the KSDS key order behind PF7/PF8 maps to `ORDER BY sec_usr_id` on the primary-key index. Keyset paging uses `sec_usr_id > :last` and `sec_usr_id < :first`.
- **Access verbs:**
  - `WRITE` maps to `INSERT`; DUPREC/DUPKEY map to PostgreSQL unique violation 23505.
  - `READ UPDATE`/`REWRITE` maps to `UPDATE … WHERE sec_usr_id = :id`; NOTFND maps to 0 rows.
  - `DELETE` maps to `DELETE … WHERE sec_usr_id = :id`.
- **Runtime login:** `carddemo_useradmin`, with SELECT, INSERT, UPDATE and DELETE on `usrsec` only (`Target/MG-03/Backend/db/least-privilege-useradmin-role.sql`). `carddemo_signon` stays SELECT-only.

## d. Seed Data
No new data. The 10 users from `DUSRSECJ.jcl` loaded in MG-02 are used: ADMIN001–005 (type A) and USER0001–005 (type U).

## Gaps
1. **High: plain-text passwords.** `sec_usr_pwd CHAR(8)` cannot hold a hash. Moving to hashed passwords needs a wider column, for example `TEXT`, plus a change to MG-02 sign-on. This is a decision for Phase 6 / Gate 2.
2. **Medium: User Type is unconstrained,** as in the legacy file. A `CHECK (sec_usr_type IN ('A','U'))` constraint is proposed for Phase 6 if you want the screen hint enforced.
3. **Medium: no optimistic concurrency column.** Concurrency checking can compare the row's values or use PostgreSQL `xmin` without a schema change.
4. **Low: no audit table.** User changes are logged as structured audit events, the same as MG-01 and MG-02.
