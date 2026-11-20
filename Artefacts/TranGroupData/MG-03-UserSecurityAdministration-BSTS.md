# MG-03 – User Security Administration – Business & Technical Summary

| Transaction | Program | Map / Mapset | Role |
|---|---|---|---|
| CU00 | COUSR00C | COUSR0A / COUSR00 | List users, 10 per page; U/D row actions |
| CU01 | COUSR01C | COUSR1A / COUSR01 | Add user |
| CU02 | COUSR02C | COUSR2A / COUSR02 | Update user |
| CU03 | COUSR03C | COUSR3A / COUSR03 | Delete user |

## a. Business Summary
MG-03 lets an administrator maintain the CardDemo sign-on users stored in USRSEC, the same file MG-02 reads at sign-on. It is reached from the admin menu: options 1–4 are User List, User Add, User Update and User Delete. The only actor is the administrator, whose access is decided by the admin-menu routing in MG-02.

**List (CU00).** The administrator sees up to ten users per page, each with User ID, First Name, Last Name and Type, in user-ID order (Source: PROCESS-PAGE-FORWARD in COUSR00C). The page can start at a typed user ID ("Search User ID"). PF8 and PF7 page forward and back. At the ends the screen says "You are already at the bottom of the page..." or "You are already at the top of the page..." (Source: PROCESS-PF8-KEY, PROCESS-PF7-KEY). Typing U or D, in either case, next to a user opens the update or delete screen for that user. Any other letter gives "Invalid selection. Valid values are U and D" (Source: PROCESS-ENTER-KEY). PF3 returns to the admin menu.

**Add (CU01).** All five fields are required and are checked in screen order: First Name, Last Name, User ID, Password, then User Type. Each has its own message, for example "First Name can NOT be empty..." (Source: PROCESS-ENTER-KEY in COUSR01C). On success the screen shows "User <id> has been added ..." in green and clears the fields. A duplicate ID gives "User ID already exist..." and any other failure gives "Unable to Add User..." (Source: WRITE-USER-SEC-FILE). The screen hint says the type is A=Admin or U=User, but the code does not check the value.

**Update (CU02).** The user selected on the list, or typed and fetched with Enter, is shown with "Press PF5 key to save your updates ...". PF5 saves. PF3 saves and then leaves. PF12 leaves without saving. PF4 clears (Source: MAIN-PARA in COUSR02C). Every field is required again. If nothing changed, the screen says "Please modify to update ..." (Source: UPDATE-USER-INFO). Success shows "User <id> has been updated ...". An unknown ID gives "User ID NOT found...".

**Delete (CU03).** The selected user is shown read-only with "Press PF5 key to delete this user ...". PF5 deletes and shows "User <id> has been deleted ..." (Source: DELETE-USER-SEC-FILE). A failed delete is reported with the update wording, "Unable to Update User...".

## b. Technical Summary
**Shared design.** All four programs are pseudo-conversational. They return with their own TRANSID and the COCOM01Y COMMAREA, extended with CDEMO-CU0x-INFO: first and last user ID on the page, page number, next-page flag, selection flag and selected user ID. Started without a COMMAREA, each program transfers (XCTL) to COSGN00C. RETURN-TO-PREV-SCREEN uses `CDEMO-TO-PROGRAM`, which defaults to COSGN00C, and sets the FROM fields and a context of 0.

**COUSR00C.** It browses USRSEC with STARTBR, READNEXT (forward) and READPREV (backward), then ENDBR, keyed on SEC-USR-ID. It fills SEL/USRID/FNAME/LNAME/UTYPE for rows 01–10. The first and last keys on the page are kept for PF7/PF8. It reads one record past the page to set the next-page flag. A row action moves `CDEMO-CU00-USR-SELECTED` and XCTLs to COUSR02C (U/u) or COUSR03C (D/d).

**COUSR01C.** It runs `EXEC CICS WRITE DATASET('USRSEC') RIDFLD(SEC-USR-ID)` with the screen values moved as typed: no upper-casing and no type check. RESP NORMAL, DUPKEY/DUPREC and OTHER drive the messages above. The PASSWD field is dark (DRK).

**COUSR02C / COUSR03C.** On first entry with a selected user, or on Enter with USRIDIN, they issue `READ DATASET('USRSEC') … UPDATE`. COUSR02C compares the screen with the record and REWRITEs only when something changed. COUSR03C DELETEs on PF5. NOTFND and OTHER give the messages above. On PF3, COUSR02C saves, then returns to `CDEMO-FROM-PROGRAM` or COADM01C.

**Resources.**

| File | Program | Operation |
|---|---|---|
| USRSEC | COUSR00C | Read (browse) |
| USRSEC | COUSR01C | Create |
| USRSEC | COUSR02C | Read, Update |
| USRSEC | COUSR03C | Read, Delete |

No Db2, IMS, MQ or TS queues.

## Gaps
1. User Type is not validated as A or U.
2. Add and update do not upper-case user IDs or passwords, but sign-on does, so mixed-case accounts can't sign on.
3. Passwords are plain text, as in MG-02.
4. Nothing stops an administrator from deleting or demoting the last administrator, or themselves.
5. There is no optimistic concurrency between display and PF5.
6. The delete failure message reuses the update text.
7. User maintenance has no audit trail.
