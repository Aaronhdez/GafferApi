# Player Partial Update — Technical

## Overview

`PlayerController.Update` (`PUT /api/v{version}/players/{id}`) accepts `UpdatePlayerRequest`, whose optional fields (`FirstName`, `LastName`, `SecondLastName` as `string?`, `BirthDate` as `DateTime?`, `Gender` as `Gender?`) map 1:1 onto `UpdatePlayerCommand`, with no string parsing in the controller. `UpdatePlayerCommandHandler.Handle` loads the `Player` via `IPlayerRepository.Get` (same `Id`-filtered search as `PlayerController.GetBy`), applies only the fields present in the command, and persists via `IPlayerRepository.Update`. `SecondLastName` is the only field applied unconditionally: `player.SetSecondLastName(string.IsNullOrEmpty(command.SecondLastName) ? null : command.SecondLastName)` runs on every call, regardless of whether the request set it. An unknown id makes `GetPlayer` throw `PlayerNotFoundException`, caught and rethrown as `CannotUpdatePlayerNotFoundException` (mapped to `404`).

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant PlayerController
    participant UpdatePlayerCommandHandler
    participant IPlayerRepository
    participant Player
    participant ExceptionHandlingMiddleware

    Client->>PlayerController: PUT /players/{id} { firstName: "X" }
    PlayerController->>UpdatePlayerCommandHandler: Handle(UpdatePlayerCommand { Id, FirstName="X", others=null })
    UpdatePlayerCommandHandler->>IPlayerRepository: Get(PlayerSearchCriteria: Id)
    alt no match
        IPlayerRepository-->>UpdatePlayerCommandHandler: []
        UpdatePlayerCommandHandler-->>UpdatePlayerCommandHandler: throw PlayerNotFoundException
        UpdatePlayerCommandHandler-->>ExceptionHandlingMiddleware: throw CannotUpdatePlayerNotFoundException
        ExceptionHandlingMiddleware-->>Client: 404 (Error.Players.Update.PlayerNotFound)
    else match found
        IPlayerRepository-->>UpdatePlayerCommandHandler: [Player]
        UpdatePlayerCommandHandler->>Player: SetFirstName("X") (only if FirstName present)
        UpdatePlayerCommandHandler->>Player: SetSecondLastName(null) (always runs; command.SecondLastName was null)
        Note over UpdatePlayerCommandHandler,Player: SetLastName / SetBirthDate / SetGender skipped: not present in the command
        UpdatePlayerCommandHandler->>IPlayerRepository: Update(player)
        IPlayerRepository-->>UpdatePlayerCommandHandler: ok
        UpdatePlayerCommandHandler-->>PlayerController: ok
        PlayerController-->>Client: 200 (no body)
    end
