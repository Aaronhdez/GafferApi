# STATUS archive - TECHDEBT-005

Detalle verbatim de ciclos APPROVED colapsados en el índice de `STATUS.md` (OPT-026, OPT-029).

## Ciclo 1 - comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 1 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 1 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 322/322 superados, 0 omitidos |
| 1 | Línea base AC1.5: `dotnet run --project Gaffer.Api --no-build --no-launch-profile` (`DatabaseConfig__Enabled=false`, `ASPNETCORE_URLS=http://localhost:5099`) + `curl /swagger/v1/swagger.json` | 21622 bytes, sha256 `54c730af...22d26` |
| 1 | Rojo AC1.1: `dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~FacilityRepositoryContractShould"` | exit 1; 2/2 con error: `Expected ... to be equal to {"Save"}, but {"Save", "Update"} contains 1 item(s) too many` (interfaz y clase) |
| 1 | Verde AC1.1/AC1.2: `dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~FacilityRepositoryContractShould\|FullyQualifiedName~SqlFacilityRepositoryShould"` | exit 0; 23/23 |
| 1 | Refactor, AC1.6: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 1 | Refactor, AC1.6: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 324/324 superados (322 + 2 nuevos), 0 omitidos |
| 1 | AC1.4/AC1.5: `dotnet test ... --filter "MakeAConcurrent\|UpdateFacilityCommandHandlerShould\|FacilityFeaturesShould\|FacilityStandsFeaturesShould\|OpenApiContractShould\|FacilitiesControllerContractShould"` (FullyQualifiedName~) | exit 0; 63/63 |
| 1 | AC1.3: `grep -n "Update(" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | L122 (cref del `<summary>`), L208 `public async Task Update(Guid id, Func<Facility, Facility> buildUpdated, ...)`; la escritura sin lock se llama `WriteFacility` (L125 `private`), único llamador L216 dentro de la transacción con `LockFacility` |
| 1 | AC1.3: `grep -rn "\.Update(" Gaffer.Api --include=*.cs` | `UpdateFacilityCommandHandler.cs:36` (sobrecarga con lock) y `UpdatePlayerCommandHandler.cs:15` (`IPlayerRepository`, fuera de alcance) |
| 1 | AC1.6: `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` | 0 |
| 1 | AC1.5 después: mismo `dotnet run` + `curl`, `cmp swagger-before.json swagger-after.json` | exit 0; idénticos (sha256 igual) |
| 1 | REVIEWER: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 1 | REVIEWER: `dotnet test gaffer-api.sln --no-build -v q` (`Quionis_DB` Up) | exit 0; 324/324, 0 omitidos |
| 1 | REVIEWER: filtro AC1.1/1.2/1.4/1.5 (8 patrones `FullyQualifiedName~`) | exit 0; 84/84 |
| 1 | REVIEWER: rojo AC1.1 en worktree `HEAD` + solo diff de tests (scratchpad, eliminado) | exit 1; 2/2 con error `{"Save", "Update"} contains 1 item(s) too many` |
| 1 | REVIEWER: greps AC1.3 y `CancellationToken.None` | Coinciden con BUILDER; 0 `CancellationToken.None` |
| 1 | REVIEWER: veredicto | APPROVED (desviación AC1.5 aceptada; ver `REVIEW.md`) |

## Ciclo 1 (BUILDER) - detalle

| Tema | Detalle |
| --- | --- |
| Paso | 01, Q1 = a) (por defecto) |
| Cambios de producto | `IFacilityRepository.cs`: eliminado `Task Update(Facility, CancellationToken)`; `<summary>` de la sobrecarga indica que es la única vía de actualización. `SqlFacilityRepository.cs`: `public Update(Facility, ...)` → `private WriteFacility(Facility, ...)` con `<summary>` (se une a la transacción ambiente del lock); L216 la llama. Cuerpo sin cambios (mantiene su `ExecuteInTransactionAsync`, que con transacción ambiente se ejecuta en línea). |
| Cambios de tests | `SqlFacilityRepositoryShould.cs`: los 3 tests (L255, L309, L431) invocan `Update(id, _ => updated, token)`; aserciones intactas; `minimumCalls: 7` sin cambios, comentario del recuento ajustado. Nueva clase `FacilityRepositoryContractShould` (mismo fichero, sin BD, `using System.Reflection`): `OnlyAcceptAWholeFacilityForCreationSoEveryUpdateGoesThroughTheLock` (interfaz) y `ExposeNoPublicFacilityWriteWithoutTheIdAndTheBuildFunction` (clase). |
| Aceptación (caracterización) | `FacilityFeaturesShould` y `FacilityStandsFeaturesShould` en verde antes (suite base 322/322) y después (63/63 filtro AC1.4). |
| Desviación | AC1.5: el `swagger.json` servido se capturó arrancando `Gaffer.Api` (mismo `Startup`) con `dotnet run` y Dummy, no con `StartupTestServer`, para no añadir un test fuera de plan. `OpenApiContractShould` y `FacilitiesControllerContractShould` (que sí usan el servidor de pruebas) en verde. |
| DI / `TestFixture` | Sin registros nuevos; no aplica. |
| Cliente generado | No tocado; sin build Release. |
| Bloqueos | Ninguno |


## Ciclo 2 - comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 2 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 2 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 2 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 324/324, 0 omitidos |
| 2 | AC2.0 + AC2.6 antes: sonda temporal `Acceptance/ZzCycle2ProbeShould.cs` (`[Explicit]`, `StartupTestServer` y host de `TestFixture`; salida a scratchpad; **eliminada** tras usarla) `dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~ZzCycle2ProbeShould"` | exit 0; tabla AC2.0 abajo; `swagger.json` vía `StartupTestServer`: 21617 bytes, sha256 `be49f0ab...a5a6bfe` |
| 2 | Rojo aceptación: `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~SqlPlayerRepositoryShould"` | exit 1; 33/76 con error: los 15 `RejectTheCreationOfAPlayerWith(*)`, 6 `RejectAPartialUpdateWith(*)`, 4 `ReturnTheValidationFormatForBodiesThatCannotBeBound`, 2 `ReportEveryInvalidField*`, `KeepThePlayerIdenticalWhenUpdatedWithAnEmptyBody`, 3 `ReplaceTheSecondLastNameOnlyWhenAValueIsSent` (null, ausente, `{}`), `UpdateAPlayer`, `KeepReadingAndPartiallyUpdatingAStoredPlayerThatBreaksTheRules` (solo por Q17: el PUT borraba `SecondLastName`; GET/search en verde) |
| 2 | Rojo unitario: `dotnet build Gaffer.Api.Tests -v q` | exit 1; CS0246/CS0103/CS1501: no existen `DomainValidationException`, `DomainValidationErrors`, `ValidationFailedException`, `ValidationErrorKeys`, `Player.Create(7 args)`, `SetBirthDate(DateTime, DateTime?)` |
| 2 | Verde (primer intento): filtro del paso (6 patrones: `PlayerFeaturesShould`, `SqlPlayerRepositoryShould`, `PlayerShould`, `PlayerCommandHandlerShould`, `ExceptionToHttpResponseConvertersShould`, `ExceptionHandlingMiddlewareShould`) | exit 1; 3 con error: 2 por la aserción `Contain([])` del helper del test (corregida: sin campos esperados no se comprueba contención) y 1 test de 29-feb mal planteado (2008+16 = 2024 es bisiesto; pasa a 2084 → 2100) |
| 2 | Verde: mismo filtro | exit 0; 196/196 |
| 2 | AC2.6 después: misma sonda `--filter ZzCycle2ProbeShould` + `cmp swagger-before.json swagger-after.json` | exit 0; `cmp` exit 0, sha256 idéntico `be49f0ab...a5a6bfe` |
| 2 | Refactor: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 2 | Refactor, AC2.8: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 413/413 superados (324 + 89), 0 omitidos |
| 2 | AC2.8: `--filter "OpenApiContractShould\|FacilitiesControllerContractShould\|TestServerDependenciesShould\|FacilityFeaturesShould\|SearchPlayersEndpointShould"` (FullyQualifiedName~) | exit 0; 46/46 |
| 2 | Filtro del paso tras refactor (6 patrones) | exit 0; 197/197 |
| 2 | REVIEWER: `docker ps` | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 2 | REVIEWER: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 2 | REVIEWER: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 413/413, 0 omitidos |
| 2 | REVIEWER: rojo aceptación en worktree `HEAD` + diff de `PlayerFeaturesShould`/`SqlPlayerRepositoryShould` | exit 1; 33/76 con error (misma lista que BUILDER); worktree eliminado |
| 2 | REVIEWER: rojo unitario en el mismo worktree + diffs `Unit/Domain\|Commands\|Exceptions` | build exit 1; CS0246/CS0103/CS1501 |
| 2 | REVIEWER: filtro del paso + contrato/regresión (11 patrones) | exit 0; 243/243 |
| 2 | REVIEWER: pipeline de producción (`dotnet run`, `GafferDB_Test`, curl) | Player: 400 con la clave por operación, `Errors` por campo y sin eco del valor; PUT id desconocido 404; `secondLastName:null` conserva el valor. Facility sin dirección 500 (`NullReferenceException`). Detalle en `REVIEW.md` ciclo 2 |

