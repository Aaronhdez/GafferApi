# Facility CRUD (Select, Update, Delete)

## Overview

This document covers the Facility operations that extend the initial creation flow (see [facility-create.md](./facility-create.md)): retrieving a single facility, listing all facilities, updating a facility (full replace of `Address` and `Stands`), and deleting a facility. It mirrors the full-CRUD pattern already used for `Player`, and is exposed through `FacilitiesController` backed by `SearchFacilitiesQuery`, `UpdateFacilityCommand`, and `DeleteFacilityCommand`.

- **Select** is implemented as `GET /{id}` (single facility, via a filtered `SearchFacilitiesQuery`) and `GET ""` (all facilities, unfiltered).
- **Update** replaces `Address` fully and replaces the `Stands` collection fully (delete + reinsert), not a partial/merge update.
- **Delete** is a hard delete, cascading removal of the facility's `Address` and `Stands` join rows before deleting the `Facilities` row itself.

## Sequence/Architecture Diagram

```mermaid
sequenceDiagram
    participant Client
    participant FacilitiesController
    participant SearchFacilitiesQueryHandler
    participant UpdateFacilityCommandHandler
    participant DeleteFacilityCommandHandler
    participant IFacilityRepository
    participant SqlFacilityRepository
    participant Database

    Client->>FacilitiesController: GET /{id}
    FacilitiesController->>SearchFacilitiesQueryHandler: Handle(query with Id filter)
    SearchFacilitiesQueryHandler->>IFacilityRepository: Get(criteria)
    IFacilityRepository->>SqlFacilityRepository: Get implementation
    SqlFacilityRepository->>Database: SELECT Facilities + Address + Stands
    Database-->>SqlFacilityRepository: rows
    SqlFacilityRepository-->>SearchFacilitiesQueryHandler: List<Facility>
    SearchFacilitiesQueryHandler-->>FacilitiesController: SearchFacilityResult / FacilityNotFoundException
    FacilitiesController-->>Client: 200 SearchFacilityResult | 404 not found

    Client->>FacilitiesController: PUT /{id} (UpdateFacilityRequest)
    FacilitiesController->>UpdateFacilityCommandHandler: Handle(UpdateFacilityCommand)
    UpdateFacilityCommandHandler->>IFacilityRepository: Get(criteria by Id)
    IFacilityRepository-->>UpdateFacilityCommandHandler: existing Facility or FacilityNotFoundException
    UpdateFacilityCommandHandler->>UpdateFacilityCommandHandler: BuildUpdatedFacility (replace Address + Stands)
    UpdateFacilityCommandHandler->>IFacilityRepository: Update(facility)
    IFacilityRepository->>SqlFacilityRepository: Update implementation
    SqlFacilityRepository->>Database: UPDATE Facilities; delete+insert Address/Stands (transaction)
    SqlFacilityRepository-->>UpdateFacilityCommandHandler: void / exception
    UpdateFacilityCommandHandler-->>FacilitiesController: void / CannotUpdateFacility(NotFound)Exception
    FacilitiesController-->>Client: 200 OK | 404 / 4xx error

    Client->>FacilitiesController: DELETE /{id}
    FacilitiesController->>DeleteFacilityCommandHandler: Handle(DeleteFacilityCommand)
    DeleteFacilityCommandHandler->>IFacilityRepository: Get(criteria by Id)
    IFacilityRepository-->>DeleteFacilityCommandHandler: existing Facility or null
    DeleteFacilityCommandHandler->>IFacilityRepository: Delete(id)
    IFacilityRepository->>SqlFacilityRepository: Delete implementation
    SqlFacilityRepository->>Database: DELETE Stands/Address links, DELETE Facilities row (transaction)
    SqlFacilityRepository-->>DeleteFacilityCommandHandler: void / exception
    DeleteFacilityCommandHandler-->>FacilitiesController: void / CannotDeleteFacility(NotFound)Exception
    FacilitiesController-->>Client: 200 OK | 404 / 4xx error
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `FacilitiesController` | `GetBy` | `Task<SearchFacilityResult> GetBy(Guid id)` | `SearchFacilityResult` | `GET /{id}`. Runs a `SearchFacilitiesQuery` filtered by `Id`; throws `FacilityNotFoundException` if no match. |
| `FacilitiesController` | `GetAll` | `Task<List<SearchFacilityResult>> GetAll()` | `List<SearchFacilityResult>` | `GET ""`. Runs an unfiltered `SearchFacilitiesQuery`. |
| `FacilitiesController` | `Update` | `Task Update(Guid id, UpdateFacilityRequest request)` | `Task` | `PUT /{id}`. Maps request to `UpdateFacilityCommand` and delegates to the handler. |
| `FacilitiesController` | `Delete` | `Task Delete(Guid id)` | `Task` | `DELETE /{id}`. Delegates to `DeleteFacilityCommandHandler`. |
| `SearchFacilitiesQuery` | `Filters` | `List<QueryFilter> Filters { get; }` | — | Optional name/value filters (e.g. `Id`) applied to the search. |
| `SearchFacilitiesQueryHandler` | `Handle` | `Task<List<SearchFacilityResult>> Handle(SearchFacilitiesQuery, CancellationToken)` | `List<SearchFacilityResult>` | Builds `FacilitySearchCriteria` from filters, fetches via `IFacilityRepository.Get`, maps to `SearchFacilityResult` (with nested `Address`/`Stands`). |
| `UpdateFacilityCommand` | properties | `Id, Name, Active, BuildDate, Type, Capacity, Address, Stands` | — | Full replacement payload for a facility, including nested `UpdateFacilityAddressCommand` and `UpdateFacilityStandCommand` list. |
| `UpdateFacilityCommandHandler` | `Handle` | `Task Handle(UpdateFacilityCommand, CancellationToken)` | `Task` | Loads the existing facility, rebuilds it with the new `Name/Active/BuildDate/Type/Capacity/Address/Stands`, and persists via `IFacilityRepository.Update`. Wraps failures in `CannotUpdateFacilityNotFoundException` / `CannotUpdateFacilityException`. |
| `DeleteFacilityCommand` | record | `DeleteFacilityCommand(Guid Id)` | — | Command identifying the facility to delete. |
| `DeleteFacilityCommandHandler` | `Handle` | `Task Handle(DeleteFacilityCommand, CancellationToken)` | `Task` | Verifies the facility exists (throws `FacilityNotFoundException` otherwise), then deletes via `IFacilityRepository.Delete`. Wraps failures in `CannotDeleteFacilityNotFoundException` / `CannotDeleteFacilityException`. |
| `IFacilityRepository` | `Get` | `Task<List<Facility>> Get(FacilitySearchCriteria, CancellationToken)` | `List<Facility>` | Returns facilities matching the criteria, including joined `Address` and `Stands`. |
| `IFacilityRepository` | `Update` | `Task Update(Facility, CancellationToken)` | `Task` | Persists a full facility replacement. |
| `IFacilityRepository` | `Delete` | `Task Delete(Guid, CancellationToken)` | `Task` | Removes a facility and its related rows. |
| `SqlFacilityRepository` | `Get` | see interface | `List<Facility>` | Builds a dynamic `WHERE` clause from an allow-list of columns (`Id, Name, Active, BuildDate, Type, Capacity`), then loads `Address`/`Stands` per row via separate joined queries. |
| `SqlFacilityRepository` | `Update` | see interface | `Task` | Single transaction: updates the `Facilities` row, deletes existing `Stands`/`Address` links, then reinserts `Address` and `Stands` (full replace). |
| `SqlFacilityRepository` | `Delete` | see interface | `Task` | Single transaction: deletes `Stands` and `Address` links/rows, then deletes the `Facilities` row. |

## Dependencies

- `IFacilityRepository` / `SqlFacilityRepository` (Dapper-based, via `IDatabaseConnectionProvider`).
- `ICommandHandler<TCommand>` / `IQueryHandler<TQuery, TResult>` abstractions (CQRS pipeline).
- `ExceptionToHttpResponseConverters` — maps `FacilityNotFoundException`, `CannotDeleteFacilityNotFoundException`, `CannotDeleteFacilityException`, `CannotUpdateFacilityNotFoundException`, and `CannotUpdateFacilityException` to HTTP error responses.
- DI registrations in `ActionsStartup` (command/query handlers) and `RepositoriesStartup` (`IFacilityRepository`).
- Domain types: `Facility`, `Address`, `Stand`, `FacilityType`.

## Usage Examples

```csharp
// Get a single facility
var facility = await facilitiesController.GetBy(facilityId);

