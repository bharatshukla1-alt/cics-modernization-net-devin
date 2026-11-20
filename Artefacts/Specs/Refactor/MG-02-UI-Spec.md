# MG-02 — UI Build Spec (Refactor)

## 1. Spec Metadata & Traceability
| Item | Value |
|---|---|
| Trans Group | MG-02 — Sign-on and Navigation Shell |
| Legacy screens | COSGN00/COSGN0A (CC00, COSGN00C) · COMEN01/COMEN1A (CM00, COMEN01C) · COADM01/COADM1A (CA00, COADM01C) |
| Inputs | Screen-metadata.md (mapsets COSGN00, COMEN01, COADM01); MG-02 BSTS; MG-02 BDD; COMEN02Y; COADM02Y; CSMSG01Y |
| Target | React 18 + Vite, Tailwind CSS, BFSI tokens (config/Style-Sheet.md → `src/styles/bfsi-theme.css`), responsive |
| Conversion order | CC00 → CM00 → CA00 |
| User decision (Phase 5) | Sign-on reproduces the legacy plain-text comparison. **Demo only**, and the screen carries a "Demo environment" notice |

## 2. Screen Purpose & User Context
- **Sign in:** the entry point for every CardDemo user. The user enters a user ID and password (Source: BSTS a, Sign-on).
- **Customer services menu:** the regular user picks one of 11 servicing functions (Source: COMEN02Y).
- **Administration menu:** the administrator picks one of 6 user or transaction-type maintenance functions (Source: COADM02Y).

## 3. Screen Rationalization
| Legacy | Target | Note |
|---|---|---|
| COSGN0A sign-on map | `/signin` page | Header items APPLID, SYSID, TRNNAME, PGMNAME, CURDATE and CURTIME are dropped as mainframe artefacts. The date and time appear in the app header. |
| COMEN1A, with 12 option slots and an OPTION entry field | `/menu`: a card grid of clickable function tiles | Typing a number is replaced by clicking a tile. A "Go to option #" number box is kept for keyboard power users, to preserve functional equivalence. |
| COADM1A, with 10 option slots | `/admin`: a tile grid | Same pattern as the customer menu |
| PF3 (Exit) on sign-on | No button. Closing the browser or tab ends the session. | The legacy "Thank you" text is shown on `/signed-out`. |
| PF3 on the menus | "Sign out" button in the header | Returns to `/signin` (Source: RETURN-TO-SIGNON-SCREEN) |
| Enter key | Primary button / tile click | — |
| Invalid AID message | Not applicable: there are no function keys | Recorded as a deviation |

