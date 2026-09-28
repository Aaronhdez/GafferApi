# REVIEW - TECHDEBT-004

## Ciclo 1 - Paso 01

Veredicto: **APPROVED** (paso 01). Estado del plan: `APPROVED` para el paso 01; los pasos 02-05 siguen pendientes (ver `STATUS.md`).

### Alcance revisado

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | `M` `STATUS.md`, `SqlConnectionProviderShould.cs`, `Gaffer.Api.csproj`, `SqlConnectionProvider.cs`; `??` `usage/BUILDER.md`. Nada fuera del alcance del paso 01; cliente generado intacto (no verificado por norma). |
| Diff completo | `Gaffer.Api.csproj`: 1 línea (`TreatWarningsAsErrors` `false`→`true`). `SqlConnectionProvider.cs`: `RunOnConnectionAsync` con `try/finally` (cierra solo si `ownsConnection`) y `<summary>` sin nota de deuda Q1. Tests: 5 nuevos + helpers `AssertPropagatesTheDriverFailure`, `UnderlyingDbConnectionOf`. |
| `TestFixture.cs` | No aplica: no hay handlers ni registros DI nuevos. |
| Preflight | `docker ps`: `Gaffer.Database Up 6 hours`. |

### Evidencia propia (REVIEWER)

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --no-incremental -v q` | 0 Advertencia(s), 0 Errores |
| `dotnet msbuild <proj> -getProperty:TreatWarningsAsErrors` | `Gaffer.Api` true; `Gaffer.Api.Tests`, `Gaffer.Api.Migrations`, `Gaffer.Api.Client` false |
| `dotnet test ... --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup"` | exit 0; 27/27 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 274/274 (base 269 + 5) |
| Red independiente: copia `git archive HEAD` en scratchpad + solo el fichero de tests nuevo; mismo filtro | exit 1; 4 fallos / 23 / 27: `CloseTheConnectionWhenAQueryFails`, `...AScalarQueryFails`, `...AnExecuteViolatesAConstraint`, `...AnOperationIsCancelledAfterOpeningIt`, todos `Expected provider.ConnectionState to be Closed, but found Open` (las aserciones de excepción previas pasan) |
| Mutación AC1.3 en copia scratchpad (repo no tocado) | Mutación CS0219 en `Gaffer.Api/Program.cs` + en test: `error CS0219` en `Gaffer.Api.csproj`, 1 error. Solo la del test: 1 advertencia, 0 errores (`Gaffer.Api.Tests.csproj`). Copias borradas. |
| `git diff --stat -- '*.csproj' Directory.Build.props .gitattributes` | Solo `Gaffer.Api/Gaffer.Api.csproj \| 2 +-` |
| `grep -n "Q1" SqlConnectionProvider.cs` | Sin coincidencias (exit 1) |
| `git ls-files --eol \| grep -c i/crlf` | 0 |

### Criterios

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC1.1 | Demostrado | diff `--stat` y `-getProperty` arriba. |
| AC1.2 | Demostrado | Build `--no-incremental` 0/0. |
| AC1.3 | Demostrado | BUILDER (red warning → green error, `STATUS.md`) + mutación propia en copia aislada. |
| AC1.4 | Demostrado | Q1/Q2 sin respuesta → `.gitattributes` sin cambios y solo listado en `STATUS.md`; `i/crlf` = 0. |
| AC1.5 | Demostrado | Red→green reproducido (`CloseTheConnectionWhenAQueryFails`). |
| AC1.6 | Demostrado | Red→green reproducido (scalar con SQL inválido; execute con PK duplicada). |
| AC1.7 | Demostrado | Determinista vía `DbConnection.StateChange` (cancela al pasar a `Open`); red→green reproducido; propaga `OperationCanceledException`. |
| AC1.8 | Demostrado | `LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails` (estado `Open` tras el fallo, rollback, `Closed` al final); tests de transacción existentes sin cambios en verde. |
| AC1.9 | Demostrado | `DbException` de tipo `MySqlException` con mensaje del driver; pasa en HEAD (red) y tras el cambio. |
| AC1.10 | Demostrado | 27/27, 274/274, `grep Q1` vacío. |

### Hallazgos (ninguno bloqueante)

