# REVIEW - TECHDEBT-003

## Ciclo 1 - Paso 01 (SqlConnectionProvider)

**Veredicto: APPROVED** (paso 01). Q1 sin respuesta: se aplica b); AC1.6 no aplica.

### Evidencia ejecutada por REVIEWER

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Git status | `git status --short` | M `SqlConnectionProvider.cs`, M `SqlConnectionProviderShould.cs`, M `STATUS.md`, ?? `usage/BUILDER.md`. Nada fuera de alcance; `Gaffer.Api.Client` no tocado; sin cambios de DI (`TestFixture.cs` no requiere registro) |
| Preflight | `docker ps` | `Gaffer.Database` Up |
| Red independiente (AC1.2) | `git archive HEAD` a scratchpad + test nuevo copiado; filtro `SqlConnectionProvider\|DatabaseStartup` | 17/18; falla `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` L121: `Expected releases to be greater than 0, but found 0.` (coincide con lo registrado por BUILDER) |
| Green (filtro) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~DatabaseStartup" --verbosity quiet` | 18/18 |
| Suite completa (AC1.7) | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 220/220, 0 omitidos (215 + 5) |
| Build | `dotnet build gaffer-api.sln --no-incremental` | 56 advertencias (57 en línea base), ninguna en `SqlConnectionProvider.cs` |
| AC1.5 | `grep -n "\.Open()"` / `grep -n "_sqlConnection\.Dispose"` | `.Open()`: 0. `_sqlConnection.Dispose()` solo L119 (dentro de `DisposeAsync`, L109) y L128 (dentro de `Dispose`, L122); ningún `_sqlConnection.DisposeAsync()` |

### Criterios

| ID | Estado | Evidencia |
| --- | --- | --- |
| AC1.1 | Demostrado | `CloseButNotReleaseTheConnectionAfterEachOperation`: 5 operaciones (incl. transacción), `Closed` e `IsDisposed == false` tras cada una. Caracterización (verde antes y después) |
| AC1.2 | Demostrado (con matiz) | Red→green reproducido. Medio: reflexión sobre `_sqlConnection` + `Component.Disposed`; no cambia API pública. Asserta 0 liberaciones tras operaciones y >0 tras `scope.DisposeAsync()`. Matiz: el red real es la mitad "se libera al fin del scope", no "se libera tras la primera operación", porque `MySqlConnection.DisposeAsync` (MySql.Data 26.7) no lanza `Disposed`; lo confirma el propio red (el código de HEAD llamaba `DisposeAsync` en cada operación y en el dispose del proveedor, y `releases` quedó en 0). La reintroducción de `DisposeAsync()` por operación solo la detecta AC1.5 (`grep`), no el test; riesgo residual aceptado y registrado |
| AC1.3 | Demostrado | El diff del test solo añade `using`, tests, `SetUp/TearDown` y helpers; aserciones previas intactas; en verde |
| AC1.4 | Demostrado | `MakeTheWorkOfACommittedTransactionVisible` (observador en otra conexión), `RollBackAndPropagateTheFailureOfATransaction`, `RunANestedTransactionInsideTheOuterOne` (rollback externo deshace la anidada) |
| AC1.5 | Demostrado | Ver tabla anterior |
| AC1.6 | No aplica | Q1 sin respuesta → b); deuda documentada en `<summary>` de `RunOnConnectionAsync` y en `STATUS.md` |
| AC1.7 | Demostrado | 220/220 |
| Refactor | Hecho | `RunOnConnectionAsync<T>` único; revalidado |

