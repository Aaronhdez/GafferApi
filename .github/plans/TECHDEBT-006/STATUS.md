# STATUS - TECHDEBT-006

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 1 |
| Aprobación del plan | Aprobado por el operador ("Apruebo el plan, espera a que cree la rama"); rama `TECHDEBT-006` creada por el operador |
| Pasos completados | 01 APPROVED (ciclo 1, REVIEWER) |
| Pasos bloqueados | Ninguno. Q8, Q10, Q14 y Q17 respondidas por el operador (2026-10-12); ninguna pregunta abierta bloquea un ciclo. El push tras el ciclo 2 requiere la acción del operador de Q14 (`00`, "Acciones del operador") |
| Siguiente | OPTIMIZER, ciclo 1; después checkpoint humano (ciclo 2 = paso 02) |
| Bloqueos | Ninguno |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 Limpieza, operación, CI, observabilidad, `null` en `stands` y test "4 gradas + 5 enviadas" (sin contrato) | APPROVED |
| 2 | 02 `ProblemDetails` en camelCase con forma RFC 7807 (contrato; MAJOR 3.0.0) | - |
| 3 | 03 `.editorconfig` de C# + `dotnet format` completo + verificación en CI | - |
| 4 | Reserva | - |
| 5 | Reserva | - |
| 6 | Reserva | - |

Re-secuenciación (PLANNER, 2026-10-12, respuestas del operador): antiguo 02 fundido en 01 (Q8 "Dejarlo como está"); antiguos 03/04 → 02/03. Correspondencia de criterios en `00-overview-and-open-questions.md`.

## Tiempos por agente (ISO 8601 UTC)

| Ciclo | Agente | Inicio | Fin |
| --- | --- | --- | --- |
| 0 | PLANNER | 2026-10-12 | 2026-10-12 |
| 1 | BUILDER | 2026-10-12T12:27:34Z | 2026-10-12T12:43:14Z |
| 1 | REVIEWER | 2026-10-12T12:43:43Z | 2026-10-12T12:47:05Z |

## Comandos y resultados

