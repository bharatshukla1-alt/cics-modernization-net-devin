# MG-03 — UI Build Spec (Refactor)

## 1. Spec Metadata & Traceability
| Item | Value |
|---|---|
| Trans Group | MG-03 — User Security Administration |
| Legacy maps | COUSR0A/COUSR00 (List Users), COUSR1A/COUSR01 (Add User), COUSR2A/COUSR02 (Update User), COUSR3A/COUSR03 (Delete User) |
| Target | React 18 + Vite + Tailwind screens in `Target/MG-03/Frontend/src`, hosted by the MG-02 shell (alias `@mg03`) under `/admin/users` |
| Style | `config/Style-Sheet.md` BFSI theme, reusing the MG-01/MG-02 components (cards, tables, alerts, ConfirmDialog) |

## 2. Screen Inventory & Navigation
| Route | Screen | Legacy | Admin menu option |
|---|---|---|---|
| `/admin/users` | User List | COUSR00 | 1 User List |
| `/admin/users/new` | Add User | COUSR01 | 2 User Add |
| `/admin/users/update` and `/admin/users/update/:userId` | Update User | COUSR02 | 3 User Update |
| `/admin/users/delete` and `/admin/users/delete/:userId` | Delete User | COUSR03 | 4 User Delete |

SignOnService `Menu:Routes` maps COUSR00C–COUSR03C to these routes, so the admin tiles become AVAILABLE. Every screen has a "Back to Administration" link (PF3). All routes use the shell's admin guard.

## 3. Screen Specifications
**User List (COUSR00).**
- Title "List Users" with a page number.
- "Search user ID" box (8 characters, upper-cased) with a Search button (Enter).
- A table with columns User ID, First Name, Last Name and Type (Admin/User badge).
- Each row has **Update** and **Delete** buttons. These replace typing U or D in the Sel column; the invalid-selection error can no longer happen.
- **Previous** and **Next** buttons (PF7/PF8) are disabled at the ends. The legacy end-of-list messages are shown as an info alert if the API returns them.
- An **Add user** button opens Add User.

**Add User (COUSR01).**
- Fields in legacy order:
  - First Name (20)
  - Last Name (20)
  - User ID (8, upper-cased as typed)
  - Password (8, masked)
  - User Type, a select with Admin (A) or User (U)
- Create (Enter), Clear (PF4), Cancel (PF12 / PF3).
- Client-side required checks run in legacy order and show the legacy message for the first failure, with focus on that field.
- On success a green alert shows "User <id> has been added ..." and the form clears.

**Update User (COUSR02).**
- An "Enter user ID" box with Fetch (Enter) is shown when there is no `:userId`.
- When fetched, First Name, Last Name, Password and User Type can be edited. The Password field is blank, with the hint "leave blank to keep the current password".
- Info line: "Press Save to save your updates ...".
- Buttons:
  - Save (PF5)
  - Save & exit (PF3)
  - Clear (PF4)
  - Cancel (PF12)
- Success shows "User <id> has been updated ...".
- `NO_CHANGES` shows "Please modify to update ...".
- `STALE_USER` offers a reload.

**Delete User (COUSR03).**
- Fetch works the same way. First Name, Last Name and Type are shown read-only, with the line "Press Delete to delete this user ...".
- **Delete** (PF5) opens a ConfirmDialog. The dialog is a target-only safety step.
- Success shows "User <id> has been deleted ..." and returns to the list.
- `CANNOT_DELETE_SELF` and `LAST_ADMIN` are shown as error alerts.

## 4. Validation & Messages
Client checks repeat the server rules: required order, lengths, type A/U. The server is authoritative, and its `message` is shown as is. Messages use the legacy text listed in the Backend spec R5–R12 and G1–G4.

## 5. Accessibility & UX
- Every field has a label.
- Errors are linked to fields with `aria-describedby`, and focus moves to the first invalid field.
- Alerts use `role="alert"`.
- Tables have a caption.
- Every action is reachable with the keyboard.

## 6. API Integration
`api/usersApi.js` calls `/api/v1/users…` with the shell's in-memory bearer token. It sends `If-Match` with the fetched version on update and delete. On a 401 the shell signs the user out.

## 7. Acceptance Criteria
- Each screen renders the legacy fields and messages.
- Row actions open the right screen.
- Paging works.
- Every server error code shows its message.
- A regular user can't reach any `/admin/users` route.
- Unit tests cover all four screens, with at least 70% line coverage.
