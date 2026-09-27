# Facility Update (Stands Matched by Id) — Technical

## Overview

`FacilitiesController.Update` (`PUT /api/v{version}/facilities/{id}`) maps `UpdateFacilityRequest` field-for-field onto `UpdateFacilityCommand`, including `List<UpdateFacilityStandRequest>? Stands` → `List<UpdateFacilityStandCommand>?` (each carrying an optional `Guid? Id`). `UpdateFacilityCommandHandler.Handle` loads the existing `Facility` via `IFacilityRepository.Get`, rebuilds its `Address` from the request (keeping the existing `Address.Id`), and merges the requested stands with `MergeStands` — a pure function with no I/O. The merged `Facility` (which re-validates the 4-stand invariant in its constructor) is then persisted with `IFacilityRepository.Update`, which updates the facility row, the address row, and each stand in place (updating matched stands, inserting new ones) inside a single transaction; it never deletes a stand or address row. An unknown facility id makes `GetFacility` throw `FacilityNotFoundException`, caught and rethrown as `CannotUpdateFacilityNotFoundException` (mapped to `404`); a repeated existing-stand `Id` makes `MergeStands` throw `CannotUpdateFacilityDuplicatedStandException` (mapped to `400`); exceeding 4 stands makes the `Facility` constructor throw `FacilityValidationException` (mapped to `400`, the same key used at creation).

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant FacilitiesController
    participant UpdateFacilityCommandHandler
    participant IFacilityRepository
    participant Facility
    participant SqlFacilityRepository
    participant Database
    participant ExceptionHandlingMiddleware

    Client->>FacilitiesController: PUT /facilities/{id} { address, stands: [{ id?, standName, standCapacity }] }
    FacilitiesController->>UpdateFacilityCommandHandler: Handle(UpdateFacilityCommand)
    UpdateFacilityCommandHandler->>IFacilityRepository: Get(FacilitySearchCriteria: Id)
    alt no match
        IFacilityRepository-->>UpdateFacilityCommandHandler: []
        UpdateFacilityCommandHandler-->>ExceptionHandlingMiddleware: throw CannotUpdateFacilityNotFoundException
        ExceptionHandlingMiddleware-->>Client: 404 (Error.Facilities.Update.FacilityNotFound)
    else match found
        IFacilityRepository-->>UpdateFacilityCommandHandler: [Facility]
        UpdateFacilityCommandHandler->>UpdateFacilityCommandHandler: MergeStands(existing, requested)
        Note over UpdateFacilityCommandHandler: matched by Id -> updated in place<br/>no/unknown Id, <4 stands -> created (new Id)<br/>no/unknown Id, 4 stands -> discarded<br/>repeated existing Id -> throw DuplicatedStand
        UpdateFacilityCommandHandler->>Facility: new Facility(..., address, mergedStands)
        alt more than 4 stands
            Facility-->>UpdateFacilityCommandHandler: throw FacilityValidationException
            UpdateFacilityCommandHandler-->>ExceptionHandlingMiddleware: propagated
            ExceptionHandlingMiddleware-->>Client: 400 (Error.Facilities.Create.Validation)
        else 4 or fewer stands
            Facility-->>UpdateFacilityCommandHandler: ok
            UpdateFacilityCommandHandler->>IFacilityRepository: Update(facility)
            IFacilityRepository->>SqlFacilityRepository: Update implementation
            SqlFacilityRepository->>Database: ExecuteInTransactionAsync: UPDATE Facilities; UPDATE Addresses; UPDATE/INSERT Stands (no DELETE)
            SqlFacilityRepository-->>UpdateFacilityCommandHandler: ok
            UpdateFacilityCommandHandler-->>FacilitiesController: ok
            FacilitiesController-->>Client: 200 (no body)
        end
    end
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `FacilitiesController` | `Update` | `Task Update(Guid id, [FromBody] UpdateFacilityRequest request, CancellationToken cancellationToken = default)` | `Task` | `PUT /{id}`. Maps `request.Stands` to `UpdateFacilityStandCommand(stand.StandName, stand.StandCapacity, stand.Id)`, forwarding the optional `Id`. |
| `UpdateFacilityStandRequest` | `Id` | `Guid? Id { get; set; }` | — | Optional/nullable in the request schema; omit or send `null` to add a new stand. |
| `UpdateFacilityStandCommand` | record | `UpdateFacilityStandCommand(string StandName, int StandCapacity, Guid? Id = null)` | — | Carries the optional `Id` through to the handler. |
| `UpdateFacilityCommandHandler` | `Handle` | `Task Handle(UpdateFacilityCommand, CancellationToken)` | `Task` | Loads the facility, builds the updated aggregate (`BuildUpdatedFacility`), persists via `IFacilityRepository.Update`. Converts `FacilityNotFoundException` into `CannotUpdateFacilityNotFoundException`; any other exception (repository/infrastructure failure, `FacilityValidationException`, `CannotUpdateFacilityDuplicatedStandException`) propagates unchanged — see [`error-handling.md`](../error-handling.md). |
| `UpdateFacilityCommandHandler` | `BuildUpdatedFacility` (private, static) | `Facility BuildUpdatedFacility(Facility existing, UpdateFacilityCommand command)` | `Facility` | Rebuilds `Address` with `existing.Address.Id` (the address is updated in place, never replaced), and calls `MergeStands` for the stand list. Constructing the returned `Facility` re-validates the 4-stand invariant. |
| `UpdateFacilityCommandHandler` | `MergeStands` (private, static) | `List<Stand> MergeStands(Facility existing, List<UpdateFacilityStandCommand> requested)` | `List<Stand>` | Pure function, no I/O. A requested stand is "matched" when its `Id` is one of `existing.Stands`' ids. Matched stands with a repeated `Id` throw `CannotUpdateFacilityDuplicatedStandException` before anything else runs. Existing stands are kept (updated in place if matched, unchanged otherwise); unmatched requested stands become new `Stand(Guid.NewGuid(), ...)` entries only while `existing.Stands.Count < 4`, otherwise they are dropped. |
| `Stand` | constructor | `Stand(Guid Id, string StandName, int StandCapacity)` | — | Immutable value; `MergeStands` always creates a new `Stand` with the surviving `Id` (existing or freshly generated) rather than mutating one. |
| `CannotUpdateFacilityDuplicatedStandException` | constructor | `CannotUpdateFacilityDuplicatedStandException(Guid id, Guid standId)` | — | Mapped to `400`, `Error.Facilities.Update.DuplicatedStand` (see [`error-handling.md`](../error-handling.md) for the converter table). |
| `SqlFacilityRepository` | `Update` | `Task Update(Facility, CancellationToken)` | `Task` | One `ExecuteInTransactionAsync`: `UPDATE Facilities`, then `UpdateAddress`, then `UpdateStands`. Never calls `DeleteStands`/`DeleteAddress` (those are only reachable from `Delete`). |
| `SqlFacilityRepository` | `UpdateAddress` (private) | `Task UpdateAddress(Facility, CancellationToken)` | `Task` | `UPDATE Addresses ... WHERE Id IN (SELECT AddressId FROM Facilities_Addresses WHERE FacilityId = @FacilityId)`. The address row's `Id` and its `Facilities_Addresses` link are untouched. |
| `SqlFacilityRepository` | `UpdateStands` (private) | `Task UpdateStands(Facility, CancellationToken)` | `Task` | Reads the stand ids already linked to the facility (`Facilities_Stands`) inside the transaction, `UPDATE`s the ones present in `facility.Stands`, and calls `InsertStand` (shared with `Create`) for every stand in `facility.Stands` whose id is not yet linked. A linked stand absent from `facility.Stands` is left untouched — never deleted. |