## Ciclo 2 (BUILDER) - detalle

### AC2.0 Caracterización previa (antes de cambiar código)

| Caso | Producción (`StartupTestServer`, `AddMvcCore`) | Host de `TestFixture` (`AddControllers`) | ¿Quién lo rechaza? |
| --- | --- | --- | --- |
| `POST` `firstName: ""` | 201 (se guarda) | 201 | Nadie (llega al handler) |
| `POST` `firstName` ausente | 500 `Unknown error` (null llega a la BD) | 400 formato ASP.NET `{"errors":{"FirstName":["The FirstName field is required."]}}` (required implícito por nulabilidad) | Prod: nadie; tests: model binding (DataAnnotations solo en el host de tests) |
| `POST` `birthDate` ausente | 201 (`0001-01-01`) | 201 | Nadie |
| `POST` fecha futura | 201 | 201 | Nadie |
| `POST` `gender: "ZZZ"` | 400 ASP.NET; el mensaje de Newtonsoft **refleja el valor** (`Error converting value "ZZZ"...`) | 400 igual + `"request":["The request field is required."]` | Model binding |
| `POST` `gender: 99` | 201 (Newtonsoft acepta el número no definido) | 201 | Nadie: llega al dominio |
| `POST` JSON mal formado | 400 ASP.NET `{"errors":{"firstName":["Unexpected end..."]}}` | 400 igual + `request` required | Model binding |
| `PUT` `firstName: ""` | 200 (se ignora) | 200 | Nadie |
| `PUT` fecha futura | 200 (se guarda) | 200 | Nadie |
| `PUT` `gender: "ZZZ"` | 400 ASP.NET (refleja el valor) | 400 igual + `request` | Model binding |
| `PUT` `gender: 99` | 200 (se guarda) | 200 | Nadie |
| `PUT` JSON mal formado | 400 ASP.NET | 400 igual + `request` | Model binding |
| `POST` facility sin dirección | **500** `Unknown error` (con `AddMvcCore` no se evalúa `[Required]`) | 400 ASP.NET `{"errors":{"Address":["The Address field is required."]}}` | Prod: nadie (falla después); tests: DataAnnotations |

Resuelve "No verificado" de `00`: (1) en producción (`AddMvcCore` sin `AddDataAnnotations`) los DataAnnotations **no** se evalúan; el host de `TestFixture` sí (llama a `AddControllers()`), por eso `FacilityFeaturesShould` L118 ve 400 y producción 500 (afecta al paso 03). (2) Newtonsoft acepta números de enum no definidos y los pasa al handler. Después del cambio (misma sonda): todos los casos de player dan 400 con el formato de Q4 y la clave de la operación en ambos hosts; facility sin cambios (paso 03).

### Diseño