### Hallazgos

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| F1 | Importante, no bloqueante (higiene, sin efecto funcional) | `SqlConnectionProvider.cs` quedó en CRLF y termina en un `\r` suelto sin `\n` (159 CR / 158 LF). Git lo clasifica `w/-text` (`git ls-files --eol`), así que `core.autocrlf=true` no lo normaliza: `git diff` muestra reescritura completa (159+/182-) en vez de ~31+/54- (`--ignore-all-space`), y al commitear el fichero entraría en el repo con CRLF (el índice está en LF). | Operador, antes del commit: normalizar a LF (p. ej. `sed -i 's/\r$//' Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs`) y comprobar `git diff --stat`. Si se prefiere, BUILDER lo corrige en el ciclo 2 |
| F2 | Menor | `ClearAsync` ejecuta sin `_currentTransaction` (sin cambio respecto a HEAD); ahora, dentro de una transacción ambiente, ya no cierra la conexión (mejora). Sin acción | - |
| F3 | Menor | `QueryScalarAsync` usa `!` sobre `QueryFirstOrDefaultAsync<T>`: mismo comportamiento que antes (puede devolver `default`), solo silencia nulabilidad. Aceptable dentro de Q8/05 | - |

### Bloqueos

Ninguno.

## Ciclo 2 - Paso 02 (Update de Facility con gradas por `Id`)

**Veredicto: APPROVED** (paso 02, AC2.1-AC2.12). Q3, Q10, Q13, Q19 con valor por defecto a), registrados por BUILDER.

### Evidencia ejecutada por REVIEWER

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Git status | `git status --short` | M: `dev-stack.md`, ficheros del plan, 5 tests y 4 ficheros de producto del alcance; ?? `CannotUpdateFacilityDuplicatedStandException.cs`. Nada fuera de alcance; `Gaffer.Api.Client` y `TestFixture.cs` sin tocar; `Facility.cs` sin cambios |
| Preflight | `docker ps` | `Gaffer.Database` Up |
| Build | `dotnet build gaffer-api.sln --no-incremental` | 56 advertencias (igual que la línea base), 0 errores. Las de `UpdateFacilityCommandHandler.cs`/`SqlFacilityRepository.cs` son CS8618/CS8603 previas en propiedades/clases de fila no tocadas |
| Red independiente | `git archive HEAD` a scratchpad + `FacilityFeaturesShould.cs` y `SqlFacilityRepositoryShould.cs` del árbol; filtro `FacilityFeaturesShould\|SqlFacilityRepositoryShould+WhenUpdatingAFacility` | 14/26; fallan los 11 de aceptación nuevos/reescritos y `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`. `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` verde en HEAD (caracterización). Los unitarios del handler/conversor/contrato no compilan en HEAD (red de compilación, coincide con `STATUS.md`) |
| Green (filtro del paso) | `dotnet test gaffer-api.sln --no-build --filter "...FacilityFeaturesShould\|...UpdateFacilityCommandHandler\|...SqlFacilityRepository\|...ExceptionToHttpResponseConverters\|...FacilitiesControllerContract"` | 89/89, 0 omitidos |
| Suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 243/243, 0 omitidos (220 + 23) |
| `UnknownStand` | `grep -rn "UnknownStand" Gaffer.Api Gaffer.Api.Tests` | 0 resultados |
| `DELETE` | `grep -n "DELETE\|DeleteStands(\|DeleteAddress(" SqlFacilityRepository.cs` | Solo en `Delete` (L206-L209) y en los helpers L282-L298, llamados solo desde `Delete`. `Update` no los invoca |
| EOL | `git ls-files --eol` | Ficheros `.cs` editados `i/lf w/crlf attr/text eol=lf`: se normalizan a LF al commitear; el diff no es reescritura completa (distinto de F1 del ciclo 1) |

### Criterios

