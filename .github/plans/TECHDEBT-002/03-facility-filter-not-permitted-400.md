# Paso 03 (ciclo 3) - Filtro no permitido en Facilities → 400

Q5 = a) (misma clave que Players): default aplicado, sin objeción del operador. Sin preguntas abiertas para este paso.

## Objetivo

Un filtro de búsqueda de Facility no permitido produce una excepción de dominio propia (p. ej. `FacilitySearchFilterNotPermittedException`, análoga a `PlayerSearchFilterNotPermittedException`) que el middleware traduce a 400 con `Error.Search.FilterNotPermitted`, en lugar de `ArgumentException` → 500.

## Nivel de test (anotado por el requisito del operador)

Hoy **no hay ruta HTTP** que envíe un filtro de Facility arbitrario: `GET /facilities/{id}` solo usa `Id` (permitido) y `GET /facilities` no filtra. Por tanto el escenario no se puede disparar por HTTP y **no se añade ninguna ruta nueva** (fuera de alcance). La evidencia se reparte en:

| Nivel | Qué demuestra |
| --- | --- |
| Repositorio (MySQL, `SqlFacilityRepositoryShould`) | `Get` con un filtro no permitido lanza la excepción nueva (no `ArgumentException`) y no ejecuta SQL. |
| Conversor (unitario) | La excepción → 400 con `Title`/`Detail`/clave iguales a Players (Q5 = a). |
| Middleware (unitario, `DefaultHttpContext` + `RequestDelegate` que lanza la excepción) | La respuesta HTTP es 400 con el ProblemDetails esperado, cerrando la cadena sin ruta HTTP. |

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Repositories/IFacilityRepository.cs` | Nueva excepción (junto a `FacilitySearchCriteria`, igual que la de Player en `IPlayerRepository.cs`). |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | `BuildFilters` lanza la excepción nueva. |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Conversor nuevo (13.ª entrada). |
| `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs` | Test de repositorio. |
| `Gaffer.Api.Tests/Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` | Test del conversor; el test que recorre todas las claves (AC2.7) incluye la nueva. `NotMapGenericArgumentExceptionsSoFacilitySearchKeepsItsBehaviour` se renombra o ajusta (su nombre deja de ser cierto; `ArgumentException` sigue sin mapearse). |
| `Gaffer.Api.Tests/Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` (nuevo) | Test del middleware para esta excepción. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | `SqlFacilityRepository.Get` con filtro `Name = "Street"` (no permitido) lanza la excepción nueva con el nombre del campo; ninguna fila se lee (el test no depende de datos). |
| AC3.2 | El conversor devuelve `Status = 400`, `Title = "Error.Search.FilterNotPermitted"`, `Detail = "FilterNotPermitted"`, `ErrorTranslationKey = "Error.Search.FilterNotPermitted"` (Q5 = a, iguales a Players). |
| AC3.3 | El middleware, ante esa excepción, escribe 400 `application/json` con esos valores e `Instance` = ruta de la petición. |
| AC3.4 | `ArgumentException` sigue sin conversor (no se mapea de forma genérica). |
| AC3.5 | Los filtros permitidos (`Id`, `Name`, `Active`, `BuildDate`, `Type`, `Capacity`) siguen funcionando: tests existentes de `SqlFacilityRepositoryShould` y `FacilityFeaturesShould` en verde. |

## Doble bucle

1. **Aceptación**: AC3.3 (middleware, nivel más alto disponible sin ruta HTTP). Red: 500 "Unkwnown error" al lanzar `ArgumentException` desde el repositorio real, o compilación fallida por la excepción inexistente (se registra cuál) → green.
2. **Unitario**: AC3.1 y AC3.2. Red → green.
3. **Refactor**: valorar compartir la construcción del ProblemDetails de "filtro no permitido" entre Player y Facility sin cambiar la salida. Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlFacilityRepository|FullyQualifiedName~ExceptionToHttpResponseConverters|FullyQualifiedName~ExceptionHandlingMiddleware"`: red y green en `STATUS.md`.
- `grep -n "ArgumentException" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` → 0.
- Suite completa en verde.
