# API Response Contract and Access

## Overview

This document covers three related changes to how `Gaffer.Api` responds to and accepts requests for `Players` and `Facilities`:

- `POST` create endpoints now return `201 Created` (previously `200 OK`), with a `Location` header pointing at the created resource and the created `Guid` as the body.
- Enum properties (`Gender`, `FacilityType`) are serialized as their text name (e.g. `"FEMALE"`, `"Arena"`) instead of their integer value. The API still accepts the previous integer form on input, so existing writers are not broken.
- JSON is produced and consumed only through Newtonsoft.Json (camelCase property names, UTC dates). `System.Text.Json` MVC formatters are no longer configured.
- The API no longer requires authentication: cookie authentication and its unused authorization checks have been removed, and every endpoint is anonymous.

### Who it is for

- Front-end and integration teams consuming the Players/Facilities endpoints or the generated `Gaffer.Api.Client`.
- Operators who previously expected a login/cookie flow before calling the API.

### Use cases

- A client creates a player or facility and needs the new id and its canonical URL without a follow-up `GET`.
- A client reads `Gender`/`FacilityType` values as human-readable text instead of decoding an integer.
- A client that still sends `"gender": 1` or `"type": 1` on create/update continues to work unchanged.
- Any client calls any endpoint without first authenticating; there is no login endpoint to call.

### Limitations

- This is a breaking change for any client written against the previous contract: code that only accepts `200 OK` on create, or that parses `gender`/`type` as an integer, must be updated. The generated `Gaffer.Api.Client` must be regenerated to reflect it.
- Error response bodies (`ProblemDetails`, e.g. `Status`, `Title`, `Extensions`) are unchanged; only success responses from create endpoints and enum-bearing fields changed.
- There is no authentication or authorization on any endpoint. This is a deliberate, temporary state; a real authentication scheme needs its own plan and is not covered here.

## See also

- Technical detail: [`doc/features/technical/api-contract-and-access.md`](./technical/api-contract-and-access.md).
- Client package versioning after a contract change like this: [`doc/features/technical/api-client-versioning.md`](./technical/api-client-versioning.md).
- Facility creation flow (updated for the new response code): [`facility-create.md`](../../.github/doc/features/facility-create.md).
