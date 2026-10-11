# Database Connection Lifecycle

## Overview

`DatabaseStartup.ConfigureDatabase` registers `IDatabaseConnectionProvider` as `AddScoped`, so ASP.NET Core creates one `SqlConnectionProvider` instance per DI scope (per HTTP request) and disposes it when the scope ends. `SqlConnectionProvider` implements `IDisposable`/`IAsyncDisposable` so the underlying `MySqlConnection` and any open transaction are closed and released at that point (`DisposeAsync` awaits `MySqlConnection.DisposeAsync()`, which in `MySqlConnector` closes the connection asynchronously and raises `Component.Disposed`). Within a single instance's lifetime, every query/execute/transaction goes through the private `RunOnConnectionAsync<T>`/`OpenIfNeededAsync`, which open the connection only if it is not already open (an ambient transaction is treated as already "owning" it) and close it back to the pool — without releasing it — once the operation that opened it finishes; the connection is reopened transparently for the next call. Every public method also accepts a `CancellationToken` (default `default`), passed through to `MySqlConnection.OpenAsync`, `BeginTransactionAsync`, and each Dapper `CommandDefinition`; an already-cancelled token short-circuits before the connection is touched. At startup, `DatabaseStartup` probes connectivity with a throwaway `SqlConnectionProvider` (`TestConnection`, a schema-independent `SELECT 1;`); if the probe fails, it registers `DummyConnectionProvider` instead and logs a single warning through a short-lived bootstrap `ILoggerFactory` created in `Startup.ConfigureServices` (the host's own logging pipeline is not yet resolvable at that point).

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Startup as Startup.ConfigureServices
    participant DatabaseStartup
    participant Probe as SqlConnectionProvider (probe)
    participant DI as IServiceCollection / DI scope
    participant Provider as SqlConnectionProvider (per request)
    participant Database

    Startup->>DatabaseStartup: ConfigureDatabase(services, configuration, bootstrapLogger)
    alt DatabaseConfig disabled or not configured
        DatabaseStartup->>DI: AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>
    else configured and enabled
        DatabaseStartup->>Probe: new SqlConnectionProvider(connectionString)
        DatabaseStartup->>Probe: Task.Run(TestConnection).GetAwaiter().GetResult()
        Probe->>Database: SELECT 1;
        alt unreachable (DbException/TimeoutException/SocketException)
            Database-->>Probe: connection failure
            Probe-->>DatabaseStartup: false
            DatabaseStartup->>DatabaseStartup: logger.LogWarning(fallback to DummyConnectionProvider)
            DatabaseStartup->>DI: AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>
        else reachable
            Database-->>Probe: ok
            Probe-->>DatabaseStartup: true
            DatabaseStartup->>DI: AddScoped<IDatabaseConnectionProvider>(_ => new SqlConnectionProvider(connectionString))
        end
    end

    Note over DI,Provider: Per HTTP request
    DI->>Provider: new SqlConnectionProvider(connectionString) (once per scope)
    loop each Query/Execute/ExecuteInTransaction call, with the request's CancellationToken
        Provider->>Provider: OpenIfNeededAsync(token): throws if already cancelled; opens only if not already open/in a transaction
        Provider->>Database: run the operation (Dapper CommandDefinition carries the token)
        Provider->>Database: close back to the pool (not released) if this call opened it
    end
    alt client aborts the request mid-call
        Provider-->>Database: OperationCanceledException (before/between calls; an in-flight query is not interrupted, see limitation)
    end
    Note over DI,Provider: Request ends, scope disposed
    DI->>Provider: DisposeAsync()/Dispose()
    Provider->>Database: close + release connection (and transaction, if any)
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `DatabaseStartup` | `ConfigureDatabase` | `void ConfigureDatabase(this IServiceCollection, IConfiguration, ILogger? logger = null)` | `void` | Reads `DatabaseConfig` (`Enabled`, `ConnectionString`). Registers the dummy provider if disabled/not configured; otherwise probes connectivity and registers either `SqlConnectionProvider` (reachable) or the dummy provider with a warning log (unreachable). |
| `DatabaseStartup` | `ConnectionIsUp` (private) | `bool ConnectionIsUp(string connectionString)` | `bool` | Creates a throwaway `SqlConnectionProvider` in a `using` block and runs `TestConnection()` via `Task.Run(...).GetAwaiter().GetResult()` (avoids a synchronization-context deadlock and unwraps `AggregateException`). |
| `DatabaseStartup` | `AddDummyConnector` (private) | `void AddDummyConnector(IServiceCollection)` | `void` | `services.AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>()`. |
| `SqlConnectionProvider` | `TestConnection` | `Task<bool> TestConnection()` | `bool` | Opens the connection if needed, runs `SELECT 1;`, and returns `true`/`false`. Catches `DbException`, `TimeoutException`, and `SocketException` (returns `false`) instead of throwing; always closes the connection it opened itself. |
| `SqlConnectionProvider` | `IsDisposed` | `bool` (get) | `bool` | `true` once `Dispose()`/`DisposeAsync()` has run; both methods are idempotent (a second call is a no-op). |
| `SqlConnectionProvider` | `ConnectionState` | `ConnectionState` (get) | `ConnectionState` | Exposes the underlying `MySqlConnection.State` (e.g. `Open`/`Closed`) for diagnostics/tests. |
| `SqlConnectionProvider` | `DisposeAsync` | `ValueTask DisposeAsync()` | `ValueTask` | Disposes any pending transaction, then awaits `MySqlConnection.DisposeAsync()`. Invoked automatically by the DI container when an async-disposable scope ends. |
| `SqlConnectionProvider` | `Dispose` | `void Dispose()` | `void` | Synchronous counterpart of `DisposeAsync`, used when the scope is disposed synchronously. |
| `SqlConnectionProvider` | `ExecuteInTransactionAsync` | `Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)` | `Task` | Begins a transaction on the instance's own connection (reused for nested/ambient calls on the same instance, which only check `cancellationToken.ThrowIfCancellationRequested()` before running inline), commits on success, rolls back and rethrows on failure; the transaction and connection are scoped to the request because the provider itself is scoped to the request. If `BeginTransactionAsync` itself fails, the connection is closed before rethrowing. |
| `SqlConnectionProvider` | `QueryScalarAsync<T>` / `QueryAsync<T>` / `ExecuteAsync` | `Task<T>`/`Task<List<T>>`/`Task QueryScalarAsync/QueryAsync/ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)` | varies | Each runs through `RunOnConnectionAsync`, which calls `OpenIfNeededAsync(cancellationToken)` (opens only if needed; throws immediately if the token is already cancelled) and closes the connection back to the pool afterwards only if this call is the one that opened it. |
| `SqlConnectionProvider` | `RunOnConnectionAsync<T>` (private) | `Task<T> RunOnConnectionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)` | `Task<T>` | "Open if needed, close if I opened it" pattern shared by every query/execute method. The operation runs in `try/finally`: when this call opened the connection it is closed even if the operation fails or is cancelled, and the original exception propagates unwrapped (TECHDEBT-004 step 01). Inside an ambient transaction the transaction owns and closes the connection. |
| `SqlConnectionProvider` | `OpenIfNeededAsync` (private) | `Task<bool> OpenIfNeededAsync(CancellationToken cancellationToken = default)` | `Task<bool>` | `cancellationToken.ThrowIfCancellationRequested()` before touching the connection; returns `false` (caller does not own the connection) when an ambient transaction already has it open, `true` otherwise, opening it if it was closed. |
| `SqlConnectionProvider` | `Command` (private) | `CommandDefinition Command(string query, object? parameters, CancellationToken cancellationToken)` | `CommandDefinition` | Single place that builds a Dapper `CommandDefinition`, binding it to the ambient `MySqlTransaction` (if any) and to the caller's token. |
| `DatabaseConfig` | properties | `bool Enabled`, `string? ConnectionString` | — | Bound from the `DatabaseConfig` configuration section. |

