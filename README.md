# Conference Booking API

ASP.NET Core API for managing conference rooms, checking availability and booking rental time with optional services. Administrators maintain the room catalogue and use reports to understand booked revenue, room demand and service popularity.

Built with .NET 10, PostgreSQL 18, EF Core, Keycloak, FluentValidation, Mapperly, MediatR and xUnit. The API, database, identity provider and migration job run together in Docker.

## Quick start

Prerequisites: Git and Docker with Compose v2. On Windows, use Docker Desktop with Linux containers. A local .NET SDK is only needed for development and tests.

Run from a new checkout:

```powershell
git clone https://github.com/daffy24/conference-room-booking-api.git
cd conference-room-booking-api
Copy-Item .env.example .env
```

On macOS/Linux, use `cp .env.example .env` for the last command. Replace **all six** values in `.env` with different, randomly generated passwords before starting. Use at least 32 random letters/digits for each password; this also avoids connection-string and Compose interpolation characters. Generate them with a password manager. `.env` is excluded from Git and the Docker build context; `.env.example` contains placeholders only.

```powershell
docker compose up --build -d --wait
```

Compose starts PostgreSQL, applies EF Core migrations through a one-shot `migrations` service, imports the development Keycloak realm, and starts the API after its dependencies are ready. An exited migration container with exit code `0` is expected. The first build and Keycloak startup can take a few minutes.

