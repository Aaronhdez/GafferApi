# STATUS - TECHDEBT-004

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 3 |
| Pasos completados | 01 (APPROVED por REVIEWER, ciclo 1); 02 (APPROVED por REVIEWER, ciclo 2); 03 (APPROVED por REVIEWER, ciclo 3) |
| Pasos bloqueados | 04 hasta Q9, Q10, Q12; 05 hasta Q17, Q18, Q19 (ver `00-overview-and-open-questions.md`) |
| Siguiente | OPTIMIZER ciclo 3 y checkpoint humano |
| Bloqueos | Ninguno |

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

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0.

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - TreatWarningsAsErrors (`Gaffer.Api`) + `.gitattributes` + cierre de conexión con `try/finally` | APPROVED |
| 2 | 02 - Entorno desde variable de entorno (`Development` por defecto) | APPROVED |
| 3 | 03 - Migración a `MySqlConnector` + cancelación en curso | APPROVED |
| 4 | 04 - CRUD de gradas + lock en transacción | - |
| 5 | 05 - CI con GitHub Actions | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-27T17:30:59Z | 2026-09-27T17:34:46Z |
| REVIEWER | 1 | 2026-09-27T17:36:05Z | 2026-09-27T17:38:43Z |
| BUILDER | 2 | 2026-09-27T17:54:07Z | 2026-09-27T17:57:48Z |
| REVIEWER | 2 | 2026-09-27T18:00:01Z | 2026-09-27T18:02:40Z |
| BUILDER | 3 | 2026-09-28T15:46:28Z | 2026-09-28T15:52:56Z |
| REVIEWER | 3 | 2026-09-28T15:54:28Z | 2026-09-28T15:58:13Z |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| Línea base (dato del operador, 2026-09-27, `main`) | `dotnet build --no-incremental`: 0 errores, 0 warnings. `dotnet test`: 269/269 contra Docker `Gaffer.Database` (MariaDB 12.3.2). BUILDER la revalida al inicio del ciclo 1. |
| Revalidación BUILDER ciclo 1 (rama `TECHDEBT-004`, árbol limpio) | `docker ps`: `Gaffer.Database mariadb Up 6 hours`. `dotnet build gaffer-api.sln --no-incremental -v q`: 0 advertencias, 0 errores. `dotnet test gaffer-api.sln --no-build --verbosity quiet`: 269/269, exit 0. |