| Pieza | Fichero | Detalle |
| --- | --- | --- |
| Excepción y colector de dominio (reutilizable en paso 03) | `Gaffer.Api/Domain/DomainValidationException.cs` (nuevo) | `DomainValidationException(Errors: campo → mensajes)`; `DomainValidationErrors` con `Add`, `Collect(Action)` (acumula los errores de operaciones que ya validan) y `ThrowIfAny()`. |
| Reglas (Q5, Q22) | `Gaffer.Api/Domain/Player.cs` | `Player.Create(..., DateTime? todayUtc = null)` valida todo y lanza con todos los campos (Q18 a)); `SetFirstName/SetLastName/SetSecondLastName(string)/SetBirthDate(DateTime, DateTime? todayUtc)/SetGender(Gender)` validan antes de asignar (un valor inválido no cambia el estado). El constructor primario **no valida** (rehidratación Dapper, Q16). Mensajes sin el valor recibido. `SetSecondLastName` pasa de `string?` a `string`. |
| Operación → clave (Q4) | `Gaffer.Api/Shared/Exceptions/ValidationFailedException.cs` (nuevo) | `ValidationFailedException(Title, ErrorTranslationKey, Errors)` + `ValidationErrorKeys.PlayersCreate/PlayersUpdate`. Los handlers envuelven la excepción de dominio con su operación. |
| Handlers | `CreatePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs` | Create usa `Player.Create`. Update (Q6/Q17/Q18): lee primero (404 antes que 400, Q21 a)), llama al setter de cada campo **no null** (incluido `SecondLastName`; `""` ya no se ignora), acumula con `Collect` y lanza antes de `IPlayerRepository.Update`. |
| Único 400 de validación | `ExceptionToHttpResponseConverters.cs` | `BuildValidationProblemDetails(title, key, errors)` (Extensions `ErrorTranslationKey` + `Errors`) y `Serialize(ProblemDetails)`; converter de `ValidationFailedException`. `ExceptionHandlingMiddleware.cs` usa `Serialize` (mismo JSON). |
| Model binding (Q19 a)) | `Gaffer.Api/Startups/ValidationStartup.cs` (nuevo) | `ConfigureValidationResponses()`: `PostConfigure<ApiBehaviorOptions>` sustituye `InvalidModelStateResponseFactory` solo para las acciones registradas (`PlayerController.Create/Update`); el resto conserva la respuesta por defecto (facility, paso 03). Mensaje genérico `The value is not valid.`, campo normalizado a PascalCase (`gender` → `Gender`, parámetro del cuerpo → `Body`). Sin atributos en DTO ni controladores. |
| DI | `Startup.cs`, `Gaffer.Api.Tests/TestFixture.cs` | `services.ConfigureValidationResponses()` en ambos en el mismo incremento (lección de `TestFixture`). Sin handlers ni repositorios nuevos. |

### Tests adaptados (Q17, antes → después)

| Test | Antes | Después |
| --- | --- | --- |
| `Unit/Commands/UpdatePlayerCommandHandlerShould.KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent` | `SecondLastName == null` | Renombrado `KeepOmittedFieldsIncludingTheSecondLastNameWhenOnlyFirstNameIsSent`; `SecondLastName == DefaultSecondLastName` (original). Resto de aserciones intactas. |
| `Unit/Commands/UpdatePlayerCommandHandlerShould.ReplaceTheSecondLastNameAndKeepTheRest` | `("Nuevo","Nuevo")`, `("", null)`, `(null, null)` | Renombrado `ReplaceTheSecondLastNameOnlyWhenAValueIsSentAndKeepTheRest`: `("Nuevo","Nuevo")`, `(null, original)`. El caso `""` pasa al test nuevo `RejectAnEmptySecondLastNameWithoutUpdating` (`""` y `"   "` → `ValidationFailedException` `Error.Players.Update.Validation`, sin llamar a `Update`). |
| `Acceptance/PlayerFeaturesShould.UpdateAPlayer` | `SecondLastName` nulo tras `{"firstName":"X"}` | `SecondLastName == DefaultSecondLastName` (original). |
| `Acceptance/PlayerFeaturesShould.ReplaceTheSecondLastNameOnUpdate` | `"Nuevo"` → `"Nuevo"`; `null` → nulo; `""` → nulo | Renombrado `ReplaceTheSecondLastNameOnlyWhenAValueIsSent`: `"Nuevo"` → `"Nuevo"`; `null`, `{"firstName":"Alexia"}` y `{}` → original. `""` y `"   "` → 400 en `RejectAPartialUpdateWith(SecondLastName empty/whitespace)` con GET sin cambios. |

Ninguna otra aserción se debilita.

### Tests nuevos por criterio

| AC | Tests |
| --- | --- |
| AC2.1 | `PlayerFeaturesShould.RejectTheCreationOfAnInvalidPlayerWithoutSavingIt` (15 casos: nombre vacío/espacios/101/ausente, apellidos, `SecondLastName` `""`/espacios/101, `BirthDate` 1899-12-31/mañana/un día menos de 16/ausente, `Gender` 99) con recuento de filas de `Players` igual |
| AC2.2 | `CreateAPlayerWhoseValuesAreAtTheLimits` (100 caracteres, 1900-01-01, 16 años exactos, sin `SecondLastName`) → 201 + GET |
| AC2.3 | `RejectAPartialUpdateWithAnInvalidFieldAndChangeNothing` (6 casos con un campo válido junto al inválido; GET idéntico), `ReturnNotFoundWhenUpdatingAnUnknownPlayerWithAnInvalidBody` (404, Q21 a)); `KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls` y `UpdateEveryFieldWhenAllFieldsAreSent` sin cambios y en verde |
| AC2.4 | `ReplaceTheSecondLastNameOnlyWhenAValueIsSent` (null/ausente/`{}`), `KeepThePlayerIdenticalWhenUpdatedWithAnEmptyBody`, casos `SecondLastName` de AC2.3 |
| AC2.5 | `ReportEveryInvalidFieldOfACreationWithoutEchoingTheRejectedValues`, `ReportEveryInvalidFieldOfAPartialUpdate` (cadena de 101 ausente del cuerpo), `ReturnTheValidationFormatForBodiesThatCannotBeBound` (JSON mal formado y `gender` texto desconocido en POST y PUT; `ZZZ_UNKNOWN` ausente del cuerpo) |
| AC2.7 | `PlayerShould`: `RejectTheCreation(*)` (15), `RejectTheSetter(*)` (12, estado sin cambios), límites, 16 años exactos, 29-feb (Q22), rehidratación sin validar, `Collect`/`ThrowIfAny`, `ReportAnAbsentBirthDateAsRequired`. `UpdatePlayerCommandHandlerShould`: `ReportEverySentInvalidFieldAndNotUpdateWhenAnyIsInvalid`, `RejectAnEmptySecondLastNameWithoutUpdating`, `ReturnNotFoundBeforeValidatingTheSentFields`, `UpdateAStoredPlayerThatBreaksTheRulesWhenOnlyValidFieldsAreSent`. `CreatePlayerCommandHandlerShould.RejectAnInvalidPlayerWithEveryInvalidFieldAndNotSaveIt`. `ExceptionToHttpResponseConvertersShould`: expectativa de `ValidationFailedException` en `CoverEveryConverter`, `ConvertAValidationFailureIntoABadRequestWithTheOperationKeyAndEveryFieldError` (ambas claves), `NameThePlayerValidationKeysByOperation` |
| AC2.9 | `PlayerFeaturesShould.KeepReadingAndPartiallyUpdatingAStoredPlayerThatBreaksTheRules` (INSERT SQL directo: `FirstName` de 150, `SecondLastName` vacío, 10 años; GET, search y PUT solo `lastName`); `SqlPlayerRepositoryShould.ReadAStoredPlayerThatBreaksTheCreationRulesWithoutValidatingIt` (integración; verde antes y después) |

