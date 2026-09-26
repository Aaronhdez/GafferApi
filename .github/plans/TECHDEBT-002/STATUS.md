# STATUS - TECHDEBT-002

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 4 |
| Pasos completados | 01 (aprobado por REVIEWER en el ciclo 1); 02 (aprobado por REVIEWER en el ciclo 2); 03 (aprobado por REVIEWER en el ciclo 3); 04 (aprobado por REVIEWER en el ciclo 4). Todos los pasos del plan aprobados. |
| Pasos bloqueados | Ninguno. Q0-Q6 y Q4.1 resueltas (Q0, Q2, Q5 por default; Q1 = reemplazo de `secondLastName` según 2ª respuesta del operador; Q4.1 = `"Internal Server Error"`). |
| Bloqueos | Ninguno de entorno. |
| Aprobación del plan | Aprobado por el operador el 2026-09-26: "I approve the plan". Listo para el ciclo 1 (BUILDER, paso 01). |

## Secuenciación (un paso = un ciclo)

En el ciclo N, BUILDER implementa solo el paso N (más las correcciones de REVIEWER del ciclo anterior, si las hay) y REVIEWER solo aprueba el paso de ese ciclo. Los ciclos 5 y 6 son reserva para `NEEDS_CHANGES`; cómo se combina una corrección con el paso siguiente: Q0 = b) (default aplicado; ver `00-overview-and-open-questions.md`).

| Ciclo | Paso previsto | Veredicto |
| --- | --- | --- |
| 1 | 01 - Update parcial de Player | APPROVED |
| 2 | 02 - Fallos de infraestructura → 500 (Q3 = a) | APPROVED |
| 3 | 03 - Filtro no permitido en Facilities → 400 | APPROVED |
| 4 | 04 - Log en el middleware (Q6: 400 Error, 404 sin log) | APPROVED |
| 5 | Reserva | - |
| 6 | Reserva | - |

## Tiempos por agente

| Agente | Ciclo | Inicio (UTC) | Fin (UTC) |
| --- | --- | --- | --- |
| PLANNER | 0 | n/a | n/a |
| BUILDER | 1 | 2026-09-26T20:29:17Z | 2026-09-26T20:35:13Z |
| REVIEWER | 1 | 2026-09-26T20:36:13Z | 2026-09-26T20:37:28Z |
| BUILDER | 2 | 2026-09-26T20:55:12Z | 2026-09-26T21:02:17Z |
| REVIEWER | 2 | 2026-09-26T21:02:46Z | 2026-09-26T21:04:51Z |
| BUILDER | 3 | 2026-09-26T21:11:41Z | 2026-09-26T21:15:10Z |
| REVIEWER | 3 | 2026-09-26T21:16:17Z | 2026-09-26T21:17:53Z |
| BUILDER | 4 | 2026-09-26T21:28:57Z | 2026-09-26T21:33:40Z |
| REVIEWER | 4 | 2026-09-26T21:34:49Z | 2026-09-26T21:36:52Z |

## Prerrequisitos

| Comprobación | Resultado |
| --- | --- |
| Línea base (dato del operador, a revalidar por BUILDER) | Build 0 errores; `dotnet test` 159/159 contra Docker `Gaffer.Database` (3306) |
| Ciclo 1 - Docker | `docker ps`: `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo |
| Ciclo 1 - línea base revalidada | `dotnet build gaffer-api.sln --verbosity quiet`: 0 errores; `dotnet test gaffer-api.sln --verbosity quiet --no-build`: 159/159 |
| Ciclo 2 - Docker | `docker ps`: `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo |
| Ciclo 2 - hora UTC | `date -u` contrastado con `[DateTime]::UtcNow` de PowerShell (mismo valor; OPT-009) |
| Ciclo 3 - Docker | `docker ps`: `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo |
| Ciclo 3 - línea base | `dotnet build gaffer-api.sln --verbosity quiet`: 0 errores; `dotnet test gaffer-api.sln --no-build --verbosity quiet`: 203/203, 0 omitidos |
| Ciclo 3 - hora UTC | `date -u` = `[DateTime]::UtcNow` de PowerShell (21:14:57Z en ambos; OPT-009) |
| Ciclo 4 - Docker | `docker ps`: `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo |
| Ciclo 4 - línea base de avisos | `dotnet build gaffer-api.sln --no-incremental -v q`: 0 errores, **59** advertencias, incluida CS0169 `ExceptionHandlingMiddleware.logger` (línea 14) |
| Ciclo 4 - hora UTC | `date -u` = `(Get-Date).ToUniversalTime()` de PowerShell (21:29:13Z y 21:33:40Z en ambos; OPT-009) |