// List all facilities
var all = await facilitiesController.GetAll();

// Update a facility (full replace of Address and Stands)
await facilitiesController.Update(facilityId, new UpdateFacilityRequest
{
    Name = "Main Stadium",
    Active = true,
    BuildDate = new DateTime(1998, 5, 1),
    Type = FacilityType.Stadium,
    Capacity = 40000,
    Address = new UpdateFacilityAddressRequest
    {
        Street = "Av. Siempre Viva",
        StreetNumber = "123",
        PostalCode = "1000",
        City = "Buenos Aires",
        Region = "CABA",
        Country = "Argentina"
    },
    Stands = new List<UpdateFacilityStandRequest>
    {
        new() { StandName = "North", StandCapacity = 10000 },
        new() { StandName = "South", StandCapacity = 10000 }
    }
});

// Delete a facility
await facilitiesController.Delete(facilityId);
```

## Known Issues / Tech Debt

- `CannotDeleteFacilityException.Reason` is declared as a `get`-only property but is never assigned in the constructor, so it is always `null`. `ExceptionToHttpResponseConverters` builds an error code `Error.Facilities.Delete.{Reason}` using this value, which will always render as `Error.Facilities.Delete.` — this should be fixed by populating `Reason` from the caught exception.
- `SqlFacilityRepository.Get` builds its `WHERE` clause by string concatenation (column names interpolated directly), relying solely on the `AllowedFilterColumns` allow-list for injection safety; parameter *values* are bound via Dapper parameters, but this pattern should be reviewed if more filter columns are added.
- `Update` and `Delete` perform delete+reinsert of `Address`/`Stands` on every update rather than diffing, which is simple but not efficient for large `Stands` collections.
