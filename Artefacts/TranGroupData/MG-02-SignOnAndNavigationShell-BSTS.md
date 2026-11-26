# MG-02 – Sign-on and Navigation Shell – Business & Technical Summary

| Transaction | Program | Map / Mapset | Role in the group |
|---|---|---|---|
| CC00 | COSGN00C | COSGN0A / COSGN00 | Sign-on: authenticates against USRSEC and routes by user type |
| CM00 | COMEN01C | COMEN1A / COMEN01 | Main menu for regular users (11 options) |
| CA00 | COADM01C | COADM1A / COADM01 | Admin menu (6 options) |

## a. Business Summary

MG-02 is the front door and the navigation hub of CardDemo. Every online function in the estate is reached through it, and every function returns to it. A user starts at the sign-on screen, enters a user id and password, and is taken either to the administrator menu or to the regular user's main menu, depending on the user type held in the security file. From a menu the user picks a numbered option, and the shell hands control to the program behind that option, passing along who the user is and where they came from.

**Actors.** There are two roles, both defined by `SEC-USR-TYPE` in the USRSEC user record: administrators ('A') and regular users ('U') (Source: CSUSR01Y; COCOM01Y 88-levels `CDEMO-USRTYP-ADMIN` / `CDEMO-USRTYP-USER`). Administrators maintain users and transaction types. Regular users work with accounts, cards, transactions, reports, bill payment and pending authorizations.

### Sign-on (CC00 / COSGN00C)
The user must supply both fields. A blank user id is rejected with "Please enter User ID ...", and a blank password with "Please enter Password ...". The user id is checked before the password (Source: PROCESS-ENTER-KEY in COSGN00C). Both values are converted to upper case before use, so sign-on is not case-sensitive (Source: PROCESS-ENTER-KEY). The user id is the key of the security file. If no record exists, the user sees "User not found. Try again ...". If the record exists but the password differs, the user sees "Wrong Password. Try again ...". Any other file problem gives "Unable to verify the User ..." (Source: READ-USER-SEC-FILE). On success, administrators go straight to the admin menu and everyone else to the main menu. The user's id and type travel with them for the rest of the session (Source: READ-USER-SEC-FILE). Pressing PF3 on the sign-on screen ends the session with "Thank you for using CardDemo application..." (Source: MAIN-PARA; CSMSG01Y). Any other key gives "Invalid key pressed. Please see below..." (Source: MAIN-PARA; CSMSG01Y).

There is no limit on failed attempts and no lockout. Passwords are up to 8 characters and are stored and compared in plain text (Source: CSUSR01Y; READ-USER-SEC-FILE).

### Main menu (CM00 / COMEN01C)
The main menu lists eleven numbered options, built from the COMEN02Y table: Account View, Account Update, Credit Card List, Credit Card View, Credit Card Update, Transaction List, Transaction View, Transaction Add, Transaction Reports, Bill Payment and Pending Authorization View (Source: BUILD-MENU-OPTIONS; COMEN02Y). The user types an option number of one or two digits. Anything that is not a number from 1 to the option count is rejected with "Please enter a valid option number..." (Source: PROCESS-ENTER-KEY). The table can mark an option as administrator-only; a regular user who picks one sees "No access - Admin Only option... ". In the shipped table, however, every option is marked for regular users (Source: PROCESS-ENTER-KEY; COMEN02Y). Pending Authorization View is opened only if that function is installed in the region. Otherwise the user sees "This option Pending Authorization View is not installed..." in red (Source: PROCESS-ENTER-KEY, INQUIRE PROGRAM). Options whose program is a "DUMMY" placeholder would be announced as "coming soon" (Source: PROCESS-ENTER-KEY). PF3 returns to sign-on (Source: MAIN-PARA).

### Admin menu (CA00 / COADM01C)
The admin menu lists six options from COADM02Y: User List, User Add, User Update and User Delete (security), plus Transaction Type List/Update and Transaction Type Maintenance (both Db2, and both delivered in MG-01) (Source: BUILD-MENU-OPTIONS; COADM02Y). Option validation and the invalid-option message are the same as on the main menu. If the chosen function is not installed, the administrator sees "This option is not installed ..." in green and stays on the menu (Source: PGMIDERR-ERR-PARA). PF3 returns to sign-on (Source: MAIN-PARA).

Neither menu re-checks the user's type when it is entered. Access control relies on the sign-on routing (Source: MAIN-PARA in COMEN01C / COADM01C).

## b. Technical Summary

### Shared design
All three programs are pseudo-conversational. Each ends with `EXEC CICS RETURN TRANSID(WS-TRANID) COMMAREA(CARDDEMO-COMMAREA)`, so the next key press re-enters the same transaction (Source: MAIN-PARA in each program). State is carried in the COCOM01Y COMMAREA:
- `CDEMO-FROM-TRANID` / `CDEMO-FROM-PROGRAM` and `CDEMO-TO-TRANID` / `CDEMO-TO-PROGRAM` for navigation
- `CDEMO-USER-ID` (X(8)) and `CDEMO-USER-TYPE` ('A' or 'U')
- `CDEMO-PGM-CONTEXT` (0 = first entry, 1 = re-entry)
- customer, account and card context fields that MG-02 does not use

