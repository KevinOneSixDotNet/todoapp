# Function Health - Task Tracker

A full-stack to-do application: a .NET 10 Web API with JWT auth and EF Core /
SQLite persistence, paired with a Vue 3 + TypeScript single-page frontend.

---

## How to Run

The backend and frontend run as two separate processes. Open two terminals.

**Prerequisites:** .NET 10 SDK and Node.js 18+ (with npm).

**API** (starts on http://localhost:5010):

```bash
cd api
dotnet run
```

The SQLite database auto-migrates on startup — `todo.db` is created in `api/`
on first run, so there is no manual database setup.

**Frontend** (starts on http://localhost:5173):

```bash
cd site
npm install
npm run dev
```

Then open http://localhost:5173, register an account, and start managing tasks.

**Tests:**

```bash
dotnet test
```

---

## Tech Stack

| Layer        | Choice                                                                  |
| ------------ | ---------------------------------------------------------------------- |
| **Backend**  | .NET 10 Web API (controllers), EF Core 10, SQLite                      |
| **Auth**     | JWT bearer tokens (HS256), BCrypt password hashing                     |
| **Frontend** | Vue 3 (Composition API) + TypeScript, Vite, Tailwind CSS, Pinia, Axios |
| **Testing**  | xUnit + `WebApplicationFactory` integration tests over SQLite `:memory:` |

---

## API Reference

All `/api/todos` endpoints require an `Authorization: Bearer <token>` header.

| Method   | Endpoint             | Auth | Description                                   |
| -------- | -------------------- | ---- | --------------------------------------------- |
| `POST`   | `/api/auth/register` | —    | Create account, returns `{ token, username }` |
| `POST`   | `/api/auth/login`    | —    | Authenticate, returns `{ token, username }`   |
| `GET`    | `/api/todos`         | ✅   | List the current user's todos                 |
| `GET`    | `/api/todos/{id}`    | ✅   | Get one todo (owned by current user)          |
| `POST`   | `/api/todos`         | ✅   | Create a todo                                 |
| `PUT`    | `/api/todos/{id}`    | ✅   | Update a todo (title, description, due, done) |
| `DELETE` | `/api/todos/{id}`    | ✅   | Delete a todo                                 |

Validation failures return **400** with an RFC 7807 `ProblemDetails` body.
Unauthenticated requests return **401**. Requesting another user's resource
returns **404** (we don't confirm it exists — see Security).

---

## Architectural Philosophy & Scope

I scoped this as a production-minded backend slice rather than a feature-heavy
todo app. I focused on auth, ownership boundaries, validation, typed API
interaction, persistence, and integration tests. For a healthcare system, the
next step would not be more UI features first; it would be auditability, token
hardening, PHI-safe logging, and operational controls.

In keeping with that, the backend is deliberately kept at the right altitude for
a single-aggregate CRUD app — **controllers talk directly to `DbContext`**:

- **No Repository / Unit-of-Work and no MediatR / CQRS.** `DbContext` is already
  a unit of work and `DbSet<T>` is already a repository; wrapping them would add
  indirection without buying anything here.
- **DTOs at the boundary.** Requests/responses use dedicated `record` types
  rather than EF entities — this prevents over-posting (e.g. a client setting
  `UserId`), keeps `PasswordHash` off the wire, and hosts the validation attributes.
- **UTC discipline.** `AppDbContext` normalizes every `DueDate` to
  `DateTimeKind.Utc` on save; the frontend renders the date portion only to avoid
  timezone drift.

---

## Security

- **Passwords** hashed with **BCrypt**; plaintext is never stored or logged.
- **JWTs** signed HS256, 7-day expiry, carrying the user id as `NameIdentifier`.
- **Ownership isolation** enforced at the data layer: every todo query is scoped
  `WHERE UserId == <caller>`, so cross-user access returns **404**, not 403 —
  avoiding information disclosure. Covered by an integration test.
- **CORS** locked to the Vite dev origin (`http://localhost:5173`).

> ⚠️ The JWT key in `appsettings.json` is a placeholder. Before any real
> deployment, supply a strong secret via environment variable or secrets manager.

---

## Testing Strategy

I intentionally prioritized backend integration tests around validation and
ownership because those are the highest-risk areas for this role and this domain.
The Vue layer is intentionally thin and manually demoable. With more time, I would
add Vitest coverage around optimistic UI rollback and form validation, plus one
Playwright happy path.

The integration tests run against `WebApplicationFactory<Program>` backed by
SQLite `:memory:` (not the EF Core InMemory provider, which ignores relational
constraints), and exercise real auth by registering users and using the returned JWT.

---

## Future Work (Production Roadmap)

**First sprint before real-user traffic:**

- Audit timestamps (`CreatedAt`/`UpdatedAt`) and access logging.
- Refresh-token flow with an HttpOnly cookie (replacing localStorage).
- Production database (SQL Server / PostgreSQL) and structured migrations.
- Rate limiting on auth endpoints.
- PHI-safe structured logging (e.g. Serilog).

**Next tier for scale:**

- Pagination, filtering, and sorting.
- Request correlation IDs.
- CI pipeline for automated testing.
- Containerized local setup (Docker Compose).
