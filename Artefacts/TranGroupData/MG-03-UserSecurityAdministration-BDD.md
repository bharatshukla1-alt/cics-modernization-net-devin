# MG-03 – User Security Administration – BDD Test Cases

| Transaction | Program | Map / Mapset | Data |
|---|---|---|---|
| CU00 | COUSR00C | COUSR0A / COUSR00 | VSAM USRSEC: browse (STARTBR / READNEXT / READPREV / ENDBR) |
| CU01 | COUSR01C | COUSR1A / COUSR01 | VSAM USRSEC: WRITE |
| CU02 | COUSR02C | COUSR2A / COUSR02 | VSAM USRSEC: READ UPDATE, REWRITE |
| CU03 | COUSR03C | COUSR3A / COUSR03 | VSAM USRSEC: READ UPDATE, DELETE |

COMMAREA: COCOM01Y plus the CDEMO-CU0x-INFO extensions (first/last user ID on the page, page number, next-page flag, selection flag, selected user). Record: CSUSR01Y. Entry is from the admin menu, CA00 options 1–4. PF3 returns to COADM01C.

## a. Business User Stories

```gherkin
Feature: List security users
  As an administrator
  I want to page through all CardDemo users
  So that I can pick one to update or delete

  Scenario: First page lists users in user-ID order
    Given USRSEC holds ADMIN001–ADMIN005 and USER0001–USER0005
    When the administrator opens User List
    Then up to 10 users are shown with User ID, First Name, Last Name and Type, in ascending user-ID order
    And the page number is 1
    # Source: PROCESS-PAGE-FORWARD (STARTBR/READNEXT) in COUSR00C; COUSR00.bms rows 10–19

  Scenario: Search positions the list at a user ID
    When the administrator enters "USER0003" in Search User ID and presses Enter
    Then the list starts at the first user ID greater than or equal to "USER0003"
    # Source: PROCESS-ENTER-KEY / STARTBR with USRIDIN in COUSR00C

  Scenario Outline: Row action transfers to maintenance
    When the administrator types "<sel>" next to "USER0002" and presses Enter
    Then control transfers to "<program>" with USER0002 as the selected user
    Examples:
      | sel | program  |
      | U   | COUSR02C |
      | u   | COUSR02C |
      | D   | COUSR03C |
      | d   | COUSR03C |
    # Source: PROCESS-ENTER-KEY EVALUATE CDEMO-CU00-USR-SEL-FLG in COUSR00C

Feature: Add a security user
  As an administrator
  I want to add a user with name, ID, password and type
  So that the user can sign on

  Scenario: User is added
    Given "USER0006" does not exist
    When the administrator enters first name "ANN", last name "LEE", user ID "USER0006", password "PASSWORD", type "U" and presses Enter
    Then the record is written to USRSEC
    And the message "User USER0006 has been added ..." is shown in green and the fields are cleared
    # Source: WRITE-USER-SEC-FILE (NORMAL) in COUSR01C

Feature: Update a security user
  Scenario: Selected user is shown for update
    Given the administrator chose U for "USER0002" on the list
    Then the user's names, password and type are shown with "Press PF5 key to save your updates ..."
    # Source: READ-USER-SEC-FILE (READ UPDATE, NORMAL) in COUSR02C

  Scenario: Changes are saved with PF5
    When the administrator changes the last name and presses PF5
    Then the record is rewritten and "User USER0002 has been updated ..." is shown
    # Source: UPDATE-USER-INFO, UPDATE-USER-SEC-FILE (REWRITE NORMAL) in COUSR02C

  Scenario: PF3 saves and exits
    When the administrator changes a field and presses PF3
    Then the change is saved and control returns to the calling program (or the admin menu)
    # Source: MAIN-PARA DFHPF3 → UPDATE-USER-INFO, RETURN-TO-PREV-SCREEN in COUSR02C

Feature: Delete a security user
  Scenario: Selected user is shown for delete confirmation
    Given the administrator chose D for "USER0005" on the list
    Then the user's names and type are shown read-only with "Press PF5 key to delete this user ..."
    # Source: READ-USER-SEC-FILE in COUSR03C

  Scenario: Delete is confirmed with PF5
    When the administrator presses PF5
    Then the record is deleted and "User USER0005 has been deleted ..." is shown
    # Source: DELETE-USER-SEC-FILE (NORMAL) in COUSR03C
```

## b. Technical User Stories

```gherkin
Feature: Pseudo-conversational control
  Scenario: No COMMAREA goes to sign-on
    Given any CU0x transaction is started with EIBCALEN = 0
    Then control transfers to COSGN00C
    # Source: MAIN-PARA in COUSR00C–COUSR03C

  Scenario: Selected user is carried in the COMMAREA
    Given COUSR00C transfers with a selected user
    Then COUSR02C/COUSR03C read CDEMO-CU0x-USR-SELECTED on first entry and look that user up
    # Source: MAIN-PARA (CDEMO-CU02/03-USR-SELECTED) in COUSR02C, COUSR03C

  Scenario: Paging state is kept in the COMMAREA
    Then the first and last user ID shown, the page number and the next-page flag are stored
    And they are used as browse keys for PF7 (READPREV) and PF8 (READNEXT)
    # Source: CDEMO-CU00-INFO; PROCESS-PF7-KEY, PROCESS-PF8-KEY in COUSR00C

  Scenario: Update and delete use READ UPDATE before writing
    Then COUSR02C issues READ UPDATE then REWRITE, and COUSR03C issues READ UPDATE then DELETE, keyed on SEC-USR-ID
    # Source: READ-USER-SEC-FILE, UPDATE-USER-SEC-FILE, DELETE-USER-SEC-FILE
```

