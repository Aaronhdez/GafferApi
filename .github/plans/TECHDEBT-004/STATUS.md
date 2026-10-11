# STATUS - TECHDEBT-004

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 6 |
| Pasos completados | 01-05 (APPROVED; paso 05 en el ciclo 6 tras R5-1 resuelta) |
| Pasos bloqueados | Ninguno |
| Siguiente | Ninguno: plan DONE. Pendiente del operador: aplicar OPT-027 a OPT-031, opcional OPT-028 (`.gitattributes`) |
| Bloqueos | Ninguno (R5-1 resuelta; ciclo 6 es el último permitido) |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 | APPROVED | AC1.1-AC1.10 | n/d (ver `git log`) |
| 2 | 02 | APPROVED | AC2.1-AC2.7 | c8eac83 |
| 3 | 03 | APPROVED | AC3.1-AC3.6 | 5550133 |
| 4 | 04 | APPROVED | AC4.1-AC4.12 | sin commit (operador) |

Nota (OPT-026/OPT-029): el detalle de los ciclos 1-3 (decisiones, evidencia por criterio de los pasos 01-03 y listado EOL) se movió a [STATUS-archive.md](./STATUS-archive.md). Las filas de ciclos 1-3 de las tablas "Secuenciación", "Tiempos por agente" y "Comandos y resultados" permanecen aquí.

## Decisiones aplicadas en el ciclo 5

Operador: "aplica las optimizaciones, luego sigue con el paso 05".

| Pregunta / cambio | Valor aplicado | Efecto |
| --- | --- | --- |
| OPT-026 | aplicada en `.claude/agents/builder.md` y `.github/agents/builder.agent.md` | Comprobación de colapso al inicio del ciclo. Estado en `MEMORIES.md` sigue `pendiente` (lo cambia OPTIMIZER/operador). |
| Q17, Q18, Q19 | Sin respuesta explícita; "sigue con el paso 05" interpretado como aceptar las recomendaciones a) | Dos jobs (ubuntu + windows con `needs`); run real anotado por el operador antes de REVIEWER; cliente regenerado por el operador antes del run. Si se prefiere otra opción, hay que avisar. |
| Q20-Q22 | Valores por defecto | `-warnaserror` en el build Debug; credenciales efímeras de contenedor; `ConnectionStrings__GafferDB_Test` para migraciones y `ConnectionStrings__Database` para tests. |

## Decisiones aplicadas en el ciclo 4

Operador: "q9 = a, q10 = a, q12 = a"; "Apruebo P3-1 (OPT-025) y el cambio a MariaDB"; "aplica OPT-25 y aplica el cambio de mariadb".

