# MG-02 – Sign-on and Navigation Shell – BDD Test Cases

| Transaction | Program | Map / Mapset | Data |
|---|---|---|---|
| CC00 | COSGN00C | COSGN0A / COSGN00 | VSAM USRSEC (Read, key = user id) |
| CM00 | COMEN01C | COMEN1A / COMEN01 | None (`WS-USRSEC-FILE` declared, unused); options table COMEN02Y |
| CA00 | COADM01C | COADM1A / COADM01 | None (`WS-USRSEC-FILE` declared, unused); options table COADM02Y |

COMMAREA: COCOM01Y (`CDEMO-USER-ID`, `CDEMO-USER-TYPE` 'A'/'U', `CDEMO-PGM-CONTEXT`, `CDEMO-FROM-*`, `CDEMO-TO-*`). User record: CSUSR01Y (`SEC-USR-ID` X(8), `SEC-USR-FNAME` X(20), `SEC-USR-LNAME` X(20), `SEC-USR-PWD` X(8), `SEC-USR-TYPE` X(1)). Common messages: CSMSG01Y.

## a. Business User Stories

```gherkin
Feature: Sign on to CardDemo and reach the right menu
  As a CardDemo user (regular or administrator)
  I want to sign on with my user id and password
  So that I reach the menu of functions I am allowed to use

  Rule: Valid credentials route by user type
    Scenario: Administrator signs on and lands on the admin menu
      Given USRSEC holds user "ADMIN001" with password "PASSWORD" and type "A"
      When the user enters user id "admin001" and password "password" and presses Enter
      Then the admin menu (CA00) is shown
      And the COMMAREA carries user id "ADMIN001" and user type "A"
      # Source: READ-USER-SEC-FILE in COSGN00C

    Scenario: Regular user signs on and lands on the main menu
      Given USRSEC holds user "USER0001" with password "PASSWORD" and type "U"
      When the user enters user id "USER0001" and password "PASSWORD" and presses Enter
      Then the main menu (CM00) is shown
      And the COMMAREA carries user type "U"
      # Source: READ-USER-SEC-FILE in COSGN00C

    Scenario: User id and password are not case-sensitive
      Given USRSEC holds user "USER0001" with password "PASSWORD"
      When the user enters user id "user0001" and password "PassWord"
      Then sign-on succeeds because both values are upper-cased before the lookup and comparison
      # Source: PROCESS-ENTER-KEY (FUNCTION UPPER-CASE) in COSGN00C

  Rule: Sign-off
    Scenario: User leaves the application from the sign-on screen
      Given the sign-on screen is displayed
      When the user presses PF3
      Then the screen shows "Thank you for using CardDemo application..."
      And the transaction ends without a next transaction id
      # Source: MAIN-PARA (DFHPF3) and SEND-PLAIN-TEXT in COSGN00C

Feature: Choose a function from the main menu
  As a regular user
  I want to pick a numbered option from the main menu
  So that I can open account, card, transaction, report, bill payment and authorization functions

  Scenario: Main menu lists the eleven user options
    Given a signed-on regular user reaches the main menu
    Then the options are shown as "1. Account View" … "11. Pending Authorization View" in OPTN001–OPTN011
    # Source: BUILD-MENU-OPTIONS in COMEN01C; COMEN02Y

  Scenario Outline: Valid option transfers to its program
    Given the main menu is displayed
    When the user enters option "<opt>" and presses Enter
    Then control transfers to program "<program>" with the COMMAREA
    And CDEMO-FROM-TRANID is "CM00", CDEMO-FROM-PROGRAM is "COMEN01C" and CDEMO-PGM-CONTEXT is 0
    Examples:
      | opt | program  |
      | 1   | COACTVWC |
      | 2   | COACTUPC |
      | 3   | COCRDLIC |
      | 4   | COCRDSLC |
      | 5   | COCRDUPC |
      | 6   | COTRN00C |
      | 7   | COTRN01C |
      | 8   | COTRN02C |
      | 9   | CORPT00C |
      | 10  | COBIL00C |
    # Source: PROCESS-ENTER-KEY (WHEN OTHER XCTL) in COMEN01C; COMEN02Y

  Scenario: Pending Authorization View transfers only when the program is installed
    Given the main menu is displayed and program COPAUS0C is installed
    When the user enters option "11"
    Then control transfers to COPAUS0C
    # Source: PROCESS-ENTER-KEY (INQUIRE PROGRAM) in COMEN01C

Feature: Choose a function from the admin menu
  As an administrator
  I want to pick a numbered option from the admin menu
  So that I can maintain users and transaction types

  Scenario: Admin menu lists the six admin options
    Given a signed-on administrator reaches the admin menu
    Then the options are shown as "1. User List (Security)" … "6. Transaction Type Maintenance (Db2)"
    # Source: BUILD-MENU-OPTIONS in COADM01C; COADM02Y

  Scenario Outline: Valid admin option transfers to its program
    Given the admin menu is displayed
    When the administrator enters option "<opt>" and presses Enter
    Then control transfers to "<program>" with CDEMO-FROM-TRANID "CA00" and CDEMO-FROM-PROGRAM "COADM01C"
    Examples:
      | opt | program  |
      | 1   | COUSR00C |
      | 2   | COUSR01C |
      | 3   | COUSR02C |
      | 4   | COUSR03C |
      | 5   | COTRTLIC |
      | 6   | COTRTUPC |
    # Source: PROCESS-ENTER-KEY in COADM01C; COADM02Y

  Scenario: Either menu returns to sign-on with PF3
    Given the main menu or the admin menu is displayed
    When the user presses PF3
    Then control transfers to COSGN00C
    # Source: MAIN-PARA (DFHPF3) and RETURN-TO-SIGNON-SCREEN in COMEN01C / COADM01C
```

