# Function Health — To-Do Task Manager

A full-stack to-do application built as a take-home exercise. A .NET 10 Web API
with JWT authentication and EF Core / SQLite persistence, paired with a Vue 3 +
TypeScript single-page frontend.

The emphasis throughout is on **finished, secure, validated features** over
architectural ceremony — the code is deliberately kept at an appropriate
"altitude" for a CRUD app of this size (see [Architecture & Trade-offs](#architecture--trade-offs)).

---

## Tech Stack

| Layer        | Choice                                                                 |
| ------------ | --------------------------------------------------------------------- |
| **Backend**  | .NET 10 Web API (controllers), EF Core 10, SQLite                     |
| **Auth**     | JWT bearer tokens (HS256), BCrypt password hashing                    |
| **Frontend** | Vue 3 (Composition API) + TypeScript, Vite, Tailwind CSS, Pinia, Axios |
| **Testing**  | xUnit + `WebApplicationFactory` integration tests over SQLite `:memory:` |

---

## Project Structure

```
todoapp/
├── api/                       # .NET 10 Web API
│   ├── Controllers/           # AuthController, TodosController
│   ├── Data/                  # AppDbContext (+ UTC normalization hook)
│   ├── DTOs/                  # Request/response records with validation attributes
│   ├── Migrations/            # EF Core migrations (InitialCreate)
│   ├── Models/                # User, Todo entities
│   └── Program.cs             # DI, JWT, CORS, global error handler, startup migrate
│
├── api.tests/                 # xUnit integration tests
│   └── TodoIntegrationTests.cs
│
├── site/                      # Vue 3 + TypeScript frontend
│   └── src/
│       ├── views/             # LoginView, RegisterView, TodosView
│       ├── stores/            # Pinia auth store
│       ├── api.ts             # Axios instance (JWT interceptor + 401 redirect)
│       ├── router.ts          # Routes + auth navigation guard
│       └── types.ts           # Shared TypeScript types
│
└── todoapp.slnx               # Solution (api + api.tests)
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 18+ (LTS recommended) and npm

---

## Getting Started

The backend and frontend run as two separate processes. Open two terminals.

### 1. Backend API

```bash
cd api
dotnet restore
dotnet run
```

The API starts on **http://localhost:5010**. On first run it automatically
applies EF Core migrations and creates `todo.db` (SQLite) in the `api/` folder —
no manual database setup required.

> OpenAPI document is served at `http://localhost:5010/openapi/v1.json` in
> Development.

### 2. Frontend

```bash
cd site
npm install
npm run dev
```

The app is served on **http://localhost:5173**. The dev server is pre-configured
(via `site/.env`) to talk to the API at `http://localhost:5010`.

### 3. Use the app

Open http://localhost:5173, register an account, and start managing tasks.

---

## Running the Tests

```bash
dotnet test
```

This runs the integration test suite (`api.tests`). The tests spin up the real
API in-process via `WebApplicationFactory<Program>`, backed by a SQLite
`:memory:` database, and exercise the HTTP surface end-to-end (register → obtain
JWT → call protected endpoints). See [Testing Strategy](#testing-strategy).

---

## API Reference

All `/api/todos` endpoints require an `Authorization: Bearer <token>` header.

| Method   | Endpoint              | Auth | Description                                   |
| -------- | --------------------- | ---- | --------------------------------------------- |
| `POST`   | `/api/auth/register`  | —    | Create account, returns `{ token, username }` |
| `POST`   | `/api/auth/login`     | —    | Authenticate, returns `{ token, username }`   |
| `GET`    | `/api/todos`          | ✅   | List the current user's todos                 |
| `GET`    | `/api/todos/{id}`     | ✅   | Get one todo (owned by current user)          |
| `POST`   | `/api/todos`          | ✅   | Create a todo                                 |
| `PUT`    | `/api/todos/{id}`     | ✅   | Update a todo (title, description, due, done) |
| `DELETE` | `/api/todos/{id}`     | ✅   | Delete a todo                                 |

Validation failures return **400** with an RFC 7807 `ProblemDetails` body.
Unauthenticated requests return **401**. Accessing another user's resource
returns **404** (see [Security](#security)).

---

## Architecture & Trade-offs

The guiding principle was to match complexity to the problem. This is a
single-aggregate CRUD application, so it is structured as **Controllers talking
directly to `DbContext`**.

- **No Repository / Unit-of-Work pattern.** `DbContext` *is* already a unit of
  work and `DbSet<T>` *is* already a repository. Wrapping them adds indirection
  and test surface without buying anything here.
- **No MediatR / CQRS.** There are no independent read/write scaling concerns or
  cross-cutting pipeline behaviors that would justify the indirection. Controller
  actions are short and readable as-is.
- **DTOs over entities at the boundary.** Requests/responses use dedicated
  `record` types (`DTOs/`) rather than exposing EF entities. This prevents
  over-posting (e.g. a client setting `UserId`), keeps `PasswordHash` off the
  wire, and lets validation attributes live on the API contract.
- **SQLite.** Zero-config and file-based, so a reviewer can clone and run with no
  database server. The EF Core provider is swappable — moving to SQL
  Server/Postgres is a connection-string and provider change, not a code change.

### Key implementation details

- **UTC discipline.** `AppDbContext` overrides `SaveChanges`/`SaveChangesAsync`
  to force every `Todo.DueDate` to `DateTimeKind.Utc` before persisting. SQLite
  has no native `datetimeoffset`, so normalizing on write avoids "kind drift."
  The frontend formats only the date portion to avoid timezone shift on display.
- **Startup migration.** `Program.cs` runs `db.Database.Migrate()` on boot so the
  schema is always current in dev. (In production I'd run migrations as an
  explicit deploy step instead — noted in [Future Work](#future-work).)
- **Global error handling.** A single exception handler returns `ProblemDetails`
  and only includes exception detail in Development.

---

## Security

- **Passwords** are hashed with **BCrypt** (work factor default). Plaintext is
  never stored or logged.
- **JWTs** are signed HS256, expire after 7 days, and carry the user id as
  `NameIdentifier`.
- **Ownership isolation** is enforced at the data layer: every todo query is
  scoped with `WHERE UserId == <caller>`. A user requesting another user's todo
  receives **404 Not Found** rather than 403 — we don't confirm the resource even
  exists, which avoids leaking information. This is covered by an integration test.
- **CORS** is locked to the Vite dev origin (`http://localhost:5173`).

> ⚠️ The JWT signing key in `appsettings.json` is a placeholder. Before any real
> deployment, replace it with a strong secret supplied via environment variable
> or a secrets manager — never commit the production key.

---

## Testing Strategy

The test project favors a small number of **high-value integration tests** over a
large number of mock-heavy unit tests. For a CRUD API the real risk lives in the
seams — routing, auth, model binding, validation, and the EF query layer — and
those are exactly what unit tests with mocked `DbContext`s tend to miss.

- Tests run against `WebApplicationFactory<Program>` (the real pipeline) backed by
  **SQLite `:memory:`** rather than the EF Core *InMemory* provider. The InMemory
  provider is not a relational database and silently ignores constraints, FKs, and
  real SQL translation — SQLite `:memory:` enforces them, so tests fail for the
  same reasons production would.
- A single `SqliteConnection` is held open for the factory's lifetime (an
  in-memory database is destroyed when its last connection closes).
- Each test registers real users through the API and uses the returned JWT,
  so auth is exercised, not bypassed.

Two tests encode the most important guarantees:

1. **Ownership / data isolation** — User A cannot read or delete User B's todo
   (asserts 404), and User B's data remains intact.
2. **Input validation** — creating a todo with an empty title returns 400 with a
   `ProblemDetails` body.

---

## Assumptions

- **Username + password** is sufficient identity for this exercise (no email
  verification, password reset, or OAuth).
- **A todo belongs to exactly one user**; there is no sharing or collaboration.
- **Due date is a calendar date**, conceptually — time-of-day is not part of the
  product requirement, so the UI works at day granularity.
- **Single-region, low-concurrency** usage is assumed, which is what makes SQLite
  and startup-migration appropriate defaults for the take-home.

---

## Future Work

Given more time, in roughly priority order:

- **Production database & migrations.** Swap SQLite for Postgres/SQL Server and
  run migrations as an explicit, gated deploy step rather than on app startup.
- **Refresh tokens & shorter-lived access tokens** for safer session handling,
  plus token revocation on logout.
- **Pagination, sorting, and server-side filtering** on `GET /api/todos` for users
  with large lists.
- **Rate limiting** on the auth endpoints to blunt credential-stuffing.
- **Structured logging / observability** (request correlation IDs, metrics).
- **More test coverage** — update/complete flows, auth edge cases, and a handful
  of frontend component tests (Vitest) plus an E2E happy-path (Playwright).
- **CI pipeline** to run `dotnet test` and `npm run build` on every push.
- **Containerization** (Docker Compose) so the whole stack starts with one command.
```
