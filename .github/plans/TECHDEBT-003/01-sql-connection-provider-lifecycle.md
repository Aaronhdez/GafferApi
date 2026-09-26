# Paso 01 (ciclo 1) - SqlConnectionProvider: cerrar sin liberar, apertura asíncrona

Q1: el cierre en caso de fallo (`try/finally`) solo entra si el operador responde a).

## Objetivo

Tras cada operación, `SqlConnectionProvider` **cierra** la conexión (vuelve al pool) pero **no la libera**; solo `DisposeAsync`/`Dispose` del proveedor (fin del scope de DI) la liberan. `ClearAsync` y `CreateAsync` abren con `OpenAsync`. Se conserva el ciclo de vida por scope de BUGFIX-001 y el comportamiento transaccional (`ExecuteInTransactionAsync` anidado, rollback y propagación).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | Quitar los `_sqlConnection.DisposeAsync()` de las operaciones y del `finally` de la transacción; `Open()` → `await OpenAsync()`; `try/finally` si Q1 = a. |
| `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` | Tests de caracterización y del ciclo de vida. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | Varias operaciones seguidas en el mismo proveedor (`QueryScalarAsync`, `ExecuteAsync`, `QueryAsync`, `ExecuteInTransactionAsync`, otra consulta) funcionan; tras cada una `ConnectionState == Closed` e `IsDisposed == false`. |
| AC1.2 | La conexión solo se libera al liberar el proveedor: un test observa la liberación de la `MySqlConnection` (p. ej. el evento `Disposed` de `Component`, expuesto al test por un medio que no cambie la interfaz pública; BUILDER registra el medio elegido) y comprueba que no ocurre tras las operaciones y sí tras `DisposeAsync` del scope. Red esperado hoy: se libera tras la primera operación. |
| AC1.3 | Los 5 tests existentes de `SqlConnectionProviderShould` y `DatabaseStartupShould` (scope, aislamiento de transacciones, doble dispose, `TestConnection`) siguen en verde sin cambiar sus aserciones. |
| AC1.4 | Transacciones: commit visible tras éxito; rollback y excepción propagada tras fallo; una llamada anidada se ejecuta en la transacción externa (test de caracterización si no existe). |
| AC1.5 | `grep -n "\.Open()" SqlConnectionProvider.cs` → 0; `DisposeAsync()`/`Dispose()` sobre `_sqlConnection` solo dentro de `DisposeAsync`/`Dispose` del proveedor. |
| AC1.6 | (Solo si Q1 = a) Tras una consulta que falla (SQL inválido), `ConnectionState == Closed` y la siguiente operación funciona. |
| AC1.7 | Suite completa en verde. |

## Doble bucle

1. **Aceptación**: AC1.1 y AC1.4 contra la BD de Docker (caracterización: green antes y después; se registra) y AC1.2 (red → green).
2. **Unitario**: AC1.6 si aplica (red → green).
3. **Refactor**: extraer el patrón "abrir si hace falta / cerrar si soy el dueño" a un único método privado. Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider|FullyQualifiedName~DatabaseStartup"`: red (AC1.2) y green en `STATUS.md`.
- Resultados de `grep` de AC1.5 antes/después.
- Suite completa en verde con recuento.
