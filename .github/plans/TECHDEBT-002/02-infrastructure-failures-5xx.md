# Paso 02 (ciclo 2) - Fallos de infraestructura en Delete/Update → 5xx

Prerequisito: Q3 (500 o 503) y Q4 (`Reason` / excepciones genéricas) respondidas.

## Objetivo

Un fallo de MySQL (o cualquier fallo no de negocio) durante Delete/Update de Player y Facility deja de devolverse como 400 y acaba en 5xx (código según Q3). `NotFound` sigue siendo 404 y la validación de Facility sigue siendo 400. Ninguna clave de traducción termina en `.` (Q4). Se añaden los tests que faltan: DELETE de player inexistente → 404 y conversores de Facility y BadRequest.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Commands/DeletePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs`, `DeleteFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs` | Solo traducir `PlayerNotFoundException`/`FacilityNotFoundException` a `CannotDelete*/CannotUpdate*NotFoundException`; el resto se propaga (Q3 = a) o se envuelve en la excepción de infraestructura (Q3 = b). Excepciones genéricas y `Reason` según Q4. |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Quitar/ajustar los 4 conversores genéricos (Q4); añadir el de infraestructura si Q3 = b. |
| `Gaffer.Api.Tests/Unit/Commands/*CommandHandlerShould.cs` (Delete/Update de Player y Facility) | Los tests `ThrowExceptionIfRepositoryFails*` pasan a esperar la excepción original (o la de infraestructura). |
| `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` | Tests de todos los conversores. |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | DELETE de player inexistente → 404. |
| `Gaffer.Api.Tests/Acceptance/` (nuevo, p. ej. `DatabaseFailureShould.cs`) + helper de host en `Gaffer.Api.Tests/TestShared/` si hace falta | `TestServer` aparte con `IDatabaseConnectionProvider` apuntando a `TestConfiguration.UnreachableDatabaseConnectionString`. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Con la BD inalcanzable, `DELETE /players/{id}`, `PUT /players/{id}`, `DELETE /facilities/{id}` y `PUT /facilities/{id}` devuelven el código de Q3 (hoy 400). |
| AC2.2 | Unitario: si el repositorio lanza (p. ej. `InvalidOperationException` o la excepción de MySQL registrada) en `Get` o en `Delete/Update`, el handler no lanza `CannotDelete*/CannotUpdate*Exception` genérica; lanza la original (Q3 = a) o la de infraestructura (Q3 = b). |
| AC2.3 | `NotFound` sin cambios: DELETE/PUT de player y facility inexistentes → 404 con sus claves actuales (`Error.Players.Delete.PlayerNotFound`, etc.). |
| AC2.4 | Nuevo test de aceptación: `DELETE /players/{guid inexistente}` → 404 con `ErrorTranslationKey = Error.Players.Delete.PlayerNotFound`. |
| AC2.5 | `PUT /facilities/{id}` con más de 4 gradas sigue devolviendo 400 `Error.Facilities.Create.Validation` (regresión de `RejectUpdatingAFacilityWithMoreThanFourStands`). |
| AC2.6 | Ninguna `ErrorTranslationKey` producida por un conversor termina en `.` ni contiene `Reason` vacío (según Q4). |
| AC2.7 | `ExceptionToHttpResponseConvertersShould` cubre **cada** entrada del diccionario tras el cambio: estado HTTP, `Title` y `ErrorTranslationKey`. Incluye un test que recorre todas las claves del diccionario y falla si alguna no tiene expectativa, para que un conversor nuevo sin test se detecte. |
| AC2.8 | Suite completa en verde. |

## Doble bucle

1. **Aceptación**: AC2.1 (red: 400) y AC2.4 (puede estar green desde el inicio: se registra como caracterización). AC2.3/AC2.5 como regresión. → green.
2. **Unitario**: AC2.2 en los 4 `*CommandHandlerShould` (red: hoy esperan/obtienen la excepción genérica) y AC2.6-AC2.7 en `ExceptionToHttpResponseConvertersShould`. → green.
3. **Refactor**: eliminar el `switch` redundante en los `catch` (filtros `catch (X ex) when ...` o `catch` específico). Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~DatabaseFailure|FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~FacilityFeaturesShould|FullyQualifiedName~CommandHandlerShould|FullyQualifiedName~ExceptionToHttpResponseConverters"`: red y green en `STATUS.md`, con el código HTTP del red de AC2.1.
- En `STATUS.md`: tipo exacto de la excepción que lanza `MySql.Data` con la BD inalcanzable.
- `grep -n "Reason" Gaffer.Api` según Q4 (0 resultados si Q4 = a/c).
- Suite completa en verde.

## Notas

- `GET /players/{id}` con la BD inalcanzable ya da 500 (el query handler no envuelve). Sirve de referencia para AC2.1 si Q3 = a.
- El log del 500 es el paso 04; aquí no se toca el middleware salvo el conversor de Q3 = b.
