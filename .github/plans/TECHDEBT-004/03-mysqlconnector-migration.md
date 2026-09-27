# Paso 03 (ciclo 3) - Migración de `MySql.Data` a `MySqlConnector`

Decisión del operador (literal): "Sí, en TECHDEBT-004". Motivo: `MySql.Data` 26.7.0 no aborta una consulta en curso al cancelar (TECHDEBT-003 AC3.4 aceptado como limitación del driver; OPT-016, OPT-019). Preguntas: Q5, Q6, Q7 (no bloquean).

Es un cambio de dependencia: BUILDER y REVIEWER leen `.github/doc/common/dev-stack-documentation.md` y la documentación oficial enlazada (`https://mysqlconnector.net/`), en particular la guía de migración desde `MySql.Data` y las opciones de cadena de conexión.

## Objetivo

- `Gaffer.Api` usa `MySqlConnector` (versión según Q5) en lugar de `MySql.Data`; sin cambio de comportamiento HTTP.
- Demostrar que cancelar el token aborta una consulta en curso contra MariaDB 12.3.2 (lo que TECHDEBT-003 AC3.4 no pudo).
- Dejar evidencia para el cierre (DOCUMENTER, Q7) y para OPT-012 (`HARNESS-001`): tipo de excepción observado y comportamiento de `DisposeAsync`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Gaffer.Api.csproj` | Quitar `MySql.Data`; añadir `MySqlConnector`. |
| `Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | `using MySqlConnector;`; tipos `MySqlConnection`/`MySqlTransaction`/`MySqlCommand`; revisar el comentario de L128 (dependiente de `MySql.Data`). |
| Otros ficheros de `Gaffer.Api` | Solo si el cambio de driver lo exige (hoy ningún otro usa `MySql.Data`). Cualquier opción nueva en cadenas de conexión (`appsettings*`, `scripts/*.ps1`) se justifica en `STATUS.md`. |
| `Gaffer.Api.Tests/Unit/ConnectionProviders/SqlConnectionProviderShould.cs` | Test de cancelación en curso; ajustes de tests que dependan del driver (registrados). |

Fuera: `Gaffer.Api.Migrations` (ya usa `MySqlConnector`; solo se toca si Q5 = b)); documentación de stack (cierre, Q7).

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC3.1 | `grep -rn "MySql.Data" --include=*.cs --include=*.csproj .` (excluyendo `bin/`/`obj/`) → 0 en código y proyectos. `Gaffer.Api.csproj` referencia `MySqlConnector` con la versión de Q5. `dotnet list gaffer-api.sln package --include-transitive` sin `MySql.Data` (registrar la salida filtrada). |
| AC3.2 | Aceptación, cancelación en curso (Q6): `SELECT SLEEP(5)` por el proveedor con token cancelado a los ~100 ms → `OperationCanceledException` (o derivada) en menos de 2 s; tras la excepción, `information_schema.PROCESSLIST` (consultado con otra conexión) ya no contiene esa consulta; la conexión del proveedor queda `Closed` (paso 01) y una consulta posterior con el mismo proveedor funciona. Test permanente en la suite por defecto (sin `[Explicit]`). Red: el mismo test contra el código previo (`MySql.Data`) tarda ~5 s sin excepción (medido en TECHDEBT-003 ciclo 3; se reproduce una vez en copia scratchpad o `git stash`, sin tocar el árbol de trabajo final). |
| AC3.3 | Transacción: una operación cancelada en curso dentro de `ExecuteInTransactionAsync` provoca rollback (sin filas escritas, comprobado con otro proveedor) y cierra la conexión. |
| AC3.4 | Caracterización del proveedor, verde antes y después sin cambiar aserciones: todos los tests de `SqlConnectionProviderShould` y `DatabaseStartupShould` (incluidos AC1.5-AC1.8 del paso 01 y los de token ya cancelado de TECHDEBT-003). Si `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` necesita cambiar por el comportamiento de `DisposeAsync`/`Component.Disposed` de `MySqlConnector`, BUILDER registra la causa (con sonda en scratchpad si hace falta) y el cambio de aserción. |
| AC3.5 | `TestConnection` sigue devolviendo `false` (sin lanzar) con la BD inalcanzable (`DatabaseFailureShould`, `DatabaseStartupShould`). |
| AC3.6 | Evidencia para el cierre registrada en `STATUS.md`: tipo concreto de excepción en (a) fallo de conexión y (b) SQL inválido; comportamiento observado de `DisposeAsync` frente a `Dispose()`; si una consulta con variable de usuario `@x` o el mapeo de `Guid`/fechas necesitó opciones de cadena de conexión. |
| AC3.7 | Regresión: suite completa en verde (recuento antes/después); build 0 warnings; `JsonContractShould` y tests de contrato en verde (el driver no cambia el OpenAPI). |

## Doble bucle

1. **Aceptación**: AC3.2-AC3.3 (red con `MySql.Data`) → cambio de driver → green.
2. **Unitario**: AC3.4-AC3.5 (caracterización) y ajustes registrados.
3. **Refactor**: comentarios y `<summary>` dependientes del driver; AC3.1, AC3.6. Revalidar suite y build.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider|FullyQualifiedName~DatabaseStartup|FullyQualifiedName~DatabaseFailure"`: red/green y duración del test de AC3.2 en `STATUS.md`.
- `grep` y `dotnet list package` de AC3.1.
- Suite completa y build `--no-incremental -v q` (solo resumen).

## Notas

- La actualización de `dev-stack.md` (L24-25) y `dev-stack-documentation.md` (L13-14) es del cierre (DOCUMENTER, cambio de paquete). No la hace BUILDER.
- Marcar OPT-016/OPT-019 en `MEMORIES.md` corresponde a OPTIMIZER con aprobación del operador.