## Comandos y resultados

Filtro F1 = `dotnet test gaffer-api.sln --filter "FullyQualifiedName~PlayerFeaturesShould|FullyQualifiedName~UpdatePlayerCommandHandler|FullyQualifiedName~OpenApiContract"`.

| Ciclo | Paso | Fase | Comando / filtro | Resultado |
| --- | --- | --- | --- | --- |
| 1 | 01 | Aceptación red | `dotnet test ... --filter FullyQualifiedName~PlayerFeaturesShould` | 5 fallos / 12. AC1.1 `UpdateAPlayer`: "Expected player.BirthDate to be <1994-02-04>, but found <0001-01-01 00:00:00.000>". AC1.2: 400 "Error converting value {null} to type 'System.DateTime'" / "Cannot convert null value to Gaffer.Api.Shared.Gender". AC1.5(a): birthDate 0001-01-01; AC1.5(b)/(c): "Expected player!.SecondLastName to be <null>, but found \"Doe\"". AC1.3 y AC1.7 en verde desde el inicio (guardas de regresión). |
| 1 | 01 | Contrato red (AC1.6) | `--filter FullyQualifiedName~OpenApiContract` | 2 fallos: "Expected required {\"firstName\", \"lastName\", \"birthDate\", \"gender\"} to not contain \"birthDate\"" (idem `gender`). |
| 1 | 01 | Unitario red (AC1.4) | `dotnet build gaffer-api.sln --verbosity quiet` | Red de compilación: CS0029 en `UpdatePlayerCommandHandlerShould.cs` (DateTime/Gender → string) por el comando tipado. |
| 1 | 01 | Green | F1 | 26/26. |
| 1 | 01 | Control negativo AC1.6 | `UpdatePlayerRequest` revertido temporalmente a `DateTime`/`Gender` no anulables; `--filter ~DeclareUpdatePlayerRequestFieldAsOptionalAndNullable` | 2/2 fallan ("RequiredProperties {\"birthDate\", \"gender\"} to not contain ..."); restaurado, F1 26/26. |
| 1 | 01 | Refactor + suite completa | `dotnet build gaffer-api.sln --verbosity quiet`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 0 errores; 171/171 (159 + 12 nuevos). |
| 1 | 01 | Revisión (REVIEWER) | `docker ps`; `dotnet build gaffer-api.sln --verbosity quiet`; F1 `--no-build`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Docker activo; 0 errores; F1 26/26; suite 171/171, 0 omitidos. Veredicto APPROVED (ver `REVIEW.md`). |
| 2 | 02 | Aceptación red (AC2.1) | `dotnet test ... --logger "console;verbosity=normal" --filter "FullyQualifiedName~DatabaseFailure\|FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~FacilityFeaturesShould"` | 4 fallos AC2.1 con **400**: "Expected response.StatusCode to be InternalServerError {value: 500} because {\"Title\":\"Cannot delete player\",\"Status\":400,...}" (idem `Cannot update player`, `Cannot delete facility`, `Cannot update facility`). AC2.4 `ReturnNotFoundWhenDeletingAnUnknownPlayer` verde desde el inicio (caracterización). GET con BD inalcanzable ya daba 500 (referencia). |
| 2 | 02 | Unitario red (AC2.2, AC2.6, AC2.7) | `--filter "FullyQualifiedName~CommandHandlerShould\|FullyQualifiedName~ExceptionToHttpResponseConverters"` | 17 fallos: 8 `PropagateTheRepositoryFailureWhile*` ("Expected type to be System.InvalidOperationException, but found ...CannotDeleteFacilityException"); `AssignTheInternalServerErrorReason...` ("Expected string to be \"Internal Server Error\", but found <null>"); 4 `ConvertEachExceptionIntoItsExpectedResponse` (p. ej. "...to be \"Error.Players.Delete.Internal Server Error\", but found \"Error.Players.Delete.\""); 4 `NeverProduceATranslationKeyEndingWithADot` ("...but found \"Error.Facilities.Update.\""). |
| 2 | 02 | Green | F2 = `dotnet test gaffer-api.sln --no-build --verbosity quiet --filter "FullyQualifiedName~DatabaseFailure\|FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~FacilityFeaturesShould\|FullyQualifiedName~CommandHandlerShould\|FullyQualifiedName~ExceptionToHttpResponseConverters"` | 88/88 (exit 0). |
| 2 | 02 | Tipo de excepción MySQL | `dotnet fsi` (script en scratchpad, fuera del repo) abriendo `MySqlConnection` a `127.0.0.1:1` con el `MySql.Data.dll` del build | `MySql.Data.MySqlClient.MySqlException` (inner `MySqlException`). |
| 2 | 02 | Greps del plan | `grep -rn "Reason" Gaffer.Api`; `grep -rn "new Cannot\(Delete\|Update\)\(Player\|Facility\)Exception" Gaffer.Api` | 4 `Reason { get; } = InternalServerErrorReason` (constante `"Internal Server Error"` en cada clase) y 4 usos en los conversores; 0 lanzamientos de las genéricas. |
| 2 | 02 | Refactor + suite completa | `dotnet build gaffer-api.sln --verbosity quiet`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 0 errores; **203/203** (171 + 32 nuevos: 5 `DatabaseFailureShould`, 1 AC2.4, 26 de conversores), 0 omitidos, exit 0. |
| 2 | 02 | Revisión (REVIEWER) | `docker ps`; `dotnet build gaffer-api.sln --verbosity quiet`; F2 `--no-build`; `dotnet test gaffer-api.sln --no-build --verbosity quiet`; greps del plan | Docker activo; 0 errores / 0 advertencias; F2 88/88; suite 203/203, 0 omitidos; grep genéricas = 0. Veredicto APPROVED (ver `REVIEW.md`; AC2.5: la clave del plan es una errata, el 400 viene de `[MaxLength(4)]`, que ya existía; no bloqueante). |
| 3 | 03 | Aceptación red (AC3.3) | `dotnet test gaffer-api.sln --logger "console;verbosity=normal" --filter "FullyQualifiedName~ExceptionHandlingMiddleware"` | 1 fallo: `ReturnABadRequestWhenAFacilitySearchUsesAFilterThatIsNotPermitted` "Expected context.Response.StatusCode to be 400, but found 500" (`ArgumentException` del repositorio real → "Unkwnown error"). `KeepReturningAnUnknownErrorForAGenericArgumentException` verde (guarda AC3.4). |
| 3 | 03 | Unitario red compilación | `dotnet build gaffer-api.sln --verbosity quiet` | CS0246 `FacilitySearchFilterNotPermittedException` (ExceptionToHttpResponseConvertersShould.cs ×3, SqlFacilityRepositoryShould.cs ×1). |
| 3 | 03 | Unitario red (AC3.1, AC3.2) | F3 = `--filter "FullyQualifiedName~SqlFacilityRepository\|FullyQualifiedName~ExceptionToHttpResponseConverters\|FullyQualifiedName~ExceptionHandlingMiddleware"` con `--logger "console;verbosity=normal"`, solo con el tipo de excepción añadido | 7 fallos: 2 `ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted` ("Expected type to be ...FacilitySearchFilterNotPermittedException, but found System.ArgumentException"); `ConvertANotPermittedFacilitySearchFilterIntoTheSameBadRequestAsPlayers`, `ConvertEachExceptionIntoItsExpectedResponse(Facility...)`, `NeverProduceATranslationKeyEndingWithADot(Facility...)` (KeyNotFoundException); `CoverEveryConverter` ("to be a collection with 13 item(s)", había 12); el de aceptación (500). |
| 3 | 03 | Green | `dotnet test gaffer-api.sln --verbosity quiet` + F3 | 44/44. |
| 3 | 03 | Refactor + revalidación | `dotnet build gaffer-api.sln --verbosity quiet`; F3 + `FacilityFeaturesShould` + `SearchPlayersEndpoint` (`--no-build`); `dotnet test gaffer-api.sln --no-build --verbosity quiet`; `grep -n "ArgumentException" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs \| wc -l` | 0 errores; 64/64 (AC3.5: tests existentes de repositorio y `FacilityFeaturesShould` en verde); **210/210** (203 + 7 nuevos: 2 middleware, 2 repositorio, 3 conversor), 0 omitidos; grep = 0. |
| 3 | 03 | Revisión (REVIEWER) | `docker ps`; `dotnet build gaffer-api.sln --verbosity quiet`; F3 + `FacilityFeaturesShould` + `SearchPlayersEndpoint` `--no-build`; `dotnet test gaffer-api.sln --no-build --verbosity quiet`; grep `ArgumentException` | Docker activo; 0 errores / 0 advertencias; 64/64; suite 210/210, 0 omitidos; grep = 0. Veredicto APPROVED (ver `REVIEW.md`; refactor `BuildSearchFilterNotPermittedProblemDetails()` en alcance y con la misma salida). |
| 4 | 04 | Aceptación red compilación | `dotnet test ... --filter "FullyQualifiedName~DatabaseFailure"` | NUnit1032 (analizador como error): el campo `_logs` debía liberarse en `[OneTimeTearDown]`; añadido `_logs?.Dispose()`. |
| 4 | 04 | Aceptación red (AC4.4) | `dotnet test gaffer-api.sln --logger "console;verbosity=normal" --filter "FullyQualifiedName~DatabaseFailure"` | 1 fallo / 6: `ReturnAnUnknownErrorAndLogItWhenGettingAPlayer` "Expected body[\"Title\"]!.Value<string>() to be the same string, but they differ at index 3" (500 con "Unkwnown error"; la aserción de log queda detrás y la cubre el red unitario). Los 5 de 500 del paso 02 en verde. |
| 4 | 04 | Unitario red compilación | `dotnet build Gaffer.Api.Tests/Gaffer.Api.Tests.csproj -v q` | CS1729 ×3: `ExceptionHandlingMiddleware` no tiene constructor de 2 argumentos. Se cambió solo el constructor primario (`next`, `logger`) para obtener el red de comportamiento. |
| 4 | 04 | Unitario red (AC4.1-AC4.3) | `dotnet test gaffer-api.sln --logger "console;verbosity=normal" --filter "FullyQualifiedName~ExceptionHandlingMiddleware"` | 3 fallos / 6: `LogAnExceptionMappedToBadRequestOnceAsAnError` y `LogAnUnmappedExceptionOnceAsAnErrorWithTheException` ("Expected entries to contain a single item, but the collection is empty"); `ReturnAnUnknownErrorForAnUnmappedException` (título, "differ at index 3"). `NotLogAnExceptionMappedToNotFound` verde desde el inicio (guarda AC4.3 b). |
| 4 | 04 | Green | F4 = `dotnet test gaffer-api.sln --filter "FullyQualifiedName~ExceptionHandlingMiddleware\|FullyQualifiedName~DatabaseFailure"` | 12/12, exit 0. |
| 4 | 04 | Refactor + revalidación | `dotnet build gaffer-api.sln --no-incremental -v q`; `grep -rn "Unkwnown" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l`; `dotnet test gaffer-api.sln` | 0 errores, **57** advertencias (59 → 57; sin CS0169 de `ExceptionHandlingMiddleware`; ninguna nueva en ficheros tocados salvo la CS8618 preexistente de `TestFixture.cs`, desplazada a la línea 35); grep = 0; **215/215** (210 + 5 nuevos: 4 unitarios del middleware, 1 de aceptación), 0 omitidos, exit 0. |
| 4 | 04 | Revisión (REVIEWER) | `docker ps`; `dotnet build gaffer-api.sln --no-incremental -v q`; F4 `--no-build`; `dotnet test gaffer-api.sln --no-build --verbosity quiet`; grep `Unkwnown` | Docker activo; 0 errores, 57 advertencias (sin CS0169); F4 12/12; suite **215/215**, 0 omitidos, exit 0; grep = 0. Veredicto APPROVED (ver `REVIEW.md`; `RecordingLoggerProvider.cs` y el parámetro opcional de `StartApiServer` dentro del alcance). |