| ID | Severidad | Hallazgo |
| --- | --- | --- |
| R1-1 | Menor / informativo | `Gaffer.Api.csproj` quedó `w/lf` en el árbol tras `sed -i` (índice `i/lf`, sin atributo `text`, `core.autocrlf=true`, BOM conservado). El diff es de 1 línea y el blob versionado sigue en LF: sin efecto en el commit. Solo cosmético en el árbol de trabajo; el operador puede hacer `git checkout -- Gaffer.Api/Gaffer.Api.csproj` tras commitear si quiere CRLF local. |
| R1-2 | Observación | Si `CloseAsync` lanzara en el `finally`, enmascararía la excepción original (AC1.9). Riesgo teórico con MySql.Data; revisar en el paso 03 (`MySqlConnector`). |
| R1-3 | Observación (preexistente, fuera de alcance) | `OpenIfNeededAsync` devuelve `true` también si la conexión ya estaba abierta fuera de transacción; no es regresión de este paso. |

## Ciclo 2 - Paso 02

Veredicto: **APPROVED** (paso 02). Q3 = a) confirmada por el operador; Q4 sin respuesta → c) (sin cambios, AC2.5 no aplica). Pasos 03-05 siguen pendientes.

### Alcance revisado

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | Producto: `M Gaffer.Api/Program.cs`; `??` `Gaffer.Api.Tests/Integration/HostingEnvironmentShould.cs`, `Gaffer.Api.Tests/Unit/Startups/EnvironmentNameResolutionShould.cs`. Resto: ficheros del plan/harness (`STATUS.md`, `OPTIMIZATION.md`, `usage/BUILDER.md`, `MEMORIES.md`), no producto. Los cambios del paso 01 ya están en `HEAD` (no aparecen en el diff). `ProductContext.cs` sin cambios. |
| Diff `Program.cs` | `Main` → `CreateGafferHostBuilder(args).Build().Run()`; `UseEnvironment(CurrentEnvironmentName())` movido detrás de `ConfigureWebHostDefaults`; `ResolveEnvironmentName` pura (`IsNullOrWhiteSpace` → `Development`); `CreateProductContext()` público con `Environment = CurrentEnvironmentName()`; `SetConfigurationProviders`/`AddDevelopmentSettings` intactos (Q4 = c)). |
| `TestFixture.cs` | No aplica: sin handlers ni registros DI nuevos. |
| Preflight | `docker ps`: `Gaffer.Database mariadb Up 6 hours`. |

