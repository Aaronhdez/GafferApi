# TECHDEBT-004 - archivo de detalle de ciclos APPROVED (OPT-029)

Detalle retirado de `STATUS.md` (ciclos 1-3, pasos 01-03). El índice resumido sigue en `STATUS.md`; la evidencia verificada está en `REVIEW.md`. Contenido movido tal cual.

## Decisiones aplicadas en el ciclo 1

Operador: "Apruebo el plan" (sin responder preguntas). Se aplican los valores por defecto de la columna "Bloquea":

| Pregunta | Valor aplicado | Efecto |
| --- | --- | --- |
| Q1 | a) (sin respuesta) | `.gitattributes` sin reglas nuevas (sigue solo `*.cs text eol=lf`). |
| Q2 | b) (sin respuesta) | Solo se listan los ficheros; no se convierte ningún EOL. Renormalizar queda para el operador. |
| Q8 | no es c) | Sin renumeración. |

## Decisiones aplicadas en el ciclo 2

Operador en el checkpoint del ciclo 1: "opción b y luego a" (la opción a decía "Ciclo 2 (confirmando Q3)").

| Pregunta | Valor aplicado | Efecto |
| --- | --- | --- |
| Q3 | a) confirmada | Se lee `ASPNETCORE_ENVIRONMENT`; si falta, está vacía o solo tiene espacios → `Development`. |
| Q4 | c) (sin respuesta; valor por defecto del plan) | `SetConfigurationProviders`, `AddDevelopmentSettings` y el `ConfigurationBuilder` sin uso de `CreateProductContext` (antes `GetProductContext`) **se mantienen** y quedan anotados. AC2.5 no aplica. |

## Decisiones aplicadas en el ciclo 3

Operador en el checkpoint del ciclo 2: "aprueba P2-1 y luego ejecuta el ciclo 3" (P2-1 lo registra OPTIMIZER).

| Pregunta | Valor aplicado | Efecto |
| --- | --- | --- |
| Q5 | a) (sin respuesta) | `MySqlConnector` 2.6.1, la misma versión que `Gaffer.Api.Migrations`. |
| Q6 | a) (sin respuesta) | `SELECT SLEEP(5)` cancelado a los 100 ms → `OperationCanceledException` en < 2 s, fuera de `PROCESSLIST`, conexión usable. |

## Paso 01 - evidencia por criterio

| AC | Evidencia |
| --- | --- |
| AC1.1 | `Gaffer.Api.csproj` L8 `true`; `git diff --stat` solo lista ese `.csproj`. |
| AC1.2 | Build `--no-incremental` final: 0/0. |
| AC1.3 | Red: warning CS0219 en ambos proyectos. Green: `error CS0219` en `Gaffer.Api`; en `Gaffer.Api.Tests` sigue `warning CS0219`. Mutaciones revertidas (`git diff` vacío en esos ficheros; el fichero de test solo contiene después los tests nuevos). |
| AC1.4 | Q1 sin respuesta → sin reglas nuevas; Q2 sin respuesta → solo listado (abajo). |
| AC1.5 | `CloseTheConnectionWhenAQueryFails` (`QueryAsync` con `SELEC 1;`): red `Open` → green `Closed`. |
| AC1.6 | `CloseTheConnectionWhenAScalarQueryFails` (SQL inválido) y `CloseTheConnectionWhenAnExecuteViolatesAConstraint` (PK duplicada, "Duplicate entry"): red `Open` → green `Closed`. |
| AC1.7 | Determinista: `CloseTheConnectionWhenAnOperationIsCancelledAfterOpeningIt` cancela el token en `DbConnection.StateChange` al pasar a `Open` (tras `OpenAsync`, antes del comando); `OperationCanceledException` propagada; red `Open` → green `Closed`. |
| AC1.8 | `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails`: estado `Open` justo tras el fallo dentro de la transacción, rollback (fila no escrita) y `Closed` al final; verde antes y después. `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne`, `RollBackATransactionWhoseOperationIsCancelled` sin cambios y en verde. |
| AC1.9 | Helper `AssertPropagatesTheDriverFailure`: `ThrowAsync<DbException>`, tipo exacto `MySqlException` (por nombre, agnóstico del driver para el paso 03) y mensaje del driver ("syntax" / "Duplicate entry"); verde antes y después. |
| AC1.10 | Filtro 27/27; suite 274/274; `grep Q1` sin coincidencias. |

## Paso 02 - evidencia por criterio

