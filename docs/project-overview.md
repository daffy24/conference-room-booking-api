# Project overview

## Business purpose

Conference Booking lets a venue operator publish conference rooms with capacity, hourly rates and optional services. Customers search by time and capacity, reserve a room and receive a server-calculated price. Administrators maintain the catalogue and inspect bookings through business reports.

The main risks addressed are double booking, inconsistent quotes after price changes, unauthorized catalogue changes and access to another customer's reservation. The implementation keeps the assignment's core workflows explicit and leaves payments, cancellations and more complex venue operations for later requirements.

## Structure and request flow

```text
src/
  Api/AspNetCore/        Controllers, HTTP models, FluentValidation, authentication, OpenAPI
  Core/Application/     Feature handlers, requests, response models, Mapperly adapters, pricing
  Core/Common/          Shared project reserved for common contracts
  Data/Data/            EF Core entities and DbContext
  Data/Postgresql/      Entity configuration, migrations, seed data, provider setup
tests/
  Core/Application.Tests/              Pricing and schedule tests
  Api/AspNetCore.Integration.Tests/     JWT and HTTP/PostgreSQL tests
docker/                 PostgreSQL initialization and development Keycloak realm
```

Project references are `AspNetCore -> Application -> Data -> Common` and `AspNetCore -> Postgresql -> Data`.

A controller accepts an HTTP model, calls its FluentValidation validator, maps it to an application request with Mapperly, and sends it through MediatR. A feature handler performs the use case through `ConferenceBookingDbContext` and returns a response model. There is no repository wrapper or validation pipeline behavior. Authorization is visible on controllers; booking ownership is checked in the query using the validated token's `sub` claim.

Files are organized by feature and responsibility. Small handlers and a separate price calculator keep business decisions testable. Mapperly generates mapping code at build time. Database configuration stays in the PostgreSQL project, and exception handling translates expected failures into HTTP Problem Details without exposing database error text. English comments explain decisions such as concurrency coordination and historical price snapshots.

## Data and consistency decisions

| Data | Purpose |
| --- | --- |
| `rooms` | Current name, capacity, base rate, deletion flag and concurrency version |
| `room_services` | Current service catalogue and prices for each room |
| `bookings` | Owner, room, UTC interval and accepted name/rate/cost snapshots |
| `booking_services` | Accepted service identifiers, names and prices |

A booking saves rental and service subtotals as decimal values. Later room renames, price edits or service removal do not change the confirmed booking. Service snapshot identifiers deliberately have no foreign key to the mutable service catalogue.

PostgreSQL enforces non-overlap for the same room with a GiST exclusion constraint on `tstzrange(starts_at, ends_at, '[)')`. It is effective across application instances and also protects writes that bypass the availability query. The `btree_gist` extension supplies equality support for the room identifier. Adjacent reservations remain valid.

Booking creation also changes the room's optimistic concurrency token. A concurrent room edit, deletion or booking therefore cannot silently use incompatible room data. Booking data and the version change are saved in one EF Core transaction. A conflicting request returns `409`; the client should refresh availability and room details before deciding whether to retry. This can also reject concurrent non-overlapping bookings of the same room, trading a retry for simpler coordination.

Deletion removes a room from future searches while retaining historical bookings. It is rejected when any booking has not yet ended. There is no cancellation workflow in this version.

## Time and pricing

The API requires timestamp offsets and stores instants in UTC. Booking rules and tariff boundaries use `Europe/Kyiv`, independent of the caller's offset. A booking occupies whole minutes during 06:00–23:00 on one local date. Availability uses half-open intervals and does not itself reserve a room.

The tariff calculator splits time at 09:00, 12:00, 14:00 and 18:00. The peak multiplier replaces the standard tariff during 12:00–14:00. It prorates minutes with decimal arithmetic and rounds the combined rental subtotal once. Selected services are added once per booking. The [README](../README.md#business-rules) lists rates and a worked example.

## Reports and business interpretation

Reports are restricted to administrators. Each accepts `from` and `to` as inclusive Kyiv calendar dates, covering at most 366 days including both endpoints, and optionally a `roomId`. Filtering uses the scheduled booking start, not the time at which the reservation was created. The local interval is converted to an inclusive UTC start and exclusive UTC end for database filtering.

| Report | Business question | Measures |
| --- | --- | --- |
| Revenue | How do booked amounts change by day? | Booking count, average booking value, rental/service/total amounts and a daily series |
| Rooms | Which rooms attract demand and booked value? | Booking count, booked hours and booked amounts per room |
| Services | Which optional services are selected and contribute value? | Selection count and booked service amounts per room/service |

The daily revenue series includes zero-booking dates. An unknown room filter produces zero totals or an empty page. Room and service reports are paginated. Rooms are ordered by booked total descending; services by selection count, then booked total, descending. Stable identifiers break ties. Room performance includes active rooms with no bookings, plus deleted rooms that have bookings in the selected period. Historical figures remain based on stored booking prices; room display names use the current catalogue name.

Services are grouped by the stable `(roomId, serviceId)` pair. Two rooms offering a service called "Projector" remain separate products with potentially different prices. Service labels use the most recently created matching booking snapshot, within the selected scheduled period, so renamed services do not split one identifier into multiple report groups.

Report values are **booked amounts in UAH**. A future reservation contributes to its scheduled period, even though no payment has been collected. Without payments, refunds, costs or cancellation states, these values cannot be interpreted as cash received, recognized revenue or profit. Booked hours describe demand; an occupancy percentage is intentionally absent because historical room opening/closure and availability data are not recorded.

## Security and operations

Keycloak owns login and credentials. Swagger and Postman use separate public clients with Authorization Code, PKCE S256 and exact redirect URIs. The API checks access-token signature, algorithm, issuer, audience and expiry through standard JWT middleware. It uses two role policies: `Admin` for catalogue changes/reports and `Customer` (also allowing Admin) for booking creation. Customers receive `404` for another customer's booking.

The local API runs without root privileges; PostgreSQL uses separate application and Keycloak databases and non-superuser roles. Request size is bounded, search/report output is paginated, query timeouts are configured and transient database retries are limited. The health endpoints separate process liveness from database readiness. EF migration bundles run as a deployment job before the API starts; API replicas do not each apply schema changes.

Docker Compose is a review/development configuration. Production work includes HTTPS and trusted proxy configuration, secrets management, database backups, monitoring, request limits, Keycloak hardening and an operational migration/rollback process. Locally verified JWTs remain usable until expiry after a user logs out or loses a role. No claim is made that the sample is already configured for production traffic.

## Verification and extension points

xUnit covers tariff boundaries, minute proration, time-zone behavior, JWT rejection, role policies, ownership and HTTP contracts. Business tests use a separate PostgreSQL database to exercise the actual migrations, constraints and concurrent writes; they are explicitly skipped when its connection string is absent. Reports are tested against persisted price snapshots and period boundaries. [Setup and test commands](../README.md#development-and-verification) are in the README.

Further requirements can be added as feature modules without introducing repository abstractions or splitting into services prematurely. Likely next steps are cancellation and payment states, request idempotency, multiple venue time zones, audit history, and measured database/query tuning. A reliable occupancy report would first require an explicit room availability history.
