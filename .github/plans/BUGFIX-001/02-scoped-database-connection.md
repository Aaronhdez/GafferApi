# Paso 02 - Conexión de BD por scope (no compartida entre peticiones)

Prerequisito: Q4 = "Si" (el proveedor por petición cierra y libera su conexión al final de la petición).

## Objetivo

Cada scope de DI (petición HTTP) recibe su propio `SqlConnectionProvider`, con su propio `MySqlConnection` y su propio `_currentTransaction`. Así, una petición no puede entrar en la transacción de otra ni cerrar su conexión. `TestFixture` se sincroniza en el mismo incremento.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Startups/DatabaseStartup.cs` | Registrar una factoría que cree una instancia nueva por scope (la comprobación de arranque se trata en el paso 04). |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | Implementar `IAsyncDisposable` (y/o `IDisposable`) para que DI cierre y libere la conexión al liberar el scope (Q4). |
| `Gaffer.Api.Tests/TestFixture.cs` | L111-112: registrar la factoría por scope; conservar una instancia aparte (no registrada) para `ClearDatabase`. |
| `Gaffer.Api.Tests/Unit/...` o `Gaffer.Api.Tests/Integration/...` (nuevo `DatabaseStartupShould.cs`) | Tests. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Con `DatabaseConfig { Enabled: true, ConnectionString: <BD de test> }`, `ConfigureDatabase` + `BuildServiceProvider()`: dos scopes distintos resuelven instancias `IDatabaseConnectionProvider` distintas, y la misma instancia dentro de un mismo scope. |
| AC2.2 | Aislamiento: mientras el scope A tiene abierta una transacción `ExecuteInTransactionAsync` con un `INSERT` sin confirmar, una lectura desde el scope B no ve la fila y no entra en la transacción de A. Tras un rollback en A, la fila no existe. |
| AC2.3 | En `TestFixture`, dos scopes del `TestServer` resuelven instancias distintas; `ClearDatabase` sigue funcionando. |
| AC2.4 | Al liberar el scope (`await scope.DisposeAsync()`), DI libera el proveedor resuelto en ese scope y su conexión queda cerrada. Se comprueba con un test del estado de la conexión o de la liberación, también cuando el scope se libera con una conexión abierta. Liberar dos veces no lanza excepción. |
| AC2.5 | Toda la suite existente sigue en verde. |

## Doble bucle

1. **Aceptación**: test sobre `ServiceCollection` + `ConfigureDatabase` con `IConfiguration` en memoria para AC2.1 y AC2.2 contra la BD de Docker. Red (hoy la instancia es la misma) → green.
2. **Unitario**: test de AC2.3 (scopes de `TestServer.Services`) y AC2.4 (liberación por scope). Red → green.
3. **Refactor**: simplificar el registro y quitar variables estáticas innecesarias en `TestFixture`. Volver a ejecutar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~DatabaseStartup"`: red y luego green en `STATUS.md`.
- `dotnet test gaffer-api.sln` completo en verde.
- En el diff de `DatabaseStartup.cs` y `TestFixture.cs` no se captura ninguna instancia compartida en la factoría `AddScoped`.

## Notas

- AC2.2 depende del aislamiento de transacciones de MariaDB (por defecto `REPEATABLE READ`); lectura sin confirmar invisible. Si el test resultara no determinista, BUILDER lo documenta y usa solo AC2.1 como evidencia principal; REVIEWER decide.