| AC | Evidencia |
| --- | --- |
| AC2.1 | `HostingEnvironmentShould.TakeTheHostEnvironmentFromTheEnvironmentVariable("Staging"/"Production")`: el host se construye con `Program.CreateGafferHostBuilder([])` (lo mismo que `Main`, sin `Run`) y da `IHostEnvironment.EnvironmentName` = el valor. **Nota**: antes del cambio el host ya daba `Staging`, porque `ConfigureWebHostDefaults` añade las variables `ASPNETCORE_*` a la configuración del host *después* de `UseEnvironment("Development")` y las pisa. El red previsto por el plan ("hoy `Development`") no se da en el host; sí en `ProductContext` y con valor en blanco. `UseEnvironment` se movió detrás de `ConfigureWebHostDefaults` para que el valor resuelto sea el definitivo. |
| AC2.2 | Casos `null`, `""` y `"   "` → `Development`. Red real: `"   "` (el host tomaba `"   "`). En .NET, `Environment.SetEnvironmentVariable(name, "")` borra la variable, así que el caso `""` equivale a ausente en el proceso; el vacío literal se cubre en el unitario. |
| AC2.3 | El mismo test comprueba `Program.CreateProductContext().Environment` == valor esperado (red: `Development` fijo con Staging/Production). El campo estático `Program.ProductContext` se inicializa una vez por proceso con `CreateProductContext()`; se prueba la función de creación (y la de resolución), que usan tanto el host como el campo, no el campo. |
| AC2.4 | `EnvironmentNameResolutionShould`: presente (`Staging`, `Production`, `Test`) → el mismo valor; `null`, `""`, `" "`, `"\t  "` → `Development`. Red 3/7 → green 7/7. |
| AC2.5 | Q4 sin respuesta → c): no se elimina nada (grep = 4 coincidencias). La carga de `appsettings*.json` no cambia (`ApiDatabaseConfigurationShould` en verde). |
| AC2.6 | `TestFixture.cs` L79 y `StartupTestServer.cs` L30 siguen con `UseEnvironment("Test")` (sin cambios); `ApiDatabaseConfigurationShould` 2/2; suite 287/287; build `--no-incremental` 0/0. |
| AC2.7 | `EnvironmentVariableScope` restaura `ASPNETCORE_ENVIRONMENT` y `DatabaseConfig__Enabled` (`false`, para no hacer la comprobación de BD al construir el host); `RestoreTheEnvironmentVariableAfterEachTest` lo demuestra. La clase lleva `[NonParallelizable]` (el ensamblado no declara `Parallelizable`; NUnit no paraleliza por defecto). |

| Riesgo | Detalle |
| --- | --- |
| NSwag (paso 05 / regeneración del operador) | El método extraído se llama `CreateGafferHostBuilder` y no `CreateHostBuilder`, a propósito: así `HostFactoryResolver` no lo detecta y NSwag sigue resolviendo el host a través de `Main`, como hasta ahora. No verificado por BUILDER (no genera el cliente). |
| Entorno distinto de `Development` | Consecuencia estándar ya descrita en `02-hosting-environment.md` (sin User Secrets, sin `appsettings.Development.json`, sin `UseDeveloperExceptionPage`). No verificado con `dotnet run` (opcional en el plan). |

## Paso 03 - evidencia por criterio

| AC | Evidencia |
| --- | --- |
| AC3.1 | grep 0 coincidencias; `Gaffer.Api.csproj` → `MySqlConnector` 2.6.1 (Q5 = a)); `dotnet list package --include-transitive` sin `MySql.Data` (ver tabla de comandos). |
| AC3.2 | `AbortARunningQueryWhenItsTokenIsCancelled` (sin `[Explicit]`): `QueryScalarAsync("SELECT SLEEP(5) /* <guid> */")` con `CancellationTokenSource(100 ms)` → `OperationCanceledException`, `Stopwatch` < 2 s; `information_schema.PROCESSLIST` consultado con otro proveedor (`ID <> CONNECTION_ID() AND INFO LIKE '%<guid>%'`) → 0; `ConnectionState.Closed`, no dispuesto; `SELECT 1` posterior con el mismo proveedor → 1. Red `MySql.Data`: 5 s sin excepción. |
| AC3.3 | `RollBackATransactionWhoseRunningOperationIsCancelled`: dentro de `ExecuteInTransactionAsync`, `INSERT` de un jugador y luego `SLEEP(5)` cancelado a 100 ms → `OperationCanceledException` en < 2 s; el jugador no existe visto desde otro proveedor (rollback); conexión `Closed`. Red `MySql.Data`: 5 s, sin excepción (la transacción confirmaba). |
| AC3.4 | Los 33 tests previos del filtro (incluidos AC1.5-AC1.9 y los de token ya cancelado de TECHDEBT-003) verdes antes (con `MySql.Data`, suite 287/287) y después, sin cambiar aserciones. `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` no necesitó cambios: `MySqlConnector` lanza `Component.Disposed` también desde `DisposeAsync` (sonda), también tras el refactor. `AssertPropagatesTheDriverFailure` (comprueba el nombre `MySqlException`) sigue verde: ahora es `MySqlConnector.MySqlException`. |
| AC3.5 | `ReportAnUnreachableDatabaseWithoutThrowing`, `DatabaseStartupShould` y `DatabaseFailureShould` verdes (en el filtro 35/35). El filtro `catch` de `TestConnection` no cambia (el fallo de conexión de `MySqlConnector` es `MySqlException`, que es `DbException`). |
| AC3.6 | Sonda: (a) fallo de conexión (puerto cerrado, `Connection Timeout=2`) → `MySqlConnector.MySqlException` "Connect Timeout expired", sin `InnerException`. (b) SQL inválido (`SELEC 1;`) → `MySqlConnector.MySqlException` "You have an error in your SQL syntax ... MariaDB ...". (c) cancelación en curso → `System.OperationCanceledException` con `InnerException` `MySqlConnector.MySqlException` "Query execution was interrupted". `DisposeAsync` y `Dispose()` sobre conexión abierta: ambos dejan `State=Closed` y lanzan `Disposed` 1 vez (también `CloseAsync` + `DisposeAsync`: 1 vez). Opciones de cadena de conexión: **ninguna nueva**; el código no usa variables de usuario `@x` (grep `SET @\|:=` sin coincidencias) y el mapeo de `Guid` (`CHAR(36)`, `GuidFormat=Default`) y fechas funciona sin opciones (suite 289/289). `appsettings*` y `scripts/*.ps1` sin cambios. |
| AC3.7 | Suite 287 → 289 (+2); build `--no-incremental` 0/0; `JsonContractShould` y contratos dentro de la suite en verde. |

