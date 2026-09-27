# STATUS - TECHDEBT-003

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 2 |
| Pasos completados | 01 (APPROVED ciclo 1; F1 de higiene EOL pendiente para el operador, ver REVIEW.md); 02 (APPROVED ciclo 2; H1-H4 no bloqueantes, ver REVIEW.md) |
| Pasos bloqueados | 03 (solo AC3.5, Q4), 04 (solo AC4.5, Q6), 05 (solo AC5.5, Q7) hasta respuesta del operador. El paso 02 no tiene preguntas bloqueantes |
| Actualización del plan (antes del ciclo 2) | Q2 respondida y después corregida por el operador (literal en `00-overview-and-open-questions.md`): la grada se identifica por su `Id` (`Guid`) existente, opcional en la petición; update sin borrado de gradas ausentes; `Id` desconocido/ajeno → 400 `Error.Facilities.Update.UnknownStand`. ULID descartado (anotado en "Posibles mejoras"); sin migración ni paquetes. Q9, Q11 y Q12 retiradas; Q10 y Q13 con valor por defecto. OPT-016 (nota `MySql.Data` `DisposeAsync` en `dev-stack.md`) incluida en el ciclo 2 como AC2.10 por decisión del operador. Ajuste final R-MAX (literal en `00-overview-and-open-questions.md`): con la facility ya a 4 gradas, una grada con `Id` no existente entre ellas se descarta sin error y el resto del update se aplica (AC2.11). Q14-Q18 respondidas por el operador (literal en `00`): Q14 = ignorar también la grada sin `Id` con 4 gradas; Q15 = crear la grada con `Id` desconocido/ajeno con menos de 4; Q16-Q18 por defecto. Clave `Error.Facilities.Update.UnknownStand` retirada (ningún caso la produce); Q13 por defecto reajustada a la clave `Error.Facilities.Update.DuplicatedStand` (AC2.12 nuevo). Q19 nueva (qué `Id` recibe la grada creada por Q15) con valor por defecto a) `Id` nuevo generado, no bloqueante. AC2.3, AC2.4, AC2.8 y AC2.11 reescritos. |
| Siguiente | OPTIMIZER ciclo 2; checkpoint humano |
| Bloqueos | Ninguno para el paso 01. Q1 sin respuesta: se aplica b) (sin `try/finally`), anotado como deuda. |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. El ciclo 6 es la única reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente es Q0 (abierta, `00-overview-and-open-questions.md`).

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - SqlConnectionProvider | APPROVED |
| 2 | 02 - Update de Facility con gradas por `Id` (`Guid`) (+ OPT-016) | APPROVED |
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
| BUILDER | 2 | 2026-09-27T12:55:34Z | 2026-09-27T13:05:14Z |
| REVIEWER | 2 | 2026-09-27T13:06:46Z | 2026-09-27T13:09:53Z |

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

## Documentación modificada

Pendiente (DOCUMENTER).
