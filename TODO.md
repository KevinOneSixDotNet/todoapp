Dev Setup

1. Chocolately (If Windows)
2. Install NVM -> Latest Node
3. Install .NET Core
4. Workspace Config - Prettier, Linter, Format On Save, Etc
5. Clone Repo
6. npm install
7. build & run dotnet/vue
8. script setup + startup

SqlLite (dotnet add package Microsoft.EntityFrameworkCore.Sqlite)

---

## Notes / Decisions

- Runtime: .NET 10 (machine default) — test spec says ".NET Core" with no version pin.
- Architecture: Controllers + DbContext only. No MediatR, CQRS, or Repository pattern.
- Auth: JWT HS256, 7-day expiry, stored in localStorage. `api.ts` uses window.location.replace for
  401 redirect to avoid circular import between router ↔ store ↔ api.
- DB: SQLite (`todo.db`) — file-based, zero-config for reviewers. Auto-migrate on startup.
- CORS: allows http://localhost:5173 (Vite dev server default).
- DueDate: stored as UTC; frontend parses date portion only to avoid timezone shift on display.
- Optimistic UI: complete toggle updates locally first, reverts if the PUT fails.

---

End-to-end smoke test (register → login → CRUD todos → sign out)

1. Authentication & Route Guarding

Register Happy Path: Create a new user. Does it log you in immediately or take you to login? (Either is fine, just must be smooth).

Register Error Path: Try to register the same username again. Does the UI display a clear error message (e.g., "Username taken"), or does it silently fail/crash?

Login Error Path: Type a wrong password. Does the UI show an error?

The Bouncer Test: Manually type http://localhost:5173/todos in the URL bar while logged out. Does it kick you back to /login?

Log Out: Click sign out. Does it clear the screen, delete the token, and send you to login?

2. Core CRUD & "Instant Updates"

Create: Add a task. CRITICAL: Does it appear in the list instantly without requiring you to hit F5/Refresh?

Delete: Click the trash can. Does it vanish instantly?

Optimistic Toggle: Check the "Complete" box. Does it instantly visually check?

The Failure Revert (Advanced QA): Turn off your .NET API in the terminal. Check a "Complete" box in the UI. Because you built optimistic UI, it should check visually, but then the API request will fail. Does the checkbox revert to unchecked? Does an error pop up?

3. The Validation Traps (The "Nonsense" Checks)

Empty Title: Try to submit a Todo with no title. The frontend should stop you, OR the backend should return a 400, and the UI should display "Title is required."

Nonsense Date: Try to bypass the date picker or send 0001-01-01 via the UI. Does the backend reject it? Does the UI tell the user?

Description Cap: Try to paste a massive block of text (over 2000 characters) into the description/title. Does it cleanly reject it?

The Timezone Trap: Create a task due on June 15, 2026. Look at the list. Does it say June 15? (A classic bug is the backend saving it as UTC midnight, and a US-based browser shifting it backward to June 14 at 8:00 PM).

4. The Data Ownership Trap (The most important test)

Window A: Log in as User A. Create a task called "User A's Secret Task". Note the Task's GUID (from your console or Swagger/.http file).

Window B (Incognito): Log in as User B.
Do you see "User A's Secret Task" in the list? (You better not).
Open your .http file. Put User B's token at the top. Write a DELETE request pointing to User A's Task ID. Hit send.
Did it return 200/204? (If yes, you fail). Did it return 403 Forbidden or 404 Not Found? (If yes, you pass).

5. UI "Clear and Usable" Check

Does the UI have loading states? (e.g., does the "Add" button disable while the network request is inflight?)

Do your filter tabs (All / Active / Completed) accurately filter the array in Vue?

Is the overdue badge showing up correctly for tasks where the due date is in the past?