## Notas del ciclo 1 (paso 01)

| Tema | Detalle |
| --- | --- |
| Contrato | `UpdatePlayerRequest`: propiedades `string?` ×3, `DateTime?`, `Gender?`. `UpdatePlayerCommand`: `DateTime?`/`Gender?` (sin conversiones string). Controller sin `ToString()` de `BirthDate`/`Gender`; eliminado `using System.Globalization`. |
| Handler | Solo aplica valores presentes; `SecondLastName` siempre se asigna (`null` si ausente/`null`/`""`, Q1). |
| `Player.cs` | `SetSecondLastName(string?)`; sobrecargas tipadas `SetBirthDateName(DateTime)` y `SetGender(Gender)`. Sin renombres (paso 04 de TECHDEBT-003). Setters string antiguos se conservan (usados por `PlayerShould`). |
| AC1.6 | El documento servido es Swagger 2.0 (`definitions`), sin `x-nullable`. El test usa la semántica de NSwag/NJsonSchema: `OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)` (en Swagger 2.0, no requerido ⇒ anulable) y `RequiredProperties` sin el campo. |
| Tests ajustados | `UpdatePlayerCommandHandlerShould.ThrowExceptionWhenPlayerNotFound` ahora hace `await` y espera `CannotUpdatePlayerNotFoundException` (antes no esperaba la aserción). Ningún test existente dependía de conservar `secondLastName`. |
| Fuera de alcance | Cliente NSwag no regenerado (operador). Los avisos de compilación (26) son preexistentes en otros ficheros (CS8618/CS8604/CS8602/CS8625). |