| Pregunta / cambio | Valor aplicado | Efecto |
| --- | --- | --- |
| Q9 | a) | PUT de un stand reemplaza nombre y capacidad; 204. |
| Q10 | a) | POST con 4 stands → 400 `Error.Facilities.Stands.Create.MaxStandsExceeded`, sin cambios. |
| Q12 | a) | GET lista y detalle; POST 201 + `Location` + Guid; 404 `...Stands.FacilityNotFound` / `...Stands.StandNotFound`. |
| Q8, Q11, Q13-Q16 | valores por defecto | Sin respuesta del operador. |
| OPT-025 | aplicada en `CLAUDE.md` (Minimal Reading) | Filtrar desde la primera ejecución la salida de sondas ad hoc. |
| Cambio a MariaDB | El operador provee `Quionis_DB` (`mariadb:latest`, 13.0.2, puerto 3306); sustituye a `Gaffer.Database` 12.3.2 | Línea base de pruebas del ciclo 4: 289/289 tras migrar solo `GafferDB_Test`. Cambiar el contenedor/versión es tarea exclusiva del operador. |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0.

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - TreatWarningsAsErrors (`Gaffer.Api`) + `.gitattributes` + cierre de conexión con `try/finally` | APPROVED |
| 2 | 02 - Entorno desde variable de entorno (`Development` por defecto) | APPROVED |
| 3 | 03 - Migración a `MySqlConnector` + cancelación en curso | APPROVED |
| 4 | 04 - CRUD de gradas + lock en transacción | APPROVED |
| 5 | 05 - CI con GitHub Actions | NEEDS_CHANGES (AC5.4 pendiente del operador) |
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
| REVIEWER | 4 | 2026-10-11T16:20:29Z | 2026-10-11T16:22:30Z |
| BUILDER | 4 | 2026-10-11T16:07:20Z | 2026-10-11T16:19:30Z |
| BUILDER | 5 | 2026-10-11T16:28:30Z | 2026-10-11T16:33:00Z |
| REVIEWER | 5 | 2026-10-11T16:35:48Z | 2026-10-11T16:37:00Z |
| BUILDER | 6 | 2026-10-11T16:59:31Z | 2026-10-11T17:03:00Z |
| REVIEWER | 6 | 2026-10-11T17:01:41Z | 2026-10-11T17:03:30Z |

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
| 4 | 04 | REVIEWER | Build `--no-incremental`; suite completa; grep `CancellationToken.None`; red independiente en copia temporal (sin `FOR UPDATE`) | 0/0; 322/322 exit 0; 0 coincidencias; red 3 con error / 17 (2 repositorio + 1 HTTP). Ver `REVIEW.md`: APPROVED |
| 4 | 04 | Prerrequisitos | `docker ps`; build; suite (hechos antes de empezar, tras `./scripts/run-migrations.ps1` solo sobre `GafferDB_Test`) | `Quionis_DB mariadb:latest Up` (13.0.2, puerto 3306); 0 advertencias / 0 errores; 289/289, exit 0 |
| 4 | 04 | Línea base OpenAPI | Test temporal `TmpSwaggerDump` (borrado después) guarda `/swagger/v1/swagger.json` servido | 16 578 bytes (antes del cambio) |
| 4 | 04 | Unitario/repositorio red (AC4.6, AC4.7) | 7 tests en `SqlFacilityRepositoryShould.WhenManagingTheStandsOfAFacility`; `LockFacility` temporalmente sin `FOR UPDATE`; filtro `~WhenManagingTheStandsOfAFacility` | exit 1; 2 con error / 5 superados / 7 (los 2 de concurrencia AC4.6 y AC4.7) |
| 4 | 04 | Unitario/repositorio green | `FOR UPDATE` restaurado; filtro `~SqlFacilityRepositoryShould\|~UpdateFacilityCommandHandlerShould` | exit 0; 34/34 |
| 4 | 04 | Aceptación red HTTP (AC4.8) | `FacilityStandsFeaturesShould.NeverExceedFourStandsWhenCreatingThemConcurrently` con `FOR UPDATE` quitado | exit 1; 1 con error |
| 4 | 04 | Aceptación green (AC4.1-AC4.5, AC4.8) | `FOR UPDATE` restaurado; filtro `~FacilityStandsFeaturesShould` | exit 0; 10/10 |
| 4 | 04 | Contrato (AC4.9) | `FacilitiesControllerContractShould.DeclareTheFiveStandOperations...`; comparación del `swagger.json` antes/después | Rutas nuevas: `/api/v1/facilities/{facilityId}/stands` (get, post) y `.../stands/{standId}` (get, put, delete); definición nueva `FacilityStandRequest`; rutas y definiciones existentes sin cambios (comparación JSON por ruta y por definición) |
| 4 | 04 | Regresión (AC4.12) | `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build --verbosity quiet`; `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` | 0 advertencias / 0 errores; 322/322, exit 0 (289 + 33 nuevos); grep: 0 coincidencias. Un fallo intermedio: `TestServerDependenciesShould.ProductionHandlers` 8 → 13 (la constante cuenta los handlers registrados; actualizada en el mismo incremento) |
| 5 | 05 | AC5.1 | `docker run --rm -v .github/workflows:/w rhysd/actionlint:latest /w/ci.yml` | exit 0, sin mensajes |
| 5 | 05 | AC5.2 | `dotnet restore gaffer-api.sln`; `dotnet build gaffer-api.sln --no-restore -warnaserror`; `./scripts/run-migrations.ps1`; `dotnet test gaffer-api.sln --no-build` | exit 0 / 0 advertencias y 0 errores / exit 0 ("Migrations for GafferDB_Test finished") / 322/322 exit 0 |
| 6 | 05 | AC5.4/AC5.5 | `curl` a `api.github.com/repos/Aaronhdez/GafferApi/actions/runs/{38157387185,38156628192}` y `/jobs` | 38157387185: success, ambos jobs y todos los pasos success; 38156628192: failure solo en el diff del cliente |
| 6 | 05 | AC5.7 | `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` (con el cliente regenerado por el operador) | 0 advertencias / 0 errores; 322/322, exit 0 |
| 5 | 05 | AC5.6 | `Select-String 'nuget push\|dotnet nuget' .github/workflows/*.yml`; `git status --short \| Select-String Client` | 0 coincidencias; sin cambios en el cliente |