Each program fills a common header: titles from COTTL01Y, transaction id, program name, and the current date (MM/DD/YY) and time (HH:MM:SS) (Source: POPULATE-HEADER-INFO). COSGN00C also shows APPLID and SYSID through `EXEC CICS ASSIGN`.

### COSGN00C (CC00)
- **First entry** (`EIBCALEN = 0`): sends COSGN0A cleared to LOW-VALUES, with the cursor on USERID (`MOVE -1 TO USERIDL`) (Source: MAIN-PARA, SEND-SIGNON-SCREEN).
- **Re-entry, by EIBAID:**
  - Enter: `PROCESS-ENTER-KEY`.
  - PF3: `SEND TEXT` of the thank-you message, then a bare `RETURN` (no TRANSID), which ends the conversation.
  - Any other key: invalid-key message.
- **Processing Enter:** `PROCESS-ENTER-KEY` receives the map with RESP/RESP2 captured (MAPFAIL is not tested explicitly) and validates USERID, then PASSWD. It upper-cases both into `WS-USER-ID` / `CDEMO-USER-ID` and `WS-USER-PWD`.
- **File read:** if there is no error, `READ-USER-SEC-FILE` issues `EXEC CICS READ DATASET('USRSEC') INTO(SEC-USER-DATA) RIDFLD(WS-USER-ID) KEYLENGTH(8)`.
- **Response handling:**
  - RESP 0 with a matching `SEC-USR-PWD`: sets the COMMAREA fields, then `XCTL PROGRAM('COADM01C')` for type 'A' or `XCTL PROGRAM('COMEN01C')` otherwise.
  - RESP 13 (NOTFND): user-not-found message.
  - Any other RESP: unable-to-verify message.
- **Map attributes:** USERID is unprotected with initial cursor (IC), 8 characters. PASSWD is dark (DRK, non-display), 8 characters. ERRMSG is bright, 78 characters (Source: COSGN00.bms).

### COMEN01C (CM00)
- **No COMMAREA:** XCTL to COSGN00C via `RETURN-TO-SIGNON-SCREEN`, which defaults `CDEMO-TO-PROGRAM` to 'COSGN00C' when it is blank.
- **First entry with a COMMAREA:** sets `CDEMO-PGM-REENTER` and sends COMEN1A.
- **Re-entry:** `RECEIVE-MENU-SCREEN`, then by EIBAID: Enter → `PROCESS-ENTER-KEY`, PF3 → back to sign-on, any other key → invalid-key message (Source: MAIN-PARA).
- **Option parsing:** `PROCESS-ENTER-KEY` right-trims OPTION (2 digits, NUM attribute), replaces embedded spaces with '0', and validates against `CDEMO-MENU-OPT-COUNT` (11). It then checks the admin-only flag `CDEMO-MENU-OPT-USRTYPE`.
- **Transfer:** COPAUS0C is first probed with `EXEC CICS INQUIRE PROGRAM … NOHANDLE` and transferred only if `EIBRESP = NORMAL`. Names starting with 'DUMMY' produce the coming-soon message. Every other option is reached by `XCTL PROGRAM(CDEMO-MENU-OPT-PGMNAME(WS-OPTION)) COMMAREA(CARDDEMO-COMMAREA)`, after setting the FROM fields to CM00/COMEN01C and the context to 0.
- **Menu text:** `BUILD-MENU-OPTIONS` builds "nn. name" strings into OPTN001–OPTN012 (Source: COMEN01C; COMEN02Y).

### COADM01C (CA00)
COADM01C has the same structure as COMEN01C and uses the COADM02Y table (6 entries; OCCURS 9).

Differences:
- It registers `HANDLE CONDITION PGMIDERR(PGMIDERR-ERR-PARA)`. A missing target program therefore redisplays the menu with "This option is not installed ..." and returns with TRANSID CA00.
- There is no admin-only check, because the menu itself is the admin scope.
- There is no INQUIRE probe.
- After a non-DUMMY XCTL the code would fall through to the "not installed" message. This is reached only when the XCTL fails (Source: PROCESS-ENTER-KEY, PGMIDERR-ERR-PARA in COADM01C).

### Resources
| Resource | Program | Operation | Key |
|---|---|---|---|
| VSAM KSDS USRSEC (80-byte record CSUSR01Y) | COSGN00C | Read | `SEC-USR-ID` X(8), upper-cased |
| USRSEC (declared only) | COMEN01C, COADM01C | None | — |

No Db2, IMS, MQ or TS/TD queues are used by MG-02.

## Gaps
1. Admin-only and DUMMY menu paths are unreachable with the shipped COMEN02Y/COADM02Y data (all options 'U'; no DUMMY entries).
2. The invalid-option path in COMEN01C continues into the admin-only check with an out-of-range index and may send the map twice. This is treated as a defect and not carried forward.
3. Plain-text, 8-character, upper-cased passwords with no lockout. The modern target will replace this with OAuth 2.0 / hashed credentials; this is a deliberate deviation that needs a Gate 2 decision.
4. Menus do not re-validate the user type on entry.
5. RECEIVE MAP RESP codes are captured but not evaluated (no explicit MAPFAIL handling) in all three programs.