| Ciclo | Paso | Comando / acción | Resultado |
| --- | --- | --- | --- |
| 1 | Preflight | `docker ps` | `Quionis_DB` (mariadb:latest) Up, `0.0.0.0:3306->3306/tcp`; tests contra `GafferDB_Test` |
| 1 | Línea base | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro OPT-006) | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | Línea base | `dotnet test Gaffer.Api.Tests --no-build --logger trx` | exit 0; 665/665 (35 s) |
| 1 | AC1.10 antes | `GAFFER_SWAGGER_DUMP_PATH=<scratch>/swagger-before.json dotnet test ... ServedOpenApiDocumentDump` (antes de editar) | exit 0; sha256 `be49f0ab…a5a6bfe` |
| 1 | Resumen del rojo (OPT-041, definido una vez) | `dotnet test Gaffer.Api.Tests --filter "<F>" --logger "console;verbosity=normal" > red.out; echo exit=$?; grep -oE "^ +(Correctas\|Con error\|Omitidas) \|error CS[0-9]+\|but found [A-Za-z0-9.]+" red.out \| sed 's/^ *//' \| sort \| uniq -c` (script `red.sh "<F>"` en scratchpad; `<F>` = filtro por clase) | Reutilizado en cada rojo/verde de abajo |
| 1 | AC1.3 (caracterización BD real) | `red.sh "FullyQualifiedName~SqlFacilityRepositoryShould+WhenDeletingAFacility"` con el test nuevo antes de tocar `Delete` | exit 0; 2 × Correctas → permiso para AC1.4 |
| 1 | AC1.1 (caracterización) | `red.sh "FullyQualifiedName~HostingEnvironmentShould"` antes de quitar el código muerto | 1.ª ejecución exit 1 (1 × Con error: error del propio test, esperaba `"false"` y la configuración da `"False"`); corregido el test → exit 0, 7 × Correctas. Después de quitar el código: verde en la suite completa |
| 1 | AC1.8/AC1.9 rojo | `red.sh "FullyQualifiedName~DatabaseHealthCheckShould"` | exit 1; 3 × CS1729 (constructor sin `logger`) |
| 1 | AC1.8/AC1.9 verde | ídem | exit 0; 25 × Correctas |
| 1 | AC1.8 cableado rojo | quitado temporalmente `logger:` de la factoría en `DatabaseStartup` (restaurado desde copia) + `red.sh "FullyQualifiedName~DatabaseStartupShould"` | exit 1; 1 × Con error, 12 × Correctas |
| 1 | AC1.8 cableado verde | ídem con la factoría restaurada | exit 0; 13 × Correctas |
| 1 | AC1.5 | `red.sh "FullyQualifiedName~SearchPlayersEndpointShould\|FullyQualifiedName~Features_Refactor\|FullyQualifiedName~PlayerFeaturesShould.GetAPlayer"` (antes de borrar la carpeta) | exit 0; 17 × Correctas (verde inmediato: caracterización, el endpoint ya filtraba) |
| 1 | AC1.12/AC1.13 rojo | `red.sh "FullyQualifiedName~FacilityValidationInTheProductionPipelineShould\|...RejectTheCreationOfAFacilityWithAnInvalidStand\|...RejectTheUpdateOfAFacilityWithInvalidStands\|FullyQualifiedName~RejectFiveSent"` | exit 1; 6 × Con error, 47 × Correctas; 6 × `but found InternalServerError` (500). Por clase: `FacilityFeaturesShould` 2 × 500, `FacilityValidationInTheProductionPipelineShould` 4 × 500. Los 2 tests 4+5 en verde inmediato |
| 1 | AC1.14 rojo | `red.sh "FullyQualifiedName~FacilityShould\|FullyQualifiedName~FacilityCommandHandlerShould"` | exit 1; 3 × CS1503, 1 × CS0266 (firma `CheckRequestedStands` / `BuildCommand` sin elementos `null`) |
| 1 | AC1.12-AC1.14 verde | `red.sh "FullyQualifiedName~FacilityShould\|FullyQualifiedName~FacilityCommandHandlerShould\|FullyQualifiedName~FacilityValidationInTheProductionPipelineShould\|FullyQualifiedName~FacilityFeaturesShould"` | exit 0; 240 × Correctas |
| 1 | AC1.11 build | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro OPT-006) | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | AC1.11 suite | `dotnet test Gaffer.Api.Tests --no-build --logger trx` | exit 0; 688/688 (31 s) |
| 1 | AC1.10 después | volcado `swagger-after.json` + `sha256sum` + `cmp` | exit 0; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` en ambos; `cmp` idéntico |
| 1 | AC1.9 tiempos | suma de `duration` de la clase en el trx (script `classtime.sh`) | Antes 18 tests, 5,323 s; después 25 tests, 0,733 s (−4,59 s) |
| 1 | AC1.11 git | `git status --short Gaffer.Api.Client Gaffer.Api.Tests/TestFixture.cs .github/workflows` | 0 líneas (cliente, `TestFixture` y CI sin cambios) |
| 1 | REVIEWER | build `--no-incremental`; suite; swagger HEAD vs árbol; rojo en worktree (OPT-033) | build exit 0, 0 advertencias; suite exit 0, 688/688; sha256 `be49f0ab…a5a6bfe` idéntico (`cmp`); rojo: compilación 3 × CS1729, 3 × CS1503, 1 × CS0266; comportamiento 8 × Con error (6 × 500, 1 cableado logger, 1 NRE handler). Veredicto APPROVED (ver `REVIEW.md`) |

## Ciclo 1 - Paso 01 (BUILDER)

Decisiones aplicadas (valores por defecto de `00`): Q1 a) migrar y borrar; Q2 a) quitar `NONE`; Q3 c) `ci.yml` sin cambios; Q4 a) sin código (para DOCUMENTER: `doc/features/technical/hosting-environment.md`); Q5 a) `Warning`; Q6 a) timeout inyectado; Q9 a) 400 con `Errors["Stands[i]"]`; Q8 "Dejarlo como está": registro MVC de `TestFixture` sin cambios.

Rama de contrato (OPT-032 + OPT-034): el diff toca `Program.cs`, `Startups/DatabaseStartup.cs` y `Controllers/FacilitiesController.cs`, así que se compara el volcado de `ServedOpenApiDocumentDump` antes de editar y después (sha256 idéntico; ver "Comandos y resultados").

Grep de usos (OPT-039):

| Tipo / miembro | Comando | Usos | Adaptados |
| --- | --- | --- | --- |
| `CriteriaType.NONE` | `grep -rn "CriteriaType.NONE\|\bNONE\b" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 5 en producto (3 handlers, `SqlFacilityRepository` L214, enum), 0 en tests | 5 → 0 |
| `SetConfigurationProviders` / `AddDevelopmentSettings` / `ConfigurationBuilderExtensions` | `grep -rn "...\|..." Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 4 líneas, todas en `Program.cs` | 0 |
| `new DatabaseHealthCheck(` | `grep -rn "new DatabaseHealthCheck(" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 6 (5 tests, 1 `DatabaseStartup`) | Parámetro opcional `logger` al final: los 6 compilan sin cambios; la factoría pasa el logger |
| `CheckRequestedStands`, `CreateFacilityCommand.Stands`, `UpdateFacilityCommand.Stands` | `grep -rn "CheckRequestedStands\|CreateFacilityStandCommand>\|UpdateFacilityStandCommand>" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 3 en producto + 3 en `FacilityShould` + 1 `BuildCommand` | Firma `IReadOnlyList<(string?, int)?>`; comandos `IReadOnlyList<T?>?` (covariante: los `List<T>` existentes siguen sin warnings); adaptados `FacilityShould` L180 y `BuildCommand` |

| AC | Evidencia | Resultado |
| --- | --- | --- |
| AC1.1 | `HostingEnvironmentShould.ReadTheAppSettingsFilesFromTheContentRoot` (nuevo): proveedores JSON `appsettings.json` y `appsettings.Development.json` en orden y `InfrastructureOptions:EnableCors` = `"False"` (el `appsettings.json` del repo es `{}`; el valor viene del de Development). Verde antes y después de quitar `SetConfigurationProviders`, su llamada, `ConfigurationBuilderExtensions` y el `config` sin uso; aserciones de `TakeTheHostEnvironmentFromTheEnvironmentVariable` sin cambios. `grep` → 0 | Verde-verde |
| AC1.2 | Q4 = a): sin cambio de código; anotado para DOCUMENTER | n/a |
| AC1.3 | `SqlFacilityRepositoryShould.WhenDeletingAFacility.RemoveTheAddressStandsAndLinksThroughTheSchemaWhenOnlyTheFacilityRowIsDeleted` (permanente): dos facilities (2 y 3 gradas), `DELETE FROM Facilities` solo del primero; filas del borrado `(Facilities, Addresses, Facilities_Addresses, Stands, Facilities_Stands)` = `(0,0,0,0,0)`, del otro `(1,1,1,3,3)`. Contra `GafferDB_Test` en `Quionis_DB` | Verde antes de AC1.4 |
| AC1.4 | `Delete` = transacción + `DELETE FROM Facilities`; `DeleteStands`/`DeleteAddress` eliminados (sin otros usos). `PassItsTokenToTheProviderWhenDeleting`: `minimumCalls` 6 → 2 (transacción + fila de facility). `RemoveTheFacilityAndItsAddressAndStands` y la aceptación `DELETE` en verde en la suite | Verde |
| AC1.5 | Tabla caso → test abajo; `Acceptance/Features_Refactor/` borrada (`ls Gaffer.Api.Tests/Acceptance \| grep -c Features_Refactor` → 0) | Verde |
| AC1.6 | `NONE` y sus 4 asignaciones quitados; `grep` → 0; tests de players/facilities en verde sin cambiar aserciones (suite) | Verde |
| AC1.7 | Q3 = c): `ci.yml` sin cambios (`git status` 0 líneas) | n/a |
| AC1.8 | `DatabaseHealthCheckShould`: `LogAWarningWithTheExceptionWhenTheSqlProbeThrows` (Warning con `InvalidOperationException`), `LogAWarningWithoutExceptionWhenTheSqlProbeFailsOrTimesOut` (Unreachable, Blocked), `LogNothingWhenTheDatabaseAnswersOrTheStatusComesFromStartup` (Healthy, Degraded, fallback); el mensaje no contiene ningún fragmento sensible; `ShouldNotExposeAnything` intacto. Cableado: `DatabaseStartupShould.GiveTheRegisteredHealthCheckTheHostLoggerSoAFailedProbeIsLogged` (rojo demostrado quitando el logger de la factoría). Nota: `SqlConnectionProvider.TestConnection` captura `DbException`/`TimeoutException`/`SocketException` y devuelve `false`, así que un fallo real de BD se registra como Warning **sin** excepción; solo otras excepciones llegan al log con la excepción | Rojo → verde |
| AC1.9 | `GiveUpAsUnhealthyWithinFiveSecondsWhenTheProbeDoesNotAnswer` sustituido por `GiveUpAsUnhealthyWhenTheProbeDoesNotAnswerWithinTheTimeout` (`ShortTimeout` 200 ms, `< ShortTimeout + 1 s`) y `WaitAtMostFiveSecondsByDefault` (sin esperar). Clase: 5,323 s → 0,733 s | Verde |
| AC1.10 | sha256 del `swagger.json` servido idéntico antes/después; `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde en la suite | Verde |
| AC1.11 | Suite 665 → 688 = 665 − 5 (`Features_Refactor`) + 28 nuevos (AC1.3 1, AC1.1 1, health check +7 netos (18 → 25), cableado 1, búsqueda 4, `FacilityFeaturesShould` 3, producción 5, dominio 4, handlers 2). Build 0/0. `TestServerDependenciesShould` en verde. `TestFixture.cs`, `Gaffer.Api.Client/` y `ci.yml` sin cambios | Verde |
| AC1.12 | Caracterización previa (rojo) en producción (`StartupTestServer`) y `TestFixture`: `POST`/`PUT` con `"stands": [null]` y con `null` entre dos gradas válidas → **500**, cuerpo `{"Type":null,"Title":"Unknown error","Status":500,"Detail":null,"Instance":"/api/v1.0/facilities/<id>","Extensions":{}}` (NRE en el mapeo del controlador). Después: 400 con `Error.Facilities.Create.Validation` / `Error.Facilities.Update.Validation` y `Errors["Stands[0]"]` / `Errors["Stands[1]"]` (mensaje "`Stands[i]` is required."); create sin filas nuevas (`CountStoredRows`), update con `GET` igual. Producción: 2 casos × 2 operaciones; `TestFixture`: 1 representante × 2 operaciones | Rojo (6 × 500) → verde |
| AC1.13 | `FacilityFeaturesShould.RejectFiveSentStandsForAFacilityThatHasFourAndChangeNothing` (`TestFixture`, rechazo por `[MaxLength(4)]` en model binding) y `FacilityValidationInTheProductionPipelineShould.RejectFiveSentStandsForAFacilityThatHasFourInTheDomainAndChangeNothing` (producción, rechazo del dominio): 400 `Error.Facilities.Update.Validation` con `Errors.Stands`, `GET` igual | Verde inmediato (caracterización) |
| AC1.14 | `FacilityShould.ReportANullRequestedStandByItsIndex` (posición 0, 1, 2) y `ReportNullRequestedStandsTogetherWithTheOtherStandErrors` (`Stands`, `Stands[0]`, `Stands[1].StandName`, `Stands[2]`, `Stands[4].StandCapacity`); handlers: `CreateFacilityCommandHandlerShould.RejectANullStandTogetherWithTheOtherStandErrorsAndNotSaveIt` (sin `Save`) y `UpdateFacilityCommandHandlerShould.RejectANullSentStandTogetherWithTheOtherStandErrorsWithoutUpdating` (sin facility actualizado) | Rojo (3 × CS1503, 1 × CS0266) → verde |

AC1.5, caso → test HTTP:

| Caso de `SearchPlayersFeatureShould` | Test HTTP |
| --- | --- |
| Por id (`GetBy`) | `PlayerFeaturesShould.GetAPlayer` (existente) |
| Sin filtros | `SearchPlayersEndpointShould.ReturnEveryPlayerWhenSearchingWithoutFilters` (nuevo) |
| Filtro `Id` | `SearchPlayersEndpointShould.ReturnOnlyThePlayerWithTheGivenId` (nuevo) |
| Filtro `FirstName` | `SearchPlayersEndpointShould.ReturnOnlyThePlayerMatchingTheNameFilter("FirstName", ...)` (nuevo) |
| Filtro `LastName` ("SecondName") | `SearchPlayersEndpointShould.ReturnOnlyThePlayerMatchingTheNameFilter("LastName", ...)` (nuevo) |

Nota AC1.5: el criterio dice "con `GET` y filtros de query", pero el endpoint real es `POST /players/search` con los filtros en el cuerpo (`PlayerController` L40 `[HttpPost("search")]`); los tests usan el endpoint existente, sin cambiar el contrato.

Observaciones para REVIEWER: un `null` en `stands` se reporta con la clave `Stands[i]` y el mensaje "`Stands[i]` is required."; los `null` también cuentan para el máximo de 4 (`Stands`). Ningún fichero fuera del alcance del paso; `Gaffer.Api.Client` sin tocar.

## Documentación modificada al cierre

Pendiente (DOCUMENTER). Notas del ciclo 1 para después de APPROVED: Q4 a) solo documentación en `doc/features/technical/hosting-environment.md`; log `Warning` del health check (Q5 a)); `null` en `stands` → 400 (Q9 a)); borrado de facility apoyado en FK en cascada + trigger.
