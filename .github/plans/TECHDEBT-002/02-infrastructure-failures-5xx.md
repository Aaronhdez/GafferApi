# Paso 02 (ciclo 2) - Fallos de infraestructura en Delete/Update → 5xx

Decisiones (ver `00-overview-and-open-questions.md`): Q3 = a) (500 vía middleware; sin excepción ni conversor nuevos), Q4 = b) (se mantienen las 4 excepciones genéricas y sus 4 conversores; `Reason` siempre asignado con un valor de error interno), Q4.1 = `"Internal Server Error"` (literal del operador). Sin preguntas abiertas para este paso.

## Objetivo

Un fallo de MySQL (o cualquier fallo no de negocio) durante Delete/Update de Player y Facility deja de devolverse como 400 y acaba en 500 (Q3 = a). `NotFound` sigue siendo 404 y la validación de Facility sigue siendo 400. Las 4 excepciones genéricas se conservan, pero su `Reason` ya no queda vacío, así que ninguna clave de traducción termina en `.` (Q4). Se añaden los tests que faltan: DELETE de player inexistente → 404 y conversores de Facility y BadRequest.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Commands/DeletePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs`, `DeleteFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs` | Solo traducir `PlayerNotFoundException`/`FacilityNotFoundException` a `CannotDelete*/CannotUpdate*NotFoundException`; el resto se propaga sin envolver (Q3 = a). Las clases `CannotDeletePlayerException`, `CannotUpdatePlayerException`, `CannotDeleteFacilityException` y `CannotUpdateFacilityException` se mantienen; su `Reason` se asigna siempre con `"Internal Server Error"` (Q4.1; constante en la clase). |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Los 4 conversores genéricos se mantienen (estado 400, `Title`/`Detail` actuales); solo cambia que la clave incluye un `Reason` no vacío. Sin conversores nuevos. |
| `Gaffer.Api.Tests/Unit/Commands/*CommandHandlerShould.cs` (Delete/Update de Player y Facility) | Los tests `ThrowExceptionIfRepositoryFails*` pasan a esperar la excepción original. |
| `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` | Tests de todos los conversores. |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | DELETE de player inexistente → 404. |
| `Gaffer.Api.Tests/Acceptance/` (nuevo, p. ej. `DatabaseFailureShould.cs`) + helper de host en `Gaffer.Api.Tests/TestShared/` si hace falta | `TestServer` aparte con `IDatabaseConnectionProvider` apuntando a `TestConfiguration.UnreachableDatabaseConnectionString`. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Con la BD inalcanzable, `DELETE /players/{id}`, `PUT /players/{id}`, `DELETE /facilities/{id}` y `PUT /facilities/{id}` devuelven **500** (hoy 400). El test asserta el estado, no el `Title` (la errata `"Unkwnown error"` se corrige en el paso 04). |
| AC2.2 | Unitario: si el repositorio lanza (p. ej. `InvalidOperationException` o la excepción de MySQL registrada) en `Get` o en `Delete/Update`, el handler no lanza `CannotDelete*/CannotUpdate*Exception` genérica; propaga la excepción original (mismo tipo e instancia). |
| AC2.3 | `NotFound` sin cambios: DELETE/PUT de player y facility inexistentes → 404 con sus claves actuales (`Error.Players.Delete.PlayerNotFound`, etc.). |
| AC2.4 | Nuevo test de aceptación: `DELETE /players/{guid inexistente}` → 404 con `ErrorTranslationKey = Error.Players.Delete.PlayerNotFound`. |
| AC2.5 | `PUT /facilities/{id}` con más de 4 gradas sigue devolviendo 400 `Error.Facilities.Create.Validation` (regresión de `RejectUpdatingAFacilityWithMoreThanFourStands`). |
| AC2.6 | Q4/Q4.1: una instancia de cada una de las 4 excepciones genéricas, creada con su constructor actual (`(message, id)`), tiene `Reason = "Internal Server Error"`, y su conversor devuelve exactamente (según el código actual de `ExceptionToHttpResponseConverters.cs`, que solo cambia en el valor de `Reason`): ver tabla "Valores literales de AC2.6" abajo. Ninguna `ErrorTranslationKey` producida por un conversor termina en `.`. |
| AC2.7 | `ExceptionToHttpResponseConvertersShould` cubre **cada** una de las 12 entradas del diccionario: estado HTTP, `Title` y `ErrorTranslationKey`. Incluye un test que recorre todas las claves del diccionario y falla si alguna no tiene expectativa, para que un conversor nuevo sin test se detecte. Las 4 genéricas usan los valores de la tabla de AC2.6. |

### Valores literales de AC2.6 (con `id` = el `Guid` pasado al constructor)

| Excepción | Estado | `Title` | `Detail` | `ErrorTranslationKey` |
| --- | --- | --- | --- | --- |
| `CannotDeletePlayerException` | 400 | `Cannot delete player` | `Player {id} cannot be deleted` | `Error.Players.Delete.Internal Server Error` |
| `CannotUpdatePlayerException` | 400 | `Cannot update player` | `Player {id} cannot be updated` | `Error.Players.Update.Internal Server Error` |
| `CannotDeleteFacilityException` | 400 | `Cannot delete facility` | `Facility {id} cannot be deleted` | `Error.Facilities.Delete.Internal Server Error` |
| `CannotUpdateFacilityException` | 400 | `Cannot update facility` | `Facility {id} cannot be updated` | `Error.Facilities.Update.Internal Server Error` |

`Title` y `Detail` no dependen de `Reason` y no cambian. La clave contiene espacios porque `Reason` se interpola literalmente (`$"Error.<X>.<Op>.{Reason}"`); el plan no transforma el valor dado por el operador.
| AC2.8 | Suite completa en verde. |

## Doble bucle

1. **Aceptación**: AC2.1 (red: 400) y AC2.4 (puede estar green desde el inicio: se registra como caracterización). AC2.3/AC2.5 como regresión. → green.
2. **Unitario**: AC2.2 en los 4 `*CommandHandlerShould` (red: hoy esperan/obtienen la excepción genérica) y AC2.6-AC2.7 en `ExceptionToHttpResponseConvertersShould`. → green.
3. **Refactor**: eliminar el `switch` redundante en los `catch` (filtros `catch (X ex) when ...` o `catch` específico). Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~DatabaseFailure|FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~FacilityFeaturesShould|FullyQualifiedName~CommandHandlerShould|FullyQualifiedName~ExceptionToHttpResponseConverters"`: red y green en `STATUS.md`, con el código HTTP del red de AC2.1.
- En `STATUS.md`: tipo exacto de la excepción que lanza `MySql.Data` con la BD inalcanzable.
- `grep -rn "Reason" Gaffer.Api`: las 4 propiedades `Reason` tienen valor asignado (ninguna `{ get; }` sin inicializar) y los 4 conversores siguen usándola.
- `grep -rn "new Cannot\(Delete\|Update\)\(Player\|Facility\)Exception" Gaffer.Api` → 0 (con Q3 = a ningún handler las lanza; se registra en `STATUS.md`).
- Suite completa en verde.

## Notas

- `GET /players/{id}` con la BD inalcanzable ya da 500 (el query handler no envuelve). Sirve de referencia para AC2.1.
- El log del 500 es el paso 04; en este paso no se toca el middleware.
- Los 4 conversores genéricos mantienen el estado 400 (Q4 "Mantenlas"; el operador no pidió 500 al responder Q4.1; ver nota en `00`).
