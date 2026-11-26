# MG-01 — Transaction Type Maintenance — BDD Test Cases

**Trans Group:** MG-01 — Transaction type maintenance (Db2) — list, filter, add, update, delete
**Conversion order (user-confirmed):** CTLI first, then CTTU.

| Transaction | Program | Map / Mapset | Data |
|---|---|---|---|
| CTLI | COTRTLIC (`Input/cbl/COTRTLIC.cbl`) | CTRTLIA / COTRTLI (`Input/bms/COTRTLI.bms`) | Db2 `CARDDEMO.TRANSACTION_TYPE` R, U, D |
| CTTU | COTRTUPC (`Input/cbl/COTRTUPC.cbl`) | CTRTUPA / COTRTUP (`Input/bms/COTRTUP.bms`) | Db2 `CARDDEMO.TRANSACTION_TYPE` C, R, U, D |

Table (`Input/ddl/TRNTYPE.ddl`): `TR_TYPE CHAR(2) NOT NULL` (PK), `TR_DESCRIPTION VARCHAR(50) NOT NULL`. COMMAREA: `COCOM01Y` (CARDDEMO-COMMAREA) + program-private area.

## a. Business User Stories

```gherkin
Feature: Maintain the transaction type reference table
  As a CardDemo administrator,
  I want to list, filter, add, update and delete transaction types,
  so that the transaction-type codes used across card transactions stay accurate.

  Rule: The list shows transaction types in code order, 7 per page
    Scenario: Administrator opens the transaction type list
      Given the TRANSACTION_TYPE table contains rows "01","02","03"
      When the administrator starts transaction "CTLI" from the admin menu
      Then up to 7 rows are shown ordered by TR_TYPE ascending
      And the page number shows "1"
      And the info message reads "Type U to update, D to delete any record"
      # Source: 8000-READ-FORWARD, 2500-SETUP-MESSAGE in COTRTLIC

  Rule: The list can be filtered by type code and/or description
    Scenario: Filter by exact type code
      Given rows "01" and "02" exist
      When the administrator enters "01" in TRTYPE and presses ENTER
      Then only the row with TR_TYPE "01" is shown
      # Source: C-TR-TYPE-FORWARD cursor, 1220-EDIT-TYPECD in COTRTLIC

    Scenario: Filter by partial description
      Given a row "02" with description "Payment" exists
      When the administrator enters "Pay" in TRDESC and presses ENTER
      Then rows whose TR_DESCRIPTION is LIKE "%Pay%" are shown
      # Source: 1230-EDIT-DESC (STRING '%' ... '%') in COTRTLIC

  Rule: A description can be updated in place on the list
    Scenario: Update a description from the list
      Given row "01" is shown with description "Purchase"
      When the administrator types "U" in the row selector and presses ENTER
      Then the row is highlighted and its description becomes editable
      And the info message reads "Update HIGHLIGHTED row. Press F10 to save"
      When the administrator changes the description to "Purchases" and presses F10
      Then TR_DESCRIPTION of "01" is updated to "Purchases" and committed
      And the info message reads "HIGHLIGHTED row was updated"
      # Source: 2300-SCREEN-ARRAY-INIT, 9200-UPDATE-RECORD in COTRTLIC

  Rule: A row can be deleted from the list after confirmation
    Scenario: Delete a transaction type from the list
      Given row "09" is shown
      When the administrator types "D" in the row selector and presses ENTER
      Then the info message reads "Delete HIGHLIGHTED row ? Press F10 to confirm"
      When the administrator presses F10
      Then row "09" is deleted from TRANSACTION_TYPE and committed
      And the info message reads "HIGHLIGHTED row deleted.Hit Enter to continue"
      # Source: 9300-DELETE-RECORD, 2500-SETUP-MESSAGE in COTRTLIC

  Rule: A new transaction type is added through the maintenance screen
    Scenario: Add a new transaction type
      Given the maintenance screen CTTU shows "Enter transaction type to be maintained"
      When the administrator enters type code "12" which does not exist and presses ENTER
      Then the error message reads "No record found for this key in database"
      And the info message reads "Press F05 to add. F12 to cancel"
      When the administrator presses F5
      Then the info message reads "Enter new transaction type details."
      When the administrator enters description "Fee reversal" and presses ENTER
      Then the info message reads "Changes validated.Press F5 to save"
      When the administrator presses F5
      Then a row ("12","Fee reversal") is inserted and committed
      And the info message reads "Changes committed to database"
      # Source: 9100-GET-TRANSACTION-TYPE, 9600-WRITE-PROCESSING, 9700-INSERT-RECORD in COTRTUPC

  Rule: An existing transaction type is updated through the maintenance screen
    Scenario: Update description on the maintenance screen
      Given type "01" exists with description "Purchase"
      When the administrator enters "01" and presses ENTER
      Then the code and description of "01" are displayed and the description is editable
      When the administrator changes the description to "Retail purchase" and presses ENTER
      Then the info message reads "Changes validated.Press F5 to save"
      When the administrator presses F5
      Then TR_DESCRIPTION of "01" is updated and committed
      And the info message reads "Changes committed to database"
      # Source: 1200-EDIT-MAP-INPUTS, 9600-WRITE-PROCESSING in COTRTUPC

  Rule: An existing transaction type is deleted through the maintenance screen
    Scenario: Delete on the maintenance screen
      Given type "09" is displayed on CTTU
      When the administrator presses F4
      Then the info message reads "Delete this record ? Press F4 to confirm"
      When the administrator presses F4 again
      Then type "09" is deleted and committed
      And the info message reads "Delete successful."
      # Source: 0000-MAIN (PFK04 branches), 9800-DELETE-PROCESSING in COTRTUPC
```

