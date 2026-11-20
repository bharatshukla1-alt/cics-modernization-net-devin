# MG-02 – Sign-on and Navigation Shell – VSAM Details

**Group:** MG-02 — CC00 (COSGN00C, COSGN0A/COSGN00), CM00 (COMEN01C, COMEN1A/COMEN01), CA00 (COADM01C, COADM1A/COADM01)
**Run mode:** `create-then-provision` (the group uses VSAM only; no Db2) · **Dialect:** postgresql (config/Config.md, Database: VSAM → Postgres)

## a. VSAM Files & CRUD Footprint

| File | Program | C | R | U | D | Browse | Key used |
|---|---|---|---|---|---|---|---|
| USRSEC | COSGN00C | – | ✔ (`EXEC CICS READ DATASET(WS-USRSEC-FILE)` in READ-USER-SEC-FILE) | – | – | – | `WS-USER-ID` (upper-cased user id), KEYLENGTH 8 |
| USRSEC | COMEN01C | – | – | – | – | – | Declared (`WS-USRSEC-FILE`), never used |
| USRSEC | COADM01C | – | – | – | – | – | Declared (`WS-USRSEC-FILE`), never used |

## b. Record Layout Analysis

**Definition sources**
1. CSD: `DEFINE FILE(USRSEC) GROUP(CARDDEMO) DSNAME(AWS.M2.CARDDEMO.USRSEC.VSAM.KSDS) RECORDFORMAT(V) ADD(YES) BROWSE(YES) DELETE(YES) READ(YES) UPDATE(YES) READINTEG(UNCOMMITTED) JOURNAL(NO)` (Input/csd/CARDDEMO.CSD, line 88).
2. IDCAMS: `DEFINE CLUSTER (NAME(AWS.M2.CARDDEMO.USRSEC.VSAM.KSDS) KEYS(8,0) RECORDSIZE(80,80) INDEXED …)` (Input/jcl/DUSRSECJ.jcl, STEP02).
3. Record copybook: `CSUSR01Y` `01 SEC-USER-DATA` (Input/cpy/CSUSR01Y.cpy).

**Organisation:** KSDS. The primary key is bytes 0–7, which equals `SEC-USR-ID`. There is no alternate index.

| Field | PIC | Offset | Length | Column |
|---|---|---|---|---|
| SEC-USR-ID | X(08) | 0 | 8 | `sec_usr_id CHAR(8)` PK |
| SEC-USR-FNAME | X(20) | 8 | 20 | `sec_usr_fname CHAR(20)` |
| SEC-USR-LNAME | X(20) | 28 | 20 | `sec_usr_lname CHAR(20)` |
| SEC-USR-PWD | X(08) | 48 | 8 | `sec_usr_pwd CHAR(8)` |
| SEC-USR-TYPE | X(01) | 56 | 1 | `sec_usr_type CHAR(1)` |
| SEC-USR-FILLER | X(23) | 57 | 23 | no column |

The fields total 80 bytes, which matches `RECORDSIZE(80,80)`. There are no REDEFINES or OCCURS clauses, and no 88-level conditions in the record.

## c. Target Schema Definition

Dialect: postgresql. Qualifier rule: unqualified, in the connected database (`DB_NAME=carddemo`).

```sql
CREATE TABLE usrsec (
    sec_usr_id    CHAR(8)  NOT NULL,
    sec_usr_fname CHAR(20) NOT NULL,
    sec_usr_lname CHAR(20) NOT NULL,
    sec_usr_pwd   CHAR(8)  NOT NULL,
    sec_usr_type  CHAR(1)  NOT NULL,
    CONSTRAINT usrsec_pk PRIMARY KEY (sec_usr_id)
);
```
The table is named after the CICS FILE name `USRSEC`. Full DDL: `Artefacts/DDL/MG-02-SignOnAndNavigationShell-vsam-schema.sql`.

## d. Seed Data

There are 10 synthetic users from DUSRSECJ.jcl STEP01 SYSUT1: ADMIN001–ADMIN005 (type 'A') and USER0001–USER0005 (type 'U'). All have the password `PASSWORD`. The INSERT is in the DDL file.

## Gaps
- (i) Missing definition source: none.
- (ii) Unconfirmed bindings: none; `USRSEC` resolves to a single cluster.
- (iii) Unmapped constructs: `SEC-USR-FILLER` X(23) has no column. Its byte span is recorded above.
- (iv) REDEFINES/OCCURS decisions: none.
- (v) Synthesised keys / PK-less tables: none.
- (vi) Record-length cross-check: passes (80 = 80).
- (vii) Variable-length NULLability: the CSD says `RECORDFORMAT(V)`, but the cluster is fixed 80/80, so every column is NOT NULL.
- (viii) CSD behaviour with no target equivalent:
  - `READINTEG(UNCOMMITTED)` maps to PostgreSQL READ COMMITTED (stricter).
  - `UPDATEMODEL(LOCKING)` maps to row locks.
  - `JOURNAL(NO)`.
- (ix) Dialect precision loss: none.
- Additional notes:
  - `SEC-USR-TYPE` has no 88-level in CSUSR01Y. The values 'A'/'U' come from COCOM01Y `CDEMO-USER-TYPE`, so no CHECK constraint was emitted. Adding one is a Gate 2 decision.
  - `sec_usr_pwd` holds plain-text 8-character passwords, as in the source. Moving to hashed credentials or OAuth is a target-design decision for Phase 6 and Gate 2.