| Service | Local address |
| --- | --- |
| Swagger UI | [localhost:5080/swagger](http://localhost:5080/swagger/index.html) |
| OpenAPI document | [localhost:5080/openapi/v1.json](http://localhost:5080/openapi/v1.json) |
| Liveness / database readiness | [health/live](http://localhost:5080/health/live) / [health/ready](http://localhost:5080/health/ready) |
| Keycloak Admin Console | [localhost:8080/admin](http://localhost:8080/admin) |
| Application PostgreSQL | `localhost:55432`, database/user `conference_booking` |

All published ports bind to loopback. PostgreSQL persists data in a named Docker volume.

```powershell
docker compose logs -f api
docker compose stop
```

Stopping preserves data. Re-run the startup command with `--build` after code changes. Database initialization and realm import apply to a new database/realm: editing `.env` or the realm JSON does not change existing role or user passwords. Update existing credentials through PostgreSQL or the Keycloak Admin Console.

## Sign in and try the API

Open Swagger using **localhost**, select **Authorize**, keep the `openid` scope, and sign in through Keycloak. Swagger uses Authorization Code with PKCE; no client secret is needed. Call `GET /api/identity/me` to inspect the authenticated identity.

| Account | Username | Password source | Purpose |
| --- | --- | --- | --- |
| Application administrator | `booking-admin` | `.env`: `DEMO_ADMIN_PASSWORD` | Manage rooms, book rooms and read reports |
| Application customer | `booking-customer` | `.env`: `DEMO_CUSTOMER_PASSWORD` | Search rooms and access own bookings |
| Keycloak bootstrap administrator | `admin` | `.env`: `KEYCLOAK_ADMIN_PASSWORD` | Manage Keycloak in the `master` realm |

The Keycloak bootstrap administrator is separate from the application administrator. The other three `.env` variables configure PostgreSQL administration (`POSTGRES_PASSWORD`), the application database role (`APP_DB_PASSWORD`) and the Keycloak database role (`KEYCLOAK_DB_PASSWORD`).

Access tokens expire after five minutes. Obtain a new token if an authenticated request returns `401`. For Postman, import the [combined collection](postman/ConferenceBooking.postman_collection.json) and [local environment](postman/ConferenceBooking.Local.postman_environment.json). It covers authentication, room CRUD, availability, bookings and reports, with IDs passed between requests automatically. Follow the [Postman setup and workflow](postman/README.md) and sign in as `booking-admin` to run every folder.

## Endpoints

Business endpoints require a validated access token. Room reads and availability search accept authenticated users; administrative actions require the `Admin` role. The `Customer` policy also allows administrators to make bookings.

| Method | Route | Access / result |
| --- | --- | --- |
| `GET` | `/api/identity/me` | Authenticated identity |
| `POST` | `/api/rooms` | Admin; creates a room (`201`) |
| `GET` | `/api/rooms/{id}` | Authenticated; room and current service prices |
| `PATCH` | `/api/rooms/{id}` | Admin; updates supplied fields |
| `DELETE` | `/api/rooms/{id}` | Admin; soft-deletes a room (`204`) |
| `GET` | `/api/rooms/available` | Authenticated; paginated availability search |
| `POST` | `/api/bookings` | Customer or Admin; booking and calculated price (`201`) |
| `GET` | `/api/bookings/{id}` | Booking owner or Admin |
| `GET` | `/api/reports/revenue` | Admin; totals and daily booked revenue |
| `GET` | `/api/reports/rooms` | Admin; paginated room performance |
| `GET` | `/api/reports/services` | Admin; paginated service popularity |

Swagger provides request and response schemas. Validation errors use HTTP `400`; missing/invalid credentials use `401`, insufficient roles use `403`, missing or inaccessible bookings use `404`, and booking overlaps or concurrent changes use `409`. Error responses use Problem Details with a trace identifier.

For `PATCH`, omitted or `null` fields retain their current values. A supplied `services` array replaces the complete service list; `[]` removes all services. Include an existing service's `id` to rename it or change its price. Deleting a room is rejected while it has a future or ongoing booking; historical bookings remain accessible.

Availability takes `startsAt`, `endsAt`, minimum `capacity`, `page` (default `1`) and `pageSize` (default `20`, maximum `100`). Supply ISO 8601 timestamps with `Z` or an explicit offset. In query strings, encode `+` as `%2B`, or let Swagger/Postman encode it. Availability is a point-in-time result; a subsequent reservation can still return `409`.

### Initial data and a booking example

Migrations seed the following rooms. Each initially offers Projector (`500` UAH), Wi-Fi (`300` UAH) and Sound (`700` UAH), with Ukrainian display names where applicable.

| Room | Capacity | Base rate, UAH/hour | ID |
| --- | ---: | ---: | --- |
| Зал А | 50 | 2,000 | `10000000-0000-0000-0000-000000000001` |
| Зал B | 100 | 3,500 | `10000000-0000-0000-0000-000000000002` |
| Зал C | 30 | 1,500 | `10000000-0000-0000-0000-000000000003` |

Retrieve a room to obtain its service identifiers. For example, send this body to `POST /api/bookings`, replacing the date with an available future date and the offset with the offset applicable to that date:

```json
{
  "roomId": "10000000-0000-0000-0000-000000000001",
  "startsAt": "2030-01-15T11:00:00+02:00",
  "durationMinutes": 240,
  "serviceIds": ["20000000-0000-0000-0000-000000000011"]
}
```

With the initial room and projector prices, 11:00–15:00 Kyiv time costs `8,600` UAH for rental plus `500` UAH for the projector: `9,100` UAH total. The response contains the booking ID, rental and service subtotals, total, currency and service snapshots. Identity and prices are determined by the server.

## Business rules

- Booking hours are **06:00–23:00 in `Europe/Kyiv`**, within one local calendar day. Time-zone conversion includes daylight-saving rules. New bookings must start in the future and use whole minutes.
- Services are charged **once per booking**. Only services offered by the selected room can be booked.
- Each portion of rental time uses its own tariff. Fractional hours are prorated by minutes; the final rental subtotal is rounded to two decimal places, midpoint away from zero. All monetary values use decimal arithmetic and UAH.
- Intervals include their start and exclude their end: 10:00–11:00 and 11:00–12:00 can coexist.
- Confirmed bookings retain their room name, hourly rate and service price snapshots after catalogue changes.

| Kyiv time | Rental multiplier |
| --- | ---: |
| 06:00–09:00 | `0.90` |
| 09:00–12:00 | `1.00` |
| 12:00–14:00 | `1.15` |
| 14:00–18:00 | `1.00` |
| 18:00–23:00 | `0.80` |

### Reports

All reports accept inclusive Kyiv dates `from` and `to` in `YYYY-MM-DD` format, with a maximum range of 366 days and an optional `roomId`. Room and service reports also support `page` and `pageSize` (maximum `100`).

For example, `GET /api/reports/revenue?from=2030-01-01&to=2030-01-31` reports reservations scheduled in January 2030. Revenue reports show booked amounts and a daily series, including dates with zero bookings. Room reports compare bookings, booked hours and booked revenue; service reports show selections and the associated service amounts. These support pricing, room investment and service planning decisions.

Amounts come from confirmed booking snapshots, selected by the booking's start date. Future bookings are included when their scheduled dates are in the requested range. These are **booked amounts, not payments received or accounting profit**: payment and cancellation workflows are outside the current scope. Deleted rooms retain their historical results. See [report definitions and technical decisions](docs/project-overview.md) for interpretation details.

## Development and verification

Install the .NET 10 SDK compatible with [global.json](global.json). Commands below use PowerShell and run from the repository root.

```powershell
dotnet restore ConferenceBooking.sln
dotnet build ConferenceBooking.sln --configuration Release --no-restore
dotnet format ConferenceBooking.sln --verify-no-changes --no-restore
dotnet test ConferenceBooking.sln --configuration Release --no-build
```

The xUnit suite includes pure pricing/schedule tests and HTTP integration tests. Authentication tests use actual JWT validation with a test RSA signing key; they do not require a running Keycloak server. PostgreSQL business integration tests are **skipped** unless `CONFERENCE_BOOKING_TEST_CONNECTION_STRING` is configured. A default test run therefore does not verify database scenarios.

To run the full suite, start the stack and create a **fresh test database** for the run:

```powershell
$testDatabase = 'conference_booking_tests_' + [guid]::NewGuid().ToString('N')
docker compose exec -T postgres psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $testDatabase OWNER conference_booking;"
$env:CONFERENCE_BOOKING_TEST_CONNECTION_STRING = "Host=localhost;Port=55432;Database=$testDatabase;Username=conference_booking;Password=<APP_DB_PASSWORD>;Timeout=5"
dotnet test ConferenceBooking.sln --configuration Release --no-build
Remove-Item Env:CONFERENCE_BOOKING_TEST_CONNECTION_STRING
```

Replace `<APP_DB_PASSWORD>` with the local value from `.env`. Do not point tests at `conference_booking` or `keycloak`. Fixtures serialize schema setup and then run requests concurrently; they apply migrations and write test records without creating, dropping or resetting databases. A fresh database keeps ranking assertions independent of earlier runs. The suite covers validation, authorization, ownership, price snapshots, database constraints, overlapping reservations and reporting.

After inspecting the results, remove the disposable database created by the commands above:

```powershell
docker compose exec -T postgres psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE $testDatabase;"
```

### Run from Rider or the .NET CLI

After the initial Docker startup has applied migrations, stop only the API container and configure the development connection string:

```powershell
docker compose stop api
dotnet user-secrets set 'ConnectionStrings:App' 'Host=localhost;Port=55432;Database=conference_booking;Username=conference_booking;Password=<APP_DB_PASSWORD>;Timeout=5' --project src/Api/AspNetCore
dotnet run --project src/Api/AspNetCore --launch-profile http
```

Replace the password placeholder with the local application database password. In Rider, run the `AspNetCore` project using its `http` launch profile. PostgreSQL and Keycloak stay in Docker. User-secrets are for local development and are not mounted into containers.

For new migrations, restore the pinned EF tool and use the PostgreSQL project:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/Data/Postgresql --startup-project src/Api/AspNetCore
```

Replace `<MigrationName>` with the migration's name. Rebuild the Docker stack to run the migration job. The API itself does not migrate the database on startup.

## Design and deployment scope

The solution separates HTTP modules, application use cases and PostgreSQL configuration. Controllers call FluentValidation explicitly, Mapperly generates mappings, and MediatR dispatches focused handlers. Handlers use EF Core directly. PostgreSQL prevents overlapping reservations with an exclusion constraint, while an optimistic room version coordinates booking with edits and deletion. See the [short project overview](docs/project-overview.md) for the structure, data model, trade-offs and extension points.

The supplied Compose configuration is for local review: HTTP, Keycloak `start-dev` and demo users. Production deployment needs HTTPS and correct public identity URLs, managed secrets, restricted administration, backups, monitoring and request limiting. The API validates JWT signatures, issuer, audience and lifetime, and requires HTTPS Keycloak metadata outside Development. Short-lived tokens do not provide immediate session revocation. Pagination, bounded report periods, asynchronous database access, constraints and timeouts provide a foundation for growth; load testing and operational configuration are still required for a production workload.
