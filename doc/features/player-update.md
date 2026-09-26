# Player Partial Update

## Overview

`PUT /api/v{version}/players/{id}` updates an existing player. Any field the caller omits from the request body is left unchanged on the player, with one exception: `secondLastName` is always replaced by the value sent (or cleared if it is absent, `null`, or an empty string). This document covers the partial-update contract and behaviour for API consumers.

### Who it is for

- Front-end and integration teams building an "edit player" screen or script that should not have to resend every field.
- Any consumer of `Gaffer.Api.Client` calling the players update endpoint.

### Use cases

- Update only `firstName` (e.g. fixing a typo) without resending `lastName`, `birthDate`, or `gender`.
- Explicitly send `birthDate: null` or `gender: null` and have the existing value kept (these fields are never cleared by omission or by an explicit `null`; only a real date/gender value replaces them).
- Send every field at once to replace the player's full editable data in one call.
- Clear a player's second last name by sending `secondLastName: null`, `secondLastName: ""`, or omitting it entirely.
- Attempt to update an unknown player id and receive a `404 Not Found` instead of a silent no-op.

### Behaviour

- **Omitted or absent field** (`firstName`, `lastName`, `birthDate`, `gender`): the player's current value is kept.
- **Explicit `null`** for `birthDate` or `gender`: also keeps the current value (these are typed fields on the wire; a `null` cannot represent "clear this date/gender" since the domain does not support an empty date or gender).
- **`secondLastName`** is the one field that does not follow the "omitted → unchanged" rule: it is always set from the request, and absent/`null`/empty-string all mean "clear it". There is no way to leave the second last name untouched other than resending its current value.
- **Unknown player id**: `404 Not Found` with `Extensions.ErrorTranslationKey = "Error.Players.Update.PlayerNotFound"`.
- **`birthDate` and `gender` are optional and nullable in the request schema** (previously required): the published OpenAPI/Swagger contract reflects this, so a generated client no longer forces the caller to supply them.

### Limitations

- `secondLastName` cannot be "left as-is" via omission — this is a deliberate, breaking behaviour choice for this field only, and differs from every other field on this endpoint. Consumers migrating from a full-payload update must resend the current `secondLastName` if they don't intend to change it.
- The response body for a successful update is empty (`200`-class status with no content); the caller must `GET` the player to see the applied result.
- This document only covers the happy path and the domain `404`. For what happens when the database itself is unreachable during this call, see [`error-handling.md`](./error-handling.md).

## See also

- Technical detail: [`doc/features/technical/player-update.md`](./technical/player-update.md).
- Player search/lookup (used internally by the update handler to find the player first): [`player-search.md`](./player-search.md).
- Infrastructure failures during this same endpoint: [`error-handling.md`](./error-handling.md).
- Contract/version-bump guidance for this kind of change: [`doc/features/technical/api-client-versioning.md`](./technical/api-client-versioning.md).