### AC2.6 (OPT-032)

Rama aplicada: **comparación con `StartupTestServer`**, porque el diff toca `Startup.cs` (`ConfigureValidationResponses`). No toca `Gaffer.Api/Controllers/` ni DTOs. Antes y después: 21617 bytes, sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe`, `cmp` exit 0. Sin acción del operador de Q10 (el cliente no cambia). Nota: el tamaño no es comparable con los 21622 bytes del ciclo 1 (capturados con `dotnet run`, otro procedimiento); solo se comparan capturas del mismo procedimiento.

### Observaciones

| # | Observación |
| --- | --- |
| 1 | El host de `TestFixture` no es igual a producción: `AddControllers()` activa DataAnnotations (required implícito y `[Required]`/`[MaxLength]`) que producción (`AddMvcCore`) no evalúa. Con este paso, en players ambos dan el mismo 400; el campo de `Errors` puede diferir en casos de model binding (p. ej. `Body` adicional en el host de tests). Relevante para el paso 03 (`POST` facility sin dirección da 500 en producción). No se cambió (fuera de alcance). |
| 2 | `BirthDate` ausente en el POST se informa como `BirthDate is required.` (`default(DateTime)`). |
| 3 | La sonda temporal se creó y eliminó dentro del ciclo; no queda en el diff. |
| 4 | Cliente generado: no tocado; sin build Release. |

## Ciclo 3 - comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 3 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 3 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 3 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 413/413, 0 omitidos |
| 3 | AC3a.0, ayudante creado (antes de tocar producto): `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 413/413, 0 omitidos (el `[Explicit]` no se ejecuta ni cuenta como omitido) |
| 3 | AC3a.0 sin variable: `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` | exit 1; mensaje `Set the environment variable GAFFER_SWAGGER_DUMP_PATH ... Nothing was written.`; ningún fichero escrito |
| 3 | AC3a.0/AC3a.8 antes (x2): `GAFFER_SWAGGER_DUMP_PATH="$(cygpath -w <scratchpad>/c3/swagger-before{1,2}.json)" dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` | exit 0 ambas; 21617 bytes; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` en las dos; `cmp` exit 0 (igual que la captura del ciclo 2) |
| 3 | Rojo aceptación: `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~FacilityFeaturesShould\|FullyQualifiedName~FacilityValidationInTheProductionPipelineShould"` | exit 1; 106/132 con error: 34 `RejectTheCreationOfAnInvalidFacilityWithoutSavingIt(*)` (201, o 400 ASP.NET sin clave), 34 `RejectTheUpdateOfAFacilityWithAnInvalidBodyAndChangeNothing(*)`, 16 + 16 `FacilityValidationInTheProductionPipelineShould` (todos **500** `Unknown error` en producción), 4 `ReturnTheValidationFormatForBodiesThatCannotBeBound`, `ReportEveryInvalidField...`, `RejectCreatingAFacilityWithMissingAddress` (400 ASP.NET sin clave). Verdes antes (caracterización): límites AC3a.4 (create/update), 404 con cuerpo inválido, AC3a.10 |
| 3 | Rojo unitario: `dotnet build Gaffer.Api.Tests -v q` | exit 1; CS0117 (`ValidationErrorKeys.FacilitiesCreate/FacilitiesUpdate`), CS1501 (`Facility.Create`, `Address.Create`) |
| 3 | Verde (primer intento), filtro del paso (6 patrones `FullyQualifiedName~`: `FacilityFeaturesShould`, `FacilityValidationInTheProductionPipelineShould`, `FacilityShould`, `FacilityCommandHandlerShould`, `ExceptionToHttpResponseConvertersShould`, `SqlFacilityRepositoryShould`) | build: 2 CS8602 en `UpdateFacilityCommandHandlerShould` L76/L228 (`command.Address` ahora anulable) → `!`; test exit 1, 1/273: `CreateFacilityCommandHandlerShould.ThrowExceptionWhenAddressIsMissing` esperaba `NullReferenceException` (adaptado, ver tabla) |
| 3 | Verde + refactor (`Player.CheckName` → `DomainValidationErrors.RequireText`): mismo filtro + `PlayerShould\|PlayerFeaturesShould` | exit 0; 367/367; build 0 Advertencia(s) |
| 3 | AC3a.9: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| 3 | AC3a.9: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 574/574 (413 + 161), 0 omitidos |
| 3 | AC3a.8 después: mismo comando del ayudante con `swagger-after.json` + `cmp swagger-before1.json swagger-after.json` | exit 0; 21617 bytes; sha256 `be49f0ab...a5a6bfe`; `cmp` exit 0 |
| 3 | AC3a.9: `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` | 0 |
| 3 | REVIEWER: `docker ps`; build `--no-incremental -v q`; `dotnet test gaffer-api.sln --no-build -v q` | `Quionis_DB` Up 3306; build exit 0, 0 Advertencia(s); test exit 0, 574/574, 0 omitidos |
| 3 | REVIEWER rojo (worktree `HEAD` temporal + tests nuevos de aceptación) | exit 1; 106/154 con error; producción (`StartupTestServer`) 32/32: 18 × 500, 7 × 201, 7 × 200 (corrige "todos 500" de la fila de rojo de BUILDER) |
| 3 | REVIEWER rojo unitario (mismo worktree + tests unitarios) | build exit 1; 12 × CS0117, 8 × CS1501 |
| 3 | REVIEWER AC3a.8/AC3a.0: ayudante en `HEAD` y en el árbol actual; sin variable | ambos exit 0, sha256 `be49f0ab...a5a6bfe`, `cmp` exit 0; sin variable exit 1 con el mensaje esperado. Worktree eliminado. Veredicto APPROVED (`REVIEW.md` ciclo 3) |

## Ciclo 3 (BUILDER) - detalle (paso 03a)

### AC3a.0 Ayudante permanente (OPT-034)

| Campo | Valor |
| --- | --- |
| Fichero | `Gaffer.Api.Tests/Acceptance/ServedOpenApiDocumentDump.cs` (clase `[Explicit]`, test `WriteTheServedSwaggerJsonToTheConfiguredPath`) |
| Variable | `GAFFER_SWAGGER_DUMP_PATH` (ruta Windows; desde Git Bash con `cygpath -w`) |
| Comando | `GAFFER_SWAGGER_DUMP_PATH="$(cygpath -w <ruta>/swagger.json)" dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` |
| Comportamiento | Arranca `StartupTestServer`, pide `/swagger/v1/swagger.json` y escribe los bytes tal cual. Sin variable: `Assert.Fail` con mensaje claro antes de arrancar el servidor; no escribe nada. |
| Suite normal | 413 antes y después de añadirlo (no aparece como omitido). Determinista: dos ejecuciones, mismo sha256. |