| ID | Estado | Evidencia |
| --- | --- | --- |
| AC2.1 | Demostrado | `KeepTheIdsOfTheStandsSentWithTheirIdWhileUpdatingTheirValues`: mismos `Id`, valores nuevos, además `Address.Id` igual. Red en HEAD |
| AC2.2 | Demostrado | `KeepTheStandsNotSentAndCreateTheOnesSentWithoutId` (3 gradas: actualizada, omitida sin cambios, nueva con `Id` distinto); `KeepTheStandsWhenTheUpdateSendsNoStands(False/True)` (`null` y vacío). Red en HEAD |
| AC2.3 | Demostrado | `CreateStandsWithNewIdsForUnknownOrForeignIdsWhenTheFacilityHasLessThanFourStands`: 4 gradas, originales intactas, creadas con nombre/capacidad enviados y `Id` distintos de los enviados; `GET` de B `BeEquivalentTo(otherBefore)`. `GET` comparado en `STATUS.md`. Red en HEAD |
| AC2.4 | Demostrado | `RejectAnUpdateLeavingMoreThanFourStandsWhenTwoExistAndThreeAreSentWithoutId` y `...WhenThreeExistAndOneIsNewAndOneUnknown`: 400 `Error.Facilities.Create.Validation` y `GET` completo equivalente al anterior (campos, dirección, gradas). Red en HEAD |
| AC2.5 | Demostrado | `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`: `Address.Id` original y campos nuevos; `Addresses`, `Facilities_Addresses`, `Stands`, `Facilities_Stands` con exactamente las filas esperadas (sin huérfanas ni duplicadas). Red en HEAD (`South` borrada) |
| AC2.6 | Demostrado (caracterización) | `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails`: falla el `INSERT` de la grada nueva (300 caracteres) después de los `UPDATE` de facility, dirección y grada emparejada; todo sin cambios y recuentos 2/2. No es red→green: el update de HEAD ya era transaccional; el criterio pide demostrar atomicidad, no un red. Aceptado |
| AC2.7 | Demostrado (con desviación aceptada, ver H1) | `DefineAnOptionalNullableIdOnUpdateFacilityStandRequest` (`Guid?`, sin `[Required]`) y `DeclareTheStandIdOfTheUpdateRequestAsOptionalAndNullableInTheServedOpenApi` (swagger servido: `id` fuera de `required`, anulable, mismo formato que `SearchFacilityStandResult.id`; `SearchFacilityStandResult` = `id`, `standName`, `standCapacity`). Ubicado en `FacilitiesControllerContractShould`, tal como indica el propio AC2.7 (no es desviación) |
| AC2.8 | Demostrado | `UpdateFacilityCommandHandlerShould`: emparejadas + no mencionadas; `null`/vacío; creación con `Id` nuevo (sin `Id`/desconocido/ajeno; `Id` únicos, distintos de `Guid.Empty`, de los enviados y de los existentes); descarte con 4 y `Update` con las 4 originales; `DuplicatedStand` con 1 y 4 existentes sin llamar a `Update`; total > 4 sin `Update`. `ExceptionToHttpResponseConvertersShould` cubre la clave (400, título, mensaje). Red de compilación en HEAD |
| AC2.9 | Demostrado | Renombrado `UpdateAFacilityReplacingFieldsAddressAndStands` → `UpdateAFacilityFieldsAndAddressKeepingItsFourStands` (la aserción cambia por Q2/R-MAX/Q14 y está registrada en `STATUS.md`; no se debilita: ahora comprueba las 4 originales y ausencia de `Only`). `ReturnNotFoundWhenUpdatingAnUnknownFacility`, `RejectUpdatingAFacilityWithMoreThanFourStands`, `JsonContractShould` y suite completa en verde. Sin servicio inyectable nuevo: `TestFixture` no aplica |
| AC2.10 | Demostrado | `dev-stack.md`: 1 inserción, sub-viñeta junto a `MySql.Data` con la nota OPT-016 y la fuente; resto sin cambios |
| AC2.11 | Demostrado | `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands`: 200 con cuerpo vacío, mismos 4 `Id`, emparejada actualizada, 3 sin cambios, ninguna `Discarded*` ni `Id` nuevo, nombre actualizado, B sin cambios. Red en HEAD |
| AC2.12 | Demostrado | `RejectAnUpdateRepeatingTheIdOfAnExistingStand(2)` y `(4)`: 400 `Error.Facilities.Update.DuplicatedStand`, `GET` sin cambios. Red en HEAD (200) |
| Refactor | Hecho | `MergeStands` puro en el handler separado del SQL (`UpdateAddress`/`UpdateStands`/`InsertStand`); revalidado 243/243 |