## Notas del ciclo 2 (paso 02)

| Tema | Detalle |
| --- | --- |
| Handlers | Delete/Update de Player y Facility: `catch (XNotFoundException ex)` específico → `CannotDelete*/CannotUpdate*NotFoundException`; el resto se propaga sin envolver (Q3 = a). En `UpdateFacilityCommandHandler` se eliminó el `try/catch (Exception)` alrededor de `Update`. El `switch` redundante desaparece en el mismo cambio (fase de refactor = revalidación de suite). |
| Excepciones genéricas (Q4/Q4.1) | Las 4 se conservan; cada una tiene `public const string InternalServerErrorReason = "Internal Server Error"` y `Reason { get; } = InternalServerErrorReason`. `ExceptionToHttpResponseConverters.cs` sin cambios (400, `Title`/`Detail` iguales; la clave ya interpola `Reason`). |
| Host de pruebas | `TestFixture.InitializeApiServer` → `internal static StartApiServer(baseAddress, connectionString)`; las registraciones DI siguen solo en `TestFixture.cs` (sin handlers nuevos). `SqlConnectionProviderTest` se crea ahora en `OneTimeSetup` y se libera en `OneTimeTearDown` (el analizador NUnit1032 lo exigió al moverlo). `DatabaseFailureShould` levanta su propio `TestServer` con `TestConfiguration.UnreachableDatabaseConnectionString`. No se usa `StartupTestServer`: `DatabaseStartup` cae a `DummyConnectionProvider` si la BD no responde al arrancar. |
| AC2.3 | NotFound de Player/Facility sigue 404 (tests de aceptación existentes en verde). Las claves de Facility (`Error.Facilities.Delete/Update.FacilityNotFound`) se verifican en los tests de conversores; los tests de aceptación de Facility existentes solo assertan el estado. |
| AC2.5 (discrepancia para REVIEWER) | `PUT /facilities/{id}` con 5 gradas devuelve **400**, pero por validación de modelo (`[MaxLength(4)]` en `FacilitiesController`, `ValidationProblemDetails` "One or more validation errors occurred"), **no** con `Error.Facilities.Create.Validation`. Comprobado al endurecer la aserción (fallo: body `{"errors":{"Stands":[...maximum length of '4'.]}...}`). No se cambia comportamiento de producto: `RejectUpdatingAFacilityWithMoreThanFourStands` pasa de `BeOneOf(400, 422)` a exactamente 400. La clave `Error.Facilities.Create.Validation` sí se cubre en los tests de conversores y en `PropagateValidationExceptionWhenStandsExceedFour` (unitario). |
| Fuera de alcance | Cliente NSwag no regenerado. `ExceptionHandlingMiddleware` sin tocar (título "Unkwnown error" → paso 04). |

