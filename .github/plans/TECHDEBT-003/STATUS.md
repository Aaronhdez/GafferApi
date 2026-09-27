# STATUS - TECHDEBT-003

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 5 |
| Pasos completados | 01 (APPROVED ciclo 1; F1 de higiene EOL pendiente para el operador, ver REVIEW.md); 02 (APPROVED ciclo 2; H1-H4 no bloqueantes, ver REVIEW.md) ; 03 (NEEDS_CHANGES ciclo 3: AC3.1-AC3.3 y AC3.6 demostrados; AC3.4 aceptado por REVIEWER como limitación del driver; AC3.5 no demostrado, pendiente de Q4); ciclo 4: 03 APPROVED (AC3.5 demostrado, H2 retirado) y 04 APPROVED (REVIEWER ciclo 4); ciclo 5: 05 APPROVED (REVIEWER ciclo 5; H1-H5 informativos/no bloqueantes, ver REVIEW.md) |
| Pasos bloqueados | Ninguno. Q4, Q6 y Q7 respondidas antes del ciclo 4 (ver "Respuestas del operador (antes del ciclo 4)") |
| Respuestas del operador (antes del ciclo 4) | Literal: "Q0. opción b / Q4. Opcion b / Q6. \"Gaffer Api\" / H2. Quitalo / Apruebo P3-1 y P3-2". Seguimiento: nivel de log de Q4 = "Information (Recomendado)"; Q7 = "c) No activar" (paso 05). Q5 sin respuesta → b) (solo los 12 registros). Q0 = b: corregir el paso 03 y avanzar al paso 04 en el mismo ciclo. P3-1/P3-2 → OPT-019/OPT-020 (OPT-019 lo aplica el operador; no aplicado por BUILDER) |
| Actualización del plan (antes del ciclo 2) | Q2 respondida y después corregida por el operador (literal en `00-overview-and-open-questions.md`): la grada se identifica por su `Id` (`Guid`) existente, opcional en la petición; update sin borrado de gradas ausentes; `Id` desconocido/ajeno → 400 `Error.Facilities.Update.UnknownStand`. ULID descartado (anotado en "Posibles mejoras"); sin migración ni paquetes. Q9, Q11 y Q12 retiradas; Q10 y Q13 con valor por defecto. OPT-016 (nota `MySql.Data` `DisposeAsync` en `dev-stack.md`) incluida en el ciclo 2 como AC2.10 por decisión del operador. Ajuste final R-MAX (literal en `00-overview-and-open-questions.md`): con la facility ya a 4 gradas, una grada con `Id` no existente entre ellas se descarta sin error y el resto del update se aplica (AC2.11). Q14-Q18 respondidas por el operador (literal en `00`): Q14 = ignorar también la grada sin `Id` con 4 gradas; Q15 = crear la grada con `Id` desconocido/ajeno con menos de 4; Q16-Q18 por defecto. Clave `Error.Facilities.Update.UnknownStand` retirada (ningún caso la produce); Q13 por defecto reajustada a la clave `Error.Facilities.Update.DuplicatedStand` (AC2.12 nuevo). Q19 nueva (qué `Id` recibe la grada creada por Q15) con valor por defecto a) `Id` nuevo generado, no bloqueante. AC2.3, AC2.4, AC2.8 y AC2.11 reescritos. |
| Siguiente | OPTIMIZER ciclo 5 y checkpoint humano (DOCUMENTER solo con aprobación explícita) |
| Bloqueos | Ninguno. Q1 sin respuesta: se aplica b) (sin `try/finally`), anotado como deuda. AC3.4 aceptado por REVIEWER (ciclo 3) como limitación del driver `MySql.Data` 26.7.0; su test `[Explicit]` retirado en el ciclo 4 (H2), la medición queda en el ciclo 3. |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0 (abierta, `00-overview-and-open-questions.md`).

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - SqlConnectionProvider | APPROVED |
| 2 | 02 - Update de Facility con gradas por `Id` (`Guid`) (+ OPT-016) | APPROVED |
| 3 | 03 - Cancelación | NEEDS_CHANGES (solo AC3.5, Q4) |
| 4 | Corrección del paso 03 (AC3.5, H2) + 04 - Cambios pequeños (Q0 = b) | APPROVED (paso 03 completo + paso 04) |
| 5 | 05 - Warnings | APPROVED |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-27T12:02:48Z | 2026-09-27T12:09:15Z |
| REVIEWER | 1 | 2026-09-27T12:09:55Z | 2026-09-27T12:13:30Z |
| BUILDER | 2 | 2026-09-27T12:55:34Z | 2026-09-27T13:05:14Z |
| REVIEWER | 2 | 2026-09-27T13:06:46Z | 2026-09-27T13:09:53Z |
| BUILDER | 3 | 2026-09-27T13:37:49Z | 2026-09-27T13:48:34Z |
| REVIEWER | 3 | 2026-09-27T13:50:10Z | 2026-09-27T13:54:34Z |
| BUILDER | 4 | 2026-09-27T14:08:59Z | 2026-09-27T14:17:17Z |
| REVIEWER | 4 | 2026-09-27T14:19:08Z | 2026-09-27T14:21:49Z |
| BUILDER | 5 | 2026-09-27T14:30:59Z | 2026-09-27T14:36:29Z |
| REVIEWER | 5 | 2026-09-27T14:37:38Z | 2026-09-27T14:42:30Z |

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
| 2 | - | Preflight | `docker ps` | `Gaffer.Database` Up |
| 2 | - | Línea base | `dotnet test gaffer-api.sln --verbosity quiet` | 220/220 |
| 2 | 02 | Aceptación (red) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~FacilityFeaturesShould"` | 12/23; 11 fallan por el motivo esperado (ver tabla de reglas abajo): `Id` distintos tras el update, gradas borradas con `Stands` `null`/vacío, 200 en vez de 400 (Q13, Q16), gradas reemplazadas con 4 existentes |
| 2 | 02 | Unitario (red, compilación) | `dotnet build gaffer-api.sln` | CS1729 `UpdateFacilityStandCommand` sin constructor de 3 argumentos; CS0246 `CannotUpdateFacilityDuplicatedStandException` no existe |
| 2 | 02 | Unitario (red, ejecución) tras añadir solo la superficie (`Guid? Id`, excepción, conversor) | filtro de `02` sin `FacilityFeaturesShould` | 9 fallan: 8 de `UpdateFacilityCommandHandlerShould` (fusión, Q13, Q16) y `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent` (3 esperadas, 2 encontradas: se borraba `South`) |
| 2 | 02 | Green | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~FacilityFeaturesShould\|FullyQualifiedName~UpdateFacilityCommandHandler\|FullyQualifiedName~SqlFacilityRepository\|FullyQualifiedName~ExceptionToHttpResponseConverters\|FullyQualifiedName~FacilitiesControllerContract"` | 89/89 |
| 2 | 02 | Refactor + suite completa | `dotnet build gaffer-api.sln --no-incremental`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 56 advertencias (sin cambio; ninguna nueva en ficheros tocados), 0 errores; 243/243 (220 + 23) |
| 2 | 02 | REVIEWER | `docker ps`; build `--no-incremental`; red en `git archive HEAD` (scratchpad); filtro del paso; suite completa | Up; 56 advertencias; red 12 fallos (11 aceptación + repositorio en el sitio); 89/89; 243/243 |
| 2 | 02 | Greps | `grep -rn "UnknownStand" Gaffer.Api Gaffer.Api.Tests`; `grep -n "DELETE" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs`; `grep -n "DisposeAsync" .github/doc/common/dev-stack.md` | 0 resultados; `DELETE` solo en `Delete` (L209) y en `DeleteStands`/`DeleteAddress` (L285-L298), llamados solo desde `Delete` (L206-L207); nota OPT-016 en L25 |
| 3 | - | Preflight | `docker ps` | `Gaffer.Database` Up |
| 3 | - | Línea base | `dotnet test gaffer-api.sln --verbosity quiet` | 243/243 |
| 3 | 03 | Aceptación (red), tras añadir solo la superficie (parámetros `CancellationToken` sin usar) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~SqlConnectionProviderShould"` | 5 fallan: `Expected a <System.OperationCanceledException> to be thrown, but no exception was thrown.` (4 de AC3.3 + AC3.4, este tras 5 s) |
| 3 | 03 | Aceptación (green AC3.3) | mismo filtro | 14/14 AC3.3 y previos en verde; AC3.4 sigue fallando (5 s, sin excepción): limitación del driver, ver abajo |
| 3 | 03 | Sonda del driver (scratchpad, `dotnet run probe.cs`, `MySql.Data` 26.7.0 + Dapper 2.1.79, cadena de conexión de user-secrets) | `SELECT SLEEP(5)` con token cancelado a los 100 ms | `MySqlCommand.ExecuteScalarAsync(ct)` / `ExecuteReaderAsync(ct)`: 5019 / 5008 ms, resultado 0, sin excepción. Dapper `QueryAsync(CommandDefinition(ct))`: 4 ejecuciones de 5001-5032 ms sin excepción (una ejecución aislada previa dio `TaskCanceledException` en 105 ms, no reproducible). `MySqlCommand.Cancel()` explícito a los 100 ms: interrumpe pero el driver lanza `NullReferenceException` (126-155 ms) |
| 3 | 03 | AC3.4 (medición) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~StopALongRunningQueryWhenItsTokenIsCancelled"` | Falla en 5 s: `Expected a <System.OperationCanceledException> to be thrown, but no exception was thrown.` Test marcado `[Explicit]` (se ejecuta solo con este filtro) |
| 3 | 03 | Unitario (red, compilación) al hacer obligatorio el parámetro de acción | `dotnet build gaffer-api.sln` | CS7036 ×5 en `Acceptance/Features_Refactor/SearchPlayersFeatureShould.cs` (fuera del alcance): se opta por `CancellationToken cancellationToken = default` en las acciones (ver desviaciones) |
| 3 | 03 | Unitario (red, ejecución) | `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~PassTheRequestToken\|FullyQualifiedName~WhenPropagatingTheCancellationToken"` | 18/18 fallan: 10 controladores `ReceivedCallsException` (el handler recibe `CancellationToken.None`); 8 repositorios `... to be empty because every provider call must receive the repository's token, but found at least one item {"ExecuteInTransactionAsync"}` / `{"QueryAsync SELECT * FROM ..."}` / `{"ExecuteAsync ..."}` |
| 3 | 03 | Green | `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~Controller\|FullyQualifiedName~Repository\|FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~ExceptionHandlingMiddleware" --verbosity quiet` | 92/92 (AC3.4 `[Explicit]` no incluido) |
| 3 | 03 | Refactor + suite completa | `dotnet build gaffer-api.sln --no-incremental`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 56 advertencias (igual que la base; dos CS8625 nuevas del test propio corregidas), 0 errores; 265/265 (243 + 22), 0 omitidos |
| 3 | 03 | AC3.6 | `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` | 0 |
| 4 | - | Preflight | `docker ps` | `Gaffer.Database` Up |
| 4 | 03 (corrección) | Unitario AC3.5 (red) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~ExceptionHandlingMiddlewareShould"` | 7/9; fallan `ReturnClientClosedRequestWhenTheRequestIsAbortedByTheClient` (`Expected context.Response.StatusCode to be 499, but found 500`) y `LogAnAbortedRequestOnceAsInformationWithoutError` (`Expected entries[0].Level to be LogLevel.Information {value: 2}, but found LogLevel.Error {value: 4}`). `KeepReturningAnUnknownErrorForACancellationWhenTheRequestIsNotAborted` ya en verde (caracterización) |
| 4 | 03 (corrección) | Unitario AC3.5 (green) | mismo filtro | 9/9 |
| 4 | 03 (corrección) | H2 | `grep -rn "StopALongRunning\|\[Explicit" Gaffer.Api.Tests --include=*.cs` | 0 (la única coincidencia de `Explicit` es el nombre `KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls`) |
| 4 | 03 (corrección) | Filtro del paso 03 | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~Controller\|FullyQualifiedName~Repository\|FullyQualifiedName~SqlConnectionProvider\|FullyQualifiedName~ExceptionHandlingMiddleware" --verbosity quiet` | 95/95 (92 del ciclo 3 + 3 de AC3.5) |
| 4 | 04 | Aceptación AC4.1 (caracterización, antes del cambio) | `dotnet test gaffer-api.sln --filter "FullyQualifiedName~TestServerDependenciesShould"` | 1/1 |
| 4 | 04 | AC4.1 (mutación, prueba de que el test puede fallar) | se borra temporalmente de `TestFixture.cs` el registro de `UpdateFacilityCommandHandler`; mismo filtro; se restaura el fichero (`git diff --stat` vacío) | Falla: `Expected scope.ServiceProvider.GetService(serviceType) not to be <null> because ICommandHandler`1 is registered in production.` |
| 4 | 04 | AC4.1 (después) | mismo filtro; `grep -n "AddScoped<" Gaffer.Api.Tests/TestFixture.cs` | 1/1; una sola coincidencia (L113, `IDatabaseConnectionProvider` contra la BD de test) |
| 4 | 04 | AC4.2 | `grep -rn "IGafferApiClient\|Shared.Clients" Gaffer.Api --include=*.cs --include=*.csproj` | antes: `Gaffer.Api/Shared/Clients/IGafferApiClient.cs` L1, L3; después: 0 |
| 4 | 04 | AC4.3 | `grep -nE "^\s*//" Gaffer.Api/Startup.cs Gaffer.Api/Startups/SwaggerStartup.cs` | antes: 24 líneas (4 de código en `Startup.cs`, 18 de código en `SwaggerStartup.cs`, 2 explicativas); después: 2 (`Startup.cs` L42-43, comentario explicativo del logger de arranque) |
| 4 | 04 | Unitario AC4.4 (red, compilación) tras renombrar en tests y `UpdatePlayerCommandHandler` | `dotnet build gaffer-api.sln` | CS1061 `"Player" no contiene una definición para "SetBirthDate"` (`UpdatePlayerCommandHandler.cs(54,20)`) |
| 4 | 04 | Unitario AC4.4 (green) | `grep -rn "BirthDateName\|commandFirstName" Gaffer.Api Gaffer.Api.Tests --include=*.cs`; `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerShould\|FullyQualifiedName~UpdatePlayerCommandHandler\|FullyQualifiedName~TestServerDependenciesShould"` | 0 coincidencias; 19/19 |
| 4 | 04 | AC4.5 | `grep -n "infoTitle" Gaffer.Api/nswag.json` | L33 `"infoTitle": "Gaffer Api",` (antes `"My Title"`). Evidencia opcional con `dotnet-nswag` no ejecutada; salida generada sin tocar |
| 4 | 04 | AC4.6 | `grep -c "new GafferApiVersioningDefinition" Gaffer.Api/Startups/VersioningStartup.cs` | 1 (antes 2) |
| 4 | 04 | AC4.7 (`swagger.json` servido) | test temporal (scratchpad, copiado y borrado después) que arranca `StartupTestServer` y guarda `/swagger/{v1,v2,v3,AutogeneratedClient}/swagger.json`; "antes" sobre `git archive HEAD` en scratchpad, "después" sobre el árbol de trabajo; `cmp` | Los 4 documentos idénticos byte a byte (v1 200; v2/v3 404 en ambos, `ApiVersioning.Current = 1`). HEAD no incluye la corrección del paso 03 sin commit, que no altera el OpenAPI (NSwag omite `CancellationToken`) |
| 4 | 03 + 04 | Refactor + suite completa | `dotnet build gaffer-api.sln --no-incremental`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 56 advertencias (igual que la base), 0 errores; ninguna nueva en ficheros tocados (la de `TestFixture.cs` `HttpClient` CS8618 es previa, línea desplazada). 269/269, 0 omitidos (265 − 0 del `[Explicit]`, que no contaba + 3 AC3.5 + 1 AC4.1) |
| 4 | 03 + 04 | REVIEWER | `docker ps`; build `--no-incremental`; suite completa; red AC3.5 en copia scratchpad (middleware de HEAD); mutación AC4.1 (sin `ConfigureActions()` en TestFixture); greps AC4.2-AC4.6 | Up; 56 advertencias, 0 errores; 269/269; red 7/9 (499 vs 500, Information vs Error); mutación falla (`IQueryHandler`2` no resuelto); greps conformes |
| 4 | 03 + 04 | AC3.6 (revalidación) | `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` | 0 |

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

## Ciclo 2 - Paso 02 (BUILDER)

| Tema | Detalle |
| --- | --- |
| Preguntas por defecto aplicadas | Q3 = a) (dirección con `UPDATE` en el sitio, conserva `Id`); Q10 = a) (sin borrado de gradas, deuda); Q13 = a) (400 `Error.Facilities.Update.DuplicatedStand`); Q19 = a) (grada creada siempre con `Guid.NewGuid()`, el `Id` recibido se ignora). Ninguna respuesta del operador las sustituyó antes del ciclo |
| Producto | `Gaffer.Api/Commands/UpdateFacilityCommandHandler.cs` (`UpdateFacilityStandCommand(..., Guid? Id = null)`; `MergeStands` puro); `Gaffer.Api/Controllers/FacilitiesController.cs` (`UpdateFacilityStandRequest.Id` `Guid?` y mapeo); `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` (`Update` = `UPDATE` facility + `UpdateAddress` + `UpdateStands`, sin `DELETE`; `InsertStand` extraído y reutilizado por `InsertStands`); nuevo `Gaffer.Api/Shared/Exceptions/CannotUpdateFacilityDuplicatedStandException.cs`; `ExceptionToHttpResponseConverters.cs` (400 "Cannot update facility", `Error.Facilities.Update.DuplicatedStand`). `Facility.cs` sin cambios |
| Orden en el handler | (1) `[MaxLength(4)]` de la petición (sin cambios); (2) Q13; (3) clasificación emparejadas / creadas (<4 existentes) / descartadas (4 existentes); (4) invariante de `Facility` (4) → `FacilityValidationException`; (5) `Update`. Las gradas no emparejadas se filtran con un predicado (no `Except`, que colapsaría gradas nuevas idénticas) |
| Repositorio | `UpdateAddress`: `UPDATE Addresses ... WHERE Id IN (SELECT AddressId FROM Facilities_Addresses WHERE FacilityId = @FacilityId)`. `UpdateStands`: lee `StandId` enlazados a la facility dentro de la transacción; `UPDATE` de los enlazados, `INSERT` (+ `Facilities_Stands`) del resto; los enlazados ausentes no se tocan. Todo en `ExecuteInTransactionAsync` |
| DI / `TestFixture` | Sin servicios inyectables nuevos: `TestFixture.cs` no cambia (AC2.9) |
| Contrato (AC2.7) - desviación | El OpenAPI servido declara todos los `Guid` con `"format": "guid"` (convención NSwag del proyecto, también en `SearchFacilityStandResult.id`), no `uuid`. El test comprueba `id` fuera de `required`, anulable y con el mismo formato que `SearchFacilityStandResult.id`; cambiar a `uuid` exigiría configurar NSwag (fuera de alcance). `SearchFacilityStandResult` sin cambios (`id`, `standName`, `standCapacity`). El test de servidor está en `FacilitiesControllerContractShould` (arranca `StartupTestServer`) para no tocar ficheros fuera del alcance |
| AC2.10 | Nota OPT-016 añadida como sub-viñeta de la línea `MySql.Data` (L24) en `.github/doc/common/dev-stack.md`; `git diff --stat` = 1 inserción |
| Cambios de aserción registrados (Q2/Q3) | Aceptación: `UpdateAFacilityReplacingFieldsAddressAndStands` → renombrado `UpdateAFacilityFieldsAndAddressKeepingItsFourStands`; la aserción `ContainSingle("Only", 60000)` pasa a "las 4 gradas originales (North/South/East/West) y ninguna `Only`" (la facility de creación tiene 4 gradas: `Only` sin `Id` se descarta por R-MAX/Q14). Repositorio: `ReplaceAllFieldsAddressAndStands` → sustituido por `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent` (antes esperaba `Address.Id` nuevo y 1 sola grada; ahora `Address.Id` original, gradas actualizadas/conservadas/insertadas y sin filas huérfanas ni duplicadas) |
| Tests nuevos (23) | Aceptación 10; `UpdateFacilityCommandHandlerShould` 8; `SqlFacilityRepositoryShould` +1 neto (2 nuevos, 1 sustituido); `ExceptionToHttpResponseConvertersShould` +2 (casos de `TestCaseSource`); `FacilitiesControllerContractShould` 2 |
| AC2.6 | `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails`: grada nueva con nombre de 300 caracteres → `Data too long for column 'StandName' at row 1`; facility, dirección, gradas y recuentos de `Stands`/`Facilities_Stands` sin cambios. Caracterización: ya verde antes del cambio (el update anterior también era transaccional) y sigue verde |

### Tabla de reglas → tests (red → green)

| Regla | Aceptación | Unitario |
| --- | --- | --- |
| `Id` de existente → se actualiza, conserva `Id` (AC2.1) | `KeepTheIdsOfTheStandsSentWithTheirIdWhileUpdatingTheirValues` (red: `Id` distintos; también `Address.Id` igual) | `KeepTheIdOfMatchedStandsUpdatingTheirValuesAndKeepTheStandsNotMentioned` (red: 1 grada en vez de 2) |
| Sin `Id`, <4 → se crea (AC2.2) | `KeepTheStandsNotSentAndCreateTheOnesSentWithoutId` (red: 2 en vez de 3) | `CreateStandsWithNewIdsForMissingUnknownOrForeignIdsWhenLessThanFourStandsExist` (red: 3 en vez de 4) |
| `Id` desconocido / ajeno, <4 → se crea con `Id` nuevo (AC2.3, Q19) | `CreateStandsWithNewIdsForUnknownOrForeignIdsWhenTheFacilityHasLessThanFourStands` (red: 2 en vez de 4) | mismo test unitario que la fila anterior (`Id` creados únicos y distintos de los enviados, de `Guid.Empty` y de los existentes) |
| Sin `Id` / desconocido / ajeno, 4 existentes → se descarta (AC2.11) | `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands` (red: `Id` reemplazados) | `DiscardEveryUnmatchedStandWhenFourStandsExist` (red) |
| `Id` de existente repetido → 400 `DuplicatedStand` (AC2.12) | `RejectAnUpdateRepeatingTheIdOfAnExistingStand(2)` y `(4)` (red: 200) | `RejectARepeatedIdOfAnExistingStandWithoutUpdating(1)` y `(4)` (red: no lanza) + conversor `ExceptionToHttpResponseConvertersShould` |
| Existente que no viene / `Stands` `null` o vacío → se conserva | `KeepTheStandsNotSentAndCreateTheOnesSentWithoutId`, `KeepTheStandsWhenTheUpdateSendsNoStands(False/True)` (red: colección vacía) | `KeepEveryExistingStandWhenTheCommandHasNoStands(null/0)` (red) |
| Total > 4 → 400 `Create.Validation` (AC2.4, Q16) | `RejectAnUpdateLeavingMoreThanFourStandsWhenTwoExistAndThreeAreSentWithoutId`, `...WhenThreeExistAndOneIsNewAndOneUnknown` (red: 200) | `RejectAnUpdateLeavingMoreThanFourStandsWithoutUpdating` (red: no lanza); `PropagateValidationExceptionWhenStandsExceedFour` sigue verde |
| Petición > 4 → 400 (existente) | `RejectUpdatingAFacilityWithMoreThanFourStands` (verde, sin cambios) | - |
| Repositorio en el sitio, sin huérfanas (AC2.5) | - | `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent` (red: `South` borrada) |
| Contrato (AC2.7) | - | `DefineAnOptionalNullableIdOnUpdateFacilityStandRequest`, `DeclareTheStandIdOfTheUpdateRequestAsOptionalAndNullableInTheServedOpenApi` (red: compilación / propiedad inexistente) |

### `GET` comparado (green)

| AC | Antes | Después | Enviados |
| --- | --- | --- | --- |
| AC2.3 | A: North=3dc2f263…, South=7685ac62… | A: North=3dc2f263…, South=7685ac62…, Unknown=661f7b45…, Foreign=e9d7e58f… (ningún `Id` creado igual a un enviado); B: aserción `BeEquivalentTo(otherBefore)` verde | unknown=665ece74…, foreign=f455e620… |
| AC2.11 | A: North=53361cce…, South=728df315…, East=a98680ef…, West=cb15aa52… | A: North Renovated=53361cce…, South, East, West con los mismos `Id`; ninguna grada `Discarded*` ni `new`; cuerpo de respuesta vacío (igual que un update correcto); B sin cambios | new=39f97629…, foreign=77f98c91… |

## Ciclo 3 - Paso 03 (BUILDER)

| Tema | Detalle |
| --- | --- |
| Decisión del operador | "aprueba P2-1 Y P2-2 y procede con el paso 3". Q4 sin respuesta → AC3.5 **no implementado** (pendiente de Q4); `ExceptionHandlingMiddleware.cs` sin cambios. Ninguna otra pregunta de `00` afecta al paso 03 |
| Producto | `IDatabaseConnectionProvider` (`CancellationToken cancellationToken = default` en `QueryScalarAsync`, `QueryAsync`, `ExecuteAsync`, `ExecuteInTransactionAsync`); `SqlConnectionProvider` (token en `OpenAsync`, `BeginTransactionAsync` y Dapper vía `CommandDefinition`; `ThrowIfCancellationRequested` antes de abrir y en la transacción anidada; si `BeginTransactionAsync` falla se cierra la conexión); `DummyConnectionProvider` (firmas); `SqlPlayerRepository`/`SqlFacilityRepository` (todas las llamadas al proveedor, incluidas `GetAddress`/`GetStands`, `ExecuteInTransactionAsync` y las privadas de insert/update/delete); `PlayerController`/`FacilitiesController` (10 acciones con parámetro `CancellationToken`, reenviado al handler). Handlers de `Commands`/`Queries` sin cambios: ya reenviaban el token |
| No ampliado | `TestConnection`, `ClearAsync`, `CreateAsync` sin token: no están en el camino de una petición (arranque, health check, tests) |
| Commit/rollback | `CommitAsync`/`RollbackAsync` sin token a propósito: un rollback no debe poder cancelarse; una operación cancelada dentro de la transacción la revierte (`RollBackATransactionWhoseOperationIsCancelled`) |
| Tests nuevos (23; 22 en la suite por defecto) | AC3.3 (`SqlConnectionProviderShould`): `NotRunAQueryWhenItsTokenIsAlreadyCancelled`, `NotWriteAnythingWhenAnExecuteTokenIsAlreadyCancelled`, `NotWriteAnythingWhenATransactionTokenIsAlreadyCancelled`, `RollBackATransactionWhoseOperationIsCancelled` (fila no insertada comprobada con otro proveedor). AC3.4: `StopALongRunningQueryWhenItsTokenIsCancelled` (`[Explicit]`). AC3.1: 5 en `PlayerControllerShould` (`PassTheRequestTokenTo...`) y 5 en el nuevo `Unit/Controllers/FacilitiesControllerShould.cs`, cada uno con `Received(1).Handle(..., token)`. AC3.2: clase anidada `WhenPropagatingTheCancellationToken` en `SqlPlayerRepositoryShould` (4) y `SqlFacilityRepositoryShould` (4) con el nuevo doble `Unit/Repositories/TokenRecordingConnectionProvider.cs` |
| AC3.2: medio de observación | Doble de prueba que sustituye `IDatabaseConnectionProvider`, registra el token de cada llamada y delega en el `SqlConnectionProvider` real (Docker). Motivo: con un sustituto NSubstitute puro, `QueryAsync<FacilityRow>` (tipo privado) devolvería `null` y `Get` no llegaría a `GetAddress`/`GetStands`. Cada test exige un mínimo de llamadas (1 jugador; 8/3/7/6 facility) y que todas reciban el token |
| AC3.4 - limitación del driver (no se da por cumplido; decide REVIEWER) | `MySql.Data` 26.7.0 no actúa sobre el token una vez que el comando está en ejecución: `SELECT SLEEP(5)` cancelado a los ~100 ms termina en ~5 s sin excepción (test y sonda). El `Cancel()` explícito del comando interrumpe la consulta pero el driver lanza `NullReferenceException`, por lo que registrar `Cancel()` no es una alternativa limpia. Lo que sí se consigue: un token ya cancelado no abre conexión ni ejecuta, y las llamadas siguientes de la misma petición se detienen (AC3.3). Cambiar a `MySqlConnector` está fuera de alcance (`00`) |
| Desviaciones | (1) Parámetro de acción con valor por defecto (`CancellationToken cancellationToken = default`): `Acceptance/Features_Refactor/SearchPlayersFeatureShould.cs` llama a las acciones directamente y está fuera del alcance de ficheros; ASP.NET Core enlaza igualmente `RequestAborted`. (2) Ficheros de test nuevos dentro de las carpetas del alcance: `Unit/Controllers/FacilitiesControllerShould.cs`, `Unit/Repositories/TokenRecordingConnectionProvider.cs`. (3) AC3.4 `[Explicit]` para no dejar la suite en rojo ni añadir 5 s por ejecución |
| DI / `TestFixture` | Sin registros nuevos: `TestFixture.cs` no cambia |
| Contrato | Los parámetros `CancellationToken` no forman parte del OpenAPI (NSwag los omite); tests de contrato en verde. No verificado sobre la salida generada de `Gaffer.Api.Client` (fuera de alcance) |
| Q1 (deuda, sin cambios) | Una operación cancelada fuera de transacción después de abrir deja la conexión abierta hasta el fin del scope (misma deuda Q1); con token ya cancelado no se abre |

## Ciclo 4 - Corrección del paso 03 + Paso 04 (BUILDER)

### Corrección del paso 03 (REVIEW.md ciclo 3: H1/AC3.5, H2)

| Tema | Detalle |
| --- | --- |
| AC3.5 (Q4 = b, log `Information`) | `ExceptionHandlingMiddleware`: `catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)` → `LogInformation` (una entrada, sin excepción adjunta) y estado 499 si la respuesta no ha empezado; sin cuerpo (el cliente ya no escucha), sin 500 ni `LogError`. Una `OperationCanceledException` sin `RequestAborted` cancelado sigue siendo 500 + `LogError` (caracterizado) |
| Tests nuevos (3) | `ExceptionHandlingMiddlewareShould`: `ReturnClientClosedRequestWhenTheRequestIsAbortedByTheClient` (499, cuerpo vacío), `LogAnAbortedRequestOnceAsInformationWithoutError` (`TaskCanceledException`, derivada), `KeepReturningAnUnknownErrorForACancellationWhenTheRequestIsNotAborted` |
| H2 | Retirado `StopALongRunningQueryWhenItsTokenIsCancelled` (`[Explicit]`, en rojo a propósito) de `SqlConnectionProviderShould.cs`. La medición de AC3.4 sigue registrada en el ciclo 3 (filas "Sonda del driver" y "AC3.4 (medición)") |
| Ficheros | `Gaffer.Api/Shared/Exceptions/ExceptionHandlingMiddleware.cs` (1 fichero de producto, sin cambio de contrato OpenAPI: cumple Q0 = b), `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs`, `Unit/ConnectionProviders/SqlConnectionProviderShould.cs` |

### Paso 04

| AC | Cambio |
| --- | --- |
| AC4.1 | `TestFixture.cs`: los 10 `AddScoped<` de repositorios/handlers sustituidos por `services.ConfigureRepositories(builderContext.Configuration)` y `services.ConfigureActions()`; se mantiene el `IDatabaseConnectionProvider` por scope contra la BD de test. Q5 = b: bloque MVC/versionado de TestFixture sin cambios. Test nuevo `Integration/TestServerDependenciesShould.cs`: obtiene los tipos de servicio de los propios `ConfigureRepositories`/`ConfigureActions` de producción (2 + 8) y exige que el `TestServer` resuelva cada uno en un scope |
| AC4.2 | `Gaffer.Api/Shared/Clients/IGafferApiClient.cs` → `Gaffer.Api.Tests/TestShared/IGafferApiClient.cs` (`namespace Gaffer.Api.Tests.TestShared`); carpeta `Shared/Clients` eliminada; `using Gaffer.Api.Shared.Clients` retirado en `FacilityFeaturesShould`, `JsonContractShould`, `SearchPlayersEndpointShould`, `GafferApiClientTest`, `TestFixture` y sustituido en `Integration/ApiShould.cs` |
| AC4.3 | Código comentado borrado: `Startup.cs` (4 líneas `//services.*`) y `SwaggerStartup.cs` (18 líneas, `AddVersioning`/OAuth2) |
| AC4.4 | `Player`: `SetLastName(newLastName)`, `SetSecondLastName(newSecondLastName)`, `GetBirthDate()`, `SetBirthDate(string newBirthDate)`, `SetBirthDate(DateTime newBirthDate)`, `SetGender(string newGender)`. `GetGenderName` se mantiene: devuelve el nombre del miembro del enum, el sufijo es correcto (criterio del plan "revisar"). Llamadores: `UpdatePlayerCommandHandler` L54, `Unit/Domain/PlayerShould.cs` (tests `ReturnBirthDate`, `SetBirthDate`) |
| AC4.5 | `nswag.json` `infoTitle` = `"Gaffer Api"` (Q6). Salida generada de `Gaffer.Api.Client` sin tocar (la regenera el operador) |
| AC4.6 | `VersioningStartup.cs`: la convención de `AddMvc` usa la instancia creada al inicio (se elimina la segunda `new GafferApiVersioningDefinition()`) |
| AC4.7 | Suite 269/269; `swagger.json` servido idéntico antes/después |
| Orden de revalidación | Filtros por AC tras cada limpieza (AC4.1, AC4.4) y suite completa + build `--no-incremental` al final (registrado arriba) |
| DI / `TestFixture` (lección permanente) | Sin handlers nuevos; TestFixture pasa a reutilizar `ConfigureActions()`, por lo que un handler nuevo registrado en producción queda registrado en tests y cubierto por `TestServerDependenciesShould` |
| Desviaciones | (1) Fichero de test nuevo `Gaffer.Api.Tests/Integration/TestServerDependenciesShould.cs` (lo exige AC4.1; el plan no fija ubicación). (2) Ficheros de test tocados solo por `using` tras mover `IGafferApiClient` (consecuencia directa de AC4.2). (3) Evidencia opcional de AC4.5 con `dotnet-nswag` no ejecutada. (4) Los ficheros editados con `sed` quedaron con LF en el árbol de trabajo; `.gitattributes` (`*.cs text eol=lf`) normaliza igualmente |