## Dependencies

- `IFacilityRepository` / `SqlFacilityRepository`, over the per-request `IDatabaseConnectionProvider` and its transaction: [`database-connection-lifecycle.md`](./database-connection-lifecycle.md).
- `ExceptionToHttpResponseConverters` / `ExceptionHandlingMiddleware` for the `404` (`FacilityNotFound`), `400` (`DuplicatedStand`, reusing `Create.Validation` for the 4-stand cap), and the unmapped `500`/cancelled-request paths: [`error-handling.md`](../error-handling.md) and its technical counterpart.
- `Facility` domain constructor: the same `"Stands cannot exceed 4"` invariant used by facility creation; this endpoint relies on it rather than re-implementing the check.
- OpenAPI contract: `UpdateFacilityStandRequest.Id` is optional and nullable in the served Swagger document (`Gaffer.Api/nswag.json`); `Gaffer.Api.Client` regeneration after this change is the operator's responsibility. The `PUT` behaviour changed from a full replace to an in-place merge — a client-visible change worth calling out when bumping the client version (see [`api-client-versioning.md`](./api-client-versioning.md)).

## Usage Examples

### Update one stand by id, leave the rest and the facility fields as sent

```http
PUT /api/v1/facilities/3d9c... HTTP/1.1
Content-Type: application/json

{
  "name": "Main Stadium",
  "active": true,
  "buildDate": "1998-05-01T00:00:00Z",
  "type": "Stadium",
  "capacity": 40000,
  "address": { "street": "Av. Siempre Viva", "streetNumber": "123", "postalCode": "1000", "city": "Buenos Aires", "region": "CABA", "country": "Argentina" },
  "stands": [
    { "id": "3dc2f263-0000-0000-0000-000000000001", "standName": "North Renovated", "standCapacity": 15000 }
  ]
}
```

