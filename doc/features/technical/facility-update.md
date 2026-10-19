# Facility Update (Stands Matched by Id) — Technical

## Overview

`FacilitiesController.Update` (`PUT /api/v{version}/facilities/{id}`) maps `UpdateFacilityRequest` field-for-field onto `UpdateFacilityCommand`, including `List<UpdateFacilityStandRequest>? Stands` → `List<UpdateFacilityStandCommand>?` (each carrying an optional `Guid? Id`). `UpdateFacilityCommandHandler.Handle` calls `IFacilityRepository.Update(id, buildUpdated, ct)`; the repository opens one transaction, locks the facility row with `SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE`, loads the existing `Facility` inside that transaction and passes it to the handler's `buildUpdated` callback, which rebuilds its `Address` from the request (keeping the existing `Address.Id`) and merges the requested stands with `MergeStands` — a pure function with no I/O. The handler first collects the validation errors (`Facility.CheckRequestedStands` for the stands sent, by index, then `Facility.Create` for the facility fields, address and the merged stand count) and throws a single `ValidationFailedException` (`Error.Facilities.Update.Validation`, `400`) if any rule is broken; stored stands that are kept are not revalidated (Q16). The resulting `Facility` is then written by the same transaction: the facility row, the address row, and each stand in place (updating matched stands, inserting new ones); it never deletes a stand or address row. Because the lock is taken before the read, two concurrent updates of one facility are serialized and the "fewer than 4 stands" decision never rests on a stale read. An unknown facility id makes the lock step throw `FacilityNotFoundException`, caught and rethrown as `CannotUpdateFacilityNotFoundException` (mapped to `404`); a repeated existing-stand `Id` makes `MergeStands` throw `CannotUpdateFacilityDuplicatedStandException` (mapped to `400`); any invalid field, or more than 4 stands sent or resulting from the merge, becomes `ValidationFailedException` (`400`, `Error.Facilities.Update.Validation`, `errors`; see [`input-validation.md`](./input-validation.md)). The unknown-id `404` is raised by the lock step, before the body is checked. The public repository contract has a single update, `Update(Guid id, Func<Facility, Facility> buildUpdated, CancellationToken)`; the former `Update(Facility)` is a private detail of `SqlFacilityRepository` and takes no part in the contract. Both callback failures roll the transaction back. Per-stand operations live in [`facility-stands.md`](./facility-stands.md).

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
    UpdateFacilityCommandHandler->>IFacilityRepository: Update(id, buildUpdated)
    IFacilityRepository->>SqlFacilityRepository: Update(id, buildUpdated)
    SqlFacilityRepository->>Database: BEGIN; SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE
    alt no row
        SqlFacilityRepository-->>UpdateFacilityCommandHandler: throw FacilityNotFoundException (rollback)
        UpdateFacilityCommandHandler-->>ExceptionHandlingMiddleware: throw CannotUpdateFacilityNotFoundException
        ExceptionHandlingMiddleware-->>Client: 404 (Error.Facilities.Update.FacilityNotFound)
    else row locked
        SqlFacilityRepository->>Database: read facility, address, stands (same transaction)
        SqlFacilityRepository->>UpdateFacilityCommandHandler: buildUpdated(existing)
        UpdateFacilityCommandHandler->>UpdateFacilityCommandHandler: MergeStands(existing, requested)
        Note over UpdateFacilityCommandHandler: matched by Id -> updated in place<br/>no/unknown Id, <4 stands -> created (new Id)<br/>no/unknown Id, 4 stands -> discarded<br/>repeated existing Id -> throw DuplicatedStand
        UpdateFacilityCommandHandler->>Facility: CheckRequestedStands(...) + Facility.Create(..., address, mergedStands)
        alt invalid field or more than 4 stands
            Facility-->>UpdateFacilityCommandHandler: DomainValidationException (all fields)
            SqlFacilityRepository->>Database: ROLLBACK
            ExceptionHandlingMiddleware-->>Client: 400 (Error.Facilities.Update.Validation, Errors)
        else valid
            Facility-->>SqlFacilityRepository: updated facility
            SqlFacilityRepository->>Database: UPDATE Facilities; UPDATE Addresses; UPDATE/INSERT Stands (no DELETE); COMMIT
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
| `UpdateFacilityCommandHandler` | `Handle` | `Task Handle(UpdateFacilityCommand, CancellationToken)` | `Task` | Calls `IFacilityRepository.Update(command.Id, existing => BuildUpdatedFacility(existing, command), ct)`. Converts `FacilityNotFoundException` into `CannotUpdateFacilityNotFoundException`; `DomainValidationException` into `ValidationFailedException` (`400`, `Error.Facilities.Update.Validation`); any other exception (repository/infrastructure failure, `CannotUpdateFacilityDuplicatedStandException`) propagates unchanged — see [`error-handling.md`](../error-handling.md). |
| `UpdateFacilityCommandHandler` | `BuildUpdatedFacility` (private, static) | `Facility BuildUpdatedFacility(Facility existing, UpdateFacilityCommand command)` | `Facility` | Rebuilds `Address` with `existing.Address.Id` (the address is updated in place, never replaced), and calls `MergeStands` for the stand list. Collects `Facility.CheckRequestedStands` for the stands sent and `Facility.Create` (name, build date, type, capacity, address, merged stand count) and throws one `DomainValidationException` with all the errors; stored stands are not revalidated. |
| `UpdateFacilityCommandHandler` | `MergeStands` (private, static) | `List<Stand> MergeStands(Facility existing, List<UpdateFacilityStandCommand> requested)` | `List<Stand>` | Pure function, no I/O. A requested stand is "matched" when its `Id` is one of `existing.Stands`' ids. Matched stands with a repeated `Id` throw `CannotUpdateFacilityDuplicatedStandException` before anything else runs. Existing stands are kept (updated in place if matched, unchanged otherwise); unmatched requested stands become new `Stand(Guid.NewGuid(), ...)` entries only while `existing.Stands.Count < 4`, otherwise they are dropped. |
| `Stand` | constructor | `Stand(Guid Id, string StandName, int StandCapacity)` | — | Immutable value; `MergeStands` always creates a new `Stand` with the surviving `Id` (existing or freshly generated) rather than mutating one. |
| `CannotUpdateFacilityDuplicatedStandException` | constructor | `CannotUpdateFacilityDuplicatedStandException(Guid id, Guid standId)` | — | Mapped to `400`, `Error.Facilities.Update.DuplicatedStand` (see [`error-handling.md`](../error-handling.md) for the converter table). |
| `SqlFacilityRepository` | `Update` (by id) | `Task Update(Guid id, Func<Facility, Facility> buildUpdated, CancellationToken)` | `Task` | One `ExecuteInTransactionAsync`: `LockFacility` (`SELECT ... FOR UPDATE`, throws `FacilityNotFoundException` when no row), reads the facility, calls `buildUpdated`, then the private `Update(Facility, ...)` (no longer part of `IFacilityRepository`) (`UPDATE Facilities`, `UpdateAddress`, `UpdateStands`). Never calls `DeleteStands`/`DeleteAddress` (those are only reachable from `Delete`). |
| `SqlFacilityRepository` | `UpdateAddress` (private) | `Task UpdateAddress(Facility, CancellationToken)` | `Task` | `UPDATE Addresses ... WHERE Id IN (SELECT AddressId FROM Facilities_Addresses WHERE FacilityId = @FacilityId)`. The address row's `Id` and its `Facilities_Addresses` link are untouched. |
| `SqlFacilityRepository` | `UpdateStands` (private) | `Task UpdateStands(Facility, CancellationToken)` | `Task` | Reads the stand ids already linked to the facility (`Facilities_Stands`) inside the transaction, `UPDATE`s the ones present in `facility.Stands`, and calls `InsertStand` (shared with `Create`) for every stand in `facility.Stands` whose id is not yet linked. A linked stand absent from `facility.Stands` is left untouched — never deleted. |

