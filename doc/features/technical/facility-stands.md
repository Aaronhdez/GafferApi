# Facility Stands (CRUD) — Technical

## Overview

`FacilityStandsController` (`[Route("api/v{version:apiVersion}/facilities/{facilityId}/stands")]`, registered in `VersioningStartup` with the shared API versions) exposes five operations through CQRS handlers registered in `ActionsStartup`. Reads go through `SearchFacilityStandsQueryHandler` and `GetFacilityStandQueryHandler`; writes through `AddFacilityStandCommandHandler`, `ReplaceFacilityStandCommandHandler` and `RemoveFacilityStandCommandHandler`. Handlers translate the repository's `FacilityNotFoundException` into `FacilityStandsFacilityNotFoundException`, so the stands endpoints use their own error keys. `SqlFacilityRepository` implements the stand operations; every write runs in one transaction that starts by locking the facility row with `SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE` (`LockFacility`). The lock serializes the writers of one facility's stands, so the count check and the `INSERT` of `AddStand` cannot interleave with another request. The same lock is taken by the facility `PUT` ([`facility-update.md`](./facility-update.md)).

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant FacilityStandsController
    participant Handler as Stand command handler
    participant SqlFacilityRepository
    participant Database
    participant ExceptionHandlingMiddleware

    Client->>FacilityStandsController: POST /facilities/{facilityId}/stands { standName, standCapacity }
    FacilityStandsController->>Handler: Handle(AddFacilityStandCommand)
    Handler->>SqlFacilityRepository: AddStand(facilityId, new Stand(Guid.NewGuid(), ...))
    SqlFacilityRepository->>Database: BEGIN; SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE
    alt no row
        SqlFacilityRepository-->>Handler: FacilityNotFoundException (rollback)
        Handler-->>ExceptionHandlingMiddleware: FacilityStandsFacilityNotFoundException
        ExceptionHandlingMiddleware-->>Client: 404 (Error.Facilities.Stands.FacilityNotFound)
    else locked
        SqlFacilityRepository->>Database: SELECT COUNT(*) FROM Facilities_Stands
        alt 4 or more
            SqlFacilityRepository-->>ExceptionHandlingMiddleware: FacilityMaxStandsExceededException (rollback)
            ExceptionHandlingMiddleware-->>Client: 400 (Error.Facilities.Stands.Create.MaxStandsExceeded)
        else fewer than 4
            SqlFacilityRepository->>Database: INSERT stand and link; COMMIT
            FacilityStandsController-->>Client: 201 + Location, body = stand id
        end
    end
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `FacilityStandsController` | `GetAll` | `GET ""` | `List<SearchFacilityStandResult>` | Lists the stands of a facility. |
| `FacilityStandsController` | `GetBy` | `GET "{standId}"` | `SearchFacilityStandResult` | Reads one stand; also the target of the `Location` header. |
| `FacilityStandsController` | `Create` | `POST ""` `(Guid facilityId, FacilityStandRequest)` | `ActionResult<Guid>` | `CreatedAtAction(GetBy, ...)`, `201`. Invalid body → `400` `Error.Facilities.Stands.Validation`. |
| `FacilityStandsController` | `Update` | `PUT "{standId}"` | `IActionResult` | `204`; invalid body → `400` `Error.Facilities.Stands.Validation`. |
| `FacilityStandsController` | `Delete` | `DELETE "{standId}"` | `IActionResult` | `204`. |
| `FacilityStandRequest` | `StandName`, `StandCapacity` | `string`, `int` | — | Body for create and replace. Without DataAnnotations: `StandValidation.Create` (in `FacilityStandCommandHandlers`) calls `Stand.Create(id, name, capacity)` before the repository and turns `DomainValidationException` into `ValidationFailedException(title, "Error.Facilities.Stands.Validation", errors)`; model binding errors on these two actions use the same key through `ValidationStartup`. Validation runs before the facility lookup (400 before 404). |
| `IFacilityRepository` | `GetStands`, `GetStand` | `Task<List<Stand>>`, `Task<Stand>` | — | `EnsureFacilityExists` first; `GetStand` throws `FacilityStandNotFoundException` for a missing or foreign stand. Reads take no lock. |
| `IFacilityRepository` | `AddStand` | `Task AddStand(Guid facilityId, Stand stand, CancellationToken)` | `Task` | Lock, `CountStands`, throws `FacilityMaxStandsExceededException` at 4, then `InsertStand`. |
| `IFacilityRepository` | `UpdateStand` | `Task UpdateStand(Guid facilityId, Stand stand, CancellationToken)` | `Task` | Lock, `EnsureStandBelongsToFacility` (`Facilities_Stands`), `UPDATE Stands`. |
| `IFacilityRepository` | `DeleteStand` | `Task DeleteStand(Guid facilityId, Guid standId, CancellationToken)` | `Task` | Lock, ownership check, `DELETE` of the stand and its `Facilities_Stands` row. |
| `IFacilityRepository` | `Delete` (whole facility) | `Task Delete(Guid id, CancellationToken)` | `Task` | TECHDEBT-006: one transaction with a single `DELETE FROM Facilities WHERE Id = @Id`. The address, stands and link rows go away through the schema: `ON DELETE CASCADE` foreign keys on the two link tables (`M0002`) and the `TRG_Facilities_BeforeDelete` trigger that deletes `Stands` and `Addresses` (`M0003`). The former `DeleteStands`/`DeleteAddress` statements were removed. Does not take the facility lock of the stand operations. Proof: `SqlFacilityRepositoryShould` deletes only the facility row and finds `(0,0,0,0,0)` rows in `Facilities, Addresses, Facilities_Addresses, Stands, Facilities_Stands` for it and `(1,1,1,3,3)` for another facility. |
| `FacilityStandsFacilityNotFoundException` | — | `(string message, Guid facilityId)` | — | Maps to `404` `Error.Facilities.Stands.FacilityNotFound`. |
| `FacilityStandNotFoundException` | — | `(Guid facilityId, Guid standId)` | — | Maps to `404` `Error.Facilities.Stands.StandNotFound`. |
| `FacilityMaxStandsExceededException` | — | `(Guid facilityId)` | — | Maps to `400` `Error.Facilities.Stands.Create.MaxStandsExceeded`. |