## 4. Layout & Responsive Behaviour
- **Sign in:** a single centred card, max-width 420 px, on the BFSI page background. The brand mark and the title "CardDemo" sit above the form. The demo notice banner sits below the form.
- **Menus:** the app header (brand, signed-in user's name and role badge, Sign out) is followed by a page title and a responsive grid: 1 column under 640 px, 2 columns from 640 px, 3 columns from 1024 px. Tiles are of equal height and gutters follow `--sp-4`. A message region sits above the grid.
- Everything sits on a 12-column grid with the token spacing scale. There are no free-positioned elements.

## 5. Component Inventory
| Component | Purpose |
|---|---|
| `SignInPage` | Form with user ID, password and the Sign in button; shows the error alert |
| `MenuPage` (shared by both menus; `variant="customer" \| "admin"`) | Loads the menu from the API and renders tiles plus the quick-option box |
| `MenuTile` | Option number badge, business name, and a status chip ("Coming soon" / "Not available") when the option is not enabled |
| `QuickOption` | 2-digit numeric input with a Go button (replaces the OPTION field) |
| `AppHeader` | Brand, user display, role badge, Sign out |
| `RequireRole` | Route guard. `/admin` needs the admin role and `/menu` needs the user role. |
| `Alert` | Error (red), info (green) and success messages, using `role="alert"` / `role="status"` |
| `SignedOutPage` | Shows "Thank you for using CardDemo." |

## 6. Field-Level Specification & Presentation-Layer Validation
| Field | Label | Type | Length | Rules (client) | Error message | Source |
|---|---|---|---|---|---|---|
| USERID | User ID | text, autocapitalised, `autocomplete="username"` | max 8 | Required; the value is upper-cased before it is sent | "Enter your user ID." | COSGN00.bms USERID LENGTH=8 (IC); PROCESS-ENTER-KEY |
| PASSWD | Password | password (masked), `autocomplete="current-password"` | max 8 | Required | "Enter your password." | COSGN00.bms PASSWD DRK LENGTH=8 |
| OPTION | Go to option | numeric | max 2 | 1..option count | "Enter a valid option number." | COMEN01/COADM01.bms OPTION NUM LENGTH=2; PROCESS-ENTER-KEY |

The user ID is validated before the password. Only the first failing field shows its message, and focus moves to that field (Source: PROCESS-ENTER-KEY EVALUATE order, cursor `MOVE -1 TO USERIDL/PASSWDL`).

## 7. Interaction & Navigation
| Action | Behaviour | Legacy |
|---|---|---|
| Sign in | `POST /api/v1/auth/sign-in`. On 200 the token is stored in memory and the user is routed by `userType`: 'A' → `/admin`, otherwise → `/menu`. | READ-USER-SEC-FILE, XCTL COADM01C/COMEN01C |
| Sign-in failure, 401 `USER_NOT_FOUND` | "User not found. Try again." Focus moves to User ID. | RESP 13 |
| Sign-in failure, 401 `WRONG_PASSWORD` | "Wrong password. Try again." Focus moves to Password and the field is cleared. | Password mismatch |
| Sign-in failure, 503 | "Unable to verify the user. Try again later." | WHEN OTHER |
| Tile or Go with an enabled option | Navigate to the option's `route`. Migrated groups (e.g. MG-01 admin options 5 and 6 → `/transaction-types`, `/transaction-types/new`) open in the same app. | XCTL option program |
| Option not yet migrated (`status = NOT_INSTALLED`) | Info message "This option <name> is not installed yet." The user stays on the menu. | INQUIRE PROGRAM / PGMIDERR |
| `status = COMING_SOON` | "This option <name> is coming soon." | DUMMY entries |
| `status = ADMIN_ONLY` shown to a regular user | "No access - admin only option." | Admin-only check |
| Sign out | Clear the token and go to `/signed-out` | PF3 → COSGN00C; CCDA-MSG-THANK-YOU |
| Opening `/menu` or `/admin` without a session | Redirect to `/signin` | EIBCALEN = 0 → sign-on |
| Admin opens `/menu`, or a user opens `/admin` | Redirect to the user's own menu | Target hardening (BSTS Gap 4) |

State model: signed-out → signing-in → (menu \| admin) → function → back to menu → signed-out.

## 8. Component States & Content
- **Loading:** the button shows a spinner and is disabled; the menu shows tile skeletons.
- **Empty:** not applicable, because menus always have options.
- **Error:** an inline alert above the form or grid.
- **Demo banner:** "Demo environment — legacy password check. Do not use real credentials."

## 9. Accessibility (WCAG 2.2 AA)
- Labels are bound to their inputs, and errors use `aria-describedby` with `aria-invalid`.
- Focus moves to the first invalid field.
- Tiles are `<a>`/`<button>` elements that can be reached in tab order, with a visible focus ring.
- Colour contrast is at least 4.5:1, including the header Sign out button (MG-01 fix).
- Status is announced via `role="status"`.

## 10. Data Privacy & BFSI Compliance
- The password is never stored, logged or echoed. The field is cleared after a failure.
- The token is held in memory only, not in localStorage.
- Idle timeout is 15 minutes, the same as MG-01.
- The demo banner states that plain-text password comparison is not for production.

## 11. Frontend Folder Structure
```
Target/MG-02/Frontend/
  src/api/authApi.js        src/api/menuApi.js
  src/auth/session.js       (in-memory token, user, role)
  src/components/AppHeader.jsx  MenuTile.jsx  QuickOption.jsx  Alert.jsx  RequireRole.jsx
  src/pages/SignInPage.jsx  MenuPage.jsx  SignedOutPage.jsx
  src/App.jsx  main.jsx  styles/bfsi-theme.css
  tests/*.test.jsx
```

## 12. Acceptance Criteria
1. Blank user ID → "Enter your user ID."; blank password → "Enter your password."
2. ADMIN001/PASSWORD (any case) lands on `/admin` with 6 tiles; USER0001 lands on `/menu` with 11 tiles.
3. Unknown user → "User not found. Try again."; wrong password → "Wrong password. Try again."
4. Admin tiles 5 and 6 open the MG-01 transaction-type screens. Tiles for groups not yet migrated show "not installed yet".
5. The quick option rejects 0, blank and any value above the count.
6. Sign out shows the thank-you page. Afterwards, `/menu` redirects to `/signin`.
7. No program, transaction, map or PF-key names appear anywhere on screen.

## 13. Deviations, Assumptions & Gaps
- D1: number entry becomes clickable tiles, with a quick-option box kept for equivalence.
- D2: there are no AIDs, so there is no invalid-key message.
- D3: the menus enforce role on entry (the legacy code did not).
- D4: the application header drops APPLID, SYSID, transaction and program names.
- A1: plain-text password comparison is kept because the user chose it. Demo only.
- G1: the "Coming soon" and "admin only" states cannot be reached with shipped data. They are implemented and covered by unit tests only.