### Fechas de los casos (OPT-036)

Hoy UTC al escribir los tests: 2026-10-12; "mañana" 2026-10-13. Los tests de aceptación calculan `DateTime.UtcNow.Date` (y +1 día) en ejecución; los unitarios fijan `todayUtc = 2026-10-12`. Límites fijos: 1800-01-01 (aceptado), 1799-12-31 (rechazado).

### Diseño

| Pieza | Fichero | Detalle |
| --- | --- | --- |
| Regla de texto compartida (refactor) | `Domain/DomainValidationException.cs` | `DomainValidationErrors.RequireText(field, value, maxLength)`: null → `is required.`; vacío/espacios → `must not be empty or contain only whitespace.`; largo → `must be at most N characters long.` Mismos textos que `Player`, que ahora la usa en `CheckName`. |
| `Address` | `Domain/Facility.cs` | `Address.Create(...)` valida; `CheckFields` (interno) añade `Address.Street/StreetNumber/PostalCode/City/Region/Country` (100/20/20/100/100/100). Constructor primario sin validar (rehidratación, Q16). |
| `Facility` | `Domain/Facility.cs` | `Facility.Create(..., Address? address, stands, DateTime? todayUtc = null)`: `Name` 1-100; `BuildDate` (`default` → requerido; < 1800-01-01; > hoy UTC); `Type` definido; `Capacity` ≥ 0; `Address` requerida y sus campos; todo acumulado (Q18 a)). Después llama al constructor, que mantiene sus guardas (`Address is required`, `Stands cannot exceed 4` con `FacilityValidationException`, paso 03b). |
| Handlers | `CreateFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs` | Usan `Facility.Create`; `DomainValidationException` → `ValidationFailedException("Cannot create facility" / "Cannot update facility", Error.Facilities.Create/Update.Validation)`. Update construye dentro del lock (404 antes que 400, Q21 a)); el error dentro del callback deshace la transacción. `Address` del comando pasa a anulable. |
| Claves | `Shared/Exceptions/ValidationFailedException.cs` | `ValidationErrorKeys.FacilitiesCreate/FacilitiesUpdate`. El converter de `ValidationFailedException` (paso 02) no cambia. |
| Model binding (Q19 a)) | `Startups/ValidationStartup.cs` | `FacilitiesController.Create/Update` registrados con su título y clave. |
| Controlador (Q23) | `Controllers/FacilitiesController.cs` | Solo `request.Address is null ? null : new ...AddressCommand(...)` en `Create` y `Update`. Sin atributos, rutas ni DTO nuevos; `[Required]`/`[MaxLength(4)]` intactos (Q20 a)). |
| DI | `Startup.cs`, `TestFixture.cs` | Sin cambios: no hay handlers nuevos y `ConfigureValidationResponses` ya estaba en ambos. Nada cambiado por la divergencia (OPT-035). |

### Tests adaptados

| Test | Antes | Después |
| --- | --- | --- |
| `Acceptance/FacilityFeaturesShould.RejectCreatingAFacilityWithMissingAddress` (L118) | 400 o 422 | 400 + `Error.Facilities.Create.Validation` + campo `Address` |
| `Unit/Commands/CreateFacilityCommandHandlerShould.ThrowExceptionWhenAddressIsMissing` | `NullReferenceException` (comando con `null!`) | `ValidationFailedException` `FacilitiesCreate`, `Errors` = `Address`, sin `Save` |
| `Unit/Commands/UpdateFacilityCommandHandlerShould` L76, L228 | `command.Address.Street` | `command.Address!.Street` (solo nulabilidad; mismas aserciones) |

### Tests nuevos por criterio

| AC | Tests |
| --- | --- |
| AC3a.1 | `FacilityFeaturesShould.RejectTheCreationOfAnInvalidFacilityWithoutSavingIt` (34 casos: `Name` vacío/espacios/101/ausente; `BuildDate` 1799-12-31/mañana/ausente; `Type` 99; `Capacity` -1; `Address` ausente; cada campo de dirección vacío/espacios/máx+1/ausente); filas de `Facilities` y `Addresses` iguales |
| AC3a.2 | `RejectTheUpdateOfAFacilityWithAnInvalidBodyAndChangeNothing` (los mismos 34, clave de update, `GET` con gradas idéntico); `ReturnNotFoundWhenUpdatingAnUnknownFacilityWithAnInvalidBody`; fusión (TECHDEBT-003) y lock (TECHDEBT-004) en verde en la suite |
| AC3a.4 | `CreateAFacilityWhoseValuesAreAtTheLimits`, `UpdateAFacilityWithValuesAtTheLimits` (100/20 exactos, `Capacity` 0, 1800-01-01, hoy UTC) |
| AC3a.5 | `ReportEveryInvalidFieldOfTheFacilityAndItsAddressWithoutEchoingTheRejectedValues` (POST y PUT; 5 campos), `ReturnTheValidationFormatForBodiesThatCannotBeBound` (JSON mal formado y `type` de texto desconocido, POST/PUT; sin eco) |
| AC3a.11 | `FacilityValidationInTheProductionPipelineShould` (en `FacilityFeaturesShould.cs`, `StartupTestServer`): 16 casos × POST/PUT (`Address` ausente/null, `Name` ausente/null, cada campo de dirección ausente/null) → 400, clave y campo, sin escritura (filas / `GET` idéntico). Rojo: los 32 daban 500 |
| AC3a.7 | `Unit/Domain/FacilityShould` (34 inválidos, límites, hoy, `todayUtc` por defecto, todos a la vez sin eco, > 4 gradas sigue en `FacilityValidationException`, rehidratación sin validar, `Address.Create`); `CreateFacilityCommandHandlerShould.RejectAnInvalidFacilityWithEveryInvalidFieldAndNotSaveIt`; `UpdateFacilityCommandHandlerShould.RejectAnInvalidBodyWithEveryInvalidFieldInsideTheLockWithoutUpdating`, `RejectAnAbsentAddressWithTheUpdateKeyWithoutUpdating`, `ReturnNotFoundBeforeValidatingAnInvalidBody`, `UpdateAStoredFacilityThatBreaksTheRulesWhenTheBodyIsValid`; `ExceptionToHttpResponseConvertersShould` (+2 casos de facility, `NameTheFacilityValidationKeysByOperation`) |
| AC3a.10 | `FacilityFeaturesShould.KeepReadingAndUpdatingAStoredFacilityThatBreaksTheRules` (INSERT SQL: `Name` 150, `Capacity` -5, `BuildDate` 1700-01-01, `PostalCode` 30, `Street` vacío; `GET` por id, `GET` lista y `PUT` válido); `SqlFacilityRepositoryShould.ReadAStoredFacilityThatBreaksTheCreationRulesWithoutValidatingIt`. Verdes antes y después |

