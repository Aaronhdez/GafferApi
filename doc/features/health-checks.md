# Health Checks (Liveness and Readiness)

## Overview

The API exposes two anonymous endpoints, outside `/api` and outside the OpenAPI document, so that operators and orchestrators can tell whether the process is up and whether the database is usable. They make visible the startup fallback to the no-op database provider described in [`database-connection-reliability.md`](./database-connection-reliability.md): the fallback itself is unchanged, but readiness now reports it.

### Who it is for

- Operators and orchestrators (load balancers, container probes) that need a liveness and a readiness signal.
- Developers who want to know whether a local run is actually talking to the database.

### Use cases

- Check that the process answers (`/health/live`).
- Check that the database is usable before sending traffic (`/health/ready`).
- Notice that the API started while the database was down and is running on the fallback provider.

### Endpoints and behaviour

Both answer with a plain-text body: `Healthy`, `Degraded` or `Unhealthy`.

| Endpoint | What it checks | Result |
|---|---|---|
| `GET /health/live` | Nothing external (no dependency is evaluated) | `200` `Healthy` while the process answers |
| `GET /health/ready` | The database, depending on how the connection provider was chosen at startup | see below |

| State of the database at startup | `/health/ready` |
|---|---|
| Enabled, reachable: real provider | Checks the database on every request: `200` `Healthy` if it answers, `503` `Unhealthy` if it does not answer, fails, or does not answer within 5 seconds |
| Enabled but unreachable at startup: fallback to the no-op provider | `503` `Unhealthy` (even if the database answers later: the API keeps the fallback until it restarts) |
| Disabled (`DatabaseConfig:Enabled` false) or no connection string: no-op provider on purpose | `200` `Degraded` |

- `Healthy` and `Degraded` answer `200`; `Unhealthy` answers `503`.
- The response carries no exception text, connection string, user or server name.
- If the caller cancels the readiness request, the check is cancelled too and no `Unhealthy` is reported for that.

### Limitations

- The fallback is kept: the API does not switch back to the real provider when the database returns; a restart is needed. Readiness stays `503` until then.
- A new database connection is opened for each `/health/ready` request (no caching), and the endpoints are anonymous and not rate limited.
- The text body is the framework default; there is no JSON detail per check.
- The endpoints are not part of the OpenAPI document or `Gaffer.Api.Client`, and CI does not call them.
- A database that stops answering after startup is detected only by readiness (it is not reflected in the other endpoints).

## See also

- Technical detail: [`technical/health-checks.md`](./technical/health-checks.md).
- Startup fallback and connection lifecycle: [`database-connection-reliability.md`](./database-connection-reliability.md).
- Database enable flag and connection string: [`database-configuration.md`](./database-configuration.md).