Result: the `North` stand keeps its `id` with the new name/capacity; every other existing stand is unchanged (not deleted, not returned in the body).

### Add a stand without an id (facility has fewer than 4 stands)

```json
{ "stands": [ { "standName": "New Stand", "standCapacity": 5000 } ] }
```

Result: a new stand is created with a freshly generated `id`; existing stands are untouched.

### Duplicated existing id (400)

```json
{ "stands": [
    { "id": "3dc2f263-...-01", "standName": "A", "standCapacity": 1 },
    { "id": "3dc2f263-...-01", "standName": "B", "standCapacity": 2 }
] }
```

Returns `400 Bad Request`, `Extensions.ErrorTranslationKey = "Error.Facilities.Update.DuplicatedStand"`; nothing is updated.

### Unrecognized stand once the facility already has 4 (silently discarded)

```json
{ "stands": [
    { "standName": "Renamed North", "id": "3dc2f263-...-01" },
    { "standName": "Ignored", "id": "00000000-0000-0000-0000-000000000000" }
] }
```

With 4 existing stands, the matched one (`North`) is renamed; the unmatched one is dropped without error and without affecting the other 3 stands.

## How to test

- `Gaffer.Api.Tests/Acceptance/FacilityFeaturesShould.cs`: `KeepTheIdsOfTheStandsSentWithTheirIdWhileUpdatingTheirValues`, `KeepTheStandsNotSentAndCreateTheOnesSentWithoutId`, `KeepTheStandsWhenTheUpdateSendsNoStands`, `CreateStandsWithNewIdsForUnknownOrForeignIdsWhenTheFacilityHasLessThanFourStands`, `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands`, `RejectAnUpdateRepeatingTheIdOfAnExistingStand`, `RejectAnUpdateLeavingMoreThanFourStandsWhenTwoExistAndThreeAreSentWithoutId` (and the "one new and one unknown" variant), `UpdateAFacilityFieldsAndAddressKeepingItsFourStands`, `ReturnNotFoundWhenUpdatingAnUnknownFacility`.
- `Gaffer.Api.Tests/Unit/Commands/UpdateFacilityCommandHandlerShould.cs`: `MergeStands` rules in isolation (matching, creation, discarding, duplicated id, over-4 rejection) without touching the database.
- `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs`: `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`, `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` (transaction atomicity), and the cancellation-token propagation tests under `WhenPropagatingTheCancellationToken`.
- `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs`: `CoverEveryConverter` (14 entries) and the `CannotUpdateFacilityDuplicatedStandException` case.
- `Gaffer.Api.Tests/Acceptance/FacilitiesControllerContractShould.cs`: `DeclareTheStandIdOfTheUpdateRequestAsOptionalAndNullableInTheServedOpenApi`.
- Run with `dotnet test gaffer-api.sln` against the Docker MySQL database (see [`database-configuration.md`](../database-configuration.md)).
