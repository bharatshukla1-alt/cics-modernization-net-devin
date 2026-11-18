# Gate 1 Review — 2026-11-18

## Summary
Reviewed `Artefacts/Discovery/TransVsPgm.md`, `CicsXref.md` and `Screen-metadata.md` against `Input/` (2 transactions, 3 COBOL programs, 2 BMS mapsets, 2 symbolic-map copybooks, 2 DB2 tables). Transaction/program and mapset mappings are consistent with the CSD and source. 6 findings, none blocking: one batch program outside CICS scope, external programs/copybooks referenced but not supplied, one unused DB2 include, one table with no CICS access, and one BMS field with no ATTRB. **Verdict: approved to proceed.**

## TransVsPgm.md
### a. Missing in documentation
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| COBTUPDT | Input/cbl/COBTUPDT.cbl | TransVsPgm.md | Batch DB2 program (no EXEC CICS, not defined in CRDDEMOD.csd). Correctly excluded from CICS mapping; noted for batch migration scope. |
### b. Missing in repo
No discrepancies found.
### c. Irrelevant/inconsistent mappings
No discrepancies found.
### d. Other discrepancies
No discrepancies found.

## CicsXref.md
### a. Missing in documentation
No discrepancies found.
### b. Missing in repo
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| COADM01C | COTRTLIC.cbl L47 / COTRTUPC.cbl L209-210 (LIT-ADMINPGM) | Input/cbl/ | XCTL target (admin menu) not supplied; treat as external boundary. |
| CVCRD01Y, COCOM01Y, COTTL01Y, CSDAT01Y, CSMSG01Y, CSMSG02Y, CSUSR01Y, CVACT02Y, CSSTRPFY, CSUTLDWY, CSSETATY | COPY statements in COTRTLIC.cbl / COTRTUPC.cbl | Input/cpy/ | Only CSDB2RPY and CSDB2RWY are supplied. COMMAREA layout (COCOM01Y) must be inferred from usage. |
### c. Irrelevant/inconsistent mappings
No discrepancies found.
### d. Other discrepancies
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| DCLTRCAT | COTRTUPC.cbl L288 (EXEC SQL INCLUDE) | CicsXref DB2 column | Included but no SQL against TRANSACTION_TYPE_CATEGORY; documented as include only. |
| TRANSACTION_TYPE_CATEGORY | Input/ddl/TRNTYCAT.ddl, Input/dcl/DCLTRCAT.dcl | CicsXref DB2 column | No CICS program accesses it; provision as dependent table only. |

## Screen-metadata.md
### a. Missing in documentation
No discrepancies found (COTRTLI 40/40 and COTRTUP 15/15 named fields match the BMS sources).
### b. Missing in repo
No discrepancies found.
### c. Irrelevant/inconsistent mappings
No discrepancies found.
### d. Other discrepancies
| Component | Found In | Expected In | Detail |
|---|---|---|---|
| PAGENO | Input/bms/COTRTLI.bms | Screen-metadata.md COTRTLI | No ATTRB coded; attribute recorded as Unknown and listed under Needs Review. |
