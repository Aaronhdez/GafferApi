# REVIEW - TECHDEBT-002

## Ciclo 1 - Paso 01 (Update parcial de Player)

Veredicto: **APPROVED** (estado del plan → `APPROVED` para el paso 01).

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Modificados: `PlayerController.cs`, `UpdatePlayerCommandHandler.cs`, `Player.cs`, `PlayerFeaturesShould.cs`, `UpdatePlayerCommandHandlerShould.cs`, `OpenApiContractShould.cs`, ficheros del plan; nuevo `usage/BUILDER.md`. Todo dentro del alcance del paso 01. Sin cambios en `Gaffer.Api.Client` (no verificado su output generado, fuera de alcance). |
| DI / `TestFixture.cs` | Sin handlers nuevos; `UpdatePlayerCommandHandler` ya registrado (`TestFixture.cs:118`, `ActionsStartup.cs:21`). No requiere cambios. |
| MySQL | Acceptance usa `SqlPlayerRepository` real (`TestFixture.cs:110`) contra Docker `Gaffer.Database` (3306), activo. Ningún test omitido. |

### Ejecución del REVIEWER

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Gaffer.Database 0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --verbosity quiet` | 0 errores |
| F1: `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~UpdatePlayerCommandHandler\|FullyQualifiedName~OpenApiContract"` | 26/26 superados, 0 omitidos |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 171/171 superados, 0 omitidos (línea base 159 + 12 nuevos) |

### Criterios de aceptación

| ID | Evidencia | Red → green | Estado |
| --- | --- | --- | --- |
| AC1.1 | `PlayerFeaturesShould.UpdateAPlayer`: PUT crudo `{"firstName":"X"}` vía `SendAsync`; GET verifica `lastName`, `birthDate`, `gender` originales (FEMALE, 1994-02-04) y `secondLastName = null`. | Red en STATUS ("Expected player.BirthDate to be <1994-02-04>, but found <0001-01-01>") → green | Demostrado |
| AC1.2 | `KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls`. | Red 400 "Error converting value {null} to type 'System.DateTime'" / Gender → green | Demostrado |
| AC1.3 | `UpdateEveryFieldWhenAllFieldsAreSent` (incluye `secondLastName` distinto). | Guarda de regresión (verde desde el inicio, registrado) | Demostrado |
| AC1.4 | `UpdateAPlayerWhileBeingExecuted`, `KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent` (a), `ReplaceTheSecondLastNameAndKeepTheRest` ("Nuevo"/""/null) (b)(c). | Red de compilación CS0029 (comando tipado) → green | Demostrado |
| AC1.5 | `ReplaceTheSecondLastNameOnUpdate` ×3 (crudo, "Nuevo"/null/""), resto de campos sin cambio. | Red (b)(c) "SecondLastName to be <null>, but found \"Doe\"" → green | Demostrado |
| AC1.6 | `OpenApiContractShould.DeclareUpdatePlayerRequestFieldAsOptionalAndNullable` (birthDate, gender) sobre `/swagger/v1/swagger.json`: no en `RequiredProperties` y `IsNullable(SchemaType)` = true. | Red 2 fallos + control negativo (revertir tipos → 2/2 fallan) → green | Demostrado |
| AC1.7 | `ReturnNotFoundWhenUpdatingAnUnknownPlayer`: 404 + `Error.Players.Update.PlayerNotFound`; suite 171/171. | Guarda de regresión | Demostrado |

Diff de `PlayerController.cs`: sin `ToString()` de `BirthDate`/`Gender`; `using System.Globalization` eliminado. Cumple "Cómo demostrarlo".

### Hallazgos

| # | Severidad | Hallazgo |
| --- | --- | --- |
| 1 | No bloqueante | AC1.6: el documento es Swagger 2.0 sin `x-nullable`; la nulabilidad se demuestra por la regla implícita de NJsonSchema (no requerido ⇒ anulable en Swagger 2.0). Coherente con cómo NSwag generará el cliente; si en el futuro se migra a OpenAPI 3, el test debería revisar `nullable`. |
| 2 | No bloqueante | `Player.SetSecondLastName(string? commandFirstName)` mantiene un nombre de parámetro engañoso y se conservan los setters string (`SetBirthDateName(string)`, `SetGender(string)`) solo usados por `PlayerShould`. Renombres/limpieza pertenecen a TECHDEBT-003 paso 04; correcto no adelantarlos. |
| 3 | No bloqueante (preexistente) | `PlayerFeaturesShould.ThrowExceptionIfPlayerIsNotFound` no hace `await` de la aserción `ThrowAsync` (patrón preexistente). La cobertura real del 404 la aporta ahora `ReturnNotFoundWhenUpdatingAnUnknownPlayer`. |
| 4 | Informativo | Cambio de contrato visible para clientes: un PUT que omita `secondLastName` lo borra (decisión Q1 del operador). El cliente NSwag lo regenera el operador. |