### AC3a.8 (OPT-032 + OPT-034)

Rama aplicada: **comparación con el ayudante**, porque el diff toca `FacilitiesController.cs` y `Startups/ValidationStartup.cs`. Antes y después: 21617 bytes, sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe`, `cmp` exit 0. Sin acción de Q10. `FacilitiesControllerContractShould` sin cambios y en verde. Cliente generado sin tocar.

### Observaciones

| # | Observación |
| --- | --- |
| 1 | AC3a.11, mensaje por host: en producción los casos divergentes llegan al dominio (`Address is required.`, `Name is required.`, `Address.Street is required.`; `... must not be empty ...` si el campo falta y el DTO pone `""`). En el host de `TestFixture` el model binding los rechaza antes (`[Required]` / required implícito) con `The value is not valid.` (factoría de `ValidationStartup`). Clave y campo coinciden (lo comprueban los tests); el mensaje difiere (deducido del código, no aserto). |
| 2 | "Manteniendo el Detail actual de dirección ausente": `FacilityValidationException("Address is required")` sigue en el constructor (solo alcanzable fuera de `Create`). En `POST`/`PUT` la dirección ausente sale como `ValidationFailedException` (Q4): `Detail` = `One or more fields are not valid.` y `Errors.Address` = `Address is required.`. Antes ese caso no llegaba a un `Detail` propio (producción 500; tests 400 de ASP.NET). A confirmar por REVIEWER. |
| 3 | El `PUT` correcto devuelve **200** (contrato actual, `UpdateAFacilityFieldsAndAddressKeepingItsFourStands`), no 204 como dicen AC3a.4/AC3a.10; los tests exigen 200. |
| 4 | Gradas (anidadas y `/stands`) sin cambios (03b). Con campos inválidos y > 4 gradas a la vez solo se informa de los campos (el límite se comprueba después, en el constructor). |
| 5 | El plan cita `Integration/SqlFacilityRepositoryShould.cs`; el fichero real es `Unit/Repositories/SqlFacilityRepositoryShould.cs` (usa la BD de Docker). |
| 6 | Sin sondas temporales; el ayudante es permanente. Documentarlo en `test-stack.md` queda para DOCUMENTER/operador (fuera de la matriz de BUILDER). |


## Ciclo 4 - comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 4 | Compactación (OPT-026/OPT-029) | Ciclo 3 colapsado en el índice; sus comandos y su detalle movidos tal cual a `STATUS-archive.md` |
| 4 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 4 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 4 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 574/574, 0 omitidos |
| 4 | AC3b.8 antes: `GAFFER_SWAGGER_DUMP_PATH="$(cygpath -w <scratchpad>/c4/swagger-before.json)" dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` | exit 0; 21617 bytes; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` |
| 4 | Rojo aceptación (solo tests, producto sin tocar): `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~FacilityFeaturesShould\|...~FacilityValidationInTheProductionPipelineShould\|...~FacilityStandsFeaturesShould\|...~FacilityStandValidationInTheProductionPipelineShould\|...~SqlFacilityRepositoryShould"` | exit 1; 32/199 con error. Detalle por clase en "Rojo (OPT-038)". |
| 4 | Rojo producción del sub-recurso, tras corregir en el test la lectura del `id` (`"Id"` → sin distinguir mayúsculas; la primera ejecución dio 4 × `ArgumentNullException` del propio test): filtro `...~FacilityStandValidationInTheProductionPipelineShould\|...~RejectAnInvalidStandBeforeLookingForTheFacility` | exit 1; 6/6 con error: 1 × 201, 1 × 204, 2 × 500, 2 × 404 |
| 4 | Rojo unitario: `dotnet build Gaffer.Api.Tests -v q` (errores únicos por `fichero(línea,col)`) | exit 1; 9 × CS0117 (`ValidationErrorKeys.FacilitiesStands` ×3, `Facility.CheckRequestedStands` ×3, `Stand.CheckFields`, `CannotCreateStandTitle`, `CannotUpdateStandTitle`), 3 × CS7036 (`Stand.Create` inexistente) |
| 4 | OPT-039: `grep -rn "<p>" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l` para `Facility.Create(` / `new Stand(` / `DomainValidationErrors` / `FacilityValidationException` / `errors.Add(` | 5 / 48 / 17 / 7 / 11 usos. Sin cambios de firma ni de nulabilidad: `new Stand(...)` (constructor) intacto; cambia el comportamiento de `Facility.Create` (> 4 gradas → `DomainValidationException` campo `Stands`) y `DomainValidationErrors.Add` (no repite el mismo mensaje). Usos adaptados: los 5 tests de la tabla "Tests adaptados" |
| 4 | Verde, filtro del paso (10 patrones: los 5 del rojo + `FacilityShould`, `StandShould`, `FacilityCommandHandlerShould`, `FacilityStandCommandHandlersShould`, `ExceptionToHttpResponseConvertersShould`) | exit 0; 350/350 al primer intento; build 0 Advertencia(s) |
| 4 | Refactor (`UpdateFacilityCommandHandler`: `MaxStands` privado → `Facility.MaxStands`) + AC3b.9: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 4 | AC3b.9: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 637/637 (574 + 63), 0 omitidos |
| 4 | AC3b.8 después: mismo comando del ayudante con `swagger-after.json` + `cmp swagger-before.json swagger-after.json` | exit 0; 21617 bytes; sha256 `be49f0ab...a5a6bfe`; `cmp` exit 0 |
| 4 | AC3b.8: `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~FacilitiesControllerContractShould\|FullyQualifiedName~OpenApiContractShould"` | exit 0; 15/15 |
| 4 | AC3b.9: `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` | 0 |
| 4 | REVIEWER: `docker ps` + build `--no-incremental -v q` + suite `--no-build -v q` | `Quionis_DB` Up 3306; build exit 0, 0 Advertencia(s); suite exit 0, 637/637, 0 omitidos |
| 4 | REVIEWER: rojo sobre `git archive HEAD` (scratchpad) con los tests nuevos copiados | Aceptación (mismo filtro de 5 clases) exit 1, 32/199 con error; unitario build exit 1, 9 × CS0117 + 3 × CS7036 |
| 4 | REVIEWER: AC3b.8 ayudante + contratos + `CancellationToken.None` | Volcado exit 0, 21617 bytes, sha256 `be49f0ab...a5a6bfe`; contratos exit 0, 15/15; grep 0. Veredicto APPROVED (`REVIEW.md`, ciclo 4) |