## Paso 04 - evidencia por criterio

Diseño (decisión de BUILDER, Q14 = a): `IFacilityRepository` gana `Update(Guid, Func<Facility, Facility>)` (bloquea con `SELECT ... FOR UPDATE`, lee y aplica la fusión del handler en la misma transacción) y `GetStands/GetStand/AddStand/UpdateStand/DeleteStand`. `UpdateFacilityCommandHandler` usa el nuevo `Update`; las reglas de `MergeStands` no cambian. `Update(Facility)` se conserva (lo usa el nuevo método y sus tests). Nuevo `FacilityStandsController` (`api/v{version}/facilities/{facilityId}/stands`), registrado en `VersioningStartup` y `TestFixture`. Errores: `FacilityStandsFacilityNotFoundException` (handlers) + `FacilityStandNotFoundException` y `FacilityMaxStandsExceededException` (repositorio), con claves `Error.Facilities.Stands.*`.

| AC | Evidencia |
| --- | --- |
| AC4.1 | `ListTheStandsOfAFacilityAndAnEmptyListWhenThereAreNone`, `ReadAStandOfAFacility`: 200 con `SearchFacilityStandResult`; lista vacía. |
| AC4.2 | `CreateAStandReturningItsServerGeneratedIdAndLocation` (201, Guid, `Location` termina en `/facilities/{id}/stands/{standId}` y responde 200); `IgnoreAnIdSentInTheCreationBody`; `RejectAFifthStandWithoutChangingTheFacility` (400 `Error.Facilities.Stands.Create.MaxStandsExceeded`, sin cambios). |
| AC4.3 | `ReplaceTheNameAndCapacityOfAStandKeepingItsId` (204, mismo `Id`, otra grada intacta). |
| AC4.4 | `DeleteAStandIncludingTheLastOne` (204; borrar la última deja lista vacía y la facility existe). Repositorio: `AddReadUpdateAndDeleteAStand` verifica que `Stands` queda sin la fila. |
| AC4.5 | `AnswerNotFoundWithTheFacilityKey...` (5 operaciones) y `AnswerNotFoundWithTheStandKey...` (inexistente y ajena en GET/PUT/DELETE; la grada ajena no cambia). |
| AC4.6 | `MakeAConcurrentStandCreationWaitForTheLockHolderAndThenRespectTheLimit`: A bloquea con 3 gradas, el `AddStand` de otro proveedor no termina en 500 ms, tras commit de A (4ª grada) falla con `FacilityMaxStandsExceededException` y quedan 4. Red sin `FOR UPDATE`: falla. |
| AC4.7 | `MakeAConcurrentFacilityUpdateWaitForTheLockHolderAndThenRespectTheLimit`: mismo patrón con `UpdateFacilityCommandHandler`; espera, descarta la grada nueva (reglas TECHDEBT-003), renombra y quedan 4. Red sin `FOR UPDATE`: falla. |
| AC4.8 | `NeverExceedFourStandsWhenCreatingThemConcurrently`: 10 repeticiones × 4 `POST` simultáneos con barrera sobre facility con 3 gradas → exactamente un 201, tres 400, 4 gradas. Red sin `FOR UPDATE`: falla. |
| AC4.9 | Ver fila "Contrato (AC4.9)" en comandos; `DeclareTheFiveStandOperations...` comprueba las 5 operaciones, los códigos 201/204 y que `FacilityStandRequest` solo tiene `standName` y `standCapacity`. |
| AC4.10 | `FacilityStandCommandHandlersShould` (5), `FacilityStandQueryHandlersShould` (4), 3 entradas nuevas en la tabla de `ExceptionToHttpResponseConvertersShould` (`CoverEveryConverter` exige entrada por conversor); `UpdateFacilityCommandHandlerShould` 11/11 con el doble preparado por `GivenExisting` (aplica la función de fusión que recibe, mismas aserciones; las de `Get`/`Update(Facility)` pasan a `Update(Guid, Func)` y al resultado de la función). |
| AC4.11 | `LeaveNoRowsWhenTheStandInsertFails` (nombre de 300 caracteres): excepción, `Facilities_Stands` 2 y `Stands` 2, sin fila para la grada fallida. Limitación: el `INSERT` en `Stands` es el que falla, así que no se demuestra el rollback del segundo `INSERT`. |
| AC4.12 | `FacilityFeaturesShould` y `JsonContractShould` incluidos en la suite 322/322; `TestServerDependenciesShould` (constante 8 → 13); build 0/0; grep `CancellationToken.None` 0. |

