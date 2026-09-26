# Paso 03 (ciclo 3) - Propagar la cancelación de la petición hasta Dapper

Prerequisito para AC3.5: Q4 (respuesta y log de una petición cancelada).

## Objetivo

Las acciones de `PlayerController` y `FacilitiesController` pasan el token de la petición (parámetro `CancellationToken` de la acción, que ASP.NET Core enlaza a `HttpContext.RequestAborted`) a los handlers; los repositorios lo pasan a `IDatabaseConnectionProvider`, y `SqlConnectionProvider` lo usa en `OpenAsync`, `BeginTransactionAsync` y en Dapper mediante `CommandDefinition`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/PlayerController.cs`, `FacilitiesController.cs` | Parámetro `CancellationToken cancellationToken` en las acciones; sin `CancellationToken.None`. |
| `Gaffer.Api/Shared/ConnectionProviders/IDatabaseConnectionProvider.cs` | `CancellationToken cancellationToken = default` en `QueryScalarAsync`, `QueryAsync`, `ExecuteAsync`, `ExecuteInTransactionAsync` (y `TestConnection`/`ClearAsync`/`CreateAsync` si BUILDER lo justifica). |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs`, `DummyConnectionProvider.cs` | Implementación; Dapper con `new CommandDefinition(query, parameters, transaction, cancellationToken: ct)`. |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs`, `SqlFacilityRepository.cs` | Pasar el token recibido en todas las llamadas. |
| `Gaffer.Api/Commands/*`, `Gaffer.Api/Queries/*` | Solo si algún handler no reenvía el token. |
| `Gaffer.Api/Shared/Exceptions/ExceptionHandlingMiddleware.cs` | Solo según Q4. |
| Tests: `Unit/Controllers/*`, `Unit/Repositories/*`, `Unit/ConnectionProviders/SqlConnectionProviderShould.cs`, `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` | Tests. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | Unitario de controladores: para cada acción (10), el handler recibe el mismo token que se pasa a la acción (NSubstitute `Received` con ese token), no `CancellationToken.None`. |
| AC3.2 | Unitario de repositorios (con `IDatabaseConnectionProvider` sustituido): cada método de `SqlPlayerRepository` y `SqlFacilityRepository` pasa su token al proveedor. |
| AC3.3 | `SqlConnectionProvider` contra Docker: con un token ya cancelado, `QueryAsync`, `ExecuteAsync` y `ExecuteInTransactionAsync` lanzan `OperationCanceledException` (o derivada) y no escriben nada (fila no insertada). |
| AC3.4 | Si `MySql.Data` lo soporta: una consulta larga (`SELECT SLEEP(5)`) cancelada a los ~100 ms termina con `OperationCanceledException` en bastante menos de 5 s. Si no lo soporta, BUILDER registra la medición como limitación del driver en `STATUS.md` (no se da por cumplido) y REVIEWER decide. |
| AC3.5 | Una `OperationCanceledException` con `RequestAborted` cancelado se trata según Q4 (test del middleware). |
| AC3.6 | `grep -rn "CancellationToken.None" Gaffer.Api` → 0. Suite completa en verde (las peticiones normales no se cancelan). |

## Doble bucle

1. **Aceptación**: AC3.3 (red: hoy el token no llega; la operación se completa) y AC3.4. → green.
2. **Unitario**: AC3.1, AC3.2, AC3.5. Red (hoy `CancellationToken.None` / sin parámetro) → green.
3. **Refactor**: una sola forma de construir `CommandDefinition` en `SqlConnectionProvider`. Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~Controller|FullyQualifiedName~Repository|FullyQualifiedName~SqlConnectionProvider|FullyQualifiedName~ExceptionHandlingMiddleware"`: red y green en `STATUS.md`, con la duración medida en AC3.4.
- `grep` de AC3.6.
- Suite completa en verde.