## Dependencies

- `Startup.ConfigureServices`: creates a short-lived bootstrap `LoggerFactory` (`Console` + the `Logging` configuration section), passes its logger into `ConfigureDatabase`, and disposes the factory immediately after — the host's own logging pipeline is not resolvable while services are still being registered.
- `IDatabaseConnectionProvider` consumers: `SqlPlayerRepository`, `SqlFacilityRepository` (any repository using `QueryAsync`/`ExecuteAsync`/`ExecuteInTransactionAsync` is bound to the same per-request instance and lifecycle described here, and now forwards the same `CancellationToken` it receives from its handler to every provider call — see [`player-search-and-persistence.md`](./player-search-and-persistence.md) and [`facility-update.md`](./facility-update.md)).
- `PlayerController`/`FacilitiesController` actions declare `CancellationToken cancellationToken = default`; ASP.NET Core model binding fills it with `HttpContext.RequestAborted`, and each action forwards it, unchanged, to its command/query handler.
- A mid-request failure to reach the database (as opposed to the startup fallback described above) during Delete/Update on Players or Facilities surfaces as an unmapped `500` through `ExceptionHandlingMiddleware`, logged once at `Error`; a mid-request client-initiated cancellation instead surfaces as `499`, logged once at `Information`: [`error-handling.md`](./error-handling.md).
- `TestConnection`, `ClearAsync`, and `CreateAsync` intentionally do not accept a `CancellationToken` — they run at startup or from test setup, outside any HTTP request's cancellation path.
- `Gaffer.Api.Tests/TestFixture.cs`: registers `IDatabaseConnectionProvider` per scope with a factory that creates `new SqlConnectionProvider(testDatabase)`, mirroring the production `AddScoped` registration for test isolation between requests hitting the in-memory `TestServer`.
- `DummyConnectionProvider`: the no-op fallback; `TestConnection()` always returns `true`, and mutating operations throw `NotImplementedException` (used only when the database is disabled/unreachable).
- `DatabaseConfig:ConnectionString` is now expected to come from User Secrets or an environment variable rather than a committed password; see [`database-configuration.md`](./database-configuration.md) for the exact keys and precedence.

