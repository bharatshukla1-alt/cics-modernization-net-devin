# Screen Metadata — CardDemo CICS Estate

Sources: every BMS file in `Input/bms/` (cross-checked with `Input/cpy-bms/` symbolic maps); Program/Transaction attribution from `Artefacts/Discovery/CicsXref.md`.

**Rules applied:** Field Initial = the field's own `INITIAL=` when coded (F-keys, date/time placeholders), otherwise the `INITIAL=` of the immediately preceding unnamed DFHMDF (its caption); `None` when neither exists. Field Type = `PICIN/PICOUT` when coded, `Numeric` for `ATTRB=NUM`, `Text` for titles/messages/F-key labels, `Character` for date/time displays, otherwise `Alphanumeric` (BMS default character data). Field Attribute: `UNPROT`→Editable, `ASKIP`/`PROT`→Read-only, `DRK`+`UNPROT`→Masked, `DRK`+`ASKIP/PROT`→Hidden; no `ATTRB` → `Unknown`.

## Mapset: COACTUP (Map CACTUPA)

Source: `Input/bms/COACTUP.bms`  
Program: COACTUPC | Transaction: CAUP (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACCTSID | Account Number : | 11 | Alphanumeric | Editable |
| ACSTTUS | Active Y/N: | 1 | Alphanumeric | Editable |
| OPNYEAR | Opened : | 4 | Alphanumeric | Editable |
| OPNMON | - | 2 | Alphanumeric | Editable |
| OPNDAY | - | 2 | Alphanumeric | Editable |
| ACRDLIM | Credit Limit        : | 15 | Alphanumeric | Editable |
| EXPYEAR | Expiry : | 4 | Alphanumeric | Editable |
| EXPMON | - | 2 | Alphanumeric | Editable |
| EXPDAY | - | 2 | Alphanumeric | Editable |
| ACSHLIM | Cash credit Limit   : | 15 | Alphanumeric | Editable |
| RISYEAR | Reissue: | 4 | Alphanumeric | Editable |
| RISMON | - | 2 | Alphanumeric | Editable |
| RISDAY | - | 2 | Alphanumeric | Editable |
| ACURBAL | Current Balance     : | 15 | Alphanumeric | Editable |
| ACRCYCR | Current Cycle Credit: | 15 | Alphanumeric | Editable |
| AADDGRP | Account Group: | 10 | Alphanumeric | Editable |
| ACRCYDB | Current Cycle Debit : | 15 | Alphanumeric | Editable |
| ACSTNUM | Customer id  : | 9 | Alphanumeric | Editable |
| ACTSSN1 | 999 | 3 | Alphanumeric | Editable |
| ACTSSN2 | 99 | 2 | Alphanumeric | Editable |
| ACTSSN3 | 9999 | 4 | Alphanumeric | Editable |
| DOBYEAR | Date of birth: | 4 | Alphanumeric | Editable |
| DOBMON | - | 2 | Alphanumeric | Editable |
| DOBDAY | - | 2 | Alphanumeric | Editable |
| ACSTFCO | FICO Score: | 3 | Alphanumeric | Editable |
| ACSFNAM | Last Name : | 25 | Alphanumeric | Editable |
| ACSMNAM | None | 25 | Alphanumeric | Editable |
| ACSLNAM | None | 25 | Alphanumeric | Editable |
| ACSADL1 | Address: | 50 | Alphanumeric | Editable |
| ACSSTTE | State | 2 | Alphanumeric | Editable |
| ACSADL2 | None | 50 | Alphanumeric | Editable |
| ACSZIPC | Zip | 5 | Alphanumeric | Editable |
| ACSCITY | City | 50 | Alphanumeric | Editable |
| ACSCTRY | Country | 3 | Alphanumeric | Editable |
| ACSPH1A | Phone 1: | 3 | Alphanumeric | Editable |
| ACSPH1B | None | 3 | Alphanumeric | Editable |
| ACSPH1C | None | 4 | Alphanumeric | Editable |
| ACSGOVT | Government Issued Id Ref    : | 20 | Alphanumeric | Editable |
| ACSPH2A | Phone 2: | 3 | Alphanumeric | Editable |
| ACSPH2B | None | 3 | Alphanumeric | Editable |
| ACSPH2C | None | 4 | Alphanumeric | Editable |
| ACSEFTC | EFT Account Id: | 10 | Alphanumeric | Editable |
| ACSPFLG | Primary Card Holder Y/N: | 1 | Alphanumeric | Editable |
| INFOMSG | None | 45 | Text | Read-only |
| ERRMSG | None | 78 | Text | Read-only |
| FKEYS | ENTER=Process F3=Exit | 21 | Text | Read-only |
| FKEY05 | F5=Save | 7 | Text | Hidden |
| FKEY12 | F12=Cancel | 10 | Text | Hidden |

## Mapset: COACTVW (Map CACTVWA)

Source: `Input/bms/COACTVW.bms`  
Program: COACTVWC | Transaction: CAVW (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACCTSID | Account Number : | 11 | Numeric | Editable |
| ACSTTUS | Active Y/N: | 1 | Alphanumeric | Read-only |
| ADTOPEN | Opened: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACRDLIM | Credit Limit        : | 15 | Numeric | Unknown (no ATTRB coded) |
| AEXPDT | Expiry: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSHLIM | Cash credit Limit   : | 15 | Numeric | Unknown (no ATTRB coded) |
| AREISDT | Reissue: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACURBAL | Current Balance     : | 15 | Numeric | Unknown (no ATTRB coded) |
| ACRCYCR | Current Cycle Credit: | 15 | Numeric | Unknown (no ATTRB coded) |
| AADDGRP | Account Group: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACRCYDB | Current Cycle Debit : | 15 | Numeric | Unknown (no ATTRB coded) |
| ACSTNUM | Customer id  : | 9 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSTSSN | SSN: | 12 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSTDOB | Date of birth: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSTFCO | FICO Score: | 3 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSFNAM | Last Name : | 25 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSMNAM | None | 25 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSLNAM | None | 25 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSADL1 | Address: | 50 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSSTTE | State | 2 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSADL2 | None | 50 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSZIPC | Zip | 5 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSCITY | City | 50 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSCTRY | Country | 3 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSPHN1 | Phone 1: | 13 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSGOVT | Government Issued Id Ref    : | 20 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSPHN2 | Phone 2: | 13 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSEFTC | EFT Account Id: | 10 | Alphanumeric | Unknown (no ATTRB coded) |
| ACSPFLG | Primary Card Holder Y/N: | 1 | Alphanumeric | Unknown (no ATTRB coded) |
| INFOMSG | None | 45 | Text | Read-only |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COADM01 (Map COADM1A)

Source: `Input/bms/COADM01.bms`  
Program: COADM01C | Transaction: CA00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| OPTN001 | Admin Menu | 40 | Alphanumeric | Read-only |
| OPTN002 | None | 40 | Alphanumeric | Read-only |
| OPTN003 | None | 40 | Alphanumeric | Read-only |
| OPTN004 | None | 40 | Alphanumeric | Read-only |
| OPTN005 | None | 40 | Alphanumeric | Read-only |
| OPTN006 | None | 40 | Alphanumeric | Read-only |
| OPTN007 | None | 40 | Alphanumeric | Read-only |
| OPTN008 | None | 40 | Alphanumeric | Read-only |
| OPTN009 | None | 40 | Alphanumeric | Read-only |
| OPTN010 | None | 40 | Alphanumeric | Read-only |
| OPTN011 | None | 40 | Alphanumeric | Read-only |
| OPTN012 | None | 40 | Alphanumeric | Read-only |
| OPTION | Please select an option : | 2 | Numeric | Editable |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COBIL00 (Map COBIL0A)

Source: `Input/bms/COBIL00.bms`  
Program: COBIL00C | Transaction: CB00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACTIDIN | Enter Acct ID: | 11 | Alphanumeric | Editable |
| CURBAL | Your current balance is: | 14 | Alphanumeric | Read-only |
| CONFIRM | Do you want to pay your balance now. Please confirm: | 1 | Alphanumeric | Editable |
| ERRMSG | (Y/N) | 78 | Text | Read-only |

## Mapset: COCRDLI (Map CCRDLIA)

Source: `Input/bms/COCRDLI.bms`  
Program: COCRDLIC | Transaction: CCLI (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| PAGENO | Page | 3 | Alphanumeric | Unknown (no ATTRB coded) |
| ACCTSID | Account Number    : | 11 | Alphanumeric | Editable |
| CARDSID | Credit Card Number: | 16 | Alphanumeric | Editable |
| CRDSEL1 | -------- | 1 | Alphanumeric | Read-only |
| ACCTNO1 | None | 11 | Alphanumeric | Read-only |
| CRDNUM1 | None | 16 | Alphanumeric | Read-only |
| CRDSTS1 | None | 1 | Alphanumeric | Read-only |
| CRDSEL2 | None | 1 | Alphanumeric | Read-only |
| CRDSTP2 | None | 1 | Alphanumeric | Hidden |
| ACCTNO2 | None | 11 | Alphanumeric | Read-only |
| CRDNUM2 | None | 16 | Alphanumeric | Read-only |
| CRDSTS2 | None | 1 | Alphanumeric | Read-only |
| CRDSEL3 | None | 1 | Alphanumeric | Read-only |
| CRDSTP3 | None | 1 | Alphanumeric | Hidden |
| ACCTNO3 | None | 11 | Alphanumeric | Read-only |
| CRDNUM3 | None | 16 | Alphanumeric | Read-only |
| CRDSTS3 | None | 1 | Alphanumeric | Read-only |
| CRDSEL4 | None | 1 | Alphanumeric | Read-only |
| CRDSTP4 | None | 1 | Alphanumeric | Hidden |
| ACCTNO4 | None | 11 | Alphanumeric | Read-only |
| CRDNUM4 | None | 16 | Alphanumeric | Read-only |
| CRDSTS4 | None | 1 | Alphanumeric | Read-only |
| CRDSEL5 | None | 1 | Alphanumeric | Read-only |
| CRDSTP5 | None | 1 | Alphanumeric | Hidden |
| ACCTNO5 | None | 11 | Alphanumeric | Read-only |
| CRDNUM5 | None | 16 | Alphanumeric | Read-only |
| CRDSTS5 | None | 1 | Alphanumeric | Read-only |
| CRDSEL6 | None | 1 | Alphanumeric | Read-only |
| CRDSTP6 | None | 1 | Alphanumeric | Hidden |
| ACCTNO6 | None | 11 | Alphanumeric | Read-only |
| CRDNUM6 | None | 16 | Alphanumeric | Read-only |
| CRDSTS6 | None | 1 | Alphanumeric | Read-only |
| CRDSEL7 | None | 1 | Alphanumeric | Read-only |
| CRDSTP7 | None | 1 | Alphanumeric | Hidden |
| ACCTNO7 | None | 11 | Alphanumeric | Read-only |
| CRDNUM7 | None | 16 | Alphanumeric | Read-only |
| CRDSTS7 | None | 1 | Alphanumeric | Read-only |
| INFOMSG | None | 45 | Text | Read-only |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COCRDSL (Map CCRDSLA)

Source: `Input/bms/COCRDSL.bms`  
Program: COCRDSLC | Transaction: CCDL (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACCTSID | Account Number    : | 11 | Alphanumeric | Editable |
| CARDSID | Card Number       : | 16 | Alphanumeric | Editable |
| CRDNAME | Name on card      : | 50 | Alphanumeric | Unknown (no ATTRB coded) |
| CRDSTCD | Card Active Y/N   : | 1 | Alphanumeric | Read-only |
| EXPMON | Expiry Date       : | 2 | Alphanumeric | Read-only |
| EXPYEAR | / | 4 | Alphanumeric | Read-only |
| INFOMSG | None | 40 | Text | Read-only |
| ERRMSG | None | 80 | Text | Read-only |
| FKEYS | ENTER=Search Cards  F3=Exit | 75 | Text | Read-only |

## Mapset: COCRDUP (Map CCRDUPA)

Source: `Input/bms/COCRDUP.bms`  
Program: COCRDUPC | Transaction: CCUP (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACCTSID | Account Number    : | 11 | Alphanumeric | Read-only |
| CARDSID | Card Number       : | 16 | Alphanumeric | Editable |
| CRDNAME | Name on card      : | 50 | Alphanumeric | Editable |
| CRDSTCD | Card Active Y/N   : | 1 | Alphanumeric | Editable |
| EXPMON | Expiry Date       : | 2 | Alphanumeric | Editable |
| EXPYEAR | / | 4 | Alphanumeric | Editable |
| EXPDAY | None | 2 | Alphanumeric | Hidden |
| INFOMSG | None | 40 | Text | Read-only |
| ERRMSG | None | 80 | Text | Read-only |
| FKEYS | ENTER=Process F3=Exit | 21 | Text | Read-only |
| FKEYSC | F5=Save F12=Cancel | 18 | Text | Hidden |

## Mapset: COMEN01 (Map COMEN1A)

Source: `Input/bms/COMEN01.bms`  
Program: COMEN01C | Transaction: CM00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| OPTN001 | Main Menu | 40 | Alphanumeric | Read-only |
| OPTN002 | None | 40 | Alphanumeric | Read-only |
| OPTN003 | None | 40 | Alphanumeric | Read-only |
| OPTN004 | None | 40 | Alphanumeric | Read-only |
| OPTN005 | None | 40 | Alphanumeric | Read-only |
| OPTN006 | None | 40 | Alphanumeric | Read-only |
| OPTN007 | None | 40 | Alphanumeric | Read-only |
| OPTN008 | None | 40 | Alphanumeric | Read-only |
| OPTN009 | None | 40 | Alphanumeric | Read-only |
| OPTN010 | None | 40 | Alphanumeric | Read-only |
| OPTN011 | None | 40 | Alphanumeric | Read-only |
| OPTN012 | None | 40 | Alphanumeric | Read-only |
| OPTION | Please select an option : | 2 | Numeric | Editable |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COPAU00 (Map COPAU0A)

Source: `Input/bms/COPAU00.bms`  
Program: COPAUS0C | Transaction: CPVS (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACCTID | Search Acct Id: | 11 | Alphanumeric | Editable |
| CNAME | Name: | 25 | Alphanumeric | Read-only |
| CUSTID | Customer Id: | 9 | Alphanumeric | Read-only |
| ADDR001 | None | 25 | Alphanumeric | Read-only |
| ACCSTAT | Acct Status: | 1 | Alphanumeric | Read-only |
| ADDR002 | None | 25 | Alphanumeric | Read-only |
| PHONE1 | PH: | 13 | Alphanumeric | Read-only |
| APPRCNT | Approval # : | 3 | Alphanumeric | Read-only |
| DECLCNT | Decline #: | 3 | Alphanumeric | Read-only |
| CREDLIM | Credit Lim: | 12 | Alphanumeric | Read-only |
| CASHLIM | Cash Lim: | 9 | Alphanumeric | Read-only |
| APPRAMT | Appr Amt: | 10 | Alphanumeric | Read-only |
| CREDBAL | Credit Bal: | 12 | Alphanumeric | Read-only |
| CASHBAL | Cash Bal: | 9 | Alphanumeric | Read-only |
| DECLAMT | Decl Amt: | 10 | Alphanumeric | Read-only |
| SEL0001 | ------------ | 1 | Alphanumeric | Editable |
| TRNID01 | None | 16 | Alphanumeric | Read-only |
| PDATE01 | None | 8 | Alphanumeric | Read-only |
| PTIME01 | None | 8 | Alphanumeric | Read-only |
| PTYPE01 | None | 4 | Alphanumeric | Read-only |
| PAPRV01 | None | 1 | Alphanumeric | Read-only |
| PSTAT01 | None | 1 | Alphanumeric | Read-only |
| PAMT001 | None | 12 | Alphanumeric | Read-only |
| SEL0002 | None | 1 | Alphanumeric | Editable |
| TRNID02 | None | 16 | Alphanumeric | Read-only |
| PDATE02 | None | 8 | Alphanumeric | Read-only |
| PTIME02 | None | 8 | Alphanumeric | Read-only |
| PTYPE02 | None | 4 | Alphanumeric | Read-only |
| PAPRV02 | None | 1 | Alphanumeric | Read-only |
| PSTAT02 | None | 1 | Alphanumeric | Read-only |
| PAMT002 | None | 12 | Alphanumeric | Read-only |
| SEL0003 | None | 1 | Alphanumeric | Editable |
| TRNID03 | None | 16 | Alphanumeric | Read-only |
| PDATE03 | None | 8 | Alphanumeric | Read-only |
| PTIME03 | None | 8 | Alphanumeric | Read-only |
| PTYPE03 | None | 4 | Alphanumeric | Read-only |
| PAPRV03 | None | 1 | Alphanumeric | Read-only |
| PSTAT03 | None | 1 | Alphanumeric | Read-only |
| PAMT003 | None | 12 | Alphanumeric | Read-only |
| SEL0004 | None | 1 | Alphanumeric | Editable |
| TRNID04 | None | 16 | Alphanumeric | Read-only |
| PDATE04 | None | 8 | Alphanumeric | Read-only |
| PTIME04 | None | 8 | Alphanumeric | Read-only |
| PTYPE04 | None | 4 | Alphanumeric | Read-only |
| PAPRV04 | None | 1 | Alphanumeric | Read-only |
| PSTAT04 | None | 1 | Alphanumeric | Read-only |
| PAMT004 | None | 12 | Alphanumeric | Read-only |
| TRNID05 | None | 16 | Alphanumeric | Read-only |
| PDATE05 | None | 8 | Alphanumeric | Read-only |
| PTIME05 | None | 8 | Alphanumeric | Read-only |
| PTYPE05 | None | 4 | Alphanumeric | Read-only |
| PAPRV05 | None | 1 | Alphanumeric | Read-only |
| PSTAT05 | None | 1 | Alphanumeric | Read-only |
| PAMT005 | None | 12 | Alphanumeric | Read-only |
| SEL0005 | None | 1 | Alphanumeric | Editable |
| ERRMSG | Type 'S' to View Authorization details from the list | 78 | Text | Read-only |

## Mapset: COPAU01 (Map COPAU1A)

Source: `Input/bms/COPAU01.bms`  
Program: COPAUS1C | Transaction: CPVD (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| CARDNUM | Card #: | 16 | Alphanumeric | Read-only |
| AUTHDT | Auth Date: | 10 | Alphanumeric | Read-only |
| AUTHTM | Auth Time: | 10 | Alphanumeric | Read-only |
| AUTHRSP | Auth Resp: | 1 | Alphanumeric | Read-only |
| AUTHRSN | Resp Reason: | 20 | Alphanumeric | Read-only |
| AUTHCD | Auth Code: | 6 | Alphanumeric | Read-only |
| AUTHAMT | Amount: | 12 | Alphanumeric | Read-only |
| POSEMD | POS Entry Mode: | 4 | Alphanumeric | Read-only |
| AUTHSRC | Source   : | 10 | Alphanumeric | Read-only |
| MCCCD | MCC Code: | 4 | Alphanumeric | Read-only |
| CRDEXP | Card Exp. Date: | 5 | Alphanumeric | Read-only |
| AUTHTYP | Auth Type: | 14 | Alphanumeric | Read-only |
| TRNID | Tran Id: | 15 | Alphanumeric | Read-only |
| AUTHMTC | Match Status: | 1 | Alphanumeric | Read-only |
| AUTHFRD | Fraud Status: | 10 | Alphanumeric | Read-only |
| MERNAME | Name: | 25 | Alphanumeric | Read-only |
| MERID | Merchant ID: | 15 | Alphanumeric | Read-only |
| MERCITY | City: | 25 | Alphanumeric | Read-only |
| MERST | State: | 2 | Alphanumeric | Read-only |
| MERZIP | Zip: | 10 | Alphanumeric | Read-only |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: CORPT00 (Map CORPT0A)

Source: `Input/bms/CORPT00.bms`  
Program: CORPT00C | Transaction: CR00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| MONTHLY | Transaction Reports | 1 | Alphanumeric | Editable |
| YEARLY | Monthly (Current Month) | 1 | Alphanumeric | Editable |
| CUSTOM | Yearly (Current Year) | 1 | Alphanumeric | Editable |
| SDTMM | Start Date : | 2 | Numeric | Editable |
| SDTDD | / | 2 | Numeric | Editable |
| SDTYYYY | / | 4 | Numeric | Editable |
| EDTMM | End Date : | 2 | Numeric | Editable |
| EDTDD | / | 2 | Numeric | Editable |
| EDTYYYY | / | 4 | Numeric | Editable |
| CONFIRM | The Report will be submitted for printing. Please confirm: | 1 | Alphanumeric | Editable |
| ERRMSG | (Y/N) | 78 | Text | Read-only |

## Mapset: COSGN00 (Map COSGN0A)

Source: `Input/bms/COSGN00.bms`  
Program: COSGN00C | Transaction: CC00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran : | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog : | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | Ahh:mm:ss | 9 | Character | Read-only |
| APPLID | AppID: | 8 | Alphanumeric | Read-only |
| SYSID | SysID: | 8 | Alphanumeric | Read-only |
| USERID | User ID     : | 8 | Alphanumeric | Editable |
| PASSWD | ________ | 8 | Alphanumeric | Masked |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COTRN00 (Map COTRN0A)

Source: `Input/bms/COTRN00.bms`  
Program: COTRN00C | Transaction: CT00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| PAGENUM | Page: | 8 | Alphanumeric | Read-only |
| TRNIDIN | Search Tran ID: | 16 | Alphanumeric | Editable |
| SEL0001 | ------------ | 1 | Alphanumeric | Editable |
| TRNID01 | None | 16 | Alphanumeric | Read-only |
| TDATE01 | None | 8 | Alphanumeric | Read-only |
| TDESC01 | None | 26 | Alphanumeric | Read-only |
| TAMT001 | None | 12 | Alphanumeric | Read-only |
| SEL0002 | None | 1 | Alphanumeric | Editable |
| TRNID02 | None | 16 | Alphanumeric | Read-only |
| TDATE02 | None | 8 | Alphanumeric | Read-only |
| TDESC02 | None | 26 | Alphanumeric | Read-only |
| TAMT002 | None | 12 | Alphanumeric | Read-only |
| SEL0003 | None | 1 | Alphanumeric | Editable |
| TRNID03 | None | 16 | Alphanumeric | Read-only |
| TDATE03 | None | 8 | Alphanumeric | Read-only |
| TDESC03 | None | 26 | Alphanumeric | Read-only |
| TAMT003 | None | 12 | Alphanumeric | Read-only |
| SEL0004 | None | 1 | Alphanumeric | Editable |
| TRNID04 | None | 16 | Alphanumeric | Read-only |
| TDATE04 | None | 8 | Alphanumeric | Read-only |
| TDESC04 | None | 26 | Alphanumeric | Read-only |
| TAMT004 | None | 12 | Alphanumeric | Read-only |
| SEL0005 | None | 1 | Alphanumeric | Editable |
| TRNID05 | None | 16 | Alphanumeric | Read-only |
| TDATE05 | None | 8 | Alphanumeric | Read-only |
| TDESC05 | None | 26 | Alphanumeric | Read-only |
| TAMT005 | None | 12 | Alphanumeric | Read-only |
| SEL0006 | None | 1 | Alphanumeric | Editable |
| TRNID06 | None | 16 | Alphanumeric | Read-only |
| TDATE06 | None | 8 | Alphanumeric | Read-only |
| TDESC06 | None | 26 | Alphanumeric | Read-only |
| TAMT006 | None | 12 | Alphanumeric | Read-only |
| SEL0007 | None | 1 | Alphanumeric | Editable |
| TRNID07 | None | 16 | Alphanumeric | Read-only |
| TDATE07 | None | 8 | Alphanumeric | Read-only |
| TDESC07 | None | 26 | Alphanumeric | Read-only |
| TAMT007 | None | 12 | Alphanumeric | Read-only |
| SEL0008 | None | 1 | Alphanumeric | Editable |
| TRNID08 | None | 16 | Alphanumeric | Read-only |
| TDATE08 | None | 8 | Alphanumeric | Read-only |
| TDESC08 | None | 26 | Alphanumeric | Read-only |
| TAMT008 | None | 12 | Alphanumeric | Read-only |
| SEL0009 | None | 1 | Alphanumeric | Editable |
| TRNID09 | None | 16 | Alphanumeric | Read-only |
| TDATE09 | None | 8 | Alphanumeric | Read-only |
| TDESC09 | None | 26 | Alphanumeric | Read-only |
| TAMT009 | None | 12 | Alphanumeric | Read-only |
| SEL0010 | None | 1 | Alphanumeric | Editable |
| TRNID10 | None | 16 | Alphanumeric | Read-only |
| TDATE10 | None | 8 | Alphanumeric | Read-only |
| TDESC10 | None | 26 | Alphanumeric | Read-only |
| TAMT010 | None | 12 | Alphanumeric | Read-only |
| ERRMSG | Type 'S' to View Transaction details from the list | 78 | Text | Read-only |

## Mapset: COTRN01 (Map COTRN1A)

Source: `Input/bms/COTRN01.bms`  
Program: COTRN01C | Transaction: CT01 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| TRNIDIN | Enter Tran ID: | 16 | Alphanumeric | Editable |
| TRNID | Transaction ID: | 16 | Alphanumeric | Read-only |
| CARDNUM | Card Number: | 16 | Alphanumeric | Read-only |
| TTYPCD | Type CD: | 2 | Alphanumeric | Read-only |
| TCATCD | Category CD: | 4 | Alphanumeric | Read-only |
| TRNSRC | Source: | 10 | Alphanumeric | Read-only |
| TDESC | Description: | 60 | Alphanumeric | Read-only |
| TRNAMT | Amount: | 12 | Alphanumeric | Read-only |
| TORIGDT | Orig Date: | 10 | Alphanumeric | Read-only |
| TPROCDT | Proc Date: | 10 | Alphanumeric | Read-only |
| MID | Merchant ID: | 9 | Alphanumeric | Read-only |
| MNAME | Merchant Name: | 30 | Alphanumeric | Read-only |
| MCITY | Merchant City: | 25 | Alphanumeric | Read-only |
| MZIP | Merchant Zip: | 10 | Alphanumeric | Read-only |
| ERRMSG | None | 78 | Text | Read-only |

## Mapset: COTRN02 (Map COTRN2A)

Source: `Input/bms/COTRN02.bms`  
Program: COTRN02C | Transaction: CT02 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| ACTIDIN | Enter Acct #: | 11 | Alphanumeric | Editable |
| CARDNIN | Card #: | 16 | Alphanumeric | Editable |
| TTYPCD | Type CD: | 2 | Alphanumeric | Editable |
| TCATCD | Category CD: | 4 | Alphanumeric | Editable |
| TRNSRC | Source: | 10 | Alphanumeric | Editable |
| TDESC | Description: | 60 | Alphanumeric | Editable |
| TRNAMT | Amount: | 12 | Alphanumeric | Editable |
| TORIGDT | Orig Date: | 10 | Alphanumeric | Editable |
| TPROCDT | Proc Date: | 10 | Alphanumeric | Editable |
| MID | Merchant ID: | 9 | Alphanumeric | Editable |
| MNAME | Merchant Name: | 30 | Alphanumeric | Editable |
| MCITY | Merchant City: | 25 | Alphanumeric | Editable |
| MZIP | Merchant Zip: | 10 | Alphanumeric | Editable |
| CONFIRM | You are about to add this transaction. Please confirm : | 1 | Alphanumeric | Editable |
| ERRMSG | (Y/N) | 78 | Text | Read-only |

## Mapset: COTRTLI (Map CTRTLIA)

Source: `Input/bms/COTRTLI.bms`  
Program: COTRTLIC | Transaction: CTLI (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| PAGENO | Page | 3 | Alphanumeric | Unknown (no ATTRB coded) |
| TRTYPE | Type Filter: | 2 | Alphanumeric | Editable |
| TRDESC | Description Filter: | 50 | Alphanumeric | Editable |
| TRTSEL1 | -------------------------------------------------- | 1 | Alphanumeric | Read-only |
| TRTTYP1 | None | 2 | Alphanumeric | Read-only |
| TRTYPD1 | None | 50 | Alphanumeric | Editable |
| TRTSEL2 | None | 1 | Alphanumeric | Read-only |
| TRTTYP2 | None | 2 | Alphanumeric | Read-only |
| TRTYPD2 | None | 50 | Alphanumeric | Editable |
| TRTSEL3 | None | 1 | Alphanumeric | Read-only |
| TRTTYP3 | None | 2 | Alphanumeric | Read-only |
| TRTYPD3 | None | 50 | Alphanumeric | Editable |
| TRTSEL4 | None | 1 | Alphanumeric | Read-only |
| TRTTYP4 | None | 2 | Alphanumeric | Read-only |
| TRTYPD4 | None | 50 | Alphanumeric | Editable |
| TRTSEL5 | None | 1 | Alphanumeric | Read-only |
| TRTTYP5 | None | 2 | Alphanumeric | Read-only |
| TRTYPD5 | None | 50 | Alphanumeric | Editable |
| TRTSEL6 | None | 1 | Alphanumeric | Read-only |
| TRTTYP6 | None | 2 | Alphanumeric | Read-only |
| TRTYPD6 | None | 50 | Alphanumeric | Editable |
| TRTSEL7 | None | 1 | Alphanumeric | Read-only |
| TRTTYP7 | None | 2 | Alphanumeric | Read-only |
| TRTYPD7 | None | 50 | Alphanumeric | Editable |
| TRTSELA | None | 1 | Alphanumeric | Read-only |
| TRTTYPA | None | 2 | Alphanumeric | Read-only |
| TRTDSCA | None | 50 | Alphanumeric | Read-only |
| INFOMSG | None | 45 | Text | Read-only |
| ERRMSG | None | 78 | Text | Read-only |
| BUTNF02 | F2=Add | 7 | Alphanumeric | Read-only |
| BUTNF03 | F3=Exit | 7 | Alphanumeric | Read-only |
| BUTNF07 | F7=Page Up | 10 | Alphanumeric | Read-only |
| BUTNF08 | F8=Page Dn | 10 | Alphanumeric | Read-only |
| BUTNF10 | F10=Save | 8 | Alphanumeric | Read-only |

## Mapset: COTRTUP (Map CTRTUPA)

Source: `Input/bms/COTRTUP.bms`  
Program: COTRTUPC | Transaction: CTTU (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| TRTYPCD | Transaction Type  : | 2 | Alphanumeric | Editable |
| TRTYDSC | Description       : | 50 | Alphanumeric | Editable |
| INFOMSG | None | 45 | Text | Read-only |
| ERRMSG | None | 78 | Text | Read-only |
| FKEYS | ENTER=Process F3=Exit | 21 | Text | Read-only |
| FKEY04 | F4=Delete | 9 | Text | Hidden |
| FKEY05 | F5=Save | 8 | Text | Hidden |
| FKEY06 | F6=Add | 6 | Text | Hidden |
| FKEY12 | F12=Cancel | 10 | Text | Hidden |

## Mapset: COUSR00 (Map COUSR0A)

Source: `Input/bms/COUSR00.bms`  
Program: COUSR00C | Transaction: CU00 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| PAGENUM | Page: | 8 | Alphanumeric | Read-only |
| USRIDIN | Search User ID: | 8 | Alphanumeric | Editable |
| SEL0001 | ---- | 1 | Alphanumeric | Editable |
| USRID01 | None | 8 | Alphanumeric | Read-only |
| FNAME01 | None | 20 | Alphanumeric | Read-only |
| LNAME01 | None | 20 | Alphanumeric | Read-only |
| UTYPE01 | None | 1 | Alphanumeric | Read-only |
| SEL0002 | None | 1 | Alphanumeric | Editable |
| USRID02 | None | 8 | Alphanumeric | Read-only |
| FNAME02 | None | 20 | Alphanumeric | Read-only |
| LNAME02 | None | 20 | Alphanumeric | Read-only |
| UTYPE02 | None | 1 | Alphanumeric | Read-only |
| SEL0003 | None | 1 | Alphanumeric | Editable |
| USRID03 | None | 8 | Alphanumeric | Read-only |
| FNAME03 | None | 20 | Alphanumeric | Read-only |
| LNAME03 | None | 20 | Alphanumeric | Read-only |
| UTYPE03 | None | 1 | Alphanumeric | Read-only |
| SEL0004 | None | 1 | Alphanumeric | Editable |
| USRID04 | None | 8 | Alphanumeric | Read-only |
| FNAME04 | None | 20 | Alphanumeric | Read-only |
| LNAME04 | None | 20 | Alphanumeric | Read-only |
| UTYPE04 | None | 1 | Alphanumeric | Read-only |
| SEL0005 | None | 1 | Alphanumeric | Editable |
| USRID05 | None | 8 | Alphanumeric | Read-only |
| FNAME05 | None | 20 | Alphanumeric | Read-only |
| LNAME05 | None | 20 | Alphanumeric | Read-only |
| UTYPE05 | None | 1 | Alphanumeric | Read-only |
| SEL0006 | None | 1 | Alphanumeric | Editable |
| USRID06 | None | 8 | Alphanumeric | Read-only |
| FNAME06 | None | 20 | Alphanumeric | Read-only |
| LNAME06 | None | 20 | Alphanumeric | Read-only |
| UTYPE06 | None | 1 | Alphanumeric | Read-only |
| SEL0007 | None | 1 | Alphanumeric | Editable |
| USRID07 | None | 8 | Alphanumeric | Read-only |
| FNAME07 | None | 20 | Alphanumeric | Read-only |
| LNAME07 | None | 20 | Alphanumeric | Read-only |
| UTYPE07 | None | 1 | Alphanumeric | Read-only |
| SEL0008 | None | 1 | Alphanumeric | Editable |
| USRID08 | None | 8 | Alphanumeric | Read-only |
| FNAME08 | None | 20 | Alphanumeric | Read-only |
| LNAME08 | None | 20 | Alphanumeric | Read-only |
| UTYPE08 | None | 1 | Alphanumeric | Read-only |
| SEL0009 | None | 1 | Alphanumeric | Editable |
| USRID09 | None | 8 | Alphanumeric | Read-only |
| FNAME09 | None | 20 | Alphanumeric | Read-only |
| LNAME09 | None | 20 | Alphanumeric | Read-only |
| UTYPE09 | None | 1 | Alphanumeric | Read-only |
| SEL0010 | None | 1 | Alphanumeric | Editable |
| USRID10 | None | 8 | Alphanumeric | Read-only |
| FNAME10 | None | 20 | Alphanumeric | Read-only |
| LNAME10 | None | 20 | Alphanumeric | Read-only |
| UTYPE10 | None | 1 | Alphanumeric | Read-only |
| ERRMSG | Type 'U' to Update or 'D' to Delete a User from the list | 78 | Text | Read-only |

## Mapset: COUSR01 (Map COUSR1A)

Source: `Input/bms/COUSR01.bms`  
Program: COUSR01C | Transaction: CU01 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| FNAME | First Name: | 20 | Alphanumeric | Editable |
| LNAME | Last Name: | 20 | Alphanumeric | Editable |
| USERID | User ID: | 8 | Alphanumeric | Editable |
| PASSWD | Password: | 8 | Alphanumeric | Masked |
| USRTYPE | User Type: | 1 | Alphanumeric | Editable |
| ERRMSG | (A=Admin, U=User) | 78 | Text | Read-only |

## Mapset: COUSR02 (Map COUSR2A)

Source: `Input/bms/COUSR02.bms`  
Program: COUSR02C | Transaction: CU02 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| USRIDIN | Enter User ID: | 8 | Alphanumeric | Editable |
| FNAME | First Name: | 20 | Alphanumeric | Editable |
| LNAME | Last Name: | 20 | Alphanumeric | Editable |
| PASSWD | Password: | 8 | Alphanumeric | Masked |
| USRTYPE | User Type: | 1 | Alphanumeric | Editable |
| ERRMSG | (A=Admin, U=User) | 78 | Text | Read-only |

## Mapset: COUSR03 (Map COUSR3A)

Source: `Input/bms/COUSR03.bms`  
Program: COUSR03C | Transaction: CU03 (per CicsXref.md)

| Field Name | Field Initial | Field Length | Field Type | Field Attribute |
|---|---|---|---|---|
| TRNNAME | Tran: | 4 | Alphanumeric | Read-only |
| TITLE01 | None | 40 | Text | Read-only |
| CURDATE | mm/dd/yy | 8 | Character | Read-only |
| PGMNAME | Prog: | 8 | Alphanumeric | Read-only |
| TITLE02 | None | 40 | Text | Read-only |
| CURTIME | hh:mm:ss | 8 | Character | Read-only |
| USRIDIN | Enter User ID: | 8 | Alphanumeric | Editable |
| FNAME | First Name: | 20 | Alphanumeric | Read-only |
| LNAME | Last Name: | 20 | Alphanumeric | Read-only |
| USRTYPE | User Type: | 1 | Alphanumeric | Read-only |
| ERRMSG | (A=Admin, U=User) | 78 | Text | Read-only |

## Summary

- BMS files processed: 21
- Mapsets found: 21 (21 maps); UNMAPPED mapsets: 0
- Named fields documented: 585
- Files that could not be parsed / malformed entries: None
- Program COCRDSEC (CDV1) has no BMS mapset in `Input/bms/` (source missing, see CicsXref.md).
- CP00, CDRA, CDRD (COPAUA0C, COACCT01, CODATE01) are MQ-driven and have no screen.

## Needs Review

- COACTVW/ADTOPEN: attribute Unknown (no ATTRB coded)
- COACTVW/ACRDLIM: attribute Unknown (no ATTRB coded)
- COACTVW/AEXPDT: attribute Unknown (no ATTRB coded)
- COACTVW/ACSHLIM: attribute Unknown (no ATTRB coded)
- COACTVW/AREISDT: attribute Unknown (no ATTRB coded)
- COACTVW/ACURBAL: attribute Unknown (no ATTRB coded)
- COACTVW/ACRCYCR: attribute Unknown (no ATTRB coded)
- COACTVW/AADDGRP: attribute Unknown (no ATTRB coded)
- COACTVW/ACRCYDB: attribute Unknown (no ATTRB coded)
- COACTVW/ACSTNUM: attribute Unknown (no ATTRB coded)
- COACTVW/ACSTSSN: attribute Unknown (no ATTRB coded)
- COACTVW/ACSTDOB: attribute Unknown (no ATTRB coded)
- COACTVW/ACSTFCO: attribute Unknown (no ATTRB coded)
- COACTVW/ACSFNAM: attribute Unknown (no ATTRB coded)
- COACTVW/ACSMNAM: attribute Unknown (no ATTRB coded)
- COACTVW/ACSLNAM: attribute Unknown (no ATTRB coded)
- COACTVW/ACSADL1: attribute Unknown (no ATTRB coded)
- COACTVW/ACSSTTE: attribute Unknown (no ATTRB coded)
- COACTVW/ACSADL2: attribute Unknown (no ATTRB coded)
- COACTVW/ACSZIPC: attribute Unknown (no ATTRB coded)
- COACTVW/ACSCITY: attribute Unknown (no ATTRB coded)
- COACTVW/ACSCTRY: attribute Unknown (no ATTRB coded)
- COACTVW/ACSPHN1: attribute Unknown (no ATTRB coded)
- COACTVW/ACSGOVT: attribute Unknown (no ATTRB coded)
- COACTVW/ACSPHN2: attribute Unknown (no ATTRB coded)
- COACTVW/ACSEFTC: attribute Unknown (no ATTRB coded)
- COACTVW/ACSPFLG: attribute Unknown (no ATTRB coded)
- COCRDLI/PAGENO: attribute Unknown (no ATTRB coded)
- COCRDSL/CRDNAME: attribute Unknown (no ATTRB coded)
- COTRTLI/PAGENO: attribute Unknown (no ATTRB coded)