## Ciclo 4 (BUILDER) - detalle (paso 03b)

### Rojo (OPT-038), por clase de test

| Clase | Test | Resultado en rojo |
| --- | --- | --- |
| `FacilityFeaturesShould` | `RejectTheCreationOfAFacilityWithAnInvalidStandWithoutSavingIt` | 3 × 201 |
| | `RejectTheUpdateOfAFacilityWithInvalidStandsAndChangeNothing` | 3 × 200 (el caso `5 stands` ya daba 400 en `TestFixture` por `[MaxLength(4)]`: verde) |
| | `ReportEveryInvalidFieldOfTheFacilityItsAddressAndItsStandsWithoutEchoingTheRejectedValues` | 1 × 400 sin los campos `Stands[i].*` |
| | `RejectAnUpdateLeavingMoreThanFourStandsWhen...` (2, adaptados) | 2 × 400 con clave `Error.Facilities.Create.Validation` |
| `FacilityValidationInTheProductionPipelineShould` (`StartupTestServer`) | creación: `5 stands` / `StandName absent` / `null` | 1 × 400 sin `Errors` (`Detail` "Stands cannot exceed 4"), 1 × 201, 1 × 500 |
| | actualización: `5 stands` / `StandName absent` / `null` | 1 × 400 con clave de create, 1 × 200, 1 × 500 |
| `FacilityStandsFeaturesShould` | `RejectAnInvalidStandWithoutChangingTheStands` | 3 × 201 (POST), 3 × 204 (PUT) |
| | `RejectAnInvalidStandBeforeLookingForTheFacility` | 2 × 404 |
| | `RejectAStandNameLongerThanItsColumnWithAValidationError` (AC3b.6) | 1 × 500 |
| | `ReturnTheValidationFormatForStandBodiesThatCannotBeBound` | 4 × 400 de ASP.NET sin clave |
| `FacilityStandValidationInTheProductionPipelineShould` (`StartupTestServer`) | POST/PUT `StandName` ausente / `null` | 1 × 201, 1 × 204, 2 × 500 |

Verdes antes y después (caracterización): `CreateAndUpdateAFacilityWithStandsAtTheLimits`, `CreateAndReplaceAStandWithValuesAtTheLimits` (AC3b.4), `KeepReadingKeepingAndDeletingAStoredStandThatBreaksTheRules` y `SqlFacilityRepositoryShould.ReadAStoredStandThatBreaksTheCreationRulesWithoutValidatingIt` (AC3b.10), y los dos tests de 5 gradas adaptados en `TestFixture` (model binding de 03a).

### Diseño

| Pieza | Fichero | Detalle |
| --- | --- | --- |
| `Stand` | `Domain/Facility.cs` | `Stand.Create(id, standName?, capacity)` (sub-recurso) y `Stand.CheckFields(errors, fieldPrefix, ...)`: `StandName` con `RequireText` (1-100, sin solo espacios; mismos textos que `Player`/`Address`), `StandCapacity` ≥ 0 (`... must not be negative.`). Constructor primario sin validar (rehidratación, Q16). |
| `Facility` | `Domain/Facility.cs` | `Create` añade el límite de gradas como error de campo `Stands` (`Stands cannot exceed 4.`), junto con los demás (Q18 a)). No valida los campos de las gradas que recibe. `CheckRequestedStands(errors, requested)`: número y cada grada enviada con índice (`Stands[1].StandName`). `MaxStands`/`StandsField`/`MaxStandsMessage`. El constructor mantiene su guarda (`FacilityValidationException`). |
| Acumulador | `Domain/DomainValidationException.cs` | `Add` no repite un mensaje idéntico en el mismo campo (el límite puede saltar por lo enviado y por el total fusionado). |
| Handlers de facility | `CreateFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs` | `CheckRequestedStands` sobre lo enviado + `errors.Collect(Facility.Create)` + `ThrowIfAny` → `ValidationFailedException` con la clave de la operación. Update: dentro del lock (404 antes que 400); solo se validan las gradas enviadas (también las que llevan `Id`); las guardadas no enviadas no se revalidan (Q16). Lo enviado > 4 se rechaza aunque la fusión las descartase (facility con 4 gradas), igual que `[MaxLength(4)]` en el host de `TestFixture`. |
| Handlers del sub-recurso | `FacilityStandCommandHandlers.cs` | `StandValidation.Create` (interno) construye la grada antes del repositorio (400 antes que 404, Q21 a)); `DomainValidationException` → `ValidationFailedException` con `Error.Facilities.Stands.Validation` y título `Cannot create stand` / `Cannot update stand`. `DELETE` sin cambios. `FacilityMaxStandsExceededException` y su `Detail` sin cambios. |
| Clave | `Shared/Exceptions/ValidationFailedException.cs` | `ValidationErrorKeys.FacilitiesStands`. Converter sin cambios (el de `ValidationFailedException` es genérico). |
| Model binding (Q19 a)) | `Startups/ValidationStartup.cs` | `FacilityStandsController.Create/Update` registrados con su título y la clave de gradas. |
| Sin cambios | `TestFixture.cs`, `Startup.cs`, controladores, DTOs, `SqlFacilityRepository.cs` | Ningún handler nuevo (no hay DI que tocar); OPT-035 respetado; la rehidratación ya no validaba. |

### Tests adaptados

| Test | Antes | Después |
| --- | --- | --- |
| `Acceptance/FacilityFeaturesShould.RejectCreatingAFacilityWithMoreThanFourStands` (L152) | 400 o 422 | 400 + `Error.Facilities.Create.Validation` + campo `Stands` |
| `Acceptance/FacilityFeaturesShould.RejectUpdatingAFacilityWithMoreThanFourStands` | 400 | 400 + `Error.Facilities.Update.Validation` + campo `Stands` |
| `Acceptance/FacilityFeaturesShould.RejectAnUpdateLeavingMoreThanFourStandsWhen{TwoExist...,ThreeExist...}` | clave `Error.Facilities.Create.Validation` | `Error.Facilities.Update.Validation` (cambio de Q4 previsto en el plan) |
| `Unit/Commands/CreateFacilityCommandHandlerShould.ThrowExceptionWhenStandsExceedFour` | `FacilityValidationException` "Stands cannot exceed 4" | `ValidationFailedException` `FacilitiesCreate`, `Errors` = `Stands`, sin `Save` |
| `Unit/Commands/UpdateFacilityCommandHandlerShould.PropagateValidationExceptionWhenStandsExceedFour`, `RejectAnUpdateLeavingMoreThanFourStandsWithoutUpdating` | `FacilityValidationException` | `ValidationFailedException` `FacilitiesUpdate`, `Errors` = `Stands` (el primero, además, un solo mensaje), sin actualizar |
| `Unit/Domain/FacilityShould.KeepRejectingMoreThanFourStandsOnCreation` | renombrado a `RejectMoreThanFourStandsOnCreationAsAFieldError`: `DomainValidationException` `Stands` = `Stands cannot exceed 4.` | La guarda del constructor sigue cubierta por `KeepTheConstructorGuardForMoreThanFourStands` |

