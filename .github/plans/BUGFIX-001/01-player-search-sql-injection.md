# Paso 01 - Inyección SQL en la búsqueda de jugadores

Prerequisitos: Q1 = 400, Q2 = solo `Id`, `FirstName`, `LastName` (sin distinguir mayúsculas, nota N1), Q6 = fuera de alcance, Q7 = cuerpo del 400 definido (ver AC1.2).

## Objetivo

`SqlPlayerRepository.Get` deja de interpolar nombres de campo y valores en el SQL: los campos se validan contra una lista blanca y los valores se enlazan como parámetros de Dapper, igual que en `SqlFacilityRepository.BuildFilters`. El `CancellationToken` deja de pasarse como `parameters`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | Lista blanca + `DynamicParameters`; eliminar el paso del `CancellationToken` como `parameters`. |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Mapear la excepción de campo no permitido de Players → 400. |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` (o junto a `SqlPlayerRepository`) | Tipo de excepción propio de Players para el campo no permitido. El middleware busca por tipo exacto, y mapear `ArgumentException` en general cambiaría el comportamiento de Facilities, que está fuera de alcance. |
| `Gaffer.Api.Tests/Acceptance/...` (nuevo test HTTP de búsqueda de jugadores o ampliación de `PlayerFeaturesShould.cs`) | Tests de aceptación. |
| `Gaffer.Api.Tests/TestShared/Uris.cs` | Añadir `SearchPlayers()` si hace falta. |
| `Gaffer.Api.Tests/Unit/Repositories/SqlPlayerRepositoryShould.cs` | Tests unitarios nuevos. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | Con 3 jugadores sembrados, `POST /api/v1/players/search` con filtro `{ Name: "FirstName", Value: "x') OR ('1'='1" }` devuelve 200 y 0 jugadores (hoy devuelve los 3). |
| AC1.2 | Un filtro cuyo `Name` no está en la lista blanca (p. ej. `"FirstName IN ('x') OR 1=1 --"`, `"SecondLastName"`, `"Gender"`, `"BirthDate"`) devuelve HTTP 400 con cuerpo ProblemDetails `Status = 400`, `Title = "Error.Search.FilterNotPermitted"`, `Detail = "FilterNotPermitted"`, extensión `ErrorTranslationKey = "Error.Search.FilterNotPermitted"` e `Instance` = ruta de la petición, y no ejecuta SQL (Q7). |
| AC1.3 | Un valor con comilla simple (p. ej. `LastName = "O'Neill"`) encuentra al jugador sembrado con ese apellido en lugar de fallar con un error SQL. |
| AC1.4 | Se mantienen sin cambios los tests existentes: `SqlPlayerRepositoryShould` (vacío, todos, Id, FirstName, LastName, AND estricto, AND/OR condicional, Save/Delete/Update) y `SearchPlayersFeatureShould`. |
| AC1.5 | `SqlPlayerRepository.Get` no pasa `cancellationToken` como argumento `parameters` de `QueryAsync`. |
| AC1.6 | La lista blanca es exactamente `{ Id, FirstName, LastName }` (Q2), comparada sin distinguir mayúsculas (N1): un filtro `"firstname"` sigue funcionando como hoy. |
| AC1.7 | Los 400 de Facilities y de Players ya existentes no cambian; la búsqueda de Facilities con un campo no permitido mantiene su comportamiento actual. |

## Doble bucle

1. **Aceptación**: añadir tests HTTP (vía `TestFixture.GafferApiClientTest`/`TestServer`) para AC1.1-AC1.3. Ejecutarlos: red (AC1.1 devuelve 3, AC1.3 da error). Implementar hasta green.
2. **Unitario**: en `SqlPlayerRepositoryShould`, añadir tests para valor malicioso → 0 resultados, campo no permitido → se lanza la excepción propia de Players, filtro `"firstname"` en minúsculas → resultado correcto, y valor con comilla. Añadir también un test del conversor: excepción propia → ProblemDetails `Status = 400` con los `Title`, `Detail` y `ErrorTranslationKey` de AC1.2. Red → green.
3. **Refactor**: extraer o limpiar la construcción del filtro (sin compartir código con Facilities salvo que sea trivial y no cambie su comportamiento). Volver a ejecutar aceptación + unitarios.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlPlayerRepositoryShould|FullyQualifiedName~SearchPlayers|FullyQualifiedName~PlayerFeaturesShould"`: salida red y luego green en `STATUS.md`.
- Diff de `SqlPlayerRepository.cs` sin interpolación de `Value` ni de `Name` sin validar (AC1.5 y AC1.6 se comprueban leyendo el diff).
- `dotnet test gaffer-api.sln` completo: ≥ 95 tests existentes en verde más los nuevos.
