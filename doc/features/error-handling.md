# Error Handling and Logging

## Overview

This document covers how `Gaffer.Api` classifies and reports failures that are not simple "not found" cases: infrastructure failures (e.g. the database is unreachable) while deleting or updating a Player/Facility, a non-permitted search filter field on Facilities, and server-side logging of every non-`404` error. It is aimed at API consumers who need to know what status code and body to expect, and at operators who need to know what gets logged.

### Who it is for

- Front-end and integration teams that need to distinguish "your request was invalid" (`400`) from "something went wrong on our side" (`500`), and know that a `404` is not logged as an error.
- Operators monitoring `Gaffer.Api` logs for unexpected failures.

### Use cases

- A client deletes or updates a Player/Facility while the database is unreachable and needs to know this surfaces as `500`, not as a misleading `400`.
- A client sends a Facility search filter on a field that isn't allowed (mirroring the existing Player search behaviour) and gets a `400` with a translation key, without any query reaching the database.
- An operator checks the server logs after a spike of `500`s and expects to find one error-level entry per failed request, with the original exception attached.
- An operator does not want routine `404 Not Found` responses (e.g. "player not found") to be logged as errors and flood the logs.

### Behaviour

- **Infrastructure failure during Delete/Update** (Player or Facility): if the repository throws anything other than the specific "not found" exception for that operation (e.g. the database is unreachable), the failure is not wrapped into a `400`. It propagates unchanged and is reported as `500 Internal Server Error` with a generic body, exactly like an infrastructure failure during a `GET`/search already did.
- **Facility search — non-permitted filter field**: `POST` search paths that build a Facility filter now reject an unknown/blank filter field before running any query, the same way Player search already did. The response is `400 Bad Request` with `Extensions.ErrorTranslationKey = "Error.Search.FilterNotPermitted"`.
- **Server-side logging**: every error response the middleware produces is logged once, at `Error` level, with the original exception attached — except `404 Not Found`, which is never logged. This applies both to `500`s (unmapped exceptions) and to `400`s produced by a recognized/mapped exception (e.g. the search-filter case above).
- **Error title on an unmapped failure**: the generic `500` body's `Title` is `"Unknown error"` (previously a misspelled `"Unkwnown error"`; no client code depended on the old spelling).

### Limitations

- The four generic "cannot delete/update" exceptions that would map to a `400` (`Error.Players.Delete.*`, `Error.Players.Update.*`, `Error.Facilities.Delete.*`, `Error.Facilities.Update.*` with `Reason = "Internal Server Error"`) are kept in the error catalogue for backward compatibility, but no production code path throws them anymore; in practice, an infrastructure failure on these operations is always a `500`, never that `400` shape. See the technical document for the full exception-to-status table.
- Logging every mapped `400` at `Error` level (not `Warning`) can be noisy for expected client mistakes; this was a deliberate choice for this iteration, not an oversight.
- This document does not cover validation errors produced by ASP.NET Core model binding (e.g. `[MaxLength]` on a request DTO), which use a different, framework-generated body shape and are out of scope here.

## See also

- Technical detail: [`doc/features/technical/error-handling.md`](./technical/error-handling.md).
- Player partial update, which relies on the same infrastructure-failure behaviour: [`player-update.md`](./player-update.md).
- Player search and its pre-existing non-permitted-filter behaviour, now mirrored by Facility search: [`player-search.md`](./player-search.md).
- Per-request database connection lifecycle (what "the database is unreachable" means at runtime): [`database-connection-reliability.md`](./database-connection-reliability.md).