AC3b.6: no existía un test HTTP de 300 caracteres (TECHDEBT-004 AC4.11 solo dejó los de repositorio); se añade `RejectAStandNameLongerThanItsColumnWithAValidationError` (400 de gradas, antes 500). `LeaveNoRowsWhenTheStandInsertFails` y `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` siguen construyendo la grada de 300 caracteres con `new Stand(...)` (camino sin validar): **sin cambios** y en verde.

### Tests nuevos por criterio (reparto OPT-037)

| AC | Capa | Tests |
| --- | --- | --- |
| AC3b.1 | Aceptación | `RejectTheCreationOfAFacilityWithAnInvalidStandWithoutSavingIt` (3: `Stands[1].StandName` solo espacios / 101; `Stands[1].StandCapacity` -1); filas de `Facilities`/`Addresses` iguales |
| AC3b.2 | Aceptación | `RejectTheUpdateOfAFacilityWithInvalidStandsAndChangeNothing` (los 3 + `5 stands`); `GET` idéntico. Fusión (TECHDEBT-003) y lock (TECHDEBT-004) en verde en la suite |
| AC3b.3 | Aceptación | `RejectAnInvalidStandWithoutChangingTheStands` (POST y PUT × 3); `RejectAnInvalidStandBeforeLookingForTheFacility` (POST/PUT con facility inexistente → 400) |
| AC3b.4 | Aceptación | `CreateAndUpdateAFacilityWithStandsAtTheLimits` (201 / 200), `CreateAndReplaceAStandWithValuesAtTheLimits` (201 / 204) |
| AC3b.5 | Aceptación | `ReportEveryInvalidFieldOfTheFacilityItsAddressAndItsStandsWithoutEchoingTheRejectedValues` (POST y PUT, 7 campos con índice, sin eco); `ReturnTheValidationFormatForStandBodiesThatCannotBeBound` (JSON mal formado y `standCapacity` de texto, POST/PUT, sin eco) |
| AC3b.11 | Producción | `FacilityValidationInTheProductionPipelineShould` + `DivergentStandBodies` (`5 stands`, `Stands[0].StandName` ausente / `null`; POST y PUT); `FacilityStandValidationInTheProductionPipelineShould` (POST/PUT `StandName` ausente / `null`). Sin escritura (filas / `GET` idéntico) |
| AC3b.6 | Aceptación | `RejectAStandNameLongerThanItsColumnWithAValidationError` |
| AC3b.7 | Dominio | `StandShould` (matriz: vacío, espacios, 101, `null`, -1, `int.MinValue`; límites 1/100 caracteres, capacidad 0 e `int.MaxValue`; dos campos a la vez sin eco; prefijo; rehidratación sin validar); `FacilityShould` (+5 y 1 renombrado: límite como campo, junto a otros errores, 4 gradas sin revalidar, guarda del constructor, `CheckRequestedStands` con índices, hasta 4 válidas) |
| AC3b.7 | Handlers | `CreateFacilityCommandHandlerShould` (+2: gradas con índice, junto a errores de facility); `UpdateFacilityCommandHandlerShould` (+3: > 4 enviadas con facility llena, gradas enviadas con índice dentro del lock, grada guardada inválida no enviada se conserva); `FacilityStandCommandHandlersShould` (+4: `StandName`/`StandCapacity` × POST/PUT sin llamar al repositorio, título y clave); `ExceptionToHttpResponseConvertersShould` (+2 casos de gradas y la clave) |
| AC3b.10 | Aceptación + repositorio | `KeepReadingKeepingAndDeletingAStoredStandThatBreaksTheRules` (INSERT SQL `StandCapacity` -5: `GET .../stands` 200, `PUT` válido 200 y la grada se conserva, `DELETE` 204); `SqlFacilityRepositoryShould.ReadAStoredStandThatBreaksTheCreationRulesWithoutValidatingIt` |

### AC3b.8 (OPT-032 + OPT-034)

Rama aplicada: **comparación con el ayudante**, porque el diff toca `Startups/ValidationStartup.cs`. Antes y después: 21617 bytes, sha256 `be49f0ab...a5a6bfe` (igual que ciclos 2-3), `cmp` exit 0. Sin acción de Q10. `FacilitiesControllerContractShould` y `OpenApiContractShould` en verde (15/15). Cliente generado sin tocar; sin build Release.

### Observaciones

| # | Observación |
| --- | --- |
| 1 | AC3b.11, mensajes por host (deducidos del código, no asertados; clave y campo sí se comprueban): producción → `Stands cannot exceed 4.`; `Stands[0].StandName` ausente → `... must not be empty or contain only whitespace.` (el DTO pone `""`); `null` → `... is required.`; igual en el sub-recurso con `StandName`. Host de `TestFixture` → `The value is not valid.` (factoría de `ValidationStartup` tras `[MaxLength(4)]` / required implícito). |
| 2 | Cambio de comportamiento visible: el 400 de más de 4 gradas anidadas pasa de `Detail` "Stands cannot exceed 4" sin `Errors` a `Detail` "One or more fields are not valid." con `Errors.Stands` = "Stands cannot exceed 4." (lo exige AC3b.11: campo de Q4). El `Detail` del máximo del sub-recurso (`FacilityMaxStandsExceededException`) no cambia. A confirmar por REVIEWER. |
| 3 | `PUT /facilities/{id}` con > 4 gradas enviadas sobre una facility que ya tiene 4: antes en producción se descartaban en silencio (200); ahora 400 `Stands` (mismo resultado que el host de `TestFixture`). `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands` (≤ 4 enviadas) sigue en verde. |
| 4 | Con gradas inválidas y un `Id` repetido a la vez, gana `DuplicatedStand` (la fusión se hace antes de construir la facility, como antes). |
| 5 | El primer rojo de `FacilityStandValidationInTheProductionPipelineShould` fue un fallo del propio test (la lista de gradas de producción sale en camelCase: `id`); se corrigió el test antes de tocar producto y se repitió el rojo (fila de comandos). |
