# Paso 02 - Restos sin uso

Prerrequisito: Q7 resuelta ("Ok, lo podemos quitar" → borrar `LoggingStartup`).

## Objetivo

Eliminar código muerto sin cambiar comportamiento: `LoggingStartup` y las clases y miembros que no referencia nadie.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Commands/DeletePlayerCommandHandler.cs` | Borrar `DeletePlayerResult`. |
| `Gaffer.Api/Commands/DeleteFacilityCommandHandler.cs` | Borrar `DeleteFacilityResult`. |
| `Gaffer.Api/Commands/UpdatePlayerCommandHandler.cs` | Borrar `UpdatePlayerResult` y quitar `ICommand<UpdatePlayerResult>` de `UpdatePlayerCommand` (se queda `ICommand`). |
| `Gaffer.Api/Controllers/PlayerController.cs` | Borrar `DeletePlayerRequest`. |
| `Gaffer.Api/Shared/Exceptions/ExceptionHandlingMiddleware.cs` | Borrar el método privado `GetProblemDetails`. |
| `Gaffer.Api/Startups/LoggingStartup.cs`, `Gaffer.Api/Startup.cs` | Borrar el fichero y la llamada `services.ConfigureLogging(...)`. El `LoggerFactory` de arranque de `Startup.ConfigureServices` (BUGFIX-001) no cambia. |

Antes de borrar cada símbolo, BUILDER vuelve a buscarlo (`Grep`) en `Gaffer.Api`, `Gaffer.Api.Tests`, `Gaffer.Api.Migrations` y en `Gaffer.Api.Client` (solo lectura, para confirmar que no hay referencias; no se edita). Si aparece una referencia, no lo borra y lo anota en `STATUS.md`.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Una búsqueda de `DeletePlayerResult`, `DeleteFacilityResult`, `UpdatePlayerResult`, `DeletePlayerRequest` y `GetProblemDetails` en `*.cs` (sin `bin/`/`obj/`) no devuelve resultados. |
| AC2.2 | No existen `LoggingStartup` ni `ConfigureLogging` en `Gaffer.Api`. |
| AC2.3 | El aviso de base de datos inalcanzable de BUGFIX-001 se sigue emitiendo: los tests `DatabaseStartupShould` que lo comprueban siguen en verde. |
| AC2.4 | `dotnet build gaffer-api.sln -v q`: 0 errores y no más advertencias que en la línea base. |
| AC2.5 | `dotnet test gaffer-api.sln`: mismo número de tests en verde que en la línea base (127), 0 omitidos. |

## Doble bucle

Limpieza sin comportamiento: no hay test red nuevo.
1. **Aceptación**: suite de aceptación en verde antes y después (`FullyQualifiedName~Acceptance`).
2. **Unitario**: suite unitaria e integración en verde antes y después.
3. **Refactor**: build + suite completa.

## Cómo demostrarlo

- Resultado de la búsqueda de AC2.1, build y `dotnet test gaffer-api.sln --verbosity quiet` (recuento antes/después) en `STATUS.md`.
