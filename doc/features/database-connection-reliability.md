# Database Connection Reliability

## Overview

This document covers how `Gaffer.Api` manages its database connection: one connection per HTTP request, and what happens when the database cannot be reached while the API is starting up. It is aimed at operators and API consumers who need to understand data isolation between concurrent requests and the API's behaviour during a database outage at boot time.

### Who it is for

- Operators deploying or monitoring `Gaffer.Api`.
- Developers debugging concurrency/isolation issues or startup failures related to the database.

### Use cases

- Two requests running at the same time must not see or interfere with each other's uncommitted data.
- The API should still start (in a degraded, read/write-disabled mode) if the configured database is temporarily unreachable, instead of crashing.

### Behaviour

- **One connection per request**: each HTTP request gets its own database connection provider (`IDatabaseConnectionProvider`). Two concurrent requests get two different provider instances; a request always sees the same instance across the handlers it calls during that request.
- **Isolation**: an uncommitted transaction started by one request is not visible to another request running at the same time; after a rollback, the change is not visible to anyone.
- **Cleanup**: the connection (and any pending transaction) is closed and released automatically when the request finishes, without requiring a repository or handler to do it manually.
- **Startup connectivity check**: at startup, if the database is configured and enabled but not reachable, the API does not fail to start. It falls back to an in-memory/no-op connection provider and writes a single warning-level log entry noting the fallback (the log entry does not include the connection string or credentials). If the database is reachable, no such log entry is written.
- **Database disabled or not configured**: the API also falls back to the same no-op provider, without a warning log — this is the pre-existing behaviour for environments without a database.

### Limitations

- The startup fallback only covers connectivity failures (e.g. host unreachable, timeout). A malformed connection string still fails at startup.
- Once the database is confirmed reachable at startup, the API does not later mark it as down if it stops responding while the API is running.

## See also

- Technical detail: [`doc/features/technical/database-connection-lifecycle.md`](./technical/database-connection-lifecycle.md).
- This mechanism underlies every database-backed feature, including [player search](./player-search.md).
- How the connection string itself is supplied (User Secrets / environment variables, no password in versioned files): [`database-configuration.md`](./database-configuration.md).
- What a request sees when the database is unreachable mid-request (not just at startup) for Delete/Update on Players and Facilities: [`error-handling.md`](./error-handling.md).
