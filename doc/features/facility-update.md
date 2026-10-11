# Facility Update (Stands Matched by Id)

## Overview

`PUT /api/v{version}/facilities/{id}` updates an existing facility. The facility's own fields (`name`, `active`, `buildDate`, `type`, `capacity`) and its address are always replaced by the value sent in the request — there is no partial merge at that level, and the address keeps its original internal id. Stands are the exception: each stand in the request is matched against the facility's existing stands by an optional `id`, so a caller can update one stand, add a new one, or leave every stand untouched, without resending the full set. This document covers the stand-matching contract and behaviour for API consumers.

### Who it is for

- Front-end and integration teams building an "edit facility" screen that lets an operator rename a stand or add a new one without resending every stand the facility already has.
- Any consumer of `Gaffer.Api.Client` calling the facilities update endpoint.

### Use cases

- Rename a facility, change its capacity/type, or update its street address in one call while leaving its existing stands untouched (omit `stands`, or send `null`/an empty list).
- Update just one stand's name or capacity by sending its known `id`; every other existing stand is kept as-is.
- Add a new stand (up to 4 total) by sending a stand without an `id` while the facility currently has fewer than 4 stands.
- Resend a stand payload that includes an unrecognized or stale `id` (e.g. from another facility) once the facility already has 4 stands, without the call failing or an existing stand being overwritten — it is silently ignored.
- Attempt to update an unknown facility id and receive a `404 Not Found` instead of a silent no-op.

### Behaviour

- **Facility fields and address**: always replaced by the request body. The address's own `id` is preserved (updated in place, not replaced).
- **Stand sent with the `id` of an existing stand of this facility**: updated in place (`standName`/`standCapacity`); its `id` never changes.
- **Stand sent without an `id`, or with an `id` that does not match any existing stand of this facility** (unknown or belonging to another facility):
  - If the facility currently has **fewer than 4 stands**: created as a new stand with a freshly generated `id` (any `id` sent for it is ignored).
  - If the facility currently has **4 stands already**: silently discarded — no error, and the rest of the update (fields, address, other matched stands) still applies.
- **An existing stand not mentioned in the request**: kept unchanged. Stands are never deleted by this endpoint.
- **`stands` omitted, `null`, or an empty list**: every existing stand is kept unchanged.
- **The `id` of an existing stand repeated more than once** in the request: `400 Bad Request`, `Extensions.ErrorTranslationKey = "Error.Facilities.Update.DuplicatedStand"`; nothing is updated.
- **Merging would leave the facility with more than 4 stands**: `400 Bad Request`, `Extensions.ErrorTranslationKey = "Error.Facilities.Create.Validation"` (the same 4-stand invariant enforced at facility creation); nothing is updated.
- **Unknown facility id**: `404 Not Found` with `Extensions.ErrorTranslationKey = "Error.Facilities.Update.FacilityNotFound"`.
- The whole update (facility fields, address, stand updates/inserts) runs as a single database transaction: if any part fails, nothing is changed.

### Limitations

- No stand can be removed through this endpoint, even if that was the caller's intent; use `DELETE /api/v{version}/facilities/{facilityId}/stands/{standId}` ([`facility-stands.md`](./facility-stands.md)).
- Concurrent changes to the stands of one facility are serialized: the update locks the facility row (`SELECT ... FOR UPDATE`) and reads it inside the same transaction, so two concurrent `PUT` calls cannot together push the facility above 4 stands. The second request waits and decides on the committed state.
- Individual stands can be added, replaced and removed through the dedicated stands endpoints: [`facility-stands.md`](./facility-stands.md).
- The response body for a successful update is empty; the caller must `GET` the facility to see the applied result (matching the [player update](./player-update.md) endpoint).
- This document only covers the happy path and the domain `400`/`404` cases above. For what happens when the database itself is unreachable during this call, or when the client cancels the request mid-flight, see [`error-handling.md`](./error-handling.md).

## See also

- Technical detail: [`doc/features/technical/facility-update.md`](./technical/facility-update.md).
- Player partial update, which uses a different (field-level, non-merge) update model: [`player-update.md`](./player-update.md).
- Infrastructure failures and request cancellation during this same endpoint: [`error-handling.md`](./error-handling.md).
- Per-request database connection and transaction used by this update: [`database-connection-reliability.md`](./database-connection-reliability.md).
