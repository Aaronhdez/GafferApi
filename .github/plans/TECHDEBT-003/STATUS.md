# STATUS - TECHDEBT-003

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 1 |
| Pasos completados | 01 (APPROVED ciclo 1; F1 de higiene EOL pendiente para el operador, ver REVIEW.md) |
| Pasos bloqueados | 02 (Q2), 03 (solo AC3.5, Q4), 04 (solo AC4.5, Q6), 05 (solo AC5.5, Q7) hasta respuesta del operador |
| Bloqueos | Ninguno para el paso 01. Q1 sin respuesta: se aplica b) (sin `try/finally`), anotado como deuda. |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0 (abierta, `00-overview-and-open-questions.md`).

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - SqlConnectionProvider | APPROVED |
| 2 | 02 - Update de Facility con IDs estables | - |
| 3 | 03 - Cancelación | - |
| 4 | 04 - Cambios pequeños | - |
| 5 | 05 - Warnings | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-27T12:02:48Z | 2026-09-27T12:09:15Z |
| REVIEWER | 1 | 2026-09-27T12:09:55Z | 2026-09-27T12:13:30Z |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| TECHDEBT-002 | `DONE` (verificado en `.github/plans/TECHDEBT-002/STATUS.md`) |
| Línea base (ciclo 1, rama `TECHDEBT-003`, árbol limpio) | `dotnet test gaffer-api.sln --verbosity quiet` → 215/215 superados, 0 omitidos. `dotnet build gaffer-api.sln --no-incremental` → 57 advertencias (resumen de MSBuild), 0 errores. Docker `Gaffer.Database` Up. |

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | - | Preflight | `docker ps` | `Gaffer.Database` Up |
| 1 | - | Línea base | `dotnet test gaffer-api.sln --verbosity quiet` / `dotnet build gaffer-api.sln --no-incremental` | 215/215; 57 advertencias, 0 errores |
| 1 | 01 | AC1.5 antes | `grep -n "\.Open()"` / `grep -n "_sqlConnection\.Dispose"` en `SqlConnectionProvider.cs` | `.Open()`: L54, L71. `DisposeAsync()` sobre `_sqlConnection`: L63, L77, L88, L100, L112, L144 (operaciones y `finally` de la transacción) + L159/L168 (dispose del proveedor) |
| 1 | 01 | Aceptación (red) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup" --verbosity quiet` | 17/18; falla `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed`: `Expected releases to be greater than 0, but found 0.` Los 5 tests nuevos de caracterización (AC1.1, AC1.4) ya en verde antes del cambio |
| 1 | 01 | Aceptación (green) | mismo filtro | 18/18 |
| 1 | 01 | Unitario (AC1.6) | - | No aplica: Q1 sin respuesta → b) |
| 1 | 01 | Refactor | mismo filtro; suite completa `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 18/18; 220/220 (215 + 5 nuevos) |
| 1 | 01 | AC1.5 después | mismos `grep` | `.Open()`: 0. `_sqlConnection.Dispose()` solo en L119 (`DisposeAsync` del proveedor, L109) y L128 (`Dispose`, L122); ningún `_sqlConnection.DisposeAsync()` |
| 1 | 01 | Warnings | `dotnet build gaffer-api.sln --no-incremental \| grep "Advertencia(s)"` | 56 (57 antes); 0 en `SqlConnectionProvider.cs` |

## Ciclo 1 - Paso 01 (BUILDER)

| Tema | Detalle |
| --- | --- |
| Ficheros | `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs`, `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` |
| Tests nuevos (5) | AC1.1 `CloseButNotReleaseTheConnectionAfterEachOperation`; AC1.2 `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed`; AC1.4 `MakeTheWorkOfACommittedTransactionVisible`, `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne`. Recuento: 215 → 220; el filtro pasa de 13 a 18. Se añade `SetUp`/`TearDown` que limpia `Players` en la clase |
| AC1.2: medio de observación | Reflexión sobre el campo privado `_sqlConnection` en el test (`UnderlyingConnectionOf`), suscripción a `Component.Disposed`. No cambia la API pública ni el `.csproj` (no hay `InternalsVisibleTo`) |
| AC1.2: hallazgo del driver | Sonda en scratchpad con `MySql.Data` 26.7.0: `MySqlConnection.DisposeAsync` está sobrescrito, **no** lanza `Component.Disposed` ni cambia ningún campo de una conexión cerrada y la conexión se puede reabrir; solo `Dispose()` síncrono ejecuta la liberación de `Component`. Por eso el red real no fue "se libera tras la primera operación" (no observable en runtime) sino "no se libera al terminar el scope". Cambio: `DisposeAsync` del proveedor hace `CloseAsync()` + `_sqlConnection.Dispose()` (sin I/O tras cerrar). La guarda contra reintroducir liberaciones por operación es AC1.5 (`grep`) y, para `Dispose()` síncrono, el propio test |
| AC1.3 | Los 5 tests previos de `SqlConnectionProviderShould` y los de `DatabaseStartupShould` sin cambiar aserciones: en verde |
| Refactor | Patrón "abrir si hace falta / cerrar si soy el dueño" en un único método privado `RunOnConnectionAsync<T>` usado por `ClearAsync`, `CreateAsync`, `QueryScalarAsync`, `QueryAsync`, `ExecuteAsync`. `ClearAsync`/`CreateAsync` pasan a usar `OpenIfNeededAsync` (antes cerraban aunque hubiese transacción ambiente). La transacción conserva su `finally` (cierra sin liberar) |
| Q1 (deuda) | Sin `try/finally`: una operación que falla fuera de transacción deja la conexión abierta hasta la siguiente transacción o el fin del scope. Documentado en el `<summary>` de `RunOnConnectionAsync` |
| Warnings | `QueryScalarAsync` usa `!` sobre el resultado de `QueryFirstOrDefaultAsync<T>` (mismo comportamiento: devuelve `default`) para no introducir CS8619; el recuento baja de 57 a 56 |
| Sin paquetes nuevos, sin cambios de DI (TestFixture no afectado), sin tocar `Gaffer.Api.Client` | - |

## Documentación modificada

Pendiente (DOCUMENTER).