## Ciclo 5 - Paso 05 (BUILDER)

| Campo | Valor |
| --- | --- |
| Inicio | 2026-09-27T14:30:59Z |
| Fin | 2026-09-27T14:36:29Z |
| Decisión del operador | Literal: "Aprueba P4 y sigue con el ciclo 5". Q7 = "c) No activar"; Q8 sin respuesta → a) |
| Base | Pasos 01-04 ya commiteados (4bd203a, 5a8c0b1, 6652ef9); árbol limpio de producto al empezar |
| Preflight | `docker ps` → `Gaffer.Database` Up |

### Recuentos (`dotnet build gaffer-api.sln --no-incremental -v q`, líneas `warning` deduplicadas con `sort -u`)

| Código | Antes | Tras producción | Final |
| --- | --- | --- | --- |
| CS8618 | 26 | 7 (tests) | 0 |
| CS8625 | 11 | 6 (tests) | 0 |
| CS8602 | 13 | 13 | 0 |
| CS0414 | 3 | 3 | 0 |
| CS8603 | 1 | 0 | 0 |
| CS8604 | 1 | 0 | 0 |
| CS9113 | 1 | 1 | 0 |
| **Total** | **56** (Api 19, Tests 34, Migrations 3, Client 0) | **30** (solo Tests) | **0** |

