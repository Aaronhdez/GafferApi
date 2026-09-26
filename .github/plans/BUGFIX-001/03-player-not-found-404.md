# Paso 03 - 404 para un jugador desconocido

Prerequisito: Q3 resuelta: 404 sin `Title`, `Detail` ni `ErrorTranslationKey` propios, construido con un método auxiliar específico.

## Objetivo

`GET /api/v1/players/{id}` con un id inexistente devuelve 404 ProblemDetails mínimo (el estándar HTTP ya describe el error), construido con un método auxiliar nuevo y dedicado en `ExceptionToHttpResponseConverters`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Método auxiliar nuevo (p. ej. `BuildNotFoundProblemDetails()`) que construye un ProblemDetails con `Status = 404` sin `Title`, `Detail` ni la extensión `ErrorTranslationKey`; mapear `PlayerNotFoundException` a ese método. |
| `Gaffer.Api.Tests/Acceptance/PlayerFeaturesShould.cs` | Test HTTP nuevo. |
| `Gaffer.Api.Tests/Unit/...` (nuevo test del conversor o del middleware, p. ej. `ExceptionToHttpResponseConvertersShould.cs`) | Test unitario. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | `GET /api/v1.0/players/{Guid.NewGuid()}` devuelve `404 NotFound` (hoy 500). |
| AC3.2 | El cuerpo es ProblemDetails con `Status = 404` e `Instance` = ruta de la petición (lo asigna el middleware); `Title` y `Detail` nulos o ausentes, y sin extensión `ErrorTranslationKey`. |
| AC3.2b | El 404 se construye con un método auxiliar nuevo y dedicado; los auxiliares existentes `BuildNotFoundProblemDetailsWithExtension`/`BuildBadRequestProblemDetailsWithExtension` y sus mapeos no cambian (los 404 de Facility y de delete/update de Player conservan su clave). |
| AC3.3 | `PlayerNotFoundException` sigue capturándose y traduciéndose en `DeletePlayerCommandHandler`/`UpdatePlayerCommandHandler`: los 404 de delete/update no cambian (tests existentes en verde). |
| AC3.4 | `GET` de un jugador existente sigue devolviendo 200 (`PlayerFeaturesShould` en verde). |

## Doble bucle

1. **Aceptación**: test HTTP `ReturnNotFoundWhenGettingAnUnknownPlayer` (mismo patrón que `FacilityFeaturesShould.ReturnNotFoundWhenGettingAnUnknownFacility`). Red (500) → green.
2. **Unitario**: test de que `PlayerConverters[typeof(PlayerNotFoundException)]` produce 404 sin `Title`, `Detail` ni `ErrorTranslationKey`. Red (la entrada no existe) → green.
3. **Refactor**: solo si hay duplicación evidente; volver a ejecutar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~ExceptionToHttpResponseConverters"`: red y luego green en `STATUS.md`.
- `dotnet test gaffer-api.sln` completo en verde.