## b. Technical User Stories

```gherkin
Feature: Pseudo-conversational control, navigation and Db2 access
  As the system,
  I want each screen interaction to run as a stateless task that carries its state in the COMMAREA,
  so that paging, selections and pending confirmations survive between user inputs.

  Rule: CTLI first entry and re-entry
    Scenario: First entry with no COMMAREA
      Given EIBCALEN is 0
      When CTLI starts
      Then CARDDEMO-COMMAREA is initialised with FROM-TRANID "CTLI", FROM-PROGRAM "COTRTLIC", user type admin
      And paging is reset to the first page
      And the forward cursor is read from the lowest TR_TYPE and map CTRTLIA is sent with ERASE
      And the task ends with RETURN TRANSID("CTLI") passing the COMMAREA
      # Source: 0000-MAIN, COMMON-RETURN in COTRTLIC

    Scenario: Re-entry receives the map
      Given CDEMO-FROM-PROGRAM is "COTRTLIC" and EIBCALEN > 0
      When the user presses an attention key
      Then RECEIVE MAP("CTRTLIA") MAPSET("COTRTLI") is executed and inputs are edited
      # Source: 0000-MAIN, 1100-RECEIVE-SCREEN in COTRTLIC

    Scenario: Priming Db2 query is executed on every interaction
      When COTRTLIC processes any input
      Then "SELECT 1 FROM SYSIBM.SYSDUMMY1" is executed before any table access
      # Source: 9998-PRIMING-QUERY in CSDB2RPY (included by COTRTLIC)

  Rule: CTLI PF-key handling
    Scenario Outline: Accepted keys on CTLI
      Given the list screen is displayed
      When the user presses <key>
      Then the program performs <action>
      Examples:
        | key   | action                                                                 |
        | ENTER | edit filters and selections and redisplay                             |
        | F2    | XCTL to COTRTUPC with CARDDEMO-COMMAREA (add), no SYNCPOINT            |
        | F3    | SYNCPOINT then XCTL to CDEMO-FROM-PROGRAM, or COADM01C when blank/self |
        | F7    | open C-TR-TYPE-BACKWARD (TR_TYPE < first key, DESC) and show previous page |
        | F8    | open C-TR-TYPE-FORWARD from last key and show next page                |
        | F10   | confirm the pending delete or update                                   |
      # Source: 0000-MAIN PF-key EVALUATE in COTRTLIC

    Scenario: Unsupported key is treated as ENTER
      Given the list screen is displayed
      When the user presses F5
      Then the input is processed as if ENTER was pressed
      # Source: 0000-MAIN (PFK-INVALID -> CCARD-AID-ENTER) in COTRTLIC

    Scenario: F10 without a pending action is treated as ENTER
      Given no row has "D" or "U" pending, or the filters or row selection changed
      When the user presses F10
      Then the input is processed as ENTER and no Db2 change is made
      # Source: 0000-MAIN (PFK10 guard) in COTRTLIC

  Rule: CTLI Db2 operations
    Scenario: Forward page read (R)
      When a page is read forward
      Then rows are FETCHed from C-TR-TYPE-FORWARD until 7 rows are loaded
      And one extra FETCH decides whether a next page exists
      # Source: 8000-READ-FORWARD in COTRTLIC

    Scenario: Filter count (R)
      Given a valid type and/or description filter
      When inputs are edited
      Then SELECT COUNT(1) FROM CARDDEMO.TRANSACTION_TYPE applies the same filter predicates
      # Source: 9100-CHECK-FILTERS in COTRTLIC

    Scenario: Update of a description (U)
      When F10 confirms an update
      Then UPDATE CARDDEMO.TRANSACTION_TYPE SET TR_DESCRIPTION WHERE TR_TYPE is executed
      And EXEC CICS SYNCPOINT is issued on SQLCODE 0
      # Source: 9200-UPDATE-RECORD in COTRTLIC

    Scenario: Delete of a row (D)
      When F10 confirms a delete
      Then DELETE FROM CARDDEMO.TRANSACTION_TYPE WHERE TR_TYPE is executed
      And EXEC CICS SYNCPOINT is issued on SQLCODE 0
      And paging state is reset to the first page
      # Source: 9300-DELETE-RECORD, 0000-MAIN in COTRTLIC

  Rule: CTTU entry, state machine and navigation
    Scenario: Fresh entry from admin menu or list
      Given COTRTUPC is entered with EIBCALEN 0, or from COADM01C/COTRTLIC with CDEMO-PGM-ENTER
      When CTTU starts
      Then the private state is reset to "details not fetched"
      And map CTRTUPA is sent with the info message "Enter transaction type to be maintained"
      # Source: 0000-MAIN, 3250-SETUP-INFOMSG in COTRTUPC

    Scenario Outline: Accepted keys on CTTU by state
      Given the maintenance state is <state>
      When the user presses <key>
      Then the key is <result>
      Examples:
        | state                     | key | result   |
        | any                       | F3  | accepted |
        | not confirm-delete        | ENTER | accepted |
        | show details / confirm delete | F4 | accepted |
        | changes validated / not found / delete in progress | F5 | accepted |
        | validated / show / not found / confirm delete / create new | F12 | accepted |
        | show details              | F5  | rejected with "Invalid key pressed" |
      # Source: 0001-CHECK-PFKEYS in COTRTUPC

    Scenario: F3 returns to the caller
      Given CTTU was entered from COTRTLIC
      When the user presses F3
      Then SYNCPOINT is issued and control XCTLs to COTRTLIC
      # Source: 0000-MAIN (PFK03) in COTRTUPC

    Scenario: F3 with no caller returns to admin menu
      Given CDEMO-FROM-PROGRAM is blank
      When the user presses F3
      Then control XCTLs to COADM01C (transaction CA00)
      # Source: 0000-MAIN (PFK03) in COTRTUPC

  Rule: CTTU Db2 operations
    Scenario: Read by key (R)
      When a valid type code is entered
      Then SELECT TR_TYPE, TR_DESCRIPTION FROM CARDDEMO.TRANSACTION_TYPE WHERE TR_TYPE = key is executed
      # Source: 9100-GET-TRANSACTION-TYPE in COTRTUPC

    Scenario: Save is an update-then-insert (U/C)
      Given changes were validated
      When the user presses F5
      Then UPDATE ... SET TR_DESCRIPTION WHERE TR_TYPE is executed
      And on SQLCODE +100 an INSERT (TR_TYPE, TR_DESCRIPTION) is executed
      And SYNCPOINT is issued after a successful UPDATE or INSERT
      # Source: 9600-WRITE-PROCESSING, 9700-INSERT-RECORD in COTRTUPC

    Scenario: Delete (D)
      Given delete is confirmed
      When the user presses F4
      Then DELETE FROM CARDDEMO.TRANSACTION_TYPE WHERE TR_TYPE = old key is executed and SYNCPOINT issued
      # Source: 9800-DELETE-PROCESSING in COTRTUPC
```

