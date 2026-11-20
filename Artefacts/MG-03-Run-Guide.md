# MG-03 — Run Guide (User Security Administration)

## One command (Docker)
```
./cics-modernization/Target/MG-03/demo/start-demo.sh
```
This starts PostgreSQL (port 5433, with the MG-01, MG-02 and MG-03 schemas and logins), SignOnService (5001), TransactionTypeService (5000), UserAdminService (5002) and the shell UI (5173). Open http://localhost:5173/signin and sign in as `ADMIN001` / `PASSWORD`. Admin menu options 1–4 open the user screens.
If Docker Hub rate-limits you, prefix the command with `NODE_IMAGE=mirror.gcr.io/library/node:20-alpine`. Stop the demo with `docker compose -f cics-modernization/Target/MG-03/demo/docker-compose.yml down -v`.

## Without Docker
1. Apply `Target/MG-03/Backend/db/least-privilege-useradmin-role.sql` as the database owner (`psql -v useradmin_password=...`).
2. Start the service:
   ```
   cd Target/MG-03/Backend/UserAdminService
   ConnectionStrings__UserAdmin="Host=localhost;Port=5433;Database=carddemo;Username=carddemo_useradmin;Password=..." \
     ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5002 dotnet run --project src/UserAdminService.Api
   ```
3. Run SignOnService (MG-02 run guide) and the shell UI with `npm run dev` in `Target/MG-02/Frontend`. The UI proxies `/api/v1/users` to `USERADMIN_PROXY_TARGET`, which defaults to http://localhost:5002.

## Tests
- **Backend:** run `dotnet test` in `Target/MG-03/Backend/UserAdminService` with `ConnectionStrings__UserAdmin` set. The tests create and remove users prefixed `ZZ`.
- **Frontend:** run `npx vitest run --coverage` in `Target/MG-02/Frontend`. The MG-03 screen tests are in `src/mg03/`.

## Demo script (3 minutes)
1. Sign in as ADMIN001 and choose **User List**: 10 users, then **Next** to page 2, then search `USER0003`.
2. Choose **Add user**, then press Add with everything blank: "First Name can NOT be empty...". Add `user0006` / Ann Lee / password `pw` / User. The message reads "User USER0006 has been added ...".
3. Sign out, then sign in as `USER0006` / `pw`. This works because the ID and password are upper-cased (gap G2 fixed).
4. As ADMIN001, open **Update** on USER0006, change the last name and Save: "User USER0006 has been updated ...". Save again with no change: "Please modify to update ...".
5. **Delete** ADMIN001 (yourself): "You cannot delete your own user ID...". Then delete USER0006 after the confirmation: "User USER0006 has been deleted ...".
