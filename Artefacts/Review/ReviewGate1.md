# Gate 1 Review — 2026-11-20

## Summary
Reviewed the three Discovery artefacts against `Input/` as ground truth: 25 TRANSACTION, 26 PROGRAM, 21 MAPSET, 8 FILE, 1 TDQUEUE, 2 DB2ENTRY and 3 DB2TRAN definitions in 4 CSD files; 26 COBOL sources in `Input/cbl/`; 21 BMS files (1,166 DFHMDF macros, 585 named fields) in `Input/bms/` and 21 symbolic maps in `Input/cpy-bms/`. All 25 transaction→program pairs match the CSD. All 21 mapsets and all 585 named fields match the symbolic-map copybooks exactly (field-by-field, per mapset). **8 findings, none blocking**: 1 program defined in the CSD with no source (COCRDSEC/CDV1, reported under both docs); 2 programs outside the transaction table (COPAUS2C LINK target, CSUTLDTC static CALL target, both informational); 3 groups of vendor-supplied copybooks absent from `Input/` (CICS, Db2, MQ); 1 read-for-update with no rewrite (COTRN01C); 30 BMS fields with no `ATTRB` coded. Files affected: TransVsPgm.md (informational), CicsXref.md, Screen-metadata.md.

## TransvsPgm.md

### a. Missing in documentation
No discrepancies found. All 25 `DEFINE TRANSACTION` entries (`Input/csd/CARDDEMO.CSD` 18, `CRDDEMOD.csd` 2, `CRDDEMO2.csd` 3, `CRDDEMOM.csd` 2) appear in TransVsPgm.md rows 8–32.

### b. Missing in repo
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| COCRDSEC (CDV1) | `Input/csd/CARDDEMO.CSD` line 211 (`DEFINE PROGRAM(COCRDSEC)`), line 388 (`DEFINE TRANSACTION(CDV1)`); TransVsPgm.md line 16 | `Input/cbl/COCRDSEC.cbl` | Mapping is correct per CSD, but no COBOL source exists. No mapset for it in `Input/bms/`. Already flagged in TransVsPgm.md Notes. |

### c. Irrelevant/inconsistent mappings
No discrepancies found. Each TransVsPgm.md row matches the `PROGRAM(...)` attribute of its `DEFINE TRANSACTION`.

### d. Other discrepancies
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| COPAUS2C | `Input/csd/CRDDEMO2.csd` line 32 (`DEFINE PROGRAM(COPAUS2C)`, line 36 `TRANSID(CPVD)`); `Input/cbl/COPAUS2C.cbl` | TransVsPgm.md table | Not an error: no `DEFINE TRANSACTION` names it; it is LINKed from COPAUS1C (`Input/cbl/COPAUS1C.cbl` line 248). Recorded in TransVsPgm.md Notes and CicsXref.md secondary table. |
| CSUTLDTC | `Input/cbl/CSUTLDTC.cbl` | `Input/csd/*` `DEFINE PROGRAM` | Source exists but has no CSD definition; it is a static COBOL CALL target (`Input/cbl/CORPT00C.cbl` line 392, `COTRN02C.cbl` line 393), so no CICS definition is needed. Documented in CicsXref.md secondary table. |

## CicsXref.md

### a. Missing in documentation
No discrepancies found. All 26 `Input/cbl/` programs appear: 25 in the main table (CicsXref.md lines 8–32), COPAUS2C and CSUTLDTC in the secondary table.

### b. Missing in repo
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| DFHAID, DFHBMSCA | CicsXref.md "Copybooks Used" (20 online rows); e.g. `Input/cbl/COSGN00C.cbl` lines 57–58 | `Input/cpy/` | CICS-supplied copybooks, not in `Input/`. Expected — resolved from the CICS SDFHCOB library. Target needs equivalent AID/attribute constants. |
| SQLCA | CicsXref.md rows CTLI, CTTU and COPAUS2C; `Input/cbl/COTRTLIC.cbl` line 331, `COTRTUPC.cbl` line 282, `COPAUS2C.cbl` line 66 | `Input/cpy/` | Db2-supplied. Expected. |
| CMQV, CMQODV, CMQMDV, CMQTML, CMQPMOV, CMQGMOV | CicsXref.md rows CP00, CDRA, CDRD; `Input/cbl/COPAUA0C.cbl` lines 149–170, `COACCT01.cbl` lines 71–90 | `Input/cpy/` | IBM MQ-supplied. Expected. |
| COCRDSEC | CicsXref.md line 16 | `Input/cbl/COCRDSEC.cbl` | Same gap as TransVsPgm.md (b); row is explicitly marked "Source not found". |

### c. Irrelevant/inconsistent mappings
No discrepancies found. Spot-verified against source: COSGN00C XCTL COADM01C/COMEN01C (`COSGN00C.cbl` lines 231, 236); COCRDLIC XCTL via `CCARD-NEXT-PROG` = LIT-CARDDTLPGM/LIT-CARDUPDPGM (`COCRDLIC.cbl` lines 526, 554, 538, 566); COTRTUPC Db2 C,R,U,D (`COTRTUPC.cbl` lines 1475, 1544, 1597, 1627); COBIL00C ACCTDAT READ UPDATE/REWRITE (`COBIL00C.cbl` lines 345, 379); COPAUS1C LINK COPAUS2C (`COPAUS1C.cbl` line 248); CORPT00C WRITEQ TD JOBS (`CORPT00C.cbl` line 517).

### d. Other discrepancies
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| COTRN01C TRANSACT | `Input/cbl/COTRN01C.cbl` line 269 (`READ … UPDATE`) | A matching REWRITE/UNLOCK in the same program | Read-for-update with no rewrite; recorded as R. Already in CicsXref.md Needs Review. Target should treat as a plain read (no lock). |

## Screen-metadata.md

### a. Missing in documentation
No discrepancies found. 21/21 BMS files have a section; for every mapset the named fields equal the `…L COMP` fields in the matching `Input/cpy-bms/` copybook (e.g. COACTUP 54/54, COPAU00 62/62, COTRTLI 40/40, COTRTUP 15/15).

### b. Missing in repo
No discrepancies found. Every documented field exists in its `Input/bms/` file and symbolic map.

### c. Irrelevant/inconsistent mappings
No discrepancies found. The 21 mapsets equal the 21 `DEFINE MAPSET` entries in `Input/csd/*`, and each is attributed to the program that SENDs/RECEIVEs it per CicsXref.md.

### d. Other discrepancies
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| 30 fields with no `ATTRB` (27 in COACTVW, e.g. ADTOPEN `Input/bms/COACTVW.bms` line 107; plus COCRDLI/PAGENO, COCRDSL/CRDNAME, COTRTLI/PAGENO) | Screen-metadata.md "Needs Review" | An `ATTRB=` operand | Attribute recorded as `Unknown`. BMS defaults an omitted ATTRB to (ASKIP,NORM), so these are most likely read-only display fields; confirm before UI spec (Phase 6). |

## Gate 1 decision needed
- Confirm CDV1/COCRDSEC is out of scope (or supply `COCRDSEC.cbl`).
- Accept vendor copybooks (CICS/Db2/MQ) as external.
- Accept treating the 30 no-ATTRB fields as Read-only.