## c. UI Validations

```gherkin
Feature: Field-level validation on CTRTLIA and CTRTUPA
  As an administrator,
  I want invalid input rejected with a clear message,
  so that only well-formed transaction types are stored.

  Rule: CTLI type-code filter (TRTYPE)
    Scenario Outline: Type filter values
      When the administrator enters "<value>" in TRTYPE and presses ENTER
      Then <outcome>
      Examples:
        | value | outcome |
        | (blank) | no type filter is applied |
        | 00    | no type filter is applied (zeros treated as blank) |
        | 05    | only TR_TYPE "05" is listed |
        | A1    | TRTYPE turns red with error "TYPE CODE FILTER,IF SUPPLIED MUST BE A 2 DIGIT NUMBER" and row selectors are protected |
      # Source: 1220-EDIT-TYPECD, 2400-SETUP-SCREEN-ATTRS in COTRTLIC

  Rule: CTLI filters that match nothing
    Scenario: No rows match the filters
      Given no row matches type "77"
      When the administrator filters on "77"
      Then the filter field turns red and the error reads "No Records found for these filter conditions"
      # Source: 1290-CROSS-EDITS in COTRTLIC

  Rule: CTLI row selector (TRTSEL)
    Scenario Outline: Row action codes
      When the administrator enters "<codes>" in the row selectors and presses ENTER
      Then <outcome>
      Examples:
        | codes        | outcome |
        | D on one row | the row is highlighted for delete confirmation |
        | U on one row | the row description is unprotected for editing |
        | X on one row | the selector is flagged and the error reads "Action code selected is invalid" |
        | D on two rows | the error reads "Please select only 1 action" |
      # Source: 1210-EDIT-ARRAY in COTRTLIC

    Scenario: Selections are cleared when a filter changes
      Given a row has "D" entered
      When the administrator also changes TRTYPE or TRDESC and presses ENTER
      Then the selections are ignored and the list is re-read for the new filter
      # Source: 1210-EDIT-ARRAY (filter changed -> INITIALIZE WS-EDIT-SELECT-FLAGS) in COTRTLIC

  Rule: Description edits (list row TRTYPD and CTTU TRTYDSC)
    Scenario Outline: Description validation
      When the description is changed to "<value>"
      Then <outcome>
      Examples:
        | value                  | outcome |
        | (blank)                | error "Transaction Desc must be supplied." |
        | Purchase-Online        | error "Transaction Desc can have numbers or alphabets only." |
        | Purchase Online 2      | accepted (letters, digits and spaces) |
        | same text, different case | "No change detected with respect to database values." (CTLI) / "No change detected with respect to values fetched." (CTTU) |
      # Source: 1211-EDIT-ARRAY-DESC, 1240-EDIT-ALPHANUM-REQD in COTRTLIC; 1205-COMPARE-OLD-NEW, 1230-EDIT-ALPHANUM-REQD in COTRTUPC

  Rule: CTTU type code (TRTYPCD)
    Scenario Outline: Type code validation
      When the administrator enters "<value>" in TRTYPCD and presses ENTER
      Then <outcome>
      Examples:
        | value   | outcome |
        | (blank) | error "Tran Type code must be supplied." and "*" is shown in the field |
        | AB      | error "Tran Type code must be numeric." |
        | 00      | error "Tran Type code must not be zero." |
        | 5       | accepted and normalised to "05" before the Db2 read |
      # Source: 1210-EDIT-TRANTYPE, 1245-EDIT-NUM-REQD, 3300-SETUP-SCREEN-ATTRS in COTRTUPC
```