## Notas del ciclo 3 (paso 03)

| Tema | Detalle |
| --- | --- |
| Excepción | `FacilitySearchFilterNotPermittedException(string fieldName) : Exception("FilterNotPermitted")` con `FieldName`, en `IFacilityRepository.cs` (análoga a la de Player). `SqlFacilityRepository.BuildFilters` la lanza en lugar de `ArgumentException` (grep `ArgumentException` en el repositorio = 0). |
| Conversor (Q5 = a) | 13.ª entrada del diccionario: 400, `Title`/clave `Error.Search.FilterNotPermitted`, `Detail` `FilterNotPermitted`. Refactor: Player y Facility comparten `BuildSearchFilterNotPermittedProblemDetails()` + constante `SearchFilterNotPermittedKey`; salida idéntica (tests de conversor y `SearchPlayersEndpointShould` en verde). |
| AC3.3 (aceptación) | `ExceptionHandlingMiddlewareShould.ReturnABadRequestWhenAFacilitySearchUsesAFilterThatIsNotPermitted`: `DefaultHttpContext` (ruta `/facilities`) + `RequestDelegate` que ejecuta el `SqlFacilityRepository` **real** (proveedor de conexión sustituido) con filtro `Street`. Más fuerte que lanzar la excepción a mano: cierra repositorio → middleware. Assert: 400, `application/json`, `Status`/`Title`/`Detail`/`Instance`/`Extensions.ErrorTranslationKey`. |
| AC3.1 | `SqlFacilityRepositoryShould.WhenGettingFacilities.ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted` (`Street` y un intento de inyección en el nombre de campo): `ThrowExactlyAsync<FacilitySearchFilterNotPermittedException>` con `FieldName`, y `connectionProvider.ReceivedCalls()` vacío (no se ejecuta SQL; `FacilityRow` es privado, por eso no se usa `DidNotReceive().QueryAsync<T>`). Corre dentro del fixture MySQL (Docker). |
| AC3.4 | `NotMapGenericArgumentExceptionsSoFacilitySearchKeepsItsBehaviour` renombrado a `NotMapGenericArgumentExceptions` (misma aserción). Además, `ExceptionHandlingMiddlewareShould.KeepReturningAnUnknownErrorForAGenericArgumentException` → 500. |
| Rojo de compilación | Antes de crear la excepción: CS0246 `FacilitySearchFilterNotPermittedException` no encontrada (4 usos en tests). Se añadió solo el tipo para obtener el rojo de comportamiento registrado abajo. |
| Fuera de alcance | Sin rutas HTTP nuevas; sin handlers nuevos (`TestFixture.cs` no requiere cambios). Cliente NSwag no regenerado. Avisos de compilación en `SqlFacilityRepository.cs` preexistentes (CS8618 en clases *Row, CS8603 línea 71). |