### Hallazgos

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| H1 | Menor, no bloqueante | AC2.7 dice "formato `uuid`"; el swagger servido usa `"format": "guid"` para todos los `Guid` (convención NSwag del proyecto, también en `SearchFacilityStandResult.id`). El test fija "mismo formato que la respuesta", que es la intención verificable; cambiar a `uuid` afectaría a todo el contrato y está fuera de alcance | Operador/PLANNER: si se quiere `uuid`, planificarlo aparte |
| H2 | Menor, no bloqueante (deuda) | Concurrencia: el handler lee las gradas existentes fuera de la transacción del repositorio y la invariante de 4 se evalúa sobre esa lectura. Dos `PUT` concurrentes sobre una facility con < 4 gradas pueden sumar más de 4 (antes el reemplazo acotaba el total a la petición). Mismo patrón de lectura previa que en HEAD; sin requisito de concurrencia en el plan | Anotar como posible mejora (bloqueo/`SELECT ... FOR UPDATE` o comprobación en la transacción) |
| H3 | Informativo | `UpdateStands` inserta cualquier grada cuyo `Id` no esté enlazada a la facility; si un llamador distinto del handler pasara el `Id` de una grada de otra facility, fallaría la PK y la transacción revertiría (sin corrupción). Inalcanzable hoy: el handler siempre genera `Guid.NewGuid()` (Q19) | Ninguna |
| H4 | Informativo | Cambio de comportamiento del `PUT` (ya no reemplaza gradas; con 4 ignora las no emparejadas) | Comunicarlo al regenerar el cliente (ya en Notas del paso 02) |

### Bloqueos

Ninguno.

## Ciclo 3 - Paso 03 (Propagar la cancelación hasta Dapper)

**Veredicto: CHANGES_REQUESTED → estado `NEEDS_CHANGES`**. Único motivo bloqueante: AC3.5 no demostrado (no implementado por Q4 sin respuesta). El resto del paso (AC3.1-AC3.3, AC3.6) está demostrado con red → green; AC3.4 se acepta como limitación del driver (ver decisión). No hay defectos de código que corregir en lo implementado.

### Evidencia ejecutada por REVIEWER

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Git status | `git status --short` | M: 4 ficheros de test y 7 de producto del alcance, ficheros del plan, `MEMORIES.md` (OPTIMIZER, aprobado); ?? `Unit/Controllers/FacilitiesControllerShould.cs`, `Unit/Repositories/TokenRecordingConnectionProvider.cs`. Sin `Gaffer.Api.Client`, `TestFixture.cs` ni `ExceptionHandlingMiddleware.cs`. Commands/Queries sin cambios (ya reenviaban el token, comprobado con `grep`) |
| Preflight | `docker ps` | `Gaffer.Database` Up |
| Build | `dotnet build gaffer-api.sln --no-incremental` | 56 advertencias (igual que la base), 0 errores |
| Red independiente | Copia del árbol en scratchpad con mutaciones que quitan el reenvío (controladores → `CancellationToken.None`; repositorios sin token al proveedor; proveedor sin `ThrowIfCancellationRequested` ni token en `OpenAsync`/`BeginTransactionAsync`/`CommandDefinition`); `dotnet test Gaffer.Api.Tests.csproj --filter "PassTheRequestToken\|WhenPropagatingTheCancellationToken\|SqlConnectionProviderShould"` | 22/32 fallan: exactamente los 22 tests nuevos (10 controladores, 8 repositorios, 4 proveedor); los 10 previos en verde |
| Green / suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 265/265, 0 omitidos (243 + 22) |
| AC3.4 | `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~StopALongRunningQueryWhenItsTokenIsCancelled"` | Falla en 5 s: `Expected a <System.OperationCanceledException> to be thrown, but no exception was thrown.` (reproduce lo registrado por BUILDER) |
| AC3.6 | `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` | 0 |
| Cobertura del reenvío | `grep` de `QueryAsync\|ExecuteAsync\|QueryScalarAsync\|ExecuteInTransactionAsync` en `Gaffer.Api` + lectura del diff | Todas las llamadas de repositorio reciben el token (las multilínea cierran con `}, cancellationToken)`); sin token solo `ClearAsync`/`CreateAsync` internos del proveedor (fuera del camino de petición, justificado) |
| Middleware | `grep` en `ExceptionHandlingMiddleware.cs` | `catch (Exception)` genérico → 500 + `LogError` para `OperationCanceledException` (sin cambios; ver H1) |