| Nota | Detalle |
| --- | --- |
| Cliente generado | Sin tocar. El operador regenera y sube versión MINOR (Q16). |
| Entorno | Pruebas contra `Quionis_DB` (MariaDB 13.0.2), no `Gaffer.Database` 12.3.2 del plan (ver "Decisiones aplicadas en el ciclo 4"). |
| Pendiente | Colapso exacto de los ciclos 1 y 2 en el índice (solo se añadió el índice). Sin documentación modificada (la hace DOCUMENTER). |

## Paso 05 - evidencia por criterio

Fichero nuevo: `.github/workflows/ci.yml` (único cambio de producto del paso). `scripts/run-migrations.ps1` y `TestConfiguration.cs` no necesitaron cambios (variables `ConnectionStrings__*` bastan). Salida generada de `Gaffer.Api.Client` sin tocar.

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC5.1 | Demostrado | `actionlint` (imagen `rhysd/actionlint:latest` en Docker sobre `.github/workflows/ci.yml`): exit 0, sin mensajes. Triggers push/PR a `main`; jobs `build-and-test` (ubuntu, servicio `mariadb:latest` con health check) y `client-and-package` (windows, `needs`); `permissions: contents: read`. `act` no instalado (no disponible). |
| AC5.2 | Demostrado (local) | `dotnet restore` exit 0; `build --no-restore -warnaserror` 0 advertencias/0 errores; `./scripts/run-migrations.ps1` exit 0 sobre `GafferDB_Test`; `dotnet test --no-build` 322/322 exit 0 (ver tabla de comandos). Contra `Quionis_DB` local, no contra el servicio del YAML. |
| AC5.3 | Cumplido | El job `client-and-package` no se ejecutó en local; `git status` sin cambios en `Gaffer.Api.Client`. |
| AC5.4 | Demostrado (ciclo 6) | Run verde [38157387185](https://github.com/Aaronhdez/GafferApi/actions/runs/38157387185), evento `pull_request` (PR #1), commit `9ebf7673dd2eb4b52ea52aefea0f020cf9046e4c`, 16:54:15Z-16:57:21Z. `build-and-test`: success (restore, build `-warnaserror`, migraciones, tests, con servicio MariaDB). `client-and-package`: success (build Release, diff del cliente limpio, `upload-artifact` success, es decir `.nupkg` encontrado). Consultado por la API pública de GitHub. |
| AC5.5 | Demostrado (ciclo 6) | Run en rojo [38156628192](https://github.com/Aaronhdez/GafferApi/actions/runs/38156628192), commit `e56d6d203272c639a03706bc14bf2ca9788f10a9`: `build-and-test` success; `client-and-package` failure solo en "Fail if the generated client differs from the versioned one" (`upload-artifact` omitido). Log aportado por el operador: diff de contenido en `OpenApiSpecs.json` (`nullable`, `oneOf` en `birthDate`/`gender`), no de EOL. Resuelto por el operador al regenerar y versionar el cliente (commit `9ebf767`). |
| AC5.6 | Demostrado | Solo credencial efímera del contenedor del servicio (`ci-ephemeral-password`); `Select-String 'nuget push\|dotnet nuget' .github/workflows/*.yml` → 0 coincidencias. |
| AC5.7 | Demostrado | Suite 322/322 y build 0 advertencias, igual que en el ciclo 4. |

Supuestos (1), (2) y (5) quedaron confirmados por el run 38157387185 (ambos jobs verdes, `upload-artifact` success). R5-2 (EOL de JSON) no fue la causa del rojo; se mantiene como mejora opcional (OPT-028). El detalle de supuestos original sigue:

Riesgos/supuestos no verificables en local: (1) `DatabaseConfig__Enabled=false` en el job Windows para que el target NSwag no necesite BD (mismo mecanismo que el paso 02; si el host la exige, el run lo mostrará). (2) `healthcheck.sh` existe en la imagen `mariadb:latest` (dato de la imagen, no verificado aquí). (3) El diff del cliente depende del `.gitattributes` del paso 01. (4) `mariadb:latest` cambia de versión (decisión del operador). (5) `Gaffer.Api.Client/bin/Release/*.nupkg` como ruta del paquete, supuesto por el `.csproj` del cliente (`GeneratePackageOnBuild` en Release).

## Documentación modificada

Documentación completada por DOCUMENTER (2026-10-11, tras `APPROVED` y aprobación del operador).

| Tipo | Documento |
| --- | --- |
| Creado | `doc/features/facility-stands.md` |
| Creado | `doc/features/technical/facility-stands.md` |
| Creado | `doc/features/technical/ci-github-actions.md` |
| Creado | `doc/features/technical/hosting-environment.md` |
| Actualizado (directo) | `doc/features/facility-update.md` y `doc/features/technical/facility-update.md` (bloqueo `FOR UPDATE`, `Update(id, buildUpdated)`, limitaciones) |
| Actualizado (directo) | `doc/features/technical/database-connection-lifecycle.md`, `doc/features/database-connection-reliability.md` (MySqlConnector, `try/finally`, cancelación a mitad de consulta) |
| Actualizado (directo) | `doc/features/technical/error-handling.md`, `doc/features/error-handling.md` (claves de errores de stands; cancelación) |
| Actualizado (indirecto) | `doc/features/technical/api-client-versioning.md` (puerta de CI, versión 2.1.0) |
| Actualizado (stack) | `.github/doc/common/dev-stack.md`, `dev-stack-documentation.md`, `test-stack.md` (MySqlConnector, MariaDB, `TreatWarningsAsErrors`, entorno, CI) |

Notas: no se documenta el workflow como función de producto. R4-1 (`Delete(Guid)` de facility sin bloqueo) figura como limitación en `facility-stands.md`. El colapso del detalle de los ciclos 1-3 de este fichero (OPT-029) sigue pendiente.
