# MG-01 — Transaction Type Maintenance — DB Details

**Trans Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
**Members:** CTLI / COTRTLIC (CTRTLIA / COTRTLI), CTTU / COTRTUPC (CTRTUPA / COTRTUP)

## a. Tables & CRUD Footprint

| Program | Transaction | DB2 Tables Accessed (CRUD) | VSAM Files Accessed (CRUD) | CICS TS (TSQ/TDQ) Used (CRUD) |
|---|---|---|---|---|
| COTRTLIC | CTLI | CARDDEMO.TRANSACTION_TYPE (R, U, D) | None | None |
| COTRTUPC | CTTU | CARDDEMO.TRANSACTION_TYPE (C, R, U, D) | None | None |

Distinct DB2 tables: `CARDDEMO.TRANSACTION_TYPE`. No VSAM or TS footprint, so `setup-vsam` is not dispatched for this group.

## b. Target Schema Definition
Dialect: postgresql
Mapping rule: PostgreSQL qualifier mapping: DB2 `<database>.<table>` qualifier is dropped; the table is created unqualified in the target Postgres database's default `public` schema. The DB2 database `CARDDEMO` maps to the Postgres database `carddemo` (DB_NAME).

```sql
-- Source: Input/ctl/DB2CREAT.ctl, Input/ddl/TRNTYPE.ddl, Input/dcl/DCLTRTYP.dcl
CREATE TABLE TRANSACTION_TYPE
(
    TR_TYPE        CHAR(2)     NOT NULL,
    TR_DESCRIPTION VARCHAR(50) NOT NULL,
    PRIMARY KEY (TR_TYPE)
);

-- Source: Input/ddl/XTRNTYPE.ddl, Input/ctl/DB2CREAT.ctl
CREATE UNIQUE INDEX XTRAN_TYPE
    ON TRANSACTION_TYPE (TR_TYPE ASC);
```

No foreign keys originate from TRANSACTION_TYPE.

## c. Seed Data

```sql
-- Source: Input/ctl/DB2LTTYP.ctl
INSERT INTO TRANSACTION_TYPE (TR_TYPE, TR_DESCRIPTION) VALUES
('01', 'PURCHASE'),
('02', 'PAYMENT'),
('03', 'CREDIT'),
('04', 'AUTHORIZATION'),
('05', 'REFUND'),
('06', 'REVERAL'),
('07', 'ADJUSTMENT');
```

## Gaps
**(i) Tables with no source definition:** None.

**(ii) Source CREATE DATABASE:** `CREATE DATABASE CARDDEMO STOGROUP AWST1STG BUFFERPOOL BP0 CCSID EBCDIC` (`Input/ctl/DB2CREAT.ctl` lines 16-19). Target database `carddemo` created by `tools/db-apply.mjs --create-database`.

**(iii) Table-level privileges recorded, not provisioned:** `GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE CARDDEMO.TRANSACTION_TYPE TO PUBLIC` (`Input/ctl/DB2CREAT.ctl` lines 56-59). No GRANT executed in the target; a DBA should apply a least-privilege role for the MG-01 service account.

**(iv) Dialect precision/length loss:** None (postgresql).

**Other notes:**
- `CARDDEMO.TRANSACTION_TYPE_CATEGORY` has `FOREIGN KEY (TRC_TYPE_CODE) REFERENCES CARDDEMO.TRANSACTION_TYPE (TR_TYPE) ON DELETE RESTRICT` (`Input/ctl/DB2CREAT.ctl` lines 96-99). MG-01 programs don't access that table, so it is not provisioned here; until it exists, the legacy SQLCODE -532 (delete blocked by child rows) can't happen in the target.
- Seed values follow `DB2LTTYP.ctl` (upper case, including the source typo `'REVERAL'`). `Input/data/ASCII/trantype.txt` has mixed-case values ("Reversal"). Kept as in the load control file; confirm which is authoritative.