## d. Error Handling

```gherkin
Feature: Db2 and input error handling
  As an administrator,
  I want failures reported with the Db2 reason,
  so that I know whether to retry or correct data.

  Rule: CTLI Db2 errors
    Scenario: Db2 unavailable
      Given the priming query returns a non-zero SQLCODE
      When CTLI processes input
      Then the screen is replaced by plain text starting "Db2 access failure.  SQLCODE:" and the task returns without a TRANSID
      # Source: 9998-PRIMING-QUERY in CSDB2RPY, SEND-LONG-TEXT in COTRTLIC

    Scenario Outline: Update failures on the list
      Given an update is confirmed with F10
      When UPDATE returns SQLCODE <code>
      Then the error message starts with "<message>" followed by " SQLCODE:" and the DSNTIAC text
      And the row stays in update mode
      Examples:
        | code | message |
        | +100 | Record not found. Deleted by others ? |
        | -911 | Deadlock. Someone else updating ? |
        | -803 | Update failed with |
      # Source: 9200-UPDATE-RECORD, 9999-FORMAT-DB2-MESSAGE in COTRTLIC

    Scenario Outline: Delete failures on the list
      When DELETE returns SQLCODE <code>
      Then the error message starts with "<message>"
      Examples:
        | code | message |
        | -532 | Please delete associated child records first: |
        | -904 | Delete failed with message: |
      # Source: 9300-DELETE-RECORD in COTRTLIC

    Scenario: Cursor errors
      When OPEN, FETCH or CLOSE of a cursor returns a negative SQLCODE
      Then the error names the cursor action (e.g. "C-TR-TYPE-FORWARD Open") with the SQLCODE
      # Source: 9400/9450/9500/9550 cursor paragraphs, 8000-READ-FORWARD in COTRTLIC

  Rule: CTTU Db2 errors
    Scenario: Read error
      When the key SELECT returns a negative SQLCODE
      Then the error reads "Error accessing: TRANSACTION_TYPE table. SQLCODE:" with SQLERRM
      # Source: 9100-GET-TRANSACTION-TYPE in COTRTUPC

    Scenario: Lock conflict on save
      When UPDATE returns SQLCODE -911
      Then the error reads "Could not lock record for update" and the info message reads "Changes unsuccessful"
      # Source: 9600-WRITE-PROCESSING, 3250-SETUP-INFOMSG in COTRTUPC

    Scenario: Update or insert failure on save
      When UPDATE returns a negative SQLCODE other than -911, or INSERT fails
      Then the error starts "Error updating: TRANSACTION_TYPE Table. SQLCODE:" or "Error inserting record into: TRANSACTION_TYPE Table. SQLCODE:"
      And the info message reads "Changes unsuccessful"
      # Source: 9600-WRITE-PROCESSING, 9700-INSERT-RECORD in COTRTUPC

    Scenario Outline: Delete failure
      When DELETE returns SQLCODE <code>
      Then the error starts "<message>"
      Examples:
        | code | message |
        | -532 | Please delete associated child records first:SQLCODE : |
        | -904 | Delete failed with message:SQLCODE : |
      # Source: 9800-DELETE-PROCESSING in COTRTUPC

    Scenario: Invalid key
      Given the state does not allow the key
      When the user presses it
      Then the error reads "Invalid key pressed" and the screen is redisplayed unchanged
      # Source: 0001-CHECK-PFKEYS in COTRTUPC

    Scenario: Unexpected state
      Given 2000-DECIDE-ACTION reaches WHEN OTHER
      Then ABEND-ROUTINE sends "UNEXPECTED DATA SCENARIO" and abends with code "0001"
      # Source: 2000-DECIDE-ACTION, ABEND-ROUTINE in COTRTUPC

  Rule: Cancel paths
    Scenario Outline: F12 cancels a pending action on CTTU
      Given the state is <state>
      When the user presses F12
      Then the error reads "<message>"
      Examples:
        | state                     | message |
        | delete confirmation       | Delete was cancelled |
        | changes validated         | Update was cancelled |
      # Source: 2000-DECIDE-ACTION in COTRTUPC
```

