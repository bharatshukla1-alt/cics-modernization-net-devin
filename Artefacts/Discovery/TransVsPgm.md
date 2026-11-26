# Transaction vs Program — CardDemo CICS Estate

Source CSD files (DFHCSDUP text unloads, all parsed successfully):
`Input/csd/CARDDEMO.CSD`, `Input/csd/CRDDEMOD.csd`, `Input/csd/CRDDEMO2.csd`, `Input/csd/CRDDEMOM.csd`.

| Transaction ID | Program Name | CICS Group |
|---|---|---|
| CAUP | COACTUPC | CARDDEMO |
| CAVW | COACTVWC | CARDDEMO |
| CA00 | COADM01C | CARDDEMO |
| CB00 | COBIL00C | CARDDEMO |
| CCDL | COCRDSLC | CARDDEMO |
| CCLI | COCRDLIC | CARDDEMO |
| CCUP | COCRDUPC | CARDDEMO |
| CC00 | COSGN00C | CARDDEMO |
| CDV1 | COCRDSEC | CARDDEMO |
| CM00 | COMEN01C | CARDDEMO |
| CR00 | CORPT00C | CARDDEMO |
| CT00 | COTRN00C | CARDDEMO |
| CT01 | COTRN01C | CARDDEMO |
| CT02 | COTRN02C | CARDDEMO |
| CU00 | COUSR00C | CARDDEMO |
| CU01 | COUSR01C | CARDDEMO |
| CU02 | COUSR02C | CARDDEMO |
| CU03 | COUSR03C | CARDDEMO |
| CTLI | COTRTLIC | CARDDEMO |
| CTTU | COTRTUPC | CARDDEMO |
| CPVD | COPAUS1C | CARDDEMO |
| CPVS | COPAUS0C | CARDDEMO |
| CP00 | COPAUA0C | CARDDEMO |
| CDRA | COACCT01 | CARDDEMO |
| CDRD | CODATE01 | CARDDEMO |

## Summary
- Transactions found: **25** (CARDDEMO.CSD 18, CRDDEMOD.csd 2, CRDDEMO2.csd 3, CRDDEMOM.csd 2).
- `UNRESOLVED` entries: **0** — every TRANSACTION names a PROGRAM that is also DEFINEd in the CSD.

## Notes (verifiable from CSD content)
- `COCRDSEC` (CDV1) is defined in CARDDEMO.CSD but no source `COCRDSEC.cbl` exists in `Input/cbl/`.
- `COPAUS2C` is defined as a PROGRAM (CRDDEMO2.csd, `TRANSID(CPVD)` attribute) but no TRANSACTION names it; it is LINKed from COPAUS1C (see CicsXref.md).
- DB2 attachments: DB2ENTRY `CARDDEMO` (plan CARDDEMO) serves CTLI and CTTU via DB2TRAN `CTLITRAN`/`CTTUTRAN`; DB2ENTRY `AWS01PLN` serves CPVD via DB2TRAN `CPVDTRAN`.
- Other resources: 8 FILE definitions (ACCTDAT, CARDAIX, CARDDAT, CCXREF, CUSTDAT, CXACAIX, TRANSACT, USRSEC), TDQUEUE `JOBS`, 21 MAPSETs across the four CSDs.
- The same group name `CARDDEMO` is used in all four CSD unloads.