CS0169 y CS8601 del plan no aparecen en la base. `Gaffer.Api.Client`: 0 warnings (nada que registrar para el operador según Q8). 4 de los CS8625 de tests (`null` como `secondLastName` en `PlayerFeatureShould`/`PlayerFeaturesShould`) desaparecen con la corrección de producción de `Player`.

### Correcciones (sin `#pragma`, sin `NoWarn`, sin cambios en `.csproj`)

| Fichero | Cambio | Justificación |
| --- | --- | --- |
| `SqlFacilityRepository.cs` | `FacilityRow`/`AddressRow`/`StandRow`: `string ... = null!` (9); `GetAddress` → `Task<Address?>` | Filas privadas de Dapper; columnas `NotNullable` en `M0002`. `GetAddress` sí devuelve `null` si no hay fila |
| `Facility.cs` | Parámetro `Address? address` | El constructor ya valida `address ?? throw FacilityValidationException`: el `null` es una entrada contemplada |
| `Player.cs` | Parámetro `string? secondLastName` | La propiedad ya es `string?` y la columna es `Nullable()` |
| `PlayerController.cs` (`SearchPlayersFilter`), `UpdateFacilityCommandHandler.cs` (`Name`, `Address`), `SearchPlayersQueryHandler.cs` (`QueryFilter`), `IPlayerRepository.cs` (`SearchCriteria`) | `= null!` | Q8 a): sin `?` ni `required` para no alterar el OpenAPI (DTO de petición) ni el valor en tiempo de ejecución (sigue `null` si no llega) |
| `Gaffer.Api.Migrations/Program.cs` | `DatabaseSettings` `= null!`; `EnsureDatabaseExists(..., string databaseName)` sin `= null` | Configuración enlazada; el único llamador pasa siempre el nombre (con `null` el SQL sería inválido) |
| Tests `Unit/{Commands,Controllers,Queries}` (13 lambdas `Arg.Is`) | Guarda `x != null && ...` | Convención ya existente en `PlayerControllerShould` (L111, L128); NSubstitute 6 anota el argumento como anulable. Un `null` sigue sin coincidir |
| `PlayerControllerShould.cs` | `SearchPlayerResult` de prueba con `TestDefaultValues.DefaultFirstName/LastName` en lugar de `null` | Los tests solo verifican la llamada al handler |
| `PlayerFeaturesShould.cs` | Borrados 3 campos sin uso (CS0414) | — |
| `TestFixture.cs` | Borrado campo estático `HttpClient` sin uso | Sin referencias (grep). DI sin cambios |
| `SqlTest.cs` / `Uris.cs` | `_tablesToDelete = []`; `BaseAddress = string.Empty` | Siempre asignados antes de usarse; `null + "api/..."` y `"" + "api/..."` dan la misma cadena |
| `UseCasesMock.cs` | Sustitutos como inicializadores de propiedad (se elimina `AddPlayerActions`) | Mismo estado inicial |
| `GafferApiClientTest.cs` | `GafferApiClientException` expone `ResponseContent` (CS9113) | Conserva el contenido de la respuesta sin cambiar `Message` |