## Dependencies

- `IFacilityRepository` / `SqlFacilityRepository`, over the per-request `IDatabaseConnectionProvider` and its transaction: [`database-connection-lifecycle.md`](./database-connection-lifecycle.md).
- `ExceptionToHttpResponseConverters` / `ExceptionHandlingMiddleware` for the `404` (`FacilityNotFound`), `400` (`DuplicatedStand`; validation through `ValidationFailedException`), and the unmapped `500`/cancelled-request paths: [`error-handling.md`](../error-handling.md) and its technical counterpart.
- `Facility.Create` / `Facility.CheckRequestedStands` / `DomainValidationErrors`: field rules and the 4-stand cap, shared with creation (`Stands` error: `Stands cannot exceed 4.`). The `Facility` constructor keeps only the structural guard used to rehydrate stored rows.
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

Returns `400 Bad Request`, `errorTranslationKey = "Error.Facilities.Update.DuplicatedStand"`; nothing is updated.

### Unrecognized stand once the facility already has 4 (silently discarded)

```json
{ "stands": [
    { "standName": "Renamed North", "id": "3dc2f263-...-01" },
    { "standName": "Ignored", "id": "00000000-0000-0000-0000-000000000000" }
] }
```

With 4 existing stands, the matched one (`North`) is renamed; the unmatched one is dropped without error and without affecting the other 3 stands.

