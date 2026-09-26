# Player Search and Lookup

## Overview

This document covers how clients search for players and how the API reports errors when a search or a single-player lookup does not resolve. It is aimed at API consumers (front-end teams, integrators) who call the Players endpoints of `Gaffer.Api`.

- `POST /api/v{version}/players/search` searches players by an optional list of `Name`/`Value` filters (`Id`, `FirstName`, `LastName`).
- `GET /api/v{version}/players/{id}` looks up a single player by id, internally reusing the same search with an `Id` filter.

### Who it is for

- Front-end and integration teams building player search/lookup screens or scripts against the Gaffer API.
- Any consumer that needs to know which filter fields are supported and what error shape to expect.

### Use cases

- Search all players (no filters).
- Search players by `Id`, `FirstName`, or `LastName` (case-insensitive field name, e.g. `firstname` or `FIRSTNAME` both work).
- Search by a value containing a single quote (e.g. last name `O'Neill`) without the request failing.
- Look up one player by id; distinguish "found" from "not found" via HTTP status alone.

### Behaviour

- **Allowed filter fields**: only `Id`, `FirstName`, `LastName` are accepted, matched case-insensitively. Any other filter field name (including an empty/blank one) is rejected before any SQL is executed.
- **Rejected filter field**: the API responds `400 Bad Request` with a `ProblemDetails` body (`Title`, `Detail`, and an `Extensions.ErrorTranslationKey` of `Error.Search.FilterNotPermitted`). No query runs against the database for that request.
- **Values with special characters** (e.g. an apostrophe) are matched safely; they no longer need any special handling by the caller.
- **Unknown player** (`GET /players/{id}` for an id with no match): the API responds a bare `404 Not Found` — only the `Status` (and the request `Instance` path) are set; there is no `Title`, `Detail`, or `ErrorTranslationKey`. Callers should treat any 404 from this endpoint as "not found" without parsing a body for details.

### Limitations

- The filter field whitelist is fixed to `Id`, `FirstName`, `LastName`; requesting a filter on any other `Player` column (e.g. `Gender`, `BirthDate`) is rejected, not silently ignored.
- A filter with a `null`/absent `Value` does not match any row (it compares as `= NULL`), so it cannot be used to mean "no filter on this value".
- The search endpoint does not paginate; it returns every matching player.

## See also

- Technical detail and API contract: [`doc/features/technical/player-search-and-persistence.md`](./technical/player-search-and-persistence.md).
- The search endpoint depends on a database connection being available per request; see [`doc/features/technical/database-connection-lifecycle.md`](./technical/database-connection-lifecycle.md) for what happens when the database is briefly unreachable at startup.