### Evidencia propia (REVIEWER)

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --no-incremental -v q` | 0 Advertencia(s), 0 Errores |
| `dotnet test ... --filter "~EnvironmentNameResolutionShould\|~HostingEnvironmentShould\|~ApiDatabaseConfigurationShould"` | exit 0; 15/15 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 287/287 |
| Misma suite con `ASPNETCORE_ENVIRONMENT=Staging` en el proceso padre | exit 0; 287/287 (restauración de la variable y fixtures `Test` no afectados) |
| Red independiente: copia del árbol en scratchpad; `Program.cs` devuelto al comportamiento previo (`UseEnvironment(ProductContext.Environment ?? Development)` antes de `ConfigureWebHostDefaults`, `Environment = "Development"`, `ResolveEnvironmentName` → `Development`); filtro de los 2 tests nuevos | exit 1; 6 fallos / 7 / 13: `("Staging")`, `("Production")` fallan en `CreateProductContext().Environment` (el host ya daba el valor); `("   ")` falla en `IHostEnvironment.EnvironmentName`; `UseThePresentValue` x3. Coincide con `STATUS.md`. Copia borrada. |

### Criterios

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC2.1 | Demostrado | Host construido con `CreateGafferHostBuilder([])` (mismo código que `Main`, sin `Run`) → `Staging`/`Production`. Red del plan ("hoy `Development`") no se da en el host (confirmado: `ConfigureWebHostDefaults` añadía `ASPNETCORE_*` después de `UseEnvironment`); el red real está en `ProductContext` y en el valor en blanco. Aceptado: la desviación está documentada y el cambio de orden es necesario para el caso en blanco. |
| AC2.2 | Demostrado | `null`, `""`, `"   "` → `Development`; red `"   "` reproducido. `""` equivale a ausente en .NET (`SetEnvironmentVariable` borra); vacío literal cubierto en AC2.4. |
| AC2.3 | Demostrado | Mismo test asserta `CreateProductContext().Environment`; el campo estático usa esa función (una vez por proceso), registrado en `STATUS.md`. Red reproducido. |
| AC2.4 | Demostrado | `EnvironmentNameResolutionShould` 7/7; red 3/7 reproducido. |
| AC2.5 | No aplica | Q4 = c): grep 4 coincidencias, sin cambios; carga de `appsettings*.json` sin tocar (`ApiDatabaseConfigurationShould` 2/2). |
| AC2.6 | Demostrado | `TestFixture`/`StartupTestServer` sin cambios (`UseEnvironment("Test")`); 287/287; build 0/0. |
| AC2.7 | Demostrado | `EnvironmentVariableScope` restaura ambas variables; `[NonParallelizable]`; sin atributo `Parallelizable` a nivel de ensamblado; suite verde también con la variable preestablecida. |

### Hallazgos (ninguno bloqueante)

| ID | Severidad | Hallazgo |
| --- | --- | --- |
| R2-1 | Observación | Con `UseEnvironment` al final, `ASPNETCORE_ENVIRONMENT` gana a `--environment` de línea de comandos y a `DOTNET_ENVIRONMENT`. Antes también se ignoraban (el `UseEnvironment("Development")` fijo los pisaba), así que no es regresión; coincide con Q3. |
| R2-2 | Observación | El host y `ProductContext` leen la variable por separado; solo divergirían si el proceso la muta entre ambas lecturas. `ProductContext.Environment` no se consume en `Gaffer.Api` (solo `Name` y `StartupSettingsFileName` en `Startup.cs`). |
| R2-3 | Riesgo (fuera de alcance, no verificado) | NSwag: `HostFactoryResolver` busca `BuildWebHost`/`CreateWebHostBuilder`/`CreateHostBuilder`; `CreateGafferHostBuilder` no coincide, por lo que el descubrimiento sigue interceptando `Main`, que construye el mismo host. Riesgo a nivel de fuente bajo; la verificación queda para la regeneración del operador / paso 05. |
| R2-4 | Menor / informativo | `HostingEnvironmentShould` está en `Integration/` aunque no usa BD (`DatabaseConfig__Enabled=false`); aceptable porque construye el host real. |

## Ciclo 3 - Paso 03

Veredicto: **APPROVED** (paso 03). Estado del plan: `APPROVED` para el paso 03; paso 04 bloqueado hasta Q9/Q10/Q12 y paso 05 hasta Q17/Q18/Q19 (ver `STATUS.md`).

### Alcance revisado

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | `M` `Gaffer.Api.csproj`, `SqlConnectionProvider.cs`, `SqlConnectionProviderShould.cs`; ficheros del plan (`STATUS.md`, `usage/BUILDER.md`, `OPTIMIZATION.md`) y `MEMORIES.md` (P2-1 de OPTIMIZER, aprobado por el operador; no es producto). Sin ficheros nuevos. |
| Paso 02 frente a paso 03 | Paso 02 ya está en `c8eac83` (`Program.cs`, `HostingEnvironmentShould`, `EnvironmentNameResolutionShould`); el diff de trabajo contiene solo el paso 03. |
| Diff completo | `.csproj`: `MySql.Data` 26.7.0 → `MySqlConnector` 2.6.1 (1 línea). Proveedor: `using MySqlConnector;`; `DisposeAsync` pasa de `CloseAsync()`+`Dispose()` a `await _sqlConnection.DisposeAsync()`; `<summary>` de `Command` anota `KILL QUERY`. Tests: 2 nuevos + helpers `SlowQuery`, `RunningQueriesWith`, constantes `CancelAfter`/`CancellationDeadline`. |
| `TestFixture.cs` | No aplica: sin handlers ni registros DI nuevos. |
| `appsettings*`, `scripts/*.ps1`, `Gaffer.Api.Migrations` | Sin cambios (sin opciones nuevas de cadena de conexión; Q5 = a) no toca Migrations). |
| Preflight | `docker ps`: `Gaffer.Database mariadb Up 28 hours`. |

### Evidencia propia (REVIEWER)

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --no-incremental -v q` | 0 Advertencia(s), 0 Errores |
| `dotnet test ... --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup\|FullyQualifiedName~DatabaseFailure"` | exit 0; 35/35 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | exit 0; 289/289 (287 + 2) |
| Repetibilidad: filtro de los 2 tests nuevos, 7 ejecuciones | 7 × 2/2 verdes; AC3.2 189-209 ms, AC3.3 134-147 ms (NUnit, incluye SetUp/TearDown) |
| Red independiente: copia `git archive HEAD` (con `MySql.Data` 26.7.0) en scratchpad + solo el fichero de tests actual | 2/2 con error, **5 s** cada uno: `Expected a <System.OperationCanceledException> to be thrown, but no exception was thrown.` |
| Sonda de duda en la misma copia con el proveedor y `.csproj` actuales + aserción extra `InnerException.Message` contiene `"interrupted"` en ambos tests | 2/2 verdes (190 / 168 ms): la cancelación mata una consulta **ya en curso** en el servidor (MariaDB "Query execution was interrupted"), no solo la apertura. Copia borrada. |
| `grep -rn "MySql.Data" --include=*.cs --include=*.csproj .` sin `bin/`/`obj/` | 0 coincidencias (exit 1) |
| `dotnet list gaffer-api.sln package --include-transitive` filtrado | `MySqlConnector 2.6.1` directo en `Gaffer.Api` y `Gaffer.Api.Migrations`, transitivo en `Gaffer.Api.Tests`; sin `MySql.Data` |

### Criterios

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC3.1 | Demostrado | grep 0; `.csproj` con `MySqlConnector` 2.6.1 (Q5 = a)); `list package` sin `MySql.Data`. |
| AC3.2 | Demostrado (red→green) | `AbortARunningQueryWhenItsTokenIsCancelled`, sin `[Explicit]`: OCE < 2 s, `PROCESSLIST` = 0 desde otra conexión, `Closed` y no dispuesto, `SELECT 1` posterior = 1. Red reproducido: 5 s sin excepción con `MySql.Data`. |
| AC3.3 | Demostrado (red→green) | `RollBackATransactionWhoseRunningOperationIsCancelled`: OCE < 2 s, jugador no escrito (otro proveedor), `Closed`. Red reproducido: 5 s sin excepción. |
| AC3.4 | Demostrado | Los 33 tests previos del filtro, sin cambios de aserciones (el diff de tests solo añade), verdes con ambos drivers. `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` sin cambios y verde tras el nuevo `DisposeAsync`. |
| AC3.5 | Demostrado | `DatabaseFailureShould`, `DatabaseStartupShould` y `ReportAnUnreachableDatabaseWithoutThrowing` dentro del 35/35; filtro `catch` de `TestConnection` sin cambios (`MySqlConnector.MySqlException` es `DbException`). |
| AC3.6 | Demostrado (documental) | Registrado en `STATUS.md` (sonda de BUILDER): tipos para fallo de conexión y SQL inválido, `DisposeAsync` frente a `Dispose()`, sin opciones nuevas de cadena de conexión. La parte de cancelación en curso (OCE con `InnerException` "interrupted") la corroboró la sonda propia; el resto no se volvió a ejecutar (sonda fuera del repo). |
| AC3.7 | Demostrado | Suite 287 → 289; build 0/0; `JsonContractShould` y contratos dentro de la suite verde. |

### Hallazgos

| ID | Severidad | Detalle |
| --- | --- | --- |
| R3-1 | Menor / no bloqueante | En AC3.2 el temporizador de 100 ms arranca antes de `OpenAsync`. Si la apertura tardase > 100 ms (pool frío, máquina cargada), el token cancelaría la apertura y el test seguiría en verde sin demostrar el aborto en curso (falso verde posible, no fallo intermitente). Hoy la sonda muestra que sí es la consulta en curso. Endurecimiento posible: comprobar `InnerException` `MySqlException` "interrupted" o arrancar `CancelAfter` tras abrir, como en AC3.3. |
| R3-2 | Observación (determinismo) | Los tests dependen del reloj (Q6 = a)): 100 ms / límite 2 s / consulta de 5 s. Observado 134-224 ms en 11 ejecuciones (BUILDER + REVIEWER): margen ~10× frente al límite y el red (5 s) nunca puede pasar. Riesgo de flake solo bajo carga extrema (p. ej. CI compartida, paso 05); aceptable. |
| R3-3 | Observación | R1-2 (excepción del `CloseAsync` del `finally` ocultando la original) no reproducido en ninguno de los caminos de fallo/cancelación con `MySqlConnector`; se acepta como riesgo residual sin código defensivo, coherente con el alcance mínimo. |
| R3-4 | Observación | `Dispose()` síncrono sigue con `Close()` + `Dispose()`; redundante con `MySqlConnector` pero inocuo y fuera del objetivo. `dev-stack.md`/`dev-stack-documentation.md` quedan para DOCUMENTER (Q7), según el plan. |
