# FunctionHealth Take-Home — Task Tracker

## Status Legend
- [ ] Not started
- [~] In progress
- [x] Done

---

## Phase 1 — Scaffold & Foundation ✅
- [x] Scaffold .NET 10 Web API (`api/`) with controllers
- [x] Add NuGet packages (EF Core SQLite, EF Core Design, JWT Bearer, BCrypt)
- [x] Scaffold Vue 3 + TypeScript project (`site/`) via Vite
- [x] Install Tailwind CSS v3, PostCSS, Autoprefixer, Axios, Vue Router 4, Pinia
- [x] Write `Models/User.cs` and `Models/Todo.cs`
- [x] Write `Data/AppDbContext.cs` with UTC DueDate normalization + cascade delete
- [x] Write `Program.cs` (SQLite, JWT, CORS for localhost:5173, auto-migrate on startup)
- [x] Write `appsettings.json` (connection string + JWT key placeholder)
- [x] Write `tailwind.config.js` with Function Health brand theme
- [x] Write `src/style.css` with Tailwind directives
- [x] Write `src/api.ts` Axios instance with JWT + 401 redirect (window.location, avoids circular import)
- [x] Write `src/router.ts` stub (Vue Router)
- [x] Create initial EF Core migration (`InitialCreate`)
- [x] Verified: `dotnet build` → 0 errors, 0 warnings
- [x] Verified: `npm run build` → clean production build

## Phase 2 — Backend Controllers ✅
- [x] `DTOs/AuthDtos.cs` — AuthRequest (validated), AuthResponse
- [x] `DTOs/TodoDtos.cs` — TodoRequest, TodoUpdateRequest, TodoResponse
- [x] `AuthController` — POST /api/auth/register, POST /api/auth/login (returns JWT)
- [x] `TodosController` — GET all, GET by id, POST, PUT, DELETE — all scoped to JWT user
- [x] Input validation via DataAnnotations + ModelState.IsValid checks
- [x] Global exception handler in Program.cs (RFC 7807 ProblemDetails, stack trace in dev only)
- [x] Verified: `dotnet build` → 0 errors, 0 warnings

## Phase 3 — Frontend Views & State ✅
- [x] Pinia auth store (`stores/auth.ts`) — token, username, login, register, logout
- [x] `src/types.ts` — Todo interface
- [x] Expand `src/router.ts` — all routes + auth guard (redirects unauth → /login, auth → /todos)
- [x] `src/main.ts` — Pinia + Router wired into app
- [x] `src/App.vue` — brand nav (Function Health serif header, sign out when authed)
- [x] `views/LoginView.vue` — form, error display, link to register
- [x] `views/RegisterView.vue` — form with confirm password, error display
- [x] `views/TodosView.vue` — full CRUD: create, list (filtered), inline edit, delete, complete toggle
  - [x] Optimistic complete toggle (reverts on API failure)
  - [x] Filter tabs: All / Active / Completed with counts
  - [x] Overdue badge (red) on past-due incomplete tasks
  - [x] Due date formatted locale-safe (no timezone shift)
  - [x] Loading state + empty states per filter
  - [x] Page-level + field-level error display
- [x] `site/.env` — VITE_API_URL=http://localhost:5010
- [x] Verified: `npm run build` → 0 TS errors, clean production build (96 modules)

## Phase 4 — Polish & Submission
- [ ] End-to-end smoke test (register → login → CRUD todos → sign out)
- [ ] `.gitignore` review (exclude todo.db, dist, obj, bin)
- [ ] README.md — setup steps, assumptions, scalability notes, future work
- [ ] Final review: no console errors, no lint errors
- [ ] Push to GitHub, verify public link

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