## Notas del ciclo 4 (paso 04)

| Tema | Detalle |
| --- | --- |
| Middleware | Constructor primario `(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)`; eliminados el campo sin asignar y el comentario. `LogException`: 404 → sin log; resto (400 mapeado, 500 no mapeado o conversor que devuelve `null`) → `LogError(exception, "Request failed with status {Status}: {Title}", ...)`. Título `"Unknown error"`. |
| Refactor | `HandleExceptionAsync` usa `PlayerConverters.TryGetValue` directamente (se elimina `GetConverterDictionary`, que copiaba el diccionario en cada excepción); `UnknownError` pasa a `static`. Respuestas sin cambios (suite en verde). |
| Logger de test | Nuevo `Gaffer.Api.Tests/TestShared/RecordingLoggerProvider.cs` (`ILoggerProvider` en memoria, sin paquetes nuevos), compartido por el unitario y la aceptación. **Fichero no listado en el alcance del paso** (el paso pide "un logger de test propio"); se ubicó en `TestShared` para no duplicarlo en los dos tests. Punto a validar por REVIEWER. |
| Host de test | `TestFixture.StartApiServer` acepta `ILoggerProvider? loggerProvider = null` y lo añade con `ConfigureLogging(AddProvider)`. Sin handlers ni registros DI nuevos; `ILogger<T>` lo aporta el `HostBuilder`. El fixture compartido no pasa proveedor (sin cambio de comportamiento). |
| AC4.4 | `DatabaseFailureShould.ReturnAnUnknownErrorAndLogItWhenGettingAPlayer`: 500, `Title = "Unknown error"`, exactamente una entrada nueva `Error` de la categoría `ExceptionHandlingMiddleware` con excepción adjunta (se cuentan las entradas previas porque el servidor es compartido en la clase). |
| AC4.6 | Ningún test existente dependía de la errata (grep `Unkwnown` = 0 en código y tests). Cambio de contrato menor visible en el cuerpo de los 500. |
| Fuera de alcance | Cliente NSwag no regenerado. Los `using` sin uso preexistentes del middleware (`Gaffer.Api.Commands`, `Gaffer.Api.Shared.Repositories`) no se tocaron. |

