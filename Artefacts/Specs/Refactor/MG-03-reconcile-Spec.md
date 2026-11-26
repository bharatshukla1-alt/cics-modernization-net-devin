# MG-03 — Reconcile Spec (Refactor)

## 1. Purpose
This spec checks that the four MG-03 screens and their USRSEC operations behave like COUSR00C–COUSR03C. Where the user chose a fix, it checks that the target differs only in the documented ways (DV1–DV4, G1–G5).

## 2. Data Reconciliation
| Check | Legacy | Target | Method |
|---|---|---|---|
| Record layout | CSUSR01Y, 80 bytes | `usrsec`, 5 columns; the filler is dropped | Column-by-column match (DB-Status) |
| Key order | KSDS ascending EBCDIC | `ORDER BY sec_usr_id` (C collation for `CHAR(8)` IDs of upper-case letters and digits) | Compare page 1 and page 2 of the 10 seed users plus 5 added users |
| Create | WRITE → new record | INSERT → row; reading it back matches the input (upper-cased ID/password) | API test plus `psql` |
| Update | REWRITE changed fields | UPDATE; unchanged fields stay as they were; a blank password is kept | API test plus `psql` |
| Delete | DELETE | Row gone; other rows untouched | API test plus `psql` |
| Counts | — | Admin count never reaches 0 | G3 tests |

## 3. Functional Reconciliation
| Legacy scenario (BDD) | Expected target result |
|---|---|
| List page 1, 10 rows | Same 10 IDs, same order |
| Search USER0003 | First row USER0003 |
| PF8 / PF7 at the ends | Buttons disabled, or the legacy message |
| Sel U / D | Row button opens Update / Delete for that user |
| Add with each field blank | Same message, same order |
| Add duplicate | "User ID already exist..." |
| Fetch unknown | "User ID NOT found..." |
| Update with no change | "Please modify to update ..." |
| Update / Delete success | Same success text |
| Invalid selection | Not reachable (DV1) |

## 4. Intentional Differences
- G1: type validated as A or U.
- G2: user ID and password upper-cased.
- G3: self and last-admin protection.
- G4: optimistic concurrency.
- G5: audit trail.
- DV2: password is optional on update and never returned.

## 5. Sign-off Evidence
- Phase 8 test results with every row above executed.
- Database snapshots before and after each write test.
