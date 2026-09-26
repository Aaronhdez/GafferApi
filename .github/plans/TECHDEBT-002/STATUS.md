# STATUS - TECHDEBT-002

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 3 |
| Pasos completados | 01 (aprobado por REVIEWER en el ciclo 1); 02 (aprobado por REVIEWER en el ciclo 2); 03 (aprobado por REVIEWER en el ciclo 3) |
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
| 4 | 04 - Log en el middleware | - |
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

## Documentación modificada

Pendiente (DOCUMENTER).