Sin defectos bloqueantes.

## Ciclo 2 - Paso 02 (Fallos de infraestructura en Delete/Update → 5xx)

Veredicto: **APPROVED** (estado del plan → `APPROVED` para el paso 02).

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Producto: 4 handlers (`Delete/UpdatePlayer`, `Delete/UpdateFacility`). Tests: 4 `*CommandHandlerShould`, `ExceptionToHttpResponseConvertersShould`, `PlayerFeaturesShould`, `FacilityFeaturesShould`, `TestFixture.cs`, nuevo `Acceptance/DatabaseFailureShould.cs`. Plan: `STATUS.md`, `usage/BUILDER.md`, `OPTIMIZATION.md`, y `.github/agent-harness/MEMORIES.md` (cambios del OPTIMIZER del ciclo 1, fuera de la revisión de producto). Sin cambios en `Controllers/` ni `ExceptionToHttpResponseConverters.cs`. `Gaffer.Api.Client` no tocado ni verificado (fuera de alcance). |
| Handlers | Solo `catch (PlayerNotFoundException/FacilityNotFoundException)` → `CannotDelete*/CannotUpdate*NotFoundException`; el resto se propaga (Q3 = a). `UpdateFacility`: eliminado el `try/catch` de `Update`; `BuildUpdatedFacility` ya estaba fuera del `try` (sin cambio para `FacilityValidationException`). `UpdatePlayer`: `Player` no lanza en sus setters, así que ningún error de negocio pasa de 400 a 500. |
| `TestFixture.cs` (reestructuración) | `InitializeApiServer` → `internal static StartApiServer(...)`, sin cambio de DI. La creación de `SqlConnectionProviderTest` pasa a `OneTimeSetup`. **Necesario y correcto**: si se hubiera dejado dentro de `StartApiServer`, `DatabaseFailureShould` habría reemplazado el proveedor estático de limpieza por el inalcanzable, y `ClearDatabase` habría fallado en `OneTimeTearDown`. Ahora también se libera en `OneTimeTearDown`. No hay handlers nuevos, así que no hacen falta registros DI nuevos. |
| `DatabaseFailureShould` | `TestServer` propio con la misma DI que `TestFixture` y `TestConfiguration.UnreachableDatabaseConnectionString`. Se ejecuta tras el `[SetUpFixture]` (usa `TestFixture.Configuration`). El red de BUILDER (400 "Cannot delete player", etc.) demuestra que la petición llega al handler con el repositorio real fallando; no llega a `DummyConnectionProvider`. |
| MySQL | Docker `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo; ningún test omitido. |

### Ejecución del REVIEWER

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Gaffer.Database` en 3306 |
| `dotnet build gaffer-api.sln --verbosity quiet` | 0 errores, 0 advertencias |
| F2 (filtro del paso 02, `--no-build`) | 88/88, 0 omitidos, exit 0 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 203/203, 0 omitidos, exit 0 |
| `grep -rnE "new Cannot(Delete\|Update)(Player\|Facility)Exception" Gaffer.Api \| wc -l` | 0 |
| `grep -rn "Reason" Gaffer.Api` | 4 `Reason { get; } = InternalServerErrorReason` (const `"Internal Server Error"`) + 4 usos en los conversores |
| `git show HEAD:Gaffer.Api/Controllers/FacilitiesController.cs \| grep MaxLength` | `[MaxLength(4)]` ya existía en las líneas 101 y 132 antes de este ciclo (AC2.5) |

### Criterios de aceptación

