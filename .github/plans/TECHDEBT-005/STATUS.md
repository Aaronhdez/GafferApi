# STATUS - TECHDEBT-005

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 2 |
| Aprobación del plan | Aprobado por el operador el 2026-10-11 (transmitido por ORCHESTRATOR): "vamos con el 2" |
| Pasos completados | 01 (APPROVED en ciclo 1); 02 (APPROVED en ciclo 2) |
| Pasos bloqueados | Ninguno por preguntas (Q16 y Q17 respondidas el 2026-10-11; Q18-Q22 con valor por defecto). 03 depende de 02 `APPROVED` |
| Siguiente | OPTIMIZER ciclo 2; checkpoint humano. Antes del paso 03, decidir el hallazgo de `TestFixture` (`AddControllers`) frente a producción (`AddMvcCore`), ver `REVIEW.md` ciclo 2 |
| Bloqueos | Ninguno |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | AC1.1-AC1.6 (rojo 2/2 → verde; suite 322 → 324; swagger idéntico) | sin commit registrado (rama `TECHDEBT-005`); detalle en `STATUS-archive.md` |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED |
| 2 | 02 Validación de dominio de Player (incluye el cambio de `SecondLastName` de Q17 respecto a TECHDEBT-002) | APPROVED |
| 3 | 03a Ayudante de volcado del `swagger.json` (OPT-034) + validación de dominio de Facility y dirección (create/update) | Pendiente |
| 4 | 03b Validación de dominio de gradas (anidadas + sub-recurso `/stands`) | Pendiente |
| 5 | 05 Health check (se mantiene el fallback a Dummy) | Pendiente |
| 6 | Reserva | - |

Re-secuenciación OPT-036 (PLANNER, 2026-10-12): antiguo paso 03 dividido en 03a/03b; antiguo 04 → 05. Correspondencia de criterios en `00-overview-and-open-questions.md`.

## Tiempos por agente (ISO 8601 UTC)

| Ciclo | Agente | Inicio | Fin |
| --- | --- | --- | --- |
| 1 | BUILDER | 2026-10-11T17:54:06Z | 2026-10-11T17:58:07Z |
| 1 | REVIEWER | 2026-10-11T17:58:29Z | 2026-10-11T18:00:44Z |
| 2 | BUILDER | 2026-10-11T18:06:15Z | 2026-10-11T18:18:44Z |
| 2 | REVIEWER | 2026-10-11T18:19:16Z | 2026-10-11T18:27:30Z |

## Comandos y resultados

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

## Documentación cambiada

Ninguna todavía.