## b. Technical User Stories

```gherkin
Feature: Pseudo-conversational control of the shell programs
  As the system
  I want each program to distinguish first entry from re-entry
  So that screens are sent and received in the right order

  Scenario: First entry to sign-on sends an empty screen with the cursor on user id
    Given CC00 is started with no COMMAREA (EIBCALEN = 0)
    Then COSGN0A is sent with LOW-VALUES, the cursor on USERID, and the header (titles, date MM/DD/YY, time HH:MM:SS, APPLID, SYSID)
    And the program returns with TRANSID "CC00" and the COMMAREA
    # Source: MAIN-PARA, SEND-SIGNON-SCREEN, POPULATE-HEADER-INFO in COSGN00C

  Scenario: Menu started without a COMMAREA goes back to sign-on
    Given CM00 or CA00 is started with EIBCALEN = 0
    Then control transfers to COSGN00C
    # Source: MAIN-PARA and RETURN-TO-SIGNON-SCREEN in COMEN01C / COADM01C

  Scenario: First entry to a menu sends the menu and marks re-entry
    Given CM00 or CA00 receives a COMMAREA with CDEMO-PGM-CONTEXT = 0
    Then CDEMO-PGM-CONTEXT is set to 1 and the menu map is sent
    # Source: MAIN-PARA (CDEMO-PGM-REENTER) in COMEN01C / COADM01C

  Scenario: Re-entry to a menu receives the map and evaluates the key
    Given CDEMO-PGM-CONTEXT = 1
    When the user presses a key
    Then the menu map is received and EIBAID is evaluated (Enter, PF3, other)
    # Source: MAIN-PARA, RECEIVE-MENU-SCREEN in COMEN01C / COADM01C

  Scenario: Successful sign-on builds the COMMAREA before transfer
    Given the password matches
    Then CDEMO-FROM-TRANID = "CC00", CDEMO-FROM-PROGRAM = "COSGN00C", CDEMO-USER-ID = upper-cased id, CDEMO-USER-TYPE = SEC-USR-TYPE and CDEMO-PGM-CONTEXT = 0
    # Source: READ-USER-SEC-FILE in COSGN00C

  Scenario: Option input is right-trimmed and zero-filled
    Given the user types option " 5" or "5 "
    Then the trailing spaces are removed, embedded spaces become "0", and the value is echoed to OPTION
    # Source: PROCESS-ENTER-KEY in COMEN01C / COADM01C
```

## c. UI Validations

```gherkin
Feature: Field validations on the shell screens
  As a user
  I want clear messages when my input is incomplete or invalid
  So that I can correct it

  Scenario: User id is required
    Given the sign-on screen
    When the user presses Enter with USERID blank
    Then the message "Please enter User ID ..." is shown and the cursor is on USERID
    # Source: PROCESS-ENTER-KEY in COSGN00C

  Scenario: Password is required
    Given the sign-on screen with a user id entered
    When the user presses Enter with PASSWD blank
    Then the message "Please enter Password ..." is shown and the cursor is on PASSWD
    # Source: PROCESS-ENTER-KEY in COSGN00C

  Scenario: Password is not displayed
    Given the sign-on screen
    Then PASSWD is a dark (non-display) field of 8 characters and USERID is 8 characters
    # Source: COSGN00.bms (PASSWD ATTRB=(DRK,FSET,UNPROT), USERID LENGTH=8)

  Scenario Outline: Menu option must be a listed number
    Given the <menu> is displayed
    When the user enters option "<input>" and presses Enter
    Then the message "Please enter a valid option number..." is shown
    Examples:
      | menu       | input |
      | main menu  | 0     |
      | main menu  | 12    |
      | main menu  | (blank) |
      | admin menu | 0     |
      | admin menu | 7     |
    # Source: PROCESS-ENTER-KEY in COMEN01C / COADM01C (NOT NUMERIC, > count, ZEROS)
```

