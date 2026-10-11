# Error Handling and Logging — Technical

## Overview

`ExceptionHandlingMiddleware.Invoke` wraps `next(context)` in `try { await next(context); } catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested) { HandleAbortedRequest(...); } catch (Exception ex) { await HandleExceptionAsync(...); }`. The first `catch` (TECHDEBT-003) intercepts only a cancellation that came from the client aborting the request (`HttpContext.RequestAborted`, itself derived from the request's `CancellationToken` which every controller action, command/query handler, and repository call now receives and forwards down to `IDatabaseConnectionProvider`); any other `OperationCanceledException` falls through to the generic handler and is treated as an unknown failure. `HandleExceptionAsync` looks up `exception.GetType()` in `ExceptionToHttpResponseConverters.PlayerConverters` (a `Type → Func<Exception, ProblemDetails>` dictionary shared by Player and Facility exceptions, 14 entries). A hit produces the mapped `ProblemDetails` (with `Instance` set to the request path); a miss falls back to `UnknownError` (`500`, `Title = "Unknown error"`). `LogException` then runs for every response except a `404`: `logger.LogError(exception, "Request failed with status {Status}: {Title}", problemDetails.Status, problemDetails.Title)`. Other producers of exceptions were changed to reach this pipeline correctly: the four Delete/Update command handlers (Player and Facility) catch only the specific `*NotFoundException` for their operation, letting every other exception — including a repository/infrastructure failure — propagate to the middleware unwrapped (so it lands in the `UnknownError`/`500` path, not in the generic `Cannot*Exception`/`400` path); `SqlFacilityRepository.BuildFilters` throws `FacilitySearchFilterNotPermittedException` instead of a plain `ArgumentException` for a non-permitted filter field, recognized by the same converter entry Player search already used (`BuildSearchFilterNotPermittedProblemDetails`); and facility update's `UpdateFacilityCommandHandler.MergeStands` throws `CannotUpdateFacilityDuplicatedStandException` for a repeated existing-stand `Id`, mapped to its own `400` entry (see [`facility-update.md`](./facility-update.md)).

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Handler as Delete/Update*CommandHandler
    participant Repository as I(Player|Facility)Repository
    participant Middleware as ExceptionHandlingMiddleware
    participant Converters as ExceptionToHttpResponseConverters
    participant Logger as ILogger<ExceptionHandlingMiddleware>

    Client->>Handler: Handle(Delete/UpdateCommand)
    Handler->>Repository: Get / Delete / Update
    alt entity not found
        Repository-->>Handler: PlayerNotFoundException / FacilityNotFoundException
        Handler-->>Middleware: throw CannotDelete*/CannotUpdate*NotFoundException
        Middleware->>Converters: PlayerConverters[type] (hit)
        Converters-->>Middleware: 404 ProblemDetails
        Middleware-->>Client: 404 (not logged)
    else infrastructure failure (e.g. DB unreachable)
        Repository-->>Handler: e.g. MySqlException / InvalidOperationException
        Handler-->>Middleware: propagated unchanged (no catch clause matches)
        Middleware->>Converters: PlayerConverters[type] (miss)
        Converters-->>Middleware: null
        Middleware->>Middleware: UnknownError(context) -> 500, Title="Unknown error"
        Middleware->>Logger: LogError(exception, "Request failed with status {Status}: {Title}", 500, "Unknown error")
        Middleware-->>Client: 500
    end

    Client->>Repository: Facility search with a non-permitted filter field
    Repository->>Repository: BuildFilters rejects the field before querying
    Repository-->>Middleware: throw FacilitySearchFilterNotPermittedException
    Middleware->>Converters: PlayerConverters[FacilitySearchFilterNotPermittedException] (hit)
    Converters-->>Middleware: 400 ProblemDetails (Error.Search.FilterNotPermitted)
    Middleware->>Logger: LogError(exception, "Request failed with status {Status}: {Title}", 400, "Error.Search.FilterNotPermitted")
    Middleware-->>Client: 400

    Client->>Handler: (any request) then the client aborts the connection
    Handler-->>Middleware: OperationCanceledException, context.RequestAborted.IsCancellationRequested == true
    Middleware->>Middleware: HandleAbortedRequest: StatusCode = 499 (if response not started)
    Middleware->>Logger: LogInformation("Request {Path} aborted by the client: {Message}", ...)
    Note over Middleware,Client: no body written, no LogError; client already disconnected
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `ExceptionHandlingMiddleware` | constructor | `ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)` | — | Primary constructor; `logger` is resolved by the host DI container (registered implicitly via `AddLogging`/`ILogger<T>`), no new DI registration was needed. |
| `ExceptionHandlingMiddleware` | `Invoke` | `Task Invoke(HttpContext)` | `Task` | Two `catch` clauses in order: `OperationCanceledException` gated on `context.RequestAborted.IsCancellationRequested` goes to `HandleAbortedRequest`; every other `Exception` (including an unrelated `OperationCanceledException`) goes to `HandleExceptionAsync`. |
| `ExceptionHandlingMiddleware` | `HandleAbortedRequest` (private) | `void HandleAbortedRequest(HttpContext, OperationCanceledException)` | `void` | `logger.LogInformation(...)` (one entry, no exception attached); sets `context.Response.StatusCode = 499` only `if (!context.Response.HasStarted)`. Writes no body. |
| `ExceptionHandlingMiddleware` | `HandleExceptionAsync` (private) | `Task HandleExceptionAsync(HttpContext, Exception)` | `Task` | Looks up the converter by exact exception type; falls back to `UnknownError` on a miss; always calls `LogException` before writing the response. |
| `ExceptionHandlingMiddleware` | `LogException` (private) | `void LogException(Exception, ProblemDetails)` | `void` | No-op when `problemDetails.Status == 404`; otherwise `logger.LogError(exception, ...)` — one entry, `Error` level, with the exception attached, for every `400`/`500`/other. |
| `ExceptionHandlingMiddleware` | `UnknownError` (private, static) | `ProblemDetails UnknownError(HttpContext)` | `ProblemDetails` | `Status = 500`, `Title = "Unknown error"` (fixed spelling), `Instance` = request path. |
| `DeletePlayerCommandHandler` / `DeleteFacilityCommandHandler` / `UpdatePlayerCommandHandler` / `UpdateFacilityCommandHandler` | `Handle` | `Task Handle(TCommand, CancellationToken)` | `Task` | `catch (PlayerNotFoundException)`/`catch (FacilityNotFoundException)` only, rethrown as the matching `CannotDelete*/CannotUpdate*NotFoundException`; every other exception (including a repository failure) is not caught here, so it reaches the middleware unwrapped. |
| `IFacilityRepository.cs` | `FacilitySearchFilterNotPermittedException` | `FacilitySearchFilterNotPermittedException(string fieldName)` | — | `Exception("FilterNotPermitted")` with `FieldName`; analogous to the pre-existing `PlayerSearchFilterNotPermittedException` in `IPlayerRepository.cs`. |
| `SqlFacilityRepository` | `BuildFilters` (private, static) | `(string whereClause, DynamicParameters parameters) BuildFilters(FacilitySearchCriteria)` | tuple | Throws `FacilitySearchFilterNotPermittedException` for a filter field outside the allow-list, before any `IDatabaseConnectionProvider` call — no SQL runs for a rejected filter. |
| `ExceptionToHttpResponseConverters` | `PlayerConverters` | `IReadOnlyDictionary<Type, Func<Exception, ProblemDetails>>` | — | 14 entries; both `PlayerSearchFilterNotPermittedException` and `FacilitySearchFilterNotPermittedException` map through the shared private helper `BuildSearchFilterNotPermittedProblemDetails()`. |
| `ExceptionToHttpResponseConverters` | `CannotDeletePlayerException` / `CannotUpdatePlayerException` / `CannotDeleteFacilityException` / `CannotUpdateFacilityException` | `Reason { get; } = "Internal Server Error"` (const `InternalServerErrorReason`) | — | Still mapped to `400` with key `Error.<Resource>.<Operation>.Internal Server Error`; kept for backward compatibility, but no handler constructs these anymore (verified by `grep -rn "new Cannot(Delete\|Update)(Player\|Facility)Exception"` = 0 matches in `Gaffer.Api`). An infrastructure failure on Delete/Update now always reaches the `UnknownError`/`500` path instead. |
| `ExceptionToHttpResponseConverters` | `CannotUpdateFacilityDuplicatedStandException` | `ex => BuildBadRequestProblemDetailsWithExtension("Cannot update facility", ex.Message, "Error.Facilities.Update.DuplicatedStand")` | `ProblemDetails` (`400`) | New entry (TECHDEBT-003); see [`facility-update.md`](./facility-update.md) for when it is thrown. |
| `ExceptionToHttpResponseConverters` | `FacilityStandsFacilityNotFoundException` | `BuildNotFoundProblemDetailsWithExtension("Facility not found", ex.Message, "Error.Facilities.Stands.FacilityNotFound")` | `ProblemDetails` (`404`) | TECHDEBT-004; see [`facility-stands.md`](./facility-stands.md). |
| `ExceptionToHttpResponseConverters` | `FacilityStandNotFoundException` | `BuildNotFoundProblemDetailsWithExtension("Stand not found", ex.Message, "Error.Facilities.Stands.StandNotFound")` | `ProblemDetails` (`404`) | Missing stand or stand of another facility. |
| `ExceptionToHttpResponseConverters` | `FacilityMaxStandsExceededException` | `BuildBadRequestProblemDetailsWithExtension("Cannot create stand", ex.Message, "Error.Facilities.Stands.Create.MaxStandsExceeded")` | `ProblemDetails` (`400`) | Creating a fifth stand. |

## Dependencies

- `ExceptionHandlingMiddleware` is registered once in the ASP.NET Core pipeline (`Startup.Configure` / `TestFixture.StartApiServer`, via `UseMiddleware<ExceptionHandlingMiddleware>()`); `ILogger<ExceptionHandlingMiddleware>` comes from the host's own logging pipeline — no new DI registration.
- `Gaffer.Api.Tests/TestShared/RecordingLoggerProvider.cs` (new, test-only, no new package): an in-memory `ILoggerProvider`/`ILogger` used by both `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` and `Acceptance/DatabaseFailureShould.cs` to assert on log entries (`EntriesFor<T>()`, `Level`, `Exception`).
- `TestFixture.StartApiServer(string baseAddress, string connectionString, ILoggerProvider? loggerProvider = null)`: optional parameter, added so a test host can capture its own logs; the shared fixture instance does not pass one, so no behaviour changes for existing tests.
- `Gaffer.Api.Tests/Acceptance/DatabaseFailureShould.cs` (new): its own `TestServer` pointed at `TestConfiguration.UnreachableDatabaseConnectionString`, exercising the real repository → handler → middleware chain for Delete/Update/Get on both Player and Facility.
- Player search's pre-existing filter-rejection behaviour and its converter entry: [`player-search-and-persistence.md`](./player-search-and-persistence.md).
- Per-request connection provider, what "unreachable" means at runtime, and how the request's `CancellationToken` reaches `IDatabaseConnectionProvider`: [`database-connection-lifecycle.md`](./database-connection-lifecycle.md).
- `HttpContext.RequestAborted` is the ASP.NET Core host's own cancellation token for the current request (cancelled when the client disconnects); every controller action now declares `CancellationToken cancellationToken = default`, which model binding fills with `RequestAborted`, and forwards it through the command/query handler into the repository and `IDatabaseConnectionProvider` calls — the same token the middleware checks with `context.RequestAborted.IsCancellationRequested`.
- Facility update's `CannotUpdateFacilityDuplicatedStandException`: [`facility-update.md`](./facility-update.md).

## Usage Examples

### Infrastructure failure on Delete (500, not 400)

```http
DELETE /api/v1/players/75d5... HTTP/1.1
```

With the database unreachable, returns `500 Internal Server Error`, `Title = "Unknown error"` — not the `400 Error.Players.Delete.Internal Server Error` shape the generic exception would have produced if it were still thrown.

### Facility search — non-permitted filter field (400)

```csharp
var criteria = new FacilitySearchCriteria
{
    criteriaList = [new SearchCriteria { Name = "Street", Value = "Main Avenue" }]
};
await facilityRepository.Get(criteria, cancellationToken); // throws FacilitySearchFilterNotPermittedException, no query executed
```

Through the middleware this becomes:

```json
{
  "Title": "Error.Search.FilterNotPermitted",
  "Detail": "FilterNotPermitted",
  "Status": 400,
  "Instance": "/facilities",
  "Extensions": { "ErrorTranslationKey": "Error.Search.FilterNotPermitted" }
}
```

### Logging: one Error entry per non-404 response

```csharp
var logs = new RecordingLoggerProvider();
var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"), logs.CreateLogger<ExceptionHandlingMiddleware>());
await middleware.Invoke(context); // 500

logs.EntriesFor<ExceptionHandlingMiddleware>().Should().ContainSingle();
// Level == LogLevel.Error, Exception is the same InvalidOperationException instance
```

A `PlayerNotFoundException`/`FacilityNotFoundException`-derived `404` produces zero entries in `logs.EntriesFor<ExceptionHandlingMiddleware>()`.

### Client-aborted request (499, Information log)

```csharp
var logs = new RecordingLoggerProvider();
var cts = new CancellationTokenSource();
var context = new DefaultHttpContext { RequestAborted = cts.Token };
var middleware = new ExceptionHandlingMiddleware(_ => throw new TaskCanceledException(), logs.CreateLogger<ExceptionHandlingMiddleware>());
cts.Cancel();
await middleware.Invoke(context); // 499, no body

logs.EntriesFor<ExceptionHandlingMiddleware>().Should().ContainSingle();
// Level == LogLevel.Information, Exception is null (not attached)
```

An `OperationCanceledException` thrown while `context.RequestAborted` is **not** cancelled still goes through `HandleExceptionAsync` and produces the usual `500` + `LogError`.

## How to test

- `Gaffer.Api.Tests/Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs`: logging per status class (`404` not logged, mapped `400` and unmapped `500` logged once at `Error` with the exception attached), the Facility real-repository → middleware chain for the non-permitted filter, and (TECHDEBT-003) `ReturnClientClosedRequestWhenTheRequestIsAbortedByTheClient`, `LogAnAbortedRequestOnceAsInformationWithoutError`, `KeepReturningAnUnknownErrorForACancellationWhenTheRequestIsNotAborted`.
- `Gaffer.Api.Tests/Acceptance/DatabaseFailureShould.cs`: end-to-end `500` for Get/Delete/Update on Player and Facility against an unreachable database, plus one log-entry assertion (`ReturnAnUnknownErrorAndLogItWhenGettingAPlayer`).
- `Gaffer.Api.Tests/Unit/Commands/*CommandHandlerShould.cs` (Delete/Update × Player/Facility): `PropagateTheRepositoryFailureWhile*` asserts the original exception type (e.g. `InvalidOperationException`) is rethrown unchanged, not wrapped.
- `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs`: `ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted` asserts `FacilitySearchFilterNotPermittedException` and zero calls to the connection provider.
- `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs`: `CoverEveryConverter` (14 entries), `ConvertEachExceptionIntoItsExpectedResponse`, `NeverProduceATranslationKeyEndingWithADot`.
- Run with `dotnet test gaffer-api.sln` against the Docker MySQL database (see [`database-configuration.md`](./database-configuration.md)); the unreachable-database tests use a deliberately invalid connection string and do not require Docker to be down.