```

## Methods & Properties Table

| Class | Member | Signature | Return | Description |
|---|---|---|---|---|
| `PlayerController` | `Update` | `Task Update(Guid id, [FromBody] UpdatePlayerRequest request)` | `Task` | `PUT /{id}`. Builds `UpdatePlayerCommand` field-for-field from `request`, with no conversion (no `ToString()`/parsing). |
| `UpdatePlayerRequest` | properties | `string? FirstName/LastName/SecondLastName`, `DateTime? BirthDate`, `Gender? Gender` | — | All fields optional and nullable on the wire (Swagger 2.0: not in `RequiredProperties`, implicitly nullable per NJsonSchema semantics for that schema version). |
| `UpdatePlayerCommand` | properties | same shape as `UpdatePlayerRequest` | — | `ICommand`; carries `Id` plus the same five optional fields, untyped-to-typed conversion is not needed since both are already `DateTime?`/`Gender?`. |
| `UpdatePlayerCommandHandler` | `Handle` | `Task Handle(UpdatePlayerCommand, CancellationToken)` | `Task` | Loads the player, calls `UpdatePlayer`, persists via `IPlayerRepository.Update`. Converts `PlayerNotFoundException` into `CannotUpdatePlayerNotFoundException`; any other exception (e.g. a repository/infrastructure failure) propagates unchanged — see [`error-handling.md`](../error-handling.md). |
| `UpdatePlayerCommandHandler` | `UpdatePlayer` (private, static) | `void UpdatePlayer(Player, UpdatePlayerCommand)` | `void` | `FirstName`/`LastName` applied only `if (!string.IsNullOrEmpty(...))`; `BirthDate`/`Gender` applied only `if (command.X.HasValue)`; `SecondLastName` always applied via `player.SetSecondLastName(string.IsNullOrEmpty(command.SecondLastName) ? null : command.SecondLastName)`. |
| `Player` | `SetSecondLastName` | `void SetSecondLastName(string? newSecondLastName)` | `void` | Unconditional setter. The parameter was renamed from the pre-existing `commandFirstName` naming artifact as part of TECHDEBT-003 (small-changes step); no behaviour change. |
| `Player` | `SetBirthDate` (overload) | `void SetBirthDate(DateTime newBirthDate)` | `void` | Typed overload used by the handler; renamed from `SetBirthDateName(DateTime)` (TECHDEBT-003). The `string`-typed overload was likewise renamed to `SetBirthDate(string newBirthDate)`, and the getter to `GetBirthDate()` (from `SetBirthDateName`/`GetBirthDateName`), used elsewhere (`PlayerShould`). `GetGenderName`/`SetGender(string)` keep their names — `GetGenderName` returns the enum member's name, which is already correct. |
| `Player` | `SetGender` (overload) | `void SetGender(Gender gender)` | `void` | Typed overload used by the handler; the pre-existing `SetGender(string)` overload remains. |
| `CannotUpdatePlayerNotFoundException` | constructor | `CannotUpdatePlayerNotFoundException(string message, Guid id)` | — | Mapped to `404`, `Error.Players.Update.PlayerNotFound` (see [`error-handling.md`](../error-handling.md) for the full converter table). |

## Dependencies

- `IPlayerRepository` / `SqlPlayerRepository` for both the lookup and the persisted update (see [`player-search-and-persistence.md`](./player-search-and-persistence.md) for the lookup side and [`database-connection-lifecycle.md`](./database-connection-lifecycle.md) for the per-request connection it uses).
- `ExceptionToHttpResponseConverters` / `ExceptionHandlingMiddleware` for the `404` mapping and, for any non-`PlayerNotFoundException` failure surfacing from this handler, the `500` unmapped-exception path — both described in [`error-handling.md`](../error-handling.md) and its technical counterpart.
- OpenAPI contract: `Gaffer.Api/nswag.json` generates `Gaffer.Api.Client` from the now-optional/nullable `birthDate`/`gender` request schema; regenerating the client after this change is the operator's responsibility (see [`api-client-versioning.md`](./api-client-versioning.md) for whether this is a MINOR or MAJOR bump — the schema relaxation itself is backward-compatible, but the always-clear behaviour of `secondLastName` is a client-visible behaviour change worth calling out when bumping).

## Usage Examples

### Partial update: only one field changes

```http
PUT /api/v1/players/75d5... HTTP/1.1
Content-Type: application/json

{ "firstName": "Alexia" }
```

Result: `firstName` changes; `lastName`, `birthDate`, `gender` are unchanged; `secondLastName` is cleared to `null` (it was not sent).

### Explicit null on birthDate/gender is a no-op for those fields

```http
PUT /api/v1/players/75d5... HTTP/1.1
Content-Type: application/json

{ "birthDate": null, "gender": null }
```

Result: `birthDate` and `gender` keep their previous values; `secondLastName` is still cleared (it was absent from this request too).

### Replacing secondLastName explicitly

```http
PUT /api/v1/players/75d5... HTTP/1.1
Content-Type: application/json

{ "secondLastName": "Segura" }
```

Result: only `secondLastName` changes, to `"Segura"`; every other field keeps its previous value.

### Unknown id

```http
PUT /api/v1/players/00000000-0000-0000-0000-000000000000 HTTP/1.1
Content-Type: application/json

{ "firstName": "X" }
```

Returns `404 Not Found` with `Extensions.ErrorTranslationKey = "Error.Players.Update.PlayerNotFound"`.

## How to test

- `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs`: `UpdateAPlayer`, `KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls`, `UpdateEveryFieldWhenAllFieldsAreSent`, `KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent`, `ReplaceTheSecondLastNameAndKeepTheRest`, `ReplaceTheSecondLastNameOnUpdate` (×3), `ReturnNotFoundWhenUpdatingAnUnknownPlayer`.
- `Gaffer.Api.Tests/Unit/Commands/UpdatePlayerCommandHandlerShould.cs`: field-by-field application rules and the `PlayerNotFoundException` → `CannotUpdatePlayerNotFoundException` conversion.
- `Gaffer.Api.Tests/Acceptance/OpenApiContractShould.cs`: `DeclareUpdatePlayerRequestFieldAsOptionalAndNullable` — asserts `birthDate`/`gender` are absent from `RequiredProperties` and nullable per the served Swagger 2.0 document.
- Run with `dotnet test gaffer-api.sln` against the Docker MySQL database (see [`database-configuration.md`](./database-configuration.md)).