## e. Edge Cases

```gherkin
Feature: Boundary conditions guarded by the source
  As the system,
  I want paging and empty results handled explicitly,
  so that the user always gets a defined screen.

  Scenario: Empty table
    Given TRANSACTION_TYPE has no rows
    When CTLI is started
    Then no rows are shown and the message state is "No records found for this search condition."
    # Source: 8000-READ-FORWARD (screen 1, row 0) in COTRTLIC

  Scenario: F7 on the first page
    Given page 1 is displayed
    When the user presses F7
    Then the first page is re-read and the error reads "No previous pages to display"
    # Source: 0000-MAIN, 2500-SETUP-MESSAGE in COTRTLIC

  Scenario: F8 on the last page
    Given the last page has already been shown
    When the user presses F8 again
    Then the error reads "No more pages to display"
    # Source: 2500-SETUP-MESSAGE in COTRTLIC

  Scenario: F8 reaching the end
    Given exactly 7 more rows remain
    When the user presses F8
    Then those rows are shown, next-page-exists is false and the message state is "No more pages for these search conditions"
    # Source: 8000-READ-FORWARD in COTRTLIC

  Scenario: Exactly 7 rows on a page
    Given 7 rows are fetched
    When the extra FETCH returns +100
    Then CA-NEXT-PAGE-NOT-EXISTS is set so F8 does not advance
    # Source: 8000-READ-FORWARD in COTRTLIC

  Scenario: Concurrent delete before update on the list
    Given row "05" was deleted by another user after the list was shown
    When the administrator saves an update to "05" with F10
    Then the error starts "Record not found. Deleted by others ?"
    # Source: 9200-UPDATE-RECORD in COTRTLIC

  Scenario: Delete of a referenced type
    Given TR_TYPE "01" is referenced by TRANSACTION_TYPE_CATEGORY.TRC_TYPE_CODE (FK ON DELETE RESTRICT)
    When the administrator deletes "01"
    Then SQLCODE -532 is reported with "Please delete associated child records first:"
    # Source: 9300-DELETE-RECORD in COTRTLIC; 9800-DELETE-PROCESSING in COTRTUPC; Input/ddl/TRNTYCAT.ddl lines 6-7

  Scenario: Re-entering the same not-found key
    Given type "12" was not found
    When the administrator presses ENTER again with "12" unchanged
    Then the key is re-read rather than treated as new data
    # Source: 1150-STORE-MAP-IN-NEW, 1200-EDIT-MAP-INPUTS in COTRTUPC

  Scenario: Return from CTTU resets the list
    Given the list was on page 3
    When the administrator presses F2, then F3 on CTTU
    Then CTLI restarts on page 1
    # Source: 0000-MAIN (CDEMO-PGM-ENTER and FROM-PROGRAM <> COTRTLIC) in COTRTLIC
```

## Gaps
- COTRTUPC defines `DATA-WAS-CHANGED-BEFORE-UPDATE` ("Record changed by some one else. Please review") and `FOUND-TRANTYPE-DATA`, but no paragraph sets them; no optimistic-concurrency check exists (9600-WRITE-PROCESSING). No scenario written.
- COTRTUPC SQLCODE -532 on delete sets `RECORD-DELETE-FAILED` but not `TTUP-DELETE-FAILED`, so the screen state after that error is not defined in 3250-SETUP-INFOMSG. Behaviour to confirm during reconciliation.
- COTRTLIC F2 sets `CDEMO-USRTYP-USER` before XCTL to COTRTUPC (0000-MAIN); no authorisation check consumes it in MG-01. Neither program checks user type.
- `Input/cpy/CSSTRPFY.cpy` (PF-key mapping) and `CSSETATY.cpy` were read only for key/attribute setting; no extra scenarios derived.