## d. Error Handling

```gherkin
Feature: Error responses in the shell
  As a user
  I want to understand why I was not let in or not transferred
  So that I know what to do next

  Scenario: Unknown user id
    Given USRSEC has no record for "NOUSER01"
    When the user signs on with "NOUSER01"
    Then the message "User not found. Try again ..." is shown and the cursor is on USERID
    # Source: READ-USER-SEC-FILE (RESP 13 NOTFND) in COSGN00C

  Scenario: Wrong password
    Given USRSEC holds "USER0001" with password "PASSWORD"
    When the user signs on with password "WRONG"
    Then the message "Wrong Password. Try again ..." is shown and the cursor is on PASSWD
    # Source: READ-USER-SEC-FILE in COSGN00C

  Scenario: USRSEC read fails for another reason
    Given the USRSEC read returns a response other than NORMAL or NOTFND
    Then the message "Unable to verify the User ..." is shown and the cursor is on USERID
    # Source: READ-USER-SEC-FILE (WHEN OTHER) in COSGN00C

  Scenario Outline: Unsupported key
    Given the <screen> is displayed
    When the user presses a key other than Enter or PF3
    Then the message "Invalid key pressed. Please see below..." is shown
    Examples:
      | screen         |
      | sign-on screen |
      | main menu      |
      | admin menu     |
    # Source: MAIN-PARA (WHEN OTHER) in COSGN00C / COMEN01C / COADM01C; CSMSG01Y

  Scenario: Pending Authorization View is not installed
    Given program COPAUS0C is not installed in the region
    When the user enters main-menu option "11"
    Then the message "This option Pending Authorization View is not installed..." is shown in red
    # Source: PROCESS-ENTER-KEY (INQUIRE PROGRAM NOHANDLE) in COMEN01C

  Scenario: Admin option program is missing
    Given the selected admin program is not defined (PGMIDERR)
    When the administrator selects it
    Then the message "This option is not installed ..." is shown in green and the admin menu is redisplayed
    # Source: HANDLE CONDITION PGMIDERR, PGMIDERR-ERR-PARA in COADM01C
```

## e. Edge Cases

```gherkin
Feature: Boundary behaviour in the shell
  Scenario: Main-menu option marked "DUMMY" is announced as coming soon
    Given a main-menu option whose program name starts with "DUMMY"
    When the user selects it
    Then the message "This option <name>is coming soon ..." is shown in green
    # Source: PROCESS-ENTER-KEY (PGMNAME(1:5) = 'DUMMY') in COMEN01C

  Scenario: Admin option marked "DUMMY" is not transferred
    Given an admin option whose program name starts with "DUMMY"
    When the administrator selects it
    Then the message "This option is not installed ..." is shown
    # Source: PROCESS-ENTER-KEY in COADM01C

  Scenario: Admin-only option chosen by a regular user
    Given a main-menu option with user type "A" and a signed-on user of type "U"
    When the user selects it
    Then the message "No access - Admin Only option... " is shown
    # Source: PROCESS-ENTER-KEY (CDEMO-USRTYP-USER AND OPT-USRTYPE = 'A') in COMEN01C

  Scenario: Return target defaults to sign-on
    Given CDEMO-TO-PROGRAM is blank or LOW-VALUES
    When a menu returns to sign-on
    Then COSGN00C is used as the target
    # Source: RETURN-TO-SIGNON-SCREEN in COMEN01C / COADM01C
```

## Gaps

1. **No admin-only options exist in data.** Every COMEN02Y entry has user type 'U', so the "No access - Admin Only option..." path cannot be reached with shipped data (COMEN02Y).
2. **No DUMMY entries exist.** The "coming soon" (COMEN01C) and DUMMY (COADM01C) paths are unreachable with shipped tables.
3. **Table sizes.** COMEN02Y declares `OCCURS 12` with 11 entries; COADM02Y declares `OCCURS 9` with 6 entries. The maps have 12 option slots.
4. **Invalid-option fall-through in COMEN01C.** After an invalid option is reported, the admin-only check still indexes `CDEMO-MENU-OPT-USRTYPE(WS-OPTION)` with an out-of-range or zero option, and the menu may be sent twice in one task. This behaviour is not reproduced.
5. **Passwords are stored and compared in plain text** (CSUSR01Y `SEC-USR-PWD`), upper-cased and limited to 8 characters. There is no lockout or attempt counter in COSGN00C.
6. **Neither menu checks the user type on entry.** A COMMAREA with type 'U' reaching COADM01C is not rejected; enforcement relies on COSGN00C routing only.
7. `WS-USRSEC-FILE` is declared in COMEN01C and COADM01C but no file command is issued.
8. Vendor copybooks DFHAID/DFHBMSCA are external (Gate 1 decision).