| Riesgo / nota | Detalle |
| --- | --- |
| R1-2 (revisado en el paso 03) | Un `CloseAsync` que lanzase en el `finally` de `RunOnConnectionAsync` ocultaría la excepción original. Observado con `MySqlConnector`: en todos los casos de fallo y cancelación (incluida la cancelación en curso, AC3.2/AC3.3) se propaga la excepción original y el estado final es `Closed`. `CloseAsync` devuelve la sesión al pool (el reset se difiere a la siguiente apertura), por lo que no se añadió código defensivo; queda como riesgo residual no reproducido. |
| Red sin stash | El red de AC3.2/AC3.3 se ejecutó en el árbol de trabajo con los tests ya añadidos y el driver aún en `MySql.Data`, antes de cambiarlo; no hizo falta copia ni `git stash`. |
| Duración | AC3.2/AC3.3 dependen de un temporizador de 100 ms y un límite de 2 s (Q6 = a)); observados 139-224 ms en 4 ejecuciones. |

## EOL (AC1.4, listado; Q2 = b): no se convierte nada)

| Comprobación | Resultado |
| --- | --- |
| Ficheros con `i/crlf` en el índice | Ninguno. |
| Ficheros sin atributo `text` | 186: 157 `.md` (todos `i/lf`), 8 `.json`, 6 `.gitkeep` (`i/none`), 4 `.csproj`, 2 `.ps1`, 1 `.sln`, 1 `.sh`, 1 `.props`, 1 `.mjs`, `.gitignore`, `.gitattributes`, `.dockerignore`, 2 `.code-workspace`. Todos `i/lf` salvo `.gitkeep`. |
| De ellos en `w/crlf` (árbol de trabajo) | `.dockerignore`, `.gitattributes`, `.gitignore`, `Directory.Build.props`, `gaffer-api.sln`, `global.json`, `.github/agent-harness/tools/usage-slice.mjs`, `Gaffer.Api.Client/Gaffer.Api.Client.csproj`, `Gaffer.Api.Client/OpenApiSpecs.json` (generado, fuera de BUILDER), `Gaffer.Api.Migrations/Gaffer.Api.Migrations.csproj`, `Gaffer.Api.Tests/Gaffer.Api.Tests.csproj`, `Gaffer.Api.Tests/testSettings.json`, `Gaffer.Api/Properties/launchSettings.json`, `Gaffer.Api/appsettings.json`, `Gaffer.Api/appsettings.Development.json`, `Gaffer.Api/nswag.json`. |
| `*.cs` con `attr eol=lf` pero `w/crlf` en el árbol | 41 ficheros (índice `i/lf`; `git status` limpio): p. ej. `Gaffer.Api/ProductContext.cs`, `Gaffer.Api/Startups/*.cs`, `Gaffer.Api.Tests/TestShared/*.cs`, `Gaffer.Api.Migrations/Migrations/M000*.cs`, `Gaffer.Api.Client/GafferApiClient.cs` (generado). Se corrige con un checkout del operador si lo desea. |
| Ficheros tocados en este ciclo | `SqlConnectionProvider.cs` y `SqlConnectionProviderShould.cs`: `i/lf w/lf attr/text eol=lf`. `Gaffer.Api.csproj`: `i/lf w/lf` (sin atributo; Git avisa "LF will be replaced by CRLF" por `core.autocrlf`; diff de 1 línea). |
