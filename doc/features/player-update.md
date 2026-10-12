# Player Partial Update

## Overview

`PUT /api/v{version}/players/{id}` updates an existing player. Any field the caller omits from the request body (or sends as `null`) is left unchanged on the player, including `secondLastName`. Every field that is sent is validated (see [`input-validation.md`](./input-validation.md)). This document covers the partial-update contract and behaviour for API consumers.

### Who it is for

- Front-end and integration teams building an "edit player" screen or script that should not have to resend every field.
- Any consumer of `Gaffer.Api.Client` calling the players update endpoint.

### Use cases

- Update only `firstName` (e.g. fixing a typo) without resending `lastName`, `birthDate`, or `gender`.
- Explicitly send `birthDate: null` or `gender: null` and have the existing value kept (these fields are never cleared by omission or by an explicit `null`; only a real date/gender value replaces them).
- Send every field at once to replace the player's full editable data in one call.
- Change only the second last name, or leave it untouched by omitting it or sending `null`.
- Send an invalid value (for example `firstName: ""` or a birth date in the future) and receive a `400` that lists every invalid field sent.
- Attempt to update an unknown player id and receive a `404 Not Found` instead of a silent no-op.

### Behaviour

- **Omitted or absent field** (`firstName`, `lastName`, `birthDate`, `gender`): the player's current value is kept.
- **Explicit `null`** for `birthDate` or `gender`: also keeps the current value (these are typed fields on the wire; a `null` cannot represent "clear this date/gender" since the domain does not support an empty date or gender).
- **`secondLastName`** follows the same rule as the other fields: absent or `null` keeps the current value. A value that is `""` or only whitespace is rejected with `400`. A body without fields changes nothing.
- **Sent fields are validated** with the player rules (names 1-100 characters and not only whitespace, `birthDate` between 1900-01-01 and today and at least 16 years old, `gender` a defined value). Only fields that were sent are checked; all invalid ones are reported together and nothing is saved. Response: `400`, `Extensions.ErrorTranslationKey = "Error.Players.Update.Validation"`, `Extensions.Errors` per field. Details: [`input-validation.md`](./input-validation.md).
- **Unknown id with an invalid body**: `404` (the player is looked up before the body is checked).
- **Existing data is not revalidated**: a `PUT` that does not send an invalid stored field (for example a stored name of 101 characters) does not fail because of it.
- **Unknown player id**: `404 Not Found` with `Extensions.ErrorTranslationKey = "Error.Players.Update.PlayerNotFound"`.
- **`birthDate` and `gender` are optional and nullable in the request schema** (previously required): the published OpenAPI/Swagger contract reflects this, so a generated client no longer forces the caller to supply them.

### Limitations

- The second last name can no longer be cleared through this endpoint: omitting it or sending `null` keeps it, and `""` is a `400`. This changes the behaviour of the earlier version (where omitted/`null`/`""` cleared it) and was an operator decision (TECHDEBT-005); no replacement way of clearing it was added. The request schema did not change, so the generated client is not affected, but consumers that relied on clearing it must be told.
- The response body for a successful update is empty (`200`-class status with no content); the caller must `GET` the player to see the applied result.
- This document only covers the happy path, the domain `404` and the validation `400`. For what happens when the database itself is unreachable during this call, see [`error-handling.md`](./error-handling.md).

## See also

- Validation rules and the `400` format: [`input-validation.md`](./input-validation.md).
- Technical detail: [`doc/features/technical/player-update.md`](./technical/player-update.md).
- Player search/lookup (used internally by the update handler to find the player first): [`player-search.md`](./player-search.md).
- Infrastructure failures during this same endpoint: [`error-handling.md`](./error-handling.md).
- Contract/version-bump guidance for this kind of change: [`doc/features/technical/api-client-versioning.md`](./technical/api-client-versioning.md).