### Criterios

| ID | Estado | Evidencia |
| --- | --- | --- |
| AC3.1 | Demostrado | 10 tests `PassTheRequestTokenTo...` (5 `PlayerControllerShould`, 5 nuevo `FacilitiesControllerShould`) con `Received(1).Handle(..., token)` de un `CancellationTokenSource` propio (≠ `None`). Red por mutación: 10/10 |
| AC3.2 | Demostrado (desviación aceptada, H3) | `WhenPropagatingTheCancellationToken` en ambos repositorios (4 + 4), todos los métodos públicos; el doble exige un mínimo de llamadas (Update facility: 7, cuadra con tx + facility + dirección + lookup + update + insert + enlace) y que todas lleven el token. Red por mutación: 8/8 |
| AC3.3 | Demostrado | `NotRunAQueryWhenItsTokenIsAlreadyCancelled` (OCE, conexión cerrada), `NotWriteAnythingWhenAnExecuteTokenIsAlreadyCancelled`, `NotWriteAnythingWhenATransactionTokenIsAlreadyCancelled` (fila no insertada, observada con otro proveedor), más `RollBackATransactionWhoseOperationIsCancelled` (cancelación a mitad de transacción revierte la primera fila). Red por mutación: 4/4 |
| AC3.4 | No cumplido; **aceptado como limitación del driver** (decisión REVIEWER) | Rama "si no lo soporta" del criterio: medición registrada en `STATUS.md` (sonda y test: ~5 s sin excepción con `MySql.Data` 26.7.0) y reproducida aquí. `MySqlCommand.Cancel()` lanza `NullReferenceException`; cambiar de driver está fuera de alcance (`00`). No bloquea: el comportamiento alcanzable (no abrir/ejecutar con token cancelado, detener las llamadas siguientes, revertir la transacción) está demostrado en AC3.3. Ver H2 sobre el test `[Explicit]` |
| AC3.5 | **No demostrado** | No implementado: Q4 sin respuesta del operador (el plan lo marca como prerrequisito de AC3.5). Sin test de middleware. Bloqueante para `APPROVED` |
| AC3.6 | Demostrado | `grep` → 0; suite completa 265/265 |
| Refactor | Hecho | Una sola construcción de `CommandDefinition` (`Command(...)`), evaluada dentro de la lambda tras abrir (toma la transacción ambiente correcta); revalidado 265/265 |

### Hallazgos