| ID | Evidencia | Red → green | Estado |
| --- | --- | --- | --- |
| AC2.1 | `DatabaseFailureShould`: DELETE/PUT de player y facility → 500 (más GET como referencia). Solo se comprueba el estado. | Red de BUILDER: 4 × **400** ("Cannot delete/update player/facility") → green | Demostrado |
| AC2.2 | 8 tests `PropagateTheRepositoryFailureWhile*` (Get y Delete/Update ×4 handlers): `ThrowExactlyAsync<InvalidOperationException>().Which.BeSameAs(failure)`. Tipo MySQL registrado en STATUS: `MySql.Data.MySqlClient.MySqlException`. | Red: 8 fallos "found ...Cannot*Exception" → green | Demostrado |
| AC2.3 | Player: `ReturnNotFoundWhenUpdatingAnUnknownPlayer` y `ReturnNotFoundWhenDeletingAnUnknownPlayer` (404 + clave). Facility: los tests de aceptación de 404 existentes (estado) + unitarios de handler (`Cannot*FacilityNotFoundException`) + conversor (`Error.Facilities.Delete/Update.FacilityNotFound`). | Guarda de regresión | Demostrado (cadena de Facility demostrada por partes; ver hallazgo 2) |
| AC2.4 | `PlayerFeaturesShould.ReturnNotFoundWhenDeletingAnUnknownPlayer`: 404 + `Error.Players.Delete.PlayerNotFound`. | Caracterización: verde desde el inicio, registrado | Demostrado |
| AC2.5 | `RejectUpdatingAFacilityWithMoreThanFourStands` ahora exige exactamente 400. | Guarda de regresión | Demostrado para lo que el paso puede cumplir (400 sin regresión). Ver hallazgo 1 |
| AC2.6 | `AssignTheInternalServerErrorReason...` (4 × `Reason = "Internal Server Error"` con el constructor `(message, id)`); `ConvertEachExceptionIntoItsExpectedResponse` con los valores literales de la tabla (400, `Title`, `Detail` con `id`, `Error.<X>.<Op>.Internal Server Error`); `NeverProduceATranslationKeyEndingWithADot` (12 casos). | Red: `Reason` <null>; claves "Error.Players.Delete." etc. → green | Demostrado |
| AC2.7 | `CoverEveryConverter` (`PlayerConverters.Keys` equivalente a las 12 expectativas; si se añade un conversor sin test, falla) + 12 casos de `ConvertEachExceptionIntoItsExpectedResponse` (estado, `Title`, `Detail`, clave). | Red en las 4 genéricas → green | Demostrado |
| AC2.8 | Suite completa 203/203, 0 omitidos (ejecutada por REVIEWER). | - | Demostrado |

### Hallazgos

| # | Severidad | Hallazgo |
| --- | --- | --- |
| 1 | No bloqueante (errata del plan; el operador debe saberlo) | AC2.5 dice "sigue devolviendo 400 `Error.Facilities.Create.Validation`", pero antes de este ciclo tampoco era así: `[MaxLength(4)]` en `UpdateFacilityRequest` (existía en `HEAD`) rechaza la petición en la validación de modelo (`ValidationProblemDetails`) antes de llegar al handler. La clave del plan se basaba en un supuesto erróneo. Lo que exige el AC ("sigue") se cumple: el estado 400 no cambia y ahora se comprueba de forma más estricta. Para producir esa clave habría que cambiar el comportamiento del producto, lo que queda fuera del alcance del paso. La clave `Error.Facilities.Create.Validation` sí está cubierta por el conversor y por el unitario `PropagateValidationExceptionWhenStandsExceedFour`. Si el operador quiere esa clave en el PUT, hace falta un paso nuevo. |
| 2 | No bloqueante | AC2.3 en Facility: ninguna prueba de aceptación comprueba de extremo a extremo la clave de 404 de DELETE/PUT de Facility; la cadena está cubierta por partes (handler → excepción → conversor). Es suficiente porque este ciclo no ha tocado ni el conversor ni la ruta NotFound. |
| 3 | Informativo | Las 4 excepciones genéricas y sus conversores (400) ya no las lanza ningún código de producción (grep = 0). Se mantienen por decisión Q4 = b). Candidatas a limpieza en TECHDEBT-003. |
| 4 | Informativo | El 500 lo produce hoy el middleware con el título "Unkwnown error" y sin log (paso 04). |
| 5 | Limitación de la revisión | REVIEWER no reprodujo el red, porque habría tenido que revertir código de producto. Acepta la evidencia de BUILDER, que incluye los mensajes exactos. |

Sin defectos bloqueantes. El paso 01 no presenta regresión (`PlayerFeaturesShould` y `UpdatePlayerCommandHandlerShould` en verde dentro de F2 y de la suite).

## Ciclo 3 - Paso 03 (Filtro no permitido en Facilities → 400)

