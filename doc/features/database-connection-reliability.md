# Database Connection Reliability

## Overview

This document covers how `Gaffer.Api` manages its database connection: one connection per HTTP request, what happens when the database cannot be reached while the API is starting up, and how a cancelled/aborted request stops using that connection. It is aimed at operators and API consumers who need to understand data isolation between concurrent requests, the API's behaviour during a database outage at boot time, and what happens to in-flight database work when a client disconnects.

### Who it is for

- Operators deploying or monitoring `Gaffer.Api`.
- Developers debugging concurrency/isolation issues or startup failures related to the database.

### Use cases

- Two requests running at the same time must not see or interfere with each other's uncommitted data.
- The API should still start (in a degraded, read/write-disabled mode) if the configured database is temporarily unreachable, instead of crashing.
- A client cancels a request (or simply disconnects) and the API should stop starting new database work for it as soon as it safely can, rather than always running every query to completion regardless.

### Behaviour

- **One connection per request**: each HTTP request gets its own database connection provider (`IDatabaseConnectionProvider`). Two concurrent requests get two different provider instances; a request always sees the same instance across the handlers it calls during that request.
- **Isolation**: an uncommitted transaction started by one request is not visible to another request running at the same time; after a rollback, the change is not visible to anyone.
- **Cleanup**: the connection (and any pending transaction) is closed and released automatically when the request finishes, without requiring a repository or handler to do it manually. Within a single request, the connection is also closed back to the pool after each individual database operation it opened (query, execute, or a committed/rolled-back transaction) — it is reopened transparently for the next operation, and only fully released once, when the request ends.
- **Request cancellation**: every database call within a request now carries that request's cancellation signal. If the token is already cancelled when a call is about to start, it does not open a connection or run — it stops immediately. A cancellation that arrives *inside* a running transaction rolls that transaction back before it propagates. Cancelling the token also aborts a query already running on the database (`MySqlConnector` sends `KILL QUERY`), not only before a call starts or between two sequential calls.
- **Client-aborted request**: when the cancellation above is the direct result of the client disconnecting, the API does not report it as a failure. See [`error-handling.md`](./error-handling.md) for the resulting `499` response and `Information`-level log entry.
- **Startup connectivity check**: at startup, if the database is configured and enabled but not reachable, the API does not fail to start. It falls back to an in-memory/no-op connection provider and writes a single warning-level log entry noting the fallback (the log entry does not include the connection string or credentials). If the database is reachable, no such log entry is written.
- **Database disabled or not configured**: the API also falls back to the same no-op provider, without a warning log — this is the pre-existing behaviour for environments without a database.

### Limitations

- The startup fallback only covers connectivity failures (e.g. host unreachable, timeout). A malformed connection string still fails at startup.
- Once the database is confirmed reachable at startup, the API does not switch provider if it stops responding while running; `/health/ready` reports it instead (see [`health-checks.md`](./health-checks.md)). The fallback itself is kept and is permanent until restart.
- If a database operation fails or is cancelled outside of a transaction, the connection is still closed back to the pool (`try/finally`) and the original exception propagates unchanged. Inside a transaction, the transaction owns and closes the connection.

## See also

- Technical detail: [`doc/features/technical/database-connection-lifecycle.md`](./technical/database-connection-lifecycle.md).
- How to see the fallback (`/health/live`, `/health/ready`): [`health-checks.md`](./health-checks.md).
- This mechanism underlies every database-backed feature, including [player search](./player-search.md).
- How the connection string itself is supplied (User Secrets / environment variables, no password in versioned files): [`database-configuration.md`](./database-configuration.md).
- What a request sees when the database is unreachable mid-request (not just at startup) for Delete/Update on Players and Facilities: [`error-handling.md`](./error-handling.md).