### More than 4 stands sent to a facility that already has 4 (400)

```json
{ "stands": [ { "standName": "A", "standCapacity": 1 }, { "standName": "B", "standCapacity": 1 }, { "standName": "C", "standCapacity": 1 }, { "standName": "D", "standCapacity": 1 }, { "standName": "E", "standCapacity": 1 } ] }
```

Returns `400`, `Error.Facilities.Update.Validation`, `Errors.Stands = ["Stands cannot exceed 4."]`; nothing is updated. Earlier versions answered `200` and discarded the extra stands; the operator confirmed the `400` (TECHDEBT-005). Other fields of the request (name, build date, address ...) must also be valid; see [`input-validation.md`](./input-validation.md).

## How to test

- `Gaffer.Api.Tests/Acceptance/FacilityFeaturesShould.cs`: `KeepTheIdsOfTheStandsSentWithTheirIdWhileUpdatingTheirValues`, `KeepTheStandsNotSentAndCreateTheOnesSentWithoutId`, `KeepTheStandsWhenTheUpdateSendsNoStands`, `CreateStandsWithNewIdsForUnknownOrForeignIdsWhenTheFacilityHasLessThanFourStands`, `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands`, `RejectAnUpdateRepeatingTheIdOfAnExistingStand`, `RejectAnUpdateLeavingMoreThanFourStandsWhenTwoExistAndThreeAreSentWithoutId` (and the "one new and one unknown" variant), plus the validation cases (`Errors` per field with `Stands[i]` names, more than 4 stands sent to a facility that already has 4 → `400`, `404` before `400`, stored invalid data still read) in the same file, `UpdateAFacilityFieldsAndAddressKeepingItsFourStands`, `ReturnNotFoundWhenUpdatingAnUnknownFacility`.
- `Gaffer.Api.Tests/Unit/Commands/Facilities/UpdateFacilityCommandHandlerShould.cs`: `MergeStands` rules in isolation (matching, creation, discarding, duplicated id, over-4 rejection) without touching the database.
- `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs`: `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`, `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` (transaction atomicity), and the cancellation-token propagation tests under `WhenPropagatingTheCancellationToken`.
- `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs`: `CoverEveryConverter` (14 entries) and the `CannotUpdateFacilityDuplicatedStandException` case.
- `Gaffer.Api.Tests/Acceptance/FacilitiesControllerContractShould.cs`: `DeclareTheStandIdOfTheUpdateRequestAsOptionalAndNullableInTheServedOpenApi`.
- Run with `dotnet test gaffer-api.sln` against the Docker MySQL database (see [`database-configuration.md`](../database-configuration.md)).
