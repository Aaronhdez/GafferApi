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