| ID | Severidad | Hallazgo | Acción |
| --- | --- | --- | --- |
| H1 | Bloqueante (vía AC3.5) | Este paso hace alcanzable `OperationCanceledException` en peticiones abortadas por el cliente (antes el token era `None`). Sin AC3.5, el middleware la trata como desconocida: intenta escribir un 500 y registra `LogError` por cada abortada. Es exactamente el caso de Q4 | Operador: responder Q4 (recomendado a). BUILDER: implementar AC3.5 con test de middleware en el siguiente ciclo |
| H2 | Menor, no bloqueante | `StopALongRunningQueryWhenItsTokenIsCancelled` queda `[Explicit]` y en rojo si se ejecuta: documenta la limitación pero es un test que falla a propósito | Opcional: mantenerlo como sonda (motivo ya en `Explicit`) o convertirlo en caracterización del comportamiento actual. Decide el operador |
| H3 | Informativo | AC3.2 pide proveedor "sustituido"; el doble `TokenRecordingConnectionProvider` sustituye la interfaz pero delega en el `SqlConnectionProvider` real (Docker), porque un stub devolvería `null` para el tipo privado `FacilityRow`. Motivo razonable; los tests viven en `BaseSqlTest`, ya dependiente de Docker | Ninguna |
| H4 | Informativo | Parámetro de acción `CancellationToken cancellationToken = default` (no obligatorio) por las llamadas directas de `Acceptance/Features_Refactor/SearchPlayersFeatureShould.cs` (fuera de alcance). ASP.NET Core enlaza `RequestAborted` igualmente; el parámetro no aparece en el OpenAPI (tests de contrato en verde) | Ninguna; la carpeta `Features_Refactor` ya está anotada como fuera de alcance |
| H5 | Informativo (deuda Q1) | Una operación cancelada fuera de transacción tras abrir deja la conexión abierta hasta el fin del scope (misma deuda Q1). `CommitAsync`/`RollbackAsync` sin token, a propósito | Ninguna |
| H6 | Higiene | `git diff` avisa CRLF → LF en `SqlConnectionProviderShould.cs` y `SqlPlayerRepositoryShould.cs`; se normaliza al commitear (`eol=lf`) | Ninguna |

### Bloqueos

- AC3.5 depende de Q4 (decisión del operador). No hay bloqueo técnico: Docker y la suite disponibles.
- Q0 (cómo combinar la corrección con el paso 04) queda para el operador en el checkpoint.

## Ciclo 4 - Corrección del paso 03 (AC3.5, H2) + Paso 04 (Cambios pequeños)

**Veredicto: APPROVED** (paso 03 completo y paso 04). Q0 = b, Q4 = b (499, log `Information`), Q5 = b (por defecto), Q6 = "Gaffer Api", H2 = retirar.

### Evidencia ejecutada por REVIEWER

| Comprobación | Comando | Resultado |
| --- | --- | --- |
| Git status / diff | `git status --short`; `git diff HEAD` de los ficheros del ciclo | Cambios dentro del alcance de 03 y 04: `ExceptionHandlingMiddleware.cs`, `TestFixture.cs`, `Player.cs`, `UpdatePlayerCommandHandler.cs`, `Startup.cs`, `SwaggerStartup.cs`, `VersioningStartup.cs`, `nswag.json`, `IGafferApiClient` movido (D + ??), `using` en 6 ficheros de test, test nuevo `Integration/TestServerDependenciesShould.cs`. Resto del diff = paso 03 ya revisado en ciclo 3. `Gaffer.Api.Client` no tocado ni verificado |
| Preflight | `docker ps` | `Gaffer.Database` (mariadb) Up |
| Build | `dotnet build gaffer-api.sln --no-incremental` | 0 errores, 56 advertencias (igual que la base) |
| Suite completa | `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 269/269, 0 omitidos |
| Red AC3.5 | Copia en scratchpad (`git archive HEAD` + ficheros del árbol salvo el middleware de HEAD); filtro `ExceptionHandlingMiddlewareShould` | 7/9: `Expected context.Response.StatusCode to be 499, but found 500`; `Expected entries[0].Level to be LogLevel.Information, but found LogLevel.Error` |
| Green AC3.5 | Suite completa en el árbol | incluidos los 9/9 del middleware |
| H2 | `grep -rn "Explicit" Gaffer.Api.Tests --include=*.cs` | Solo el nombre de un test de aceptación ajeno (`KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls`); ningún atributo `[Explicit]` |
| Mutación AC4.1 | Misma copia, borrando `services.ConfigureActions();` de `TestFixture.cs`; filtro `TestServerDependenciesShould` | Falla: `... not to be <null> because IQueryHandler`2 is registered in production.` (en el árbol: verde) |
| Greps AC4.2-AC4.6 | ver Criterios | ver Criterios |

