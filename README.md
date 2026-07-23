# DailyMart

Mini departmental store management system - suppliers, purchasing, inventory, barcode POS sales, customer
dues, and audit logging. See `CLAUDE.md` for architecture/conventions and
`Mini_Departmental_Store_Modular_Monolith_Requirements.txt` for the full business requirements.

## Quick start (Docker)

Requires only [Docker Desktop](https://www.docker.com/products/docker-desktop/) - nothing else needs to be
installed locally (no .NET SDK, no Node.js, no Postgres).

```bash
docker-compose up --build
```

That single command builds and starts three containers:

| Service | What it is                              | Port                              |
|---------|------------------------------------------|------------------------------------|
| `db`    | PostgreSQL 16, with a persistent volume  | internal only (not published)      |
| `api`   | The .NET 8 backend                       | http://localhost:5299 (Swagger at `/swagger`) |
| `web`   | The Angular app, served by nginx         | **http://localhost:4200**          |

On first boot the API automatically applies all EF Core migrations and seeds a default admin user - there
is no manual database setup step, and no local secrets file to create either (see below).

Open **http://localhost:4200** - the login form is pre-filled with the default admin credentials, so a
first run is just "open the page, click Sign in":

- **Username:** `admin`
- **Password:** `Admin@123456`

Change this password via `POST /api/auth/change-password` before using this for anything beyond local
evaluation (there's no change-password screen in the UI yet).

To stop everything: `docker-compose down` (add `-v` to also delete the database volume and start fresh).

The JWT secret, admin credentials, and CORS origin the `api` container needs are supplied directly in
`docker-compose.yml` (with the same defaults as above) - not read from the gitignored
`appsettings.Development.json` described below, which won't exist on a fresh clone. Override any of them by
creating a top-level `.env` file with `JWT_SECRET` / `ADMIN_USERNAME` / `ADMIN_PASSWORD` - docker compose
picks it up automatically, no changes to `docker-compose.yml` needed.

### How the pieces talk to each other

The Angular app is built for production with a relative API base URL (`/api`), and nginx (`web`'s container)
reverse-proxies any `/api/*` request straight to the `api` container. The browser only ever talks to one
origin (`:4200`), so this works with zero CORS configuration in production - CORS is only needed (and only
configured) for the `ng serve`-based local dev workflow below, where the frontend and backend really are on
different origins (`:4200` and `:5299`).

## Local development (without Docker)

Useful if you're actively changing code and want hot reload, rather than rebuilding a container image on
every change.

Prerequisites: .NET 8 SDK, Node.js 22+, and a PostgreSQL 16 instance reachable at the connection string in
`backend/src/DailyMart.API/appsettings.json` (or override it, e.g. via `docker run -p 5432:5432 -e
POSTGRES_PASSWORD=postgres postgres:16-alpine`, then create a `dailymart` database on it).

`appsettings.Development.json` is gitignored (it holds the JWT secret and default admin credentials, so
it's a local file, not something pulled from git) - copy the committed template once before your first run:

```bash
cp backend/src/DailyMart.API/appsettings.Development.json.example backend/src/DailyMart.API/appsettings.Development.json
```

Then fill in a real `Jwt:Secret` (any long random string) and pick whatever `Admin:DefaultPassword` you
want the seeded admin account to start with.

```bash
# Backend - applies migrations automatically on startup, same as the Docker path
cd backend
dotnet run --project src/DailyMart.API

# Frontend (separate terminal)
cd frontend/dailymart-ui
npm ci
npm start   # ng serve on http://localhost:4200, talking to the API on http://localhost:5299
```

Run the test suites with `dotnet test` (from `backend/`) and `npm test` (from `frontend/dailymart-ui/`).