Veredicto: **APPROVED** (estado del plan → `APPROVED` para el paso 03).

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Producto: `IFacilityRepository.cs` (nueva `FacilitySearchFilterNotPermittedException`), `SqlFacilityRepository.cs` (1 línea), `ExceptionToHttpResponseConverters.cs`. Tests: `SqlFacilityRepositoryShould.cs`, `ExceptionToHttpResponseConvertersShould.cs`, nuevo `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs`. Plan: `STATUS.md`, `usage/BUILDER.md`; `OPTIMIZATION.md` y `MEMORIES.md` son del OPTIMIZER (fuera de la revisión de producto). Todo dentro del alcance de ficheros del paso. `Gaffer.Api.Client` no tocado ni verificado. |
| Excepción / repositorio | Análoga a `PlayerSearchFilterNotPermittedException` (mensaje `"FilterNotPermitted"`, `FieldName`). `BuildFilters` la lanza antes de cualquier acceso a `dbConnectionProvider`. `grep -rn ArgumentException Gaffer.Api` = 0, así que ningún llamador dependía de capturar `ArgumentException`. |
| Refactor `BuildSearchFilterNotPermittedProblemDetails()` | Dentro del alcance (fase 3 del paso: "valorar compartir la construcción del ProblemDetails ... sin cambiar la salida"). Conserva el comportamiento: delega en `BuildBadRequestProblemDetailsWithExtension` con los mismos 3 literales que antes (la constante `SearchFilterNotPermittedKey` = `"Error.Search.FilterNotPermitted"`). Crea un `ProblemDetails` nuevo en cada llamada, así que no se comparte estado mutable (el middleware muta `Instance`). Cubierto por `ConvertEachExceptionIntoItsExpectedResponse(PlayerSearchFilterNotPermitted...)` y `SearchPlayersEndpointShould`, ambos en verde. |
| Middleware | Sin cambios; resuelve por tipo exacto en `PlayerConverters`, así que la entrada 13 basta. |
| DI / `TestFixture.cs` | No hay handlers nuevos, así que no hacen falta registros. |
| MySQL | Docker `Gaffer.Database 0.0.0.0:3306->3306/tcp` activo; 0 omitidos. |

### Ejecución del REVIEWER

| Comando | Resultado |
| --- | --- |
| `dotnet build gaffer-api.sln --verbosity quiet` | 0 errores, 0 advertencias |
| F3 + `FacilityFeaturesShould` + `SearchPlayersEndpoint` (`--no-build`) | 64/64, 0 omitidos |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` | 210/210, 0 omitidos |
| `grep -n "ArgumentException" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | 0 |

### Criterios de aceptación

| ID | Evidencia | Red → green | Estado |
| --- | --- | --- | --- |
| AC3.1 | `SqlFacilityRepositoryShould.WhenGettingFacilities.ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted` (`Street` y un nombre de campo con inyección): `ThrowExactlyAsync<FacilitySearchFilterNotPermittedException>` con `FieldName`; `ReceivedCalls()` vacío (sin SQL). | Red de compilación CS0246 → red de comportamiento "found System.ArgumentException" → green | Demostrado |
| AC3.2 | `ConvertANotPermittedFacilitySearchFilterIntoTheSameBadRequestAsPlayers` + caso en `ConvertEachExceptionIntoItsExpectedResponse`/`NeverProduceATranslationKeyEndingWithADot`; `CoverEveryConverter` ahora con 13. | Red: KeyNotFoundException; "13 item(s)" → green | Demostrado |
| AC3.3 | `ExceptionHandlingMiddlewareShould.ReturnABadRequestWhenAFacilitySearchUsesAFilterThatIsNotPermitted`: el `RequestDelegate` ejecuta el `SqlFacilityRepository` real; aserciones 400, `application/json`, `Status`/`Title`/`Detail`/`Instance` = `/facilities`/`ErrorTranslationKey`. | Red: "Expected ... 400, but found 500" (ArgumentException → "Unkwnown error") → green | Demostrado |
| AC3.4 | `NotMapGenericArgumentExceptions` (renombrado, misma aserción) + `KeepReturningAnUnknownErrorForAGenericArgumentException` → 500. | Guarda de regresión (verde desde el inicio, registrado) | Demostrado |
| AC3.5 | Tests existentes de `SqlFacilityRepositoryShould` (filtros permitidos, contra MySQL) y `FacilityFeaturesShould` en verde (64/64); suite 210/210. | Guarda de regresión | Demostrado |

### Hallazgos

| # | Severidad | Hallazgo |
| --- | --- | --- |
| 1 | Informativo | El diccionario se sigue llamando `PlayerConverters` aunque ya contiene conversores de Facility. El nombre ya era impreciso antes de este ciclo y renombrarlo queda fuera del alcance. Candidato para TECHDEBT-003. |
| 2 | Informativo | AC3.3 cierra la cadena repositorio → middleware sin ruta HTTP, como dice el plan. No existe ninguna ruta pública que pueda producir este 400 hoy. |
| 3 | Limitación de la revisión | REVIEWER no reprodujo el red, porque habría tenido que revertir código de producto. Acepta la evidencia de BUILDER, que incluye los mensajes exactos. |

Sin defectos bloqueantes. Los pasos 01 y 02 no presentan regresión: `PlayerFeaturesShould`, `DatabaseFailureShould`, los handlers y los conversores están en verde dentro de la suite de 210.
