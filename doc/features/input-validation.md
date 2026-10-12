# Input Validation (Players, Facilities, Addresses and Stands)

## Overview

The create and update operations for players, facilities (with their address and nested stands) and the stands sub-resource reject invalid input with a `400 Bad Request` before anything is saved. The `400` uses one format for every operation: a `ProblemDetails` body with a translation key per operation and the list of invalid fields with a message each. All the invalid fields of a request are reported together, not one at a time. This document covers the rules and the response for API consumers.

### Who it is for

- Front-end and integration teams that validate forms against the API and need to show one message per field.
- Any consumer of `Gaffer.Api.Client` that sends data to the operations listed below.

### Use cases

- Create a player and get every wrong field flagged at once (for example a too-short last name and a future birth date).
- Send a partial player update and have only the fields that were sent checked.
- Create or update a facility with an address and stands and learn which stand (by position) is wrong.
- Send malformed JSON or an unknown enum text to one of these operations and receive the same `400` format instead of the default ASP.NET one.

### Operations covered

| Operation | Translation key (`errorTranslationKey`) | `title` |
|---|---|---|
| `POST /players` | `Error.Players.Create.Validation` | Cannot create player |
| `PUT /players/{id}` | `Error.Players.Update.Validation` | Cannot update player |
| `POST /facilities` | `Error.Facilities.Create.Validation` | Cannot create facility |
| `PUT /facilities/{id}` | `Error.Facilities.Update.Validation` | Cannot update facility |
| `POST` and `PUT /facilities/{facilityId}/stands...` | `Error.Facilities.Stands.Validation` | Cannot create stand / Cannot update stand |

### Response format

- RFC 7807 body (TECHDEBT-006): `type`, `title`, `status`, `detail` and `instance` in camelCase, with `errorTranslationKey` and `errors` at the root. There is no `extensions` object.
- `status` `400`, `detail` = `One or more fields are not valid.`
- `errorTranslationKey`: the key of the table above.
- `errors`: an object whose property names are the invalid fields in camelCase per path segment (`firstName`, `address.street`, `stands[1].standName`) and whose values are lists of messages.
- The messages keep naming the field in PascalCase (`LastName must be at most 100 characters long.`, `Stands[1].StandName must not be empty ...`), so the key and the text differ in capitalisation; that is intended (messages are unchanged).
- The messages never repeat the value that was received.

```json
{
  "type": "",
  "title": "Cannot create player",
  "status": 400,
  "detail": "One or more fields are not valid.",
  "instance": "/api/v1.0/players",
  "errorTranslationKey": "Error.Players.Create.Validation",
  "errors": {
    "lastName": ["LastName must be at most 100 characters long."],
    "birthDate": ["BirthDate must not be in the future."]
  }
}
```

Before TECHDEBT-006 the same body was PascalCase with `Extensions.ErrorTranslationKey` and `Extensions.Errors`; consumers of the old shape must be updated ([`error-handling.md`](./error-handling.md)).

A `null` element in the `stands` list of a facility create or update (for example `"stands": [null]`) answers this same `400`, with the error under `errors["stands[0]"]` (index of the element) and the message `Stands[0] is required.`, together with the other field errors; previously it answered `500`. A `null` element counts toward the maximum of 4.

### Rules per field

Text fields are never trimmed: a value made only of spaces is rejected, and the length is that of the value sent.

| Entity | Field | Rule |
|---|---|---|
| Player | `FirstName`, `LastName` | Required, not empty or only whitespace, at most 100 characters |
| Player | `SecondLastName` | Optional; if sent, not empty or only whitespace and at most 100 characters |
| Player | `BirthDate` | Required, on or after 1900-01-01, not in the future (UTC date) and the player must be at least 16 years old (a 29 February birth date turns 16 on 28 February) |
| Player | `Gender` | A defined value of the enum |
| Facility | `Name` | Required, not empty or only whitespace, at most 100 characters |
| Facility | `BuildDate` | Required, on or after 1800-01-01, not in the future (UTC date) |
| Facility | `Type` | A defined value of the enum |
| Facility | `Capacity` | 0 or more (no maximum) |
| Facility | `Stands` | At most 4 in the request (error under `Stands`) |
| Address | `Address` | Required (error under `Address`) |
| Address | `Address.Street`, `Address.City`, `Address.Region`, `Address.Country` | Required, not empty or only whitespace, at most 100 characters |
| Address | `Address.StreetNumber`, `Address.PostalCode` | Required, not empty or only whitespace, at most 20 characters (no per-country format) |
| Stand | `StandName` | Required, not empty or only whitespace, at most 100 characters |
| Stand | `StandCapacity` | 0 or more |

- In a facility request the stand fields are named by their position in the list: `Stands[0].StandName`, `Stands[1].StandCapacity`. In the stands sub-resource they are `StandName` and `StandCapacity`. The rule tables name fields as in the request model (PascalCase); in the response `errors` the keys are camelCase (`stands[0].standName`).
- `PUT /players/{id}` checks only the fields that were sent (see [`player-update.md`](./player-update.md)). `PUT /facilities/{id}` replaces the facility fields and address, so those are always checked; only the stands that are sent are checked (see [`facility-update.md`](./facility-update.md)).

### Malformed requests

For the six operations above, errors raised before the rules are evaluated (malformed JSON, an unknown enum text, a value of the wrong type) answer the same `400` format and key. Each affected field is listed with the generic message `The value is not valid.` (the received value is never echoed); a body that cannot be read at all is reported under `body`. Any other endpoint keeps the default ASP.NET `400`.

### Limitations

- Existing data is not revalidated: players, facilities, addresses and stands saved before these rules are still read and returned even if they break them (for example a 101-character name). Rules apply when a record is created or modified; a `PUT` of a facility keeps stored stands it does not mention without checking them.
- Which error comes first depends on the operation: `PUT /players/{id}` and `PUT /facilities/{id}` answer `404` for an unknown id before checking the body; the stands sub-resource (`POST`/`PUT .../stands`) checks the body (`400`) before looking the facility up (`404`).
- The `400` is not declared in the OpenAPI document, so the generated client does not describe it.
- Rules are checked only by length, range and presence: there is no uniqueness of names, no sum-of-capacities rule and no format check on postal codes.
- Other `400`s keep their own keys: `Error.Facilities.Update.DuplicatedStand`, `Error.Facilities.Stands.Create.MaxStandsExceeded` (see [`facility-update.md`](./facility-update.md), [`facility-stands.md`](./facility-stands.md)).

## See also

- Technical detail: [`technical/input-validation.md`](./technical/input-validation.md).
- Other error shapes and the database/cancellation failures: [`error-handling.md`](./error-handling.md).
- Operation-specific behaviour: [`player-update.md`](./player-update.md), [`facility-update.md`](./facility-update.md), [`facility-stands.md`](./facility-stands.md).