## c. UI Validations

```gherkin
Feature: Required fields
  Scenario Outline: Add requires every field, in screen order
    When the administrator presses Enter on Add User with "<blank>" empty and the earlier fields filled
    Then "<message>" is shown and the cursor is on that field
    Examples:
      | blank      | message                         |
      | First Name | First Name can NOT be empty...  |
      | Last Name  | Last Name can NOT be empty...   |
      | User ID    | User ID can NOT be empty...     |
      | Password   | Password can NOT be empty...    |
      | User Type  | User Type can NOT be empty...   |
    # Source: PROCESS-ENTER-KEY in COUSR01C

  Scenario: Update and delete need a user ID to fetch
    When Enter is pressed with Enter User ID blank
    Then "User ID can NOT be empty..." is shown
    # Source: PROCESS-ENTER-KEY in COUSR02C, COUSR03C

  Scenario: Update requires every field and a change
    When PF5 is pressed with no field changed
    Then "Please modify to update ..." is shown
    # Source: UPDATE-USER-INFO in COUSR02C

  Scenario: Password is not displayed
    Then the Password field is dark (non-display), 8 characters, on Add and Update
    # Source: COUSR01.bms, COUSR02.bms PASSWD ATTRB=(DRK,…)

  Scenario: Invalid row action
    When a value other than U/u/D/d is typed in Sel and Enter is pressed
    Then "Invalid selection. Valid values are U and D" is shown
    # Source: PROCESS-ENTER-KEY WHEN OTHER in COUSR00C
```

## d. Error Handling

```gherkin
Feature: File errors
  Scenario: Duplicate user ID on add
    Given "USER0001" exists
    When the administrator adds user ID "USER0001"
    Then "User ID already exist..." is shown and the cursor is on User ID
    # Source: WRITE-USER-SEC-FILE (DUPKEY/DUPREC) in COUSR01C

  Scenario: Add fails for another reason
    Then "Unable to Add User..." is shown
    # Source: WRITE-USER-SEC-FILE (OTHER) in COUSR01C

  Scenario Outline: Fetch of an unknown user
    When "NOUSER01" is fetched on <screen>
    Then "User ID NOT found..." is shown
    Examples:
      | screen      |
      | Update User |
      | Delete User |
    # Source: READ-USER-SEC-FILE (NOTFND) in COUSR02C, COUSR03C

  Scenario: Lookup fails for another reason
    Then "Unable to lookup User..." is shown
    # Source: READ-USER-SEC-FILE (OTHER)

  Scenario: Rewrite or delete fails
    Then "Unable to Update User..." is shown (the delete program reuses this text)
    # Source: UPDATE-USER-SEC-FILE / DELETE-USER-SEC-FILE (OTHER)

  Scenario: Unsupported key
    Then "Invalid key pressed. Please see below..." is shown
    # Source: MAIN-PARA WHEN OTHER; CSMSG01Y
```

## e. Edge Cases

```gherkin
Feature: Paging boundaries
  Scenario: PF7 on the first page
    Then "You are already at the top of the page..." is shown
    # Source: PROCESS-PF7-KEY in COUSR00C

  Scenario: PF8 on the last page
    Then "You are already at the bottom of the page..." is shown
    # Source: PROCESS-PF8-KEY in COUSR00C

  Scenario: Exactly 10 users
    Given USRSEC holds exactly 10 users
    Then PF8 from page 1 reports the bottom of the page
    # Source: PROCESS-PAGE-FORWARD next-page check in COUSR00C

  Scenario: PF4 clears the screen; PF12 leaves without saving
    # Source: CLEAR-CURRENT-SCREEN; MAIN-PARA DFHPF12 in COUSR01C–COUSR03C
```

## Gaps
1. **No value check on User Type.** COUSR01C/COUSR02C accept any non-blank character; the screen hint says A or U, but the code never enforces it.
2. **No upper-casing on add or update,** while sign-on (COSGN00C) upper-cases what the user types. A lower-case user ID or password added here can never sign on.
3. **Passwords are stored in plain text** (CSUSR01Y `SEC-USR-PWD` X(8)). This is the same decision as MG-02.
4. **An administrator can delete or demote themselves,** including the last administrator. No rule prevents it.
5. **No optimistic concurrency.** READ UPDATE locks only within a single task; between showing the user and pressing PF5, another administrator's change can be overwritten.
6. **COUSR03C reports a delete failure with the text "Unable to Update User...".**
7. **No audit trail of user maintenance** in the legacy code.