## Dependencies

- `IDatabaseConnectionProvider.ExecuteInTransactionAsync`, per-request connection and cancellation: [`database-connection-lifecycle.md`](./database-connection-lifecycle.md).
- `ExceptionToHttpResponseConverters`: [`error-handling.md`](./error-handling.md).
- `Stand` domain: `Stand.Create` validates `StandName` (1-100, not whitespace) and `StandCapacity` (>= 0); rehydration through the constructor does not validate. The 4-stand cap is `MaxStands` in `SqlFacilityRepository` for this endpoint family and `Facility.Create` / `CheckRequestedStands` for creation and `PUT`. See [`input-validation.md`](./input-validation.md).
- OpenAPI/client: the five operations change the served contract; `Gaffer.Api.Client` is regenerated and versioned by the operator (see [`api-client-versioning.md`](./api-client-versioning.md) and [`ci-github-actions.md`](./ci-github-actions.md)).
- Tests need the handler registrations in `Gaffer.Api.Tests/TestFixture.cs`.

## Usage Examples

```http
POST /api/v1/facilities/3d9c.../stands HTTP/1.1
Content-Type: application/json

{ "standName": "North", "standCapacity": 5000 }
```

`201 Created`, `Location: .../stands/{newId}`, body `"{newId}"`. A fifth stand answers `400` with `errorTranslationKey = "Error.Facilities.Stands.Create.MaxStandsExceeded"`.

```http
DELETE /api/v1/facilities/3d9c.../stands/{standId}
```

`204 No Content`; `404` with `Error.Facilities.Stands.StandNotFound` when the stand does not exist or belongs to another facility.

Invalid body:

```http
POST /api/v1/facilities/3d9c.../stands HTTP/1.1
Content-Type: application/json

{ "standName": "", "standCapacity": -1 }
```

`400`, `errorTranslationKey = "Error.Facilities.Stands.Validation"`, `errors` with `standName` and `standCapacity`; nothing is saved.

## How to test

- `Gaffer.Api.Tests/Acceptance/FacilityStandsFeaturesShould.cs`: list, read, create (`201`, `Location`, ignored body id), fifth stand rejected, replace keeping the id, delete including the last one, both `404` keys, `NeverExceedFourStandsWhenCreatingThemConcurrently`.
- `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs`: `AddReadUpdateAndDeleteAStand`, `RejectAStandWhenTheFacilityAlreadyHasFour`, `ReportAMissingFacilityOnEveryStandOperation`, `ReportAMissingOrForeignStandWithoutTouchingIt`, `LeaveNoRowsWhenTheStandInsertFails`, and the two lock tests `MakeAConcurrentStandCreationWaitForTheLockHolderAndThenRespectTheLimit` / `MakeAConcurrentFacilityUpdateWaitForTheLockHolderAndThenRespectTheLimit`. Reviewed by removing `FOR UPDATE` in a temporary copy: 3 of 17 tests failed.
- `Gaffer.Api.Tests/Unit/Commands/FacilityStandCommandHandlersShould.cs`, `.../Queries/FacilityStandQueryHandlersShould.cs`, `.../Controllers/FacilitiesControllerContractShould.cs`, `.../Exceptions/ExceptionToHttpResponseConvertersShould.cs`.
- Run with `dotnet test gaffer-api.sln` against the MariaDB test database ([`database-configuration.md`](./database-configuration.md)).