## Comandos y resultados

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | AC1.3 red (antes de cambiar el `.csproj`) | Mutación temporal: `var ac13Mutation = 1;` en `Gaffer.Api/Program.cs` L17 y `var ac13TestMutation = 1;` en `SqlConnectionProviderShould.cs` L15; `dotnet build gaffer-api.sln --no-incremental -v q` | Compilación correcta, 2 advertencias: `Program.cs(17,17): warning CS0219` y `SqlConnectionProviderShould.cs(15,13): warning CS0219` |
| 1 | 01 | AC1.1/AC1.3 green | `TreatWarningsAsErrors` `false` → `true` en `Gaffer.Api/Gaffer.Api.csproj`; mismo build con ambas mutaciones | ERROR al compilar, 1 error: `Gaffer.Api\Program.cs(17,17): error CS0219 ... [Gaffer.Api.csproj]` |
| 1 | 01 | AC1.3 (tests siguen en warning) | `git checkout -- Gaffer.Api/Program.cs` (solo la mutación); mismo build con la mutación de tests | Compilación correcta, 1 advertencia: `SqlConnectionProviderShould.cs(15,13): warning CS0219 ... [Gaffer.Api.Tests.csproj]`, 0 errores |
| 1 | 01 | AC1.3 reversión | `git checkout -- <fichero de test>` (antes de añadir tests); `git diff --stat -- Gaffer.Api/Program.cs Gaffer.Api.Tests/` | Vacío |
| 1 | 01 | Aceptación red (AC1.5-AC1.9) | 5 tests nuevos en `SqlConnectionProviderShould`; `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup" --verbosity quiet` | exit 1; 4 con error / 23 superados / 27. Los 4 (`CloseTheConnectionWhenAQueryFails`, `...AScalarQueryFails`, `...AnExecuteViolatesAConstraint`, `...AnOperationIsCancelledAfterOpeningIt`): `Expected provider.ConnectionState to be ConnectionState.Closed, but found ConnectionState.Open`. `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails` (AC1.8, caracterización) ya en verde. Las aserciones de AC1.9 (`DbException` de tipo `MySqlException` con el mensaje del driver) pasan antes del fallo de estado. |
| 1 | 01 | Aceptación green | `RunOnConnectionAsync` con `try/finally` (cierra solo si abrió); `<summary>` sin nota de deuda Q1; mismo filtro | exit 0; 27/27 |
| 1 | 01 | Refactor / AC1.2 | `dotnet build gaffer-api.sln --no-incremental -v q` | 0 advertencias, 0 errores |
| 1 | 01 | AC1.10 suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 274/274 (antes 269; +5 tests nuevos) |
| 1 | 01 | AC1.10 estático | `grep -n "Q1" Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` | Sin coincidencias (exit 1) |
| 1 | 01 | AC1.1 | `git diff --stat -- '*.csproj' Directory.Build.props` | Solo `Gaffer.Api/Gaffer.Api.csproj \| 2 +-` |
| 1 | 01 | AC1.4 (Q1 = a), Q2 = b)) | `git ls-files --eol` filtrado | Ver "EOL (AC1.4)" abajo. `.gitattributes` sin cambios. |
| 1 | 01 | REVIEWER | Build `--no-incremental`; `-getProperty:TreatWarningsAsErrors`; filtro 27/27; suite 274/274; red reproducido en copia `git archive HEAD` (4/27 fallan con `Open`); mutación AC1.3 en copia aislada | Ver `REVIEW.md`: APPROVED |
| 2 | 02 | Prerrequisitos | `docker ps`; `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | `Gaffer.Database mariadb Up 6 hours`; 0 advertencias / 0 errores; 274/274, exit 0 |
| 2 | 02 | Extracción sin cambio de comportamiento | `Main` → `CreateGafferHostBuilder(args).Build().Run()`; `GetProductContext` (privado) → `CreateProductContext()` (público), con el mismo `Environment = "Development"` fijo | Compila, 0 advertencias |
| 2 | 02 | Aceptación red (AC2.1-AC2.3) | `HostingEnvironmentShould` (6 casos); `dotnet test --filter "FullyQualifiedName~HostingEnvironmentShould"` | exit 1; 3 con error / 3 superados / 6. `("Staging")` y `("Production")`: `Expected Program.CreateProductContext().Environment ... differ at index 0` (el host ya daba `Staging`, ver nota AC2.1). `("   ")`: `Expected ...IHostEnvironment>().EnvironmentName ... differ at index 0` (el host tomaba `"   "`). `null` y `""` en verde (`Development`). |
| 2 | 02 | Unitario red (AC2.4) | `ResolveEnvironmentName` stub que devuelve `Development`; `EnvironmentNameResolutionShould` (7 casos); filtro `~EnvironmentNameResolutionShould` | 3 con error (`UseThePresentValue` Staging/Production/Test), 4 superados (null, `""`, `" "`, `"\t  "`) |
| 2 | 02 | Green | `ResolveEnvironmentName` con `string.IsNullOrWhiteSpace` → `Development`; `CurrentEnvironmentName()` lee `ASPNETCORE_ENVIRONMENT`; `UseEnvironment(CurrentEnvironmentName())` movido **después** de `ConfigureWebHostDefaults`; `ProductContext.Environment = CurrentEnvironmentName()`; filtro `~EnvironmentNameResolutionShould\|~HostingEnvironmentShould\|~ApiDatabaseConfigurationShould` | exit 0; 15/15 (7 + 6 + 2) |
| 2 | 02 | Refactor / AC2.6 | `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 0 advertencias / 0 errores; 287/287, exit 0 (274 + 13 nuevos) |
| 2 | 02 | AC2.5 (Q4 = c)) | `grep -rn "SetConfigurationProviders\|AddDevelopmentSettings" Gaffer.Api --include=*.cs` | 4 coincidencias (sin cambios, esperado con Q4 = c)) |
| 2 | 02 | Alcance | `git diff --stat -- Gaffer.Api/Program.cs Gaffer.Api/ProductContext.cs`; `git status --short` | `Program.cs` 29+/7-; `ProductContext.cs` sin cambios; nuevos `Integration/HostingEnvironmentShould.cs` y `Unit/Startups/EnvironmentNameResolutionShould.cs` (`w/lf`, `eol=lf`) |
| 2 | 02 | REVIEWER | Build `--no-incremental`; filtro 15/15; suite 287/287 (también con `ASPNETCORE_ENVIRONMENT=Staging` preestablecida); red reproducido en copia aislada (6/13 fallan, como BUILDER) | Ver `REVIEW.md`: APPROVED |
| 3 | 03 | Prerrequisitos (paso 02 ya en `c8eac83`; árbol con solo `MEMORIES.md`/`OPTIMIZATION.md` modificados) | `docker ps`; `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | `Gaffer.Database Up 28 hours`; 0 advertencias / 0 errores; 287/287, exit 0 |
| 3 | 03 | Aceptación red (AC3.2, AC3.3) con `MySql.Data` 26.7.0 | 2 tests nuevos en `SqlConnectionProviderShould`; `dotnet test gaffer-api.sln --filter "FullyQualifiedName~AbortARunningQuery\|FullyQualifiedName~RollBackATransactionWhoseRunningOperation"` | exit 1; 2/2 con error, **5 s** cada uno: `Expected a <System.OperationCanceledException> to be thrown, but no exception was thrown.` (ejecutado en el árbol antes de cambiar el driver; no hizo falta stash) |
| 3 | 03 | Cambio de driver | `Gaffer.Api.csproj`: `MySql.Data` 26.7.0 → `MySqlConnector` 2.6.1; `SqlConnectionProvider.cs`: `using MySqlConnector;` (ningún otro cambio de tipos o API necesario) | Compila, 0 advertencias |
| 3 | 03 | Aceptación green + caracterización (AC3.2-AC3.5) | `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup\|FullyQualifiedName~DatabaseFailure" --verbosity quiet` | exit 0; 35/35 (33 previos sin cambiar aserciones + 2 nuevos). AC3.2 162 ms, AC3.3 138 ms (duración NUnit, incluye SetUp/TearDown) |
| 3 | 03 | Sonda AC3.6 (scratchpad, fuera del repo) | Consola `net10.0` con `MySqlConnector` 2.6.1 + Dapper 2.1.79, connection string de los user-secrets de `Gaffer.Api.Tests` (no impreso) | Ver "Paso 03 - evidencia por criterio" (AC3.6) |
| 3 | 03 | Refactor | `DisposeAsync`: `CloseAsync()` + `Dispose()` síncrono (apaño de `MySql.Data`) → `await _sqlConnection.DisposeAsync()`; `<summary>` de `Command` anota que cancelar aborta la consulta en curso; build `--no-incremental -v q`; mismo filtro; suite completa | 0 advertencias / 0 errores; 35/35; **289/289**, exit 0 (287 + 2 nuevos) |
| 3 | 03 | Repetibilidad AC3.2/AC3.3 | Mismo filtro de los 2 tests, 3 ejecuciones | 3 × 2/2 verdes: 217/148, 224/166, 199/139 ms |
| 3 | 03 | AC3.1 | `grep -rn "MySql.Data" --include=*.cs --include=*.csproj .` (sin `bin/`/`obj/`); `dotnet list gaffer-api.sln package --include-transitive \| grep -iE "Project\|MySql"` | grep: 0 coincidencias (exit 1). `list package`: `MySqlConnector 2.6.1` directo en `Gaffer.Api` y `Gaffer.Api.Migrations`, transitivo `2.6.1` en `Gaffer.Api.Tests`; `FluentMigrator.*.MySql 8.0.1` (Migrations); sin `MySql.Data` |
| 3 | 03 | Alcance | `git status --short`; `git diff --stat`; `git ls-files --eol` de los 3 ficheros | Solo `Gaffer.Api.csproj` (1 línea), `SqlConnectionProvider.cs` (+8/-4), `SqlConnectionProviderShould.cs` (+55) y ficheros del plan. Los 3 en `i/lf w/lf`. `appsettings*`, `scripts/*.ps1`, `TestFixture.cs` y cliente generado sin cambios |
| 3 | 03 | REVIEWER | Build `--no-incremental`; filtro 35/35; suite 289/289; 2 tests nuevos 7 × 2/2 (134-209 ms); red reproducido en copia `git archive HEAD` (2/2 fallan, 5 s); sonda: `InnerException` "interrupted" en ambos tests | Ver `REVIEW.md`: APPROVED |

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

## Documentación modificada

Pendiente (DOCUMENTER, tras `APPROVED` y aprobación del operador). Incluye `dev-stack.md`/`dev-stack-documentation.md` por el cambio de driver (Q7).
