# CardDemo MG-02 demo: sign-on, menus and MG-01 transaction types

One command starts the whole modernised slice. It runs PostgreSQL with the MG-01 and MG-02 schemas, **SignOnService** (.NET 9, :5001), the MG-01 **TransactionTypeService** (.NET 9, :5000), and the **CardDemo shell UI** (React, :5173).

## Run
Prerequisites: Docker with Compose v2, and `openssl`. On Windows, use Git Bash or WSL.
```
./cics-modernization/Target/MG-02/demo/start-demo.sh
```
The first run creates `demo/.env` with random database passwords and `demo/keys/signon.pem` with the token signing key. Both are git-ignored.

When the script prints "CardDemo demo is up", open http://localhost:5173/signin.

| User | Password | Lands on |
|---|---|---|
| ADMIN001 – ADMIN005 | PASSWORD | Administration menu (6 options) |
| USER0001 – USER0005 | PASSWORD | Customer services menu (11 options) |

Stop with `docker compose -f cics-modernization/Target/MG-02/demo/docker-compose.yml down`. Add `-v` to reset the database.

If Docker Hub rate-limits the pull, set `NODE_IMAGE=mirror.gcr.io/library/node:20-alpine` before running the script.

## 5-minute demo script
1. **Sign in validation:** click Sign in with empty fields, then enter a user ID only. The legacy validation order is kept: user ID first, then password.
2. **Wrong password:** sign in as `USER0001` / `WRONG`. You get "Wrong password. Try again." and the password field is cleared.
3. **Customer services:** sign in as `user0001` / `password`. Credentials are not case-sensitive, as on the mainframe. 11 tiles appear. Click *Account View*: it shows "not installed yet" because MG-04 isn't migrated. Type `12` in *Go to option* to see the range check.
4. **Sign out:** you get the thank-you page. Opening `/menu` afterwards returns to sign-in.
5. **Administration:** sign in as `ADMIN001`. Options 5 and 6 are available. Click *Transaction Type List/Update* to open the MG-01 screens with the same token. Filter, edit and add as in the MG-01 demo.
6. **Security talking points:**
   - RS256 bearer tokens that expire after 15 minutes.
   - MG-01 trusts SignOnService's published signing keys, and its old dev token is rejected.
   - A regular user gets 403 from MG-01.
   - The sign-in limit is 5 per minute per client.
   - Every sign-in writes an audit event.
   - The database login can only read `usrsec`.

## Notes
- This is a local demo. Both APIs run in Development mode (HTTP, no TLS). Production settings are enforced by `StartupGuards`: TLS 1.3, an https issuer, a mounted signing key and explicit host names.
- Passwords are compared in plain text, matching the legacy USRSEC file. This was a user decision for the demo, and the service refuses to start this way outside Development.