### Evidencia

| AC | Comando | Resultado |
| --- | --- | --- |
| AC5.1 | build `--no-incremental -v q` (base) | 56 (tabla) |
| AC5.2 | mismo build tras producción / final / final tras borrar el test temporal | 30 / 0 / 0 advertencias, 0 errores |
| AC5.3 | test temporal (scratchpad, copiado y borrado) que arranca `StartupTestServer` y vuelca `/swagger/{v1,v2,v3,AutogeneratedClient}/swagger.json`, antes y después; `cmp` | 4/4 idénticos byte a byte (v1 y AutogeneratedClient 200; v2/v3 404 en ambos) |
| AC5.4 | — | Ninguna supresión; `null!` solo en las propiedades de la tabla, justificadas |
| AC5.5 | — | Q7 = c: sin cambios en `TreatWarningsAsErrors` |
| AC5.6 | `dotnet test gaffer-api.sln --verbosity quiet` antes / después | 269/269 / 269/269, 0 omitidos |
| Rojo→verde | Rojo = las 56 advertencias de la base; verde = 0 con la suite igual | Refactor sin cambio de comportamiento: sin tests nuevos |

### Desviaciones

1. Builds por proyecto (producción, luego tests) en lugar de uno por código de warning; el recuento por código queda en la tabla.
2. Ficheros editados con `sed` quedan con LF en el árbol (`.gitattributes` normaliza).
3. `DummyConnectionProvider`, `.csproj` y `Directory.Build.props` sin tocar.

## Documentación modificada

Pendiente (DOCUMENTER).
