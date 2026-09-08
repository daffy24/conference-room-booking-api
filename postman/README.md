# Postman collections

Two focused collections share one local environment: authentication checks and administrator reports. Room management and booking request schemas are available in [Swagger UI](http://localhost:5080/swagger/index.html).

## Authentication setup

1. Start the stack using the [repository quick start](../README.md#quick-start).
2. Import [ConferenceBooking.Auth.postman_collection.json](ConferenceBooking.Auth.postman_collection.json) and [ConferenceBooking.Local.postman_environment.json](ConferenceBooking.Local.postman_environment.json) into Postman Desktop.
3. Select the **Conference Booking · Local** environment.
4. Open the collection's **Authorization** tab and choose **Get New Access Token**.
5. Sign in on the Keycloak page, return to Postman and select **Use Token**.
6. Send **02 · Identity → Current user · 200** and inspect **Test Results**.

| Application user | Password in the root `.env` | Expected role |
| --- | --- | --- |
| `booking-customer` | `DEMO_CUSTOMER_PASSWORD` | `Customer` |
| `booking-admin` | `DEMO_ADMIN_PASSWORD` | `Admin` |

The Keycloak `admin` account in the `master` realm is for server administration, not application API access. To switch application users, repeat **Get New Access Token → Use Token**; the configured `prompt=login` requests a fresh login form.

## OAuth configuration

The imported collection uses Authorization Code with PKCE, S256 and **Authorize using browser**. Leave client secret and manual code verifier empty. The environment contains only public configuration:

| Variable | Value |
| --- | --- |
| `base_url` | `http://localhost:5080` |
| `authority` | `http://localhost:8080/realms/conference-booking` |
| `client_id` | `conference-booking-postman` |
| `callback_url` | `https://oauth.pstmn.io/v1/browser-callback` |

The exact callback is permitted by the bundled Keycloak client. An `Invalid parameter: redirect_uri` error means the actual callback and the client's allowed redirect URIs do not match. Realm imports skip existing realms, so an older local realm must be updated through the Admin Console using the configuration in `docker/keycloak/conference-booking-realm.json`.

No passwords or real tokens are included in the exported files. Enter passwords on the Keycloak page, keep tokens in Postman's local OAuth manager and leave **Share Token** disabled. Access tokens expire after five minutes; obtain a new token when the authenticated request returns `401`.

## Authentication requests

| Request | Expected result |
| --- | --- |
| API is alive | `200`, `Healthy`; independent of PostgreSQL |
| Database is ready | `200`, `Healthy`; `503` while PostgreSQL is unavailable |
| Current user | `200`, subject, username and assigned application role |
| No token | `401`, authentication challenge |
| Invalid token | `401`, without internal token-validation details |
| OpenID Connect discovery | `200`, matching issuer and PKCE S256 support |

After signing in, use **Run collection** to execute all six requests. Negative requests override collection authorization deliberately; **Current user** should inherit it. Newman/CLI execution does not perform the interactive browser login automatically.

To call room and booking endpoints manually in Postman, add a request with **Inherit auth from parent**, using the routes and schemas in Swagger. A customer should receive `403` when creating/editing/deleting a room or reading a report. Booking reads are restricted to the owner or an administrator. The xUnit integration suite verifies these policies alongside the business workflows.

## Administrator reports

1. Import [ConferenceBooking.Reports.postman_collection.json](ConferenceBooking.Reports.postman_collection.json) and select the same **Conference Booking · Local** environment.
2. Open the report collection's **Variables** tab. Set `report_from` and `report_to` to inclusive Kyiv dates in `YYYY-MM-DD` format, covering no more than 366 days. They are intentionally blank in the export.
3. In this collection's **Authorization** tab, obtain and select a token for **booking-admin**. Selecting a token in the authentication collection does not automatically select one here.
4. Send **Booked revenue by day**, **Room performance** or **Service popularity**. These are read-only `GET` requests and include basic response assertions.

For a single room, set the `room_id` collection variable and enable the `roomId` parameter in the request's **Params** tab. Leave that parameter disabled for all rooms. Room and service requests default to `page=1&pageSize=20`; increase the page to inspect more results.

Choose a period containing the bookings' scheduled dates, not their creation dates. Revenue and service totals reflect stored booking prices, including future reservations. Empty booking periods return zero daily amounts; active rooms can still appear with zero results. See [report definitions](../docs/project-overview.md#reports-and-business-interpretation).