## Usage Examples

### Startup registration (production)

```csharp
using (var startupLoggerFactory = LoggerFactory.Create(builder => builder
           .AddConfiguration(configuration.GetSection("Logging"))
           .AddConsole()))
{
    services.ConfigureDatabase(configuration, startupLoggerFactory.CreateLogger(typeof(DatabaseStartup)));
}
```

### Consuming the per-request provider

```csharp
public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var (whereClause, parameters) = BuildFilters(criteria);
        return await dbConnectionProvider.QueryAsync<Player>($"SELECT * FROM `Players`{whereClause};", parameters);
    }
}
```

Two concurrent requests resolve two different `IDatabaseConnectionProvider` instances (from two different DI scopes); each instance is closed and disposed automatically when its request's scope ends.

### Observed log entry on startup fallback

```
warn: Gaffer.Api.Startups.DatabaseStartup[0]
      Database is not reachable at startup; falling back to DummyConnectionProvider.
```

No connection string or credentials appear in this message.

### Cancellation stops the in-flight call and the next one

```csharp
var cts = new CancellationTokenSource();
var queryTask = dbConnectionProvider.QueryAsync<Player>("SELECT * FROM `Players`;", cancellationToken: cts.Token);
cts.Cancel();
await queryTask; // if the query already started, MySqlConnector sends KILL QUERY and the call throws OperationCanceledException

cts.Cancel();
await dbConnectionProvider.QueryAsync<Player>("SELECT * FROM `Players`;", cancellationToken: cts.Token);
// throws OperationCanceledException immediately: the token was already cancelled before this call opened a connection
```

## How to test

- `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs`: `CloseButNotReleaseTheConnectionAfterEachOperation`, `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed`, `MakeTheWorkOfACommittedTransactionVisible`, `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne`, `NotRunAQueryWhenItsTokenIsAlreadyCancelled`, `NotWriteAnythingWhenAnExecuteTokenIsAlreadyCancelled`, `NotWriteAnythingWhenATransactionTokenIsAlreadyCancelled`, `RollBackATransactionWhoseOperationIsCancelled`.
- `Gaffer.Api.Tests/Unit/Repositories/TokenRecordingConnectionProvider.cs` (test double that delegates to the real `SqlConnectionProvider` while recording the token of every call) backs `WhenPropagatingTheCancellationToken` in `SqlPlayerRepositoryShould`/`SqlFacilityRepositoryShould`.
- `Gaffer.Api.Tests/Unit/Controllers/PlayerControllerShould.cs`/`FacilitiesControllerShould.cs`: `PassTheRequestTokenTo...` asserts each action forwards its own (non-`None`) token to its handler.
- `SqlConnectionProviderShould` (TECHDEBT-004 step 03) also covers mid-query cancellation: a `SELECT SLEEP(...)` cancelled shortly after it starts is aborted on the server and throws `OperationCanceledException`, which the earlier `MySql.Data` driver could not do.
- Run with `dotnet test gaffer-api.sln` against the Docker MySQL database (see [`database-configuration.md`](./database-configuration.md)).
