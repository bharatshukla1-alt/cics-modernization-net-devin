# MG-01 — Transaction Type Maintenance — DB Status

**Trans Group:** MG-01 — CTLI / COTRTLIC, CTTU / COTRTUPC
**Agents run:** Setup-DB only (no VSAM footprint, so Setup-VSAM was not dispatched)

## a. Target Database
| Dialect | DB_NAME | Reachable |
|---|---|---|
| postgresql | carddemo | Yes (`--list-tables` succeeded) |

## b. DB2 Tables
| Table | Status | Live check |
|---|---|---|
| TRANSACTION_TYPE | Present | `tr_type character(2) NOT NULL` (PK `transaction_type_pkey`), `tr_description character varying(50) NOT NULL`, unique index `xtran_type (tr_type)`. 7 seed rows. Matches `-DB-Details.md` section b. |

## c. VSAM-Derived Tables
Not applicable — this group's source has no VSAM footprint.

## d. Gaps by Criticality

### Critical
None

### High
None

### Medium
- Table-level grant `DELETE, INSERT, SELECT, UPDATE ON CARDDEMO.TRANSACTION_TYPE TO PUBLIC` recorded but not provisioned. Source: `-DB-Details.md` Gaps (iii).

### Low
- Source `CREATE DATABASE CARDDEMO` recorded for audit; target `carddemo` exists. Source: `-DB-Details.md` Gaps (ii).