## Documentación modificada

DOCUMENTER, 2026-09-26. Basado solo en código aprobado, `STATUS.md` y `REVIEW.md` (ciclos 1-4, todos `APPROVED`).

| Tipo | Fichero | Motivo |
| --- | --- | --- |
| Creado | `doc/features/player-update.md` | Funcional: update parcial de Player (paso 01) — `secondLastName` siempre se reemplaza, `birthDate`/`gender` opcionales y anulables, 404 en id desconocido. |
| Creado | `doc/features/technical/player-update.md` | Técnico: `UpdatePlayerCommandHandler`, contrato `UpdatePlayerRequest`/`UpdatePlayerCommand`, cómo probarlo. |
| Creado | `doc/features/error-handling.md` | Funcional: fallos de infraestructura en Delete/Update → 500 (pasos 02/04), filtro no permitido en Facilities → 400 (paso 03), logging del middleware (paso 04). |
| Creado | `doc/features/technical/error-handling.md` | Técnico: `ExceptionHandlingMiddleware` (constructor con `ILogger`, `LogException`), catálogo de excepciones (`Cannot*Exception` genéricas conservadas pero no lanzadas), `FacilitySearchFilterNotPermittedException`, `RecordingLoggerProvider`, `TestFixture.StartApiServer(..., ILoggerProvider?)`. |
| Actualizado (directo) | `doc/features/technical/player-search-and-persistence.md` | Nota sobre el helper compartido `BuildSearchFilterNotPermittedProblemDetails()` (ahora también usado por Facility) y referencias cruzadas a los documentos nuevos. |
| Actualizado (indirecto) | `doc/features/player-search.md` | Referencia cruzada: Facility ahora replica el mismo rechazo de filtro y el logging del middleware. |
| Actualizado (indirecto) | `doc/features/database-connection-reliability.md` | Referencia cruzada: fallo de BD a mitad de request (Delete/Update) además del fallback de arranque ya documentado. |
| Actualizado (indirecto) | `doc/features/technical/database-connection-lifecycle.md` | Referencia cruzada al mismo comportamiento. |
| Actualizado (indirecto) | `doc/features/technical/api-client-versioning.md` | Nota sobre el cambio de contrato de `UpdatePlayerRequest` (relajación de esquema vs. cambio de comportamiento en `secondLastName`) a valorar en el próximo bump. |

Sin cambios en `.github/doc/common/*-stack*.md`: ningún paquete ni herramienta de test nuevos (confirmado en `STATUS.md`/`REVIEW.md`: `RecordingLoggerProvider.cs` no añade paquetes). `Gaffer.Api.Client` no regenerado (fuera de alcance de DOCUMENTER).
