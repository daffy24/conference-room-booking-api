# Postman collection

One collection covers authentication, room management, availability, bookings and administrator reports. Requests include example bodies, response checks and automatic ID capture.

## Import and sign in

1. Start the stack using the [repository quick start](../README.md#quick-start).
2. Import [ConferenceBooking.postman_collection.json](ConferenceBooking.postman_collection.json) and [ConferenceBooking.Local.postman_environment.json](ConferenceBooking.Local.postman_environment.json) into Postman Desktop. The combined collection replaces the previous Authentication and Reports collections.
3. Select the **Conference Booking · Local** environment.
4. Open the **Conference Booking** collection's **Authorization** tab and choose **Get New Access Token**.
5. Sign in as **booking-admin** on the Keycloak page, return to Postman and select **Use Token**. The administrator can run every folder.
6. Send the current-user request in **02 · Authentication** and inspect **Test Results**.

| Application user | Password in the root `.env` | Role |
| --- | --- | --- |
| `booking-admin` | `DEMO_ADMIN_PASSWORD` | `Admin` |
| `booking-customer` | `DEMO_CUSTOMER_PASSWORD` | `Customer` |

The Keycloak `admin` account in the `master` realm is for server administration. Use one of the application accounts above for API requests. To switch users, repeat **Get New Access Token → Use Token**; `prompt=login` requests a fresh login form.

Protected requests inherit the collection's token. Public health/discovery requests and deliberate authentication failures override authorization. Access tokens expire after five minutes; refresh the token or obtain a new one if an authenticated request returns `401`.

## Try the complete workflow

Run the folders in order, either by sending individual requests or using **Run collection** with one iteration:

| Folder | What it checks |
| --- | --- |
| **01 · Health** | API liveness and database readiness |
| **02 · Authentication** | OpenID Connect discovery, current identity, missing token and invalid token |
| **03 · Rooms** | Create a demo room, read it, update its rate/services and search availability |
| **04 · Bookings** | Create a booking, read its calculated price and reject an overlapping duplicate with `409` |
| **05 · Reports** | Booked revenue by day, room performance and service popularity |
| **06 · Room deletion** | Create a separate unbooked room, delete it with `204` and confirm it returns `404` |

The room and booking responses populate `room_id`, `projector_id`, `wifi_id` and `booking_id` in the collection's variables. Deletion uses its own `delete_room_id`. Run room creation before dependent requests; there is no need to copy IDs manually.

The workflow creates a new demo room and a real booking in the selected database, preserving the seeded rooms. Each complete rerun creates another room and booking. The booked room remains: the API has no booking cancellation endpoint and rejects deletion while a booking is future or ongoing. The final folder demonstrates deletion with a separate unbooked room.

To check customer permissions, select a token for **booking-customer**. Customers can read rooms, search availability and create/read their own bookings. Room writes and reports return `403`; reading an administrator's booking returns `404`. To create a customer booking after the administrator run, choose another available time. The full collection's expected-success assertions assume **booking-admin**, so role-denial checks can be inspected manually.

## Dates and reports

The collection fills blank date variables with a working example. Change them in the collection's **Variables** tab to try another scenario:

| Variable | Default / purpose |
| --- | --- |
| `starts_at` | Tomorrow at `08:00:00Z`, which falls within Kyiv opening hours |
| `duration_minutes` | `240`; the availability end is derived from this duration |
| `report_from` | Booking date, as an inclusive Kyiv date in `YYYY-MM-DD` format |
| `report_to` | Same date; extend it for a report period of up to 366 days |

Defaults only fill blank values. For a later run, change an old `starts_at` to a future timestamp or clear it to generate a new default. When changing the booking date, update or clear both report dates as well. Bookings must fit within one Kyiv day, between 06:00 and 23:00, and use whole minutes. Keep `Z` or an explicit UTC offset in timestamps.

Reports cover the bookings' scheduled dates, not their creation dates. Future reservations contribute booked amounts; these are not payment receipts. Empty periods return zero daily amounts, and active rooms can appear with zero bookings. See [report definitions](../docs/project-overview.md#reports-and-business-interpretation).

For a single-room report, enable the `roomId` parameter in the request's **Params** tab; it uses `room_id`. Leave it disabled for all rooms. Room and service reports default to `page=1&pageSize=20`.

## OAuth configuration

The collection uses Authorization Code with PKCE, S256 and **Authorize using browser**. Leave the client secret and manual code verifier empty. The environment contains only public configuration:

| Variable | Value |
| --- | --- |
| `base_url` | `http://localhost:5080` |
| `authority` | `http://localhost:8080/realms/conference-booking` |
| `client_id` | `conference-booking-postman` |
| `callback_url` | `https://oauth.pstmn.io/v1/browser-callback` |

The exact callback is permitted by the bundled Keycloak client. An `Invalid parameter: redirect_uri` error means the actual callback and the client's allowed redirect URIs do not match. Realm imports skip existing realms; update an older local realm through the Admin Console using `docker/keycloak/conference-booking-realm.json`.

No passwords or real tokens are included in the exported files. Enter passwords on the Keycloak page, keep tokens in Postman's local OAuth manager and leave **Share Token** disabled. Newman/CLI execution requires a token supplied at runtime; it does not perform the interactive browser login automatically.