### Criterios

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC3.5 | Demostrado | `catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)` → `LogInformation` + 499 si la respuesta no ha empezado, sin cuerpo. Tests: 499 y cuerpo vacío; una sola entrada `Information` (con `TaskCanceledException`); sin `RequestAborted` sigue 500 + `LogError`. Red→green reproducido |
| Paso 03 global | Demostrado | AC3.1-AC3.3, AC3.6 demostrados en ciclo 3; AC3.4 aceptado en ciclo 3 como limitación del driver (medición registrada allí); AC3.5 ahora demostrado; test `[Explicit]` retirado (H2) |
| AC4.1 | Demostrado | `grep -n "AddScoped<" TestFixture.cs` → solo L113 (`IDatabaseConnectionProvider`); `ConfigureRepositories`/`ConfigureActions` no registran el proveedor de conexión, así que el de la BD de test no se sobrescribe. `TestServerDependenciesShould` toma los tipos de los startups de producción (2 + 8) y los resuelve en un scope; la mutación demuestra que puede fallar |
| AC4.2 | Demostrado | `grep -rn "IGafferApiClient\|Shared.Clients" Gaffer.Api` → 0; carpeta `Shared/Clients` inexistente; fichero movido idéntico salvo `namespace`; tests compilan y pasan |
| AC4.3 | Demostrado | `grep -nE "^\s*//"` → solo `Startup.cs` L42-43 (comentario explicativo) |
| AC4.4 | Demostrado | `grep -rn "BirthDateName\|commandFirstName"` en `Gaffer.Api` y tests → 0; `SetFirstName(newFirstName)`. `GetGenderName` conservado: devuelve el nombre del enum, nombre correcto (criterio "revisar"). Tests de `Player` y del handler en la suite verde |
| AC4.5 | Demostrado | `nswag.json` L33 `"infoTitle": "Gaffer Api"`. Evidencia opcional `dotnet-nswag` no ejecutada (opcional) |
| AC4.6 | Demostrado | `grep -c "new GafferApiVersioningDefinition" VersioningStartup.cs` → 1; la convención usa la instancia registrada como singleton (mismas `Versions`); tests de versionado/OpenAPI en verde |
| AC4.7 | Demostrado | 269 = 265 + 3 (AC3.5) + 1 (AC4.1). `swagger.json` idéntico antes/después según BUILDER (comparación byte a byte con test temporal); REVIEWER no lo repitió, pero ninguno de los cambios afecta al documento servido (el título servido sale de `productName`, no de `nswag.json`; la instancia de versionado tiene las mismas versiones; el código borrado era comentario) y los tests de contrato OpenAPI están en verde |

### Hallazgos

| ID | Severidad | Detalle |
| --- | --- | --- |
| H1 | No bloqueante | `HandleAbortedRequest` fija 499 pero no escribe cuerpo y no llama a `CompleteAsync`; correcto para un cliente que ya no escucha. Si la respuesta ya había empezado solo se registra el log (coherente con Q4). |
| H2 | No bloqueante (higiene) | EOL: varios `.cs` del árbol quedan con CRLF / mezcla (avisos `CRLF will be replaced by LF`); `.gitattributes` los normaliza al hacer `git add`. Misma familia que F1 del ciclo 1; lo resuelve el operador al commitear. |
| H3 | Informativo | `TestServerDependenciesShould` construye `ConfigureRepositories` con una configuración vacía; hoy el método ignora la configuración. Si en el futuro la usa, el test podría necesitar la configuración de test. |
| H4 | Informativo | Paso 04 no añade handlers: la lección permanente de `TestFixture.cs` queda cubierta estructuralmente (TestFixture reutiliza `ConfigureActions()`). |

### Bloqueos

Ninguno. MySQL/MariaDB local en Docker disponible.
