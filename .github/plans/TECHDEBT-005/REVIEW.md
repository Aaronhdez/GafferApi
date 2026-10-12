# REVIEW - TECHDEBT-005

## Ciclo 1 - Paso 01 (`Update(Facility)` fuera del contrato público)

**Veredicto: APPROVED**

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Del plan: `IFacilityRepository.cs`, `SqlFacilityRepository.cs`, `SqlFacilityRepositoryShould.cs`, `.github/plans/TECHDEBT-005/`. Ajenos al ciclo (no revisados, preservados): `MEMORIES.md`, `HARNESS-001/*`. |
| Fuera de alcance | Sin cambios en handlers, controladores, OpenAPI, migraciones ni cliente generado. |
| `TestFixture.cs` | Sin registros DI nuevos; no aplica. |
| Implementaciones de `IFacilityRepository` | Solo `SqlFacilityRepository` (más sustitutos NSubstitute en tests); ninguna otra rompe al quitar el método. |
| Transacción | `WriteFacility` conserva su `ExecuteInTransactionAsync`, que ya se anidaba antes del cambio dentro de `Update(Guid, ...)`; sin cambio de comportamiento. |

### Evidencia por criterio (re-ejecutada por REVIEWER, `Quionis_DB` Up en `0.0.0.0:3306`)

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC1.1 | Rojo reproducido en worktree desacoplado en `HEAD` (scratchpad, ya eliminado) con solo el diff de tests aplicado: `--filter FacilityRepositoryContractShould` exit 1, 2/2 con error (`Expected methodsTakingAFacility ... {"Save", "Update"} contains 1 item(s) too many` y lo mismo para `publicMethodsTakingAFacility`). Verde en el árbol actual (ver AC1.4 filtro, exit 0). | Demostrado |
| AC1.2 | Diff: los 3 tests solo cambian la invocación a `Update(id, _ => updated, token)`; aserciones intactas; `minimumCalls: 7` sin cambios; comentario del recuento ajustado. En verde dentro del filtro de 84 tests. | Demostrado |
| AC1.3 | `grep -n "Update(" SqlFacilityRepository.cs` → L122 (cref en `<summary>`), L208 `public ... Update(Guid id, Func<Facility, Facility> ...)`. `WriteFacility` es `private` (L125), único llamador L216 tras `LockFacility`. `grep -rn "\.Update(" Gaffer.Api` → `UpdateFacilityCommandHandler.cs:36` (sobrecarga con lock) y `UpdatePlayerCommandHandler.cs:15` (`IPlayerRepository`, ajeno). | Demostrado |
| AC1.4 | `dotnet test Gaffer.Api.Tests --no-build --filter "FacilityRepositoryContractShould\|SqlFacilityRepositoryShould\|MakeAConcurrent\|UpdateFacilityCommandHandlerShould\|FacilityFeaturesShould\|FacilityStandsFeaturesShould\|OpenApiContractShould\|FacilitiesControllerContractShould"` (FullyQualifiedName~) → exit 0, 84/84, 0 omitidos. | Demostrado |
| AC1.5 | `OpenApiContractShould` y `FacilitiesControllerContractShould` (ambos con `StartupTestServer`) en verde en el filtro anterior. Comparación del `swagger.json` servido: ver desviación abajo. | Demostrado (con desviación aceptada) |
| AC1.6 | `dotnet build gaffer-api.sln --no-incremental -v q` → exit 0, 0 Advertencia(s). `dotnet test gaffer-api.sln --no-build -v q` → exit 0, 324/324 (322 + 2 nuevos), 0 omitidos. `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` → 0. | Demostrado |

### Desviación AC1.5 (`dotnet run` + Dummy en lugar de `StartupTestServer`)

Se acepta como evidencia suficiente, no bloqueante:
- `dotnet run` y `StartupTestServer` usan el mismo `Startup` (`UseStartup<Startup>()`); `UseOpenApi()` no está condicionado por entorno ni por `DatabaseConfig`, y el documento NSwag se genera desde controladores y DTOs, no desde repositorios ni desde el proveedor de conexión.
- El diff no toca ningún tipo alcanzable desde controladores (solo interfaz de repositorio, su implementación y tests), así que el documento no puede variar por este cambio.
- BUILDER registra `cmp` exit 0 y sha256 igual antes/después (21622 bytes) con el mismo procedimiento en ambos lados, y los dos tests de contrato que sí sirven el `swagger.json` vía `StartupTestServer` están en verde.
- No se re-ejecutó la captura `dotnet run`; la equivalencia se apoya en la evidencia registrada por BUILDER y en el análisis anterior.

### Observaciones no bloqueantes

| # | Observación |
| --- | --- |
| 1 | `FacilityRepositoryContractShould` (sin BD) vive en `SqlFacilityRepositoryShould.cs`, pegada a la llave de cierre de la clase anterior sin línea en blanco. Cosmético; podría moverse a su propio fichero en un refactor posterior. |
| 2 | El test de clase comprueba métodos públicos que reciben `Facility`; un futuro método público que reciba otro tipo (p. ej. un DTO) y escriba sin lock no lo detectaría. Cubre lo que pide AC1.1. |

### Bloqueos

Ninguno.

## Ciclo 2 - Paso 02 (validación de dominio de Player y 400 común)

**Veredicto: APPROVED**

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Del paso 02: `Player.cs`, `DomainValidationException.cs` (nuevo), `Create/UpdatePlayerCommandHandler.cs`, `ExceptionToHttpResponseConverters.cs`, `ExceptionHandlingMiddleware.cs`, `ValidationFailedException.cs` (nuevo), `Startups/ValidationStartup.cs` (nuevo), `Startup.cs`, `TestFixture.cs` y tests de player/excepciones. Del paso 01 (ciclo 1, sin commit): `IFacilityRepository.cs`, `SqlFacilityRepository.cs`, `SqlFacilityRepositoryShould.cs`. Ajenos (preservados, no revisados): `MEMORIES.md`, `HARNESS-001/*`. |
| Sonda temporal `ZzCycle2ProbeShould.cs` | Sin rastro: no está en `git status`, ni en `Gaffer.Api.Tests/Acceptance/`, ni en `grep -rn ZzCycle --include=*.cs`. |
| Fuera de alcance | Sin cambios en `Gaffer.Api/Controllers/`, DTOs, migraciones, `SqlPlayerRepository.cs` ni cliente generado. |
| `TestFixture.cs` | `services.ConfigureValidationResponses()` añadido en el mismo incremento que en `Startup.cs`. Sin handlers ni repositorios nuevos. |
| Construcción de `Player` | `grep "new Player("` en `Gaffer.Api`: solo dentro de `Player.Create`; Dapper rehidrata por el constructor primario, que no valida (Q16). |
| Model binding | `InvalidModelStateResponseFactory` vía `PostConfigure<ApiBehaviorOptions>` solo para `PlayerController.Create/Update`; el resto cae en la factoría por defecto (facility intacta para el paso 03). |

### Evidencia por criterio (re-ejecutada por REVIEWER; `Quionis_DB` Up en `0.0.0.0:3306`)

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC2.0 | Tabla de caracterización en `STATUS.md` (ciclo 2), capturada antes del cambio en ambos hosts. Contrastada por REVIEWER en el pipeline de producción (ver "Comprobación del pipeline de producción"): `POST` facility sin dirección → 500 `Unknown error` por `NullReferenceException` en `FacilitiesController.Create` con BD real, lo que confirma la fila de facility. | Demostrado |
| AC2.1 | Rojo reproducido en un worktree desacoplado en `HEAD` (scratchpad, ya eliminado) con solo el diff de `PlayerFeaturesShould.cs` y `SqlPlayerRepositoryShould.cs`: `--filter "PlayerFeaturesShould\|SqlPlayerRepositoryShould"` → exit 1, 33/76 con error, la misma lista que BUILDER (15 `RejectTheCreationOfAPlayerWith(*)`, 6 `RejectAPartialUpdateWith(*)`, 4 `ReturnTheValidationFormat...`, 2 `ReportEveryInvalidField*`, 3 `ReplaceTheSecondLastNameOnlyWhenAValueIsSent`, `KeepThePlayerIdentical...`, `UpdateAPlayer`, `KeepReadingAndPartiallyUpdating...`). Verde en el árbol actual (filtro de 243, exit 0). Los 15 casos comprueban la clave `Error.Players.Create.Validation`, el campo en `Errors` y `COUNT(*)` de `Players` sin cambios. | Demostrado |
| AC2.2 | `CreateAPlayerWhoseValuesAreAtTheLimits` (100 caracteres, 1900-01-01, `TodayUtc.AddYears(-16)`, sin `SecondLastName`) → 201 y GET con los valores enviados; en verde. | Demostrado |
| AC2.3 | `RejectAPartialUpdateWithAnInvalidFieldAndChangeNothing` (6 casos con un campo válido junto al inválido; `after == before`), en rojo en `HEAD` y en verde ahora. `ReturnNotFoundWhenUpdatingAnUnknownPlayerWithAnInvalidBody` → 404 (verde también en `HEAD`, caracterización de Q21 a)). Producción: `PUT` con id desconocido y `firstName: ""` → 404 `Error.Players.Update.PlayerNotFound`. Los tests de TECHDEBT-002 siguen en verde salvo los cambios de Q17 listados en `STATUS.md`. | Demostrado |
| AC2.4 | `ReplaceTheSecondLastNameOnlyWhenAValueIsSent` (null, ausente, `{}` → original) y `KeepThePlayerIdenticalWhenUpdatedWithAnEmptyBody`: rojos en `HEAD`, verdes ahora. `""` y espacios → 400 en `RejectAPartialUpdateWith(SecondLastName *)`. El diff de los 4 tests de `00` coincide con la tabla "antes → después" de `STATUS.md`; no se debilitó ninguna otra aserción (`UpdateAPlayer` solo cambia la línea de `SecondLastName`). Handler: `SetSecondLastName` solo con valor no null y la firma pasa a `string`, así que no hay camino para borrar. Producción: `{"secondLastName":null}` → 200 y GET con `"Orig"`. | Demostrado |
| AC2.5 | `ReportEveryInvalidField*` (4 y 3 campos; la cadena de 101 caracteres no está en el cuerpo) y `ReturnTheValidationFormatForBodiesThatCannotBeBound` (JSON mal formado y `ZZZ_UNKNOWN` en POST/PUT): rojos en `HEAD`, verdes ahora. En producción: varios campos → `Errors` con `FirstName`, `LastName`, `BirthDate` y `Gender`; los mensajes no repiten el valor; `ZZZ_UNKNOWN` → `{"Gender":["The value is not valid."]}`; JSON mal formado → clave de la operación y mensaje genérico. | Demostrado |
| AC2.6 | Se aplica la rama `StartupTestServer` (OPT-032) porque el diff toca `Startup.cs`. Evidencia de BUILDER: `swagger.json` antes y después vía `StartupTestServer`, 21617 bytes, `cmp` exit 0, sha256 `be49f0ab...a5a6bfe`. REVIEWER no repitió la captura (la sonda ya no existe y no se crean tests). Se acepta porque `OpenApiContractShould` y `FacilitiesControllerContractShould` (`StartupTestServer`) están en verde, el diff no toca controladores ni DTOs, y `ConfigureValidationResponses` solo sustituye `ApiBehaviorOptions.InvalidModelStateResponseFactory`, que es una respuesta en tiempo de ejecución y NSwag no la lee. | Demostrado |
| AC2.7 | Rojo unitario reproducido en el worktree de `HEAD` con los diffs de `Unit/Domain`, `Unit/Commands` y `Unit/Exceptions`: build exit 1 (CS0246 `DomainValidationException`/`DomainValidationErrors`/`ValidationFailedException`, CS0103 `ValidationErrorKeys`, CS1501 `Create` con 7 argumentos y `SetBirthDate` con 2). En verde ahora (`PlayerShould`, `*PlayerCommandHandlerShould`, `ExceptionToHttpResponseConvertersShould` dentro del filtro de 243). Fecha de referencia controlable (`todayUtc`) en `Create` y `SetBirthDate`. | Demostrado |
| AC2.8 | `dotnet build gaffer-api.sln --no-incremental -v q` → exit 0, 0 Advertencia(s). `dotnet test gaffer-api.sln --no-build -v q` → exit 0, 413/413, 0 omitidos (324 → 413). Filtro del paso + `OpenApiContractShould\|FacilitiesControllerContractShould\|TestServerDependenciesShould\|FacilityFeaturesShould\|SearchPlayersEndpointShould` → exit 0, 243/243. | Demostrado |
| AC2.9 | `KeepReadingAndPartiallyUpdatingAStoredPlayerThatBreaksTheRules` (INSERT directo: `FirstName` de 150, `SecondLastName` vacío y 10 años; GET, search y PUT de solo `lastName` → 200 sin cambiar el resto) está en verde. En `HEAD` solo fallaba por Q17 (el PUT borraba `SecondLastName`), como indicó BUILDER. `SqlPlayerRepositoryShould.ReadAStoredPlayerThatBreaksTheCreationRulesWithoutValidatingIt` está en verde en `HEAD` y ahora. | Demostrado |

### Decisiones comprobadas

| Decisión | Comprobación |
| --- | --- |
| Q5/Q22 (16 años) | `CheckBirthDate`: `date.AddYears(16) > today` → error; el 29-feb pasa a 28-feb en años no bisiestos (test con 2084 → 2100). El límite exacto de 16 años es 201 (AC2.2) y un día menos es 400 (AC2.1). |
| Q6/Q17 (PUT) | Ausente/null no cambia ningún campo (incluido `SecondLastName`); `""`/espacios → 400 (antes se ignoraban o borraban). Verificado en tests y en producción. |
| Q16 | El constructor primario no valida; un player antiguo se lee, se busca y se actualiza parcialmente (AC2.9). |
| Q4/Q18 | `BuildValidationProblemDetails` es el único constructor del 400 (dominio y model binding): `ErrorTranslationKey` por operación (`Error.Players.Create/Update.Validation`), `Errors` con todos los campos inválidos y mensajes sin el valor recibido. |
| Q21 a) | Se lee primero, así que un id desconocido da 404 antes de validar (tests y producción). |

### Comprobación del pipeline de producción (`AddMvcCore`)

`dotnet run --no-build --no-launch-profile` en el puerto 5987, con `DatabaseConfig` apuntando a `GafferDB_Test` (cadena de user-secrets del proyecto de tests, no impresa). Un primer intento con el conector Dummy dio 500 por `NotImplementedException` en las lecturas y se descartó como evidencia. El player creado para la comprobación se borró con `DELETE` (200). El servidor está parado.

| Petición | Resultado |
| --- | --- |
| `POST` `firstName: ""` | 400 `Error.Players.Create.Validation`, `{"FirstName":["FirstName must not be empty or contain only whitespace."]}` |
| `POST` sin `firstName` | 400, `{"FirstName":["FirstName is required."]}` (lo rechaza el dominio; en el host de tests lo rechaza el model binding) |
| `POST` 101 caracteres + `lastName: ""` + fecha futura + `gender: 99` | 400 con los 4 campos; el cuerpo no contiene la cadena |
| `POST` `gender: "ZZZ_UNKNOWN"` / JSON mal formado | 400 con la clave de create y `The value is not valid.` |
| `PUT` JSON mal formado | 400 `Error.Players.Update.Validation` |
| `PUT` id desconocido + `firstName: ""` | 404 `Error.Players.Update.PlayerNotFound` |
| `PUT` `secondLastName: ""` + `firstName` válido / `gender: 99` | 400 de update con el campo correspondiente |
| `PUT` `secondLastName: null` + `GET` | 200; el GET conserva `firstName: "Rev"` y `secondLastName: "Orig"` (los PUT rechazados no cambiaron nada) |
| `POST /facilities` `{"name":"X"}` | 500 `Unknown error` (`NullReferenceException` en `FacilitiesController.Create`) |

### Hallazgo para ORCHESTRATOR y operador: el host de `TestFixture` no es equivalente a producción (no bloqueante para el paso 02)

| Aspecto | Valoración |
| --- | --- |
| Hecho | `TestFixture` llama a `AddMvcCore()` y además a `AddControllers()` (L85 y L112), lo que activa la validación DataAnnotations (incluido el `required` implícito de las referencias no anulables). Producción solo usa `AddMvcCore()`. Confirmado con BD real: `POST` facility sin dirección → 500 en producción; `FacilityFeaturesShould` (L118) ve 400. |
| Paso 02 | **La evidencia sigue siendo válida.** Los DTO de player no tienen atributos, así que en el host de tests solo divergen los casos de `FirstName`/`LastName` ausentes en el POST (los rechaza el model binding en lugar del dominio, con el mismo formato y la misma clave gracias a la factoría) y la entrada adicional `Body` que puede aparecer en `Errors`. REVIEWER comprobó en el pipeline de producción el camino de dominio de `firstName` ausente, el model binding (enum desconocido, JSON mal formado) y el PUT (tabla anterior). El resto de los casos llegan al dominio en ambos hosts. |
| Paso 03 | **Riesgo alto para la validez de la evidencia.** Los DTO de facility/gradas tienen `[Required]`/`[MaxLength]`, así que una aceptación en el host de `TestFixture` puede salir en verde por DataAnnotations mientras producción no rechaza nada (como ya ocurre con la dirección). Antes de empezar el paso 03 conviene decidir una de estas opciones: (a) alinear el host de tests con producción (quitar `AddControllers()` de `TestFixture`, revisando qué tests dependen de él), (b) activar las DataAnnotations en producción (`AddDataAnnotations()`), o (c) exigir en el paso 03 una aceptación sobre `StartupTestServer`/pipeline real para cada regla. Lo deciden el operador o PLANNER; REVIEWER no lo corrige. |

### Observaciones no bloqueantes

| # | Observación |
| --- | --- |
| 1 | En los errores de model binding, el nombre del campo sale de la ruta JSON de `ModelState`. Con JSON mal formado puede ser un nombre de propiedad enviado por el cliente (en PascalCase). No es el valor rechazado y la respuesta es `application/json`, pero conviene tenerlo en cuenta en el paso 03. |
| 2 | `ExceptionHandlingMiddleware` registra cada 400 de validación como `fail` con la traza completa. Ya era el comportamiento para las demás excepciones de 400, pero ahora habrá más ruido en el log. |
| 3 | En el PUT, los setters válidos modifican el `Player` en memoria antes de que `ThrowIfAny` lance. No se persiste nada (no se llama a `Update`), así que no tiene efecto. |
| 4 | "Hoy" se calcula con `birthDate.Date`. Una fecha con hora y desplazamiento que Newtonsoft convierta a hora local podría cambiar de día en el límite exacto de los 16 años. Es la semántica de fechas que ya existía, no algo introducido por este paso. |

### Bloqueos

Ninguno.

## Ciclo 3 - Paso 03a (validación de dominio de Facility y dirección; ayudante del `swagger.json`)

**Veredicto: APPROVED**

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Del plan: `Domain/Facility.cs`, `Domain/DomainValidationException.cs`, `Domain/Player.cs` (refactor), handlers de create/update, `FacilitiesController.cs` (solo mapeo, Q23), `ValidationFailedException.cs`, `Startups/ValidationStartup.cs`; tests `FacilityFeaturesShould.cs`, `Unit/Domain/FacilityShould.cs` (nuevo), handlers, `ExceptionToHttpResponseConvertersShould.cs`, `SqlFacilityRepositoryShould.cs`, `Acceptance/ServedOpenApiDocumentDump.cs` (nuevo). Ajenos (preservados, no revisados): `MEMORIES.md`, `HARNESS-001/*`. Redirecciones `03-*.md`/`04-*.md` sin cambios en el diff. |
| Fuera de alcance | Sin cambios en `TestFixture.cs`, `Startup.cs` (OPT-035), migraciones, DTOs, rutas, atributos, `Stand`/`/stands` (03b), `nswag.json` ni cliente generado. |
| DI `TestFixture.cs` | No requerida: no hay handlers nuevos. |
| Controlador (Q23) | Diff de 2 líneas: `request.Address is null ? null : new ...AddressCommand(...)` en `Create` y `Update`. Nada más. |
| Update dentro del lock (Q21 a)) | `BuildUpdatedFacility` se ejecuta en el callback de `SqlFacilityRepository.Update` (L210-216) antes de `WriteFacility`; la excepción sale del `ExecuteInTransactionAsync` → nada escrito; 404 antes que 400. |

### Evidencia por criterio (re-ejecutada por REVIEWER; `Quionis_DB` Up en `0.0.0.0:3306`)

Rojo reproducido en un worktree temporal `HEAD` (scratchpad, eliminado al terminar) con los tests nuevos copiados; el árbol de trabajo no se tocó.

| AC | Evidencia REVIEWER | Estado |
| --- | --- | --- |
| AC3a.0 | Ayudante `[Explicit]` estable (`ServedOpenApiDocumentDump.WriteTheServedSwaggerJsonToTheConfiguredPath`, variable `GAFFER_SWAGGER_DUMP_PATH`). Sin variable: exit 1 con `Set the environment variable GAFFER_SWAGGER_DUMP_PATH ... Nothing was written.` (falla antes de arrancar el servidor). Suite normal 574/574, 0 omitidos (no se ejecuta ni cuenta). Determinismo: BUILDER 2 capturas iguales; REVIEWER 2 capturas (HEAD y actual) con el mismo sha256. | Demostrado |
| AC3a.1 | Rojo HEAD: 34/34 `RejectTheCreationOfAnInvalidFacilityWithoutSavingIt` + `RejectCreatingAFacilityWithMissingAddress` con error. Verde: suite exit 0. Cubre vacío, espacios, máx+1, 1799-12-31, mañana, `Type` 99, `Capacity` -1, dirección ausente; filas `Facilities`/`Addresses` sin cambio. | Demostrado |
| AC3a.2 | Rojo HEAD: 34/34 `RejectTheUpdateOfAFacilityWithAnInvalidBodyAndChangeNothing`. Verde: clave `Error.Facilities.Update.Validation`, `GET` idéntico. `ReturnNotFoundWhenUpdatingAnUnknownFacilityWithAnInvalidBody` verde. Fusión (TECHDEBT-003) y lock (TECHDEBT-004) en verde en la suite. | Demostrado |
| AC3a.4 | `CreateAFacilityWhoseValuesAreAtTheLimits` (201) y `UpdateAFacilityWithValuesAtTheLimits` verdes; `GET` devuelve lo enviado. PUT devuelve 200, no 204 (ver decisión 1). | Demostrado |
| AC3a.5 | Rojo HEAD: `ReportEveryInvalidField...` y 4/4 `ReturnTheValidationFormatForBodiesThatCannotBeBound`. Verde: 5 campos en `Errors`, sin eco del valor; JSON mal formado y enum de texto desconocido → formato Q4 con clave de la operación. | Demostrado |
| AC3a.11 | `FacilityValidationInTheProductionPipelineShould` sobre `StartupTestServer` (`AddMvcCore`), 16 casos × POST/PUT. Rojo HEAD 32/32: **18 × 500 `Unknown error`, 7 × 201 y 7 × 200** (datos inválidos escritos en producción). Verde: 400, clave y campo, sin escritura. Divergencia de mensaje registrada en `STATUS.md` (obs. 1). | Demostrado |
| AC3a.7 | Rojo unitario HEAD: build exit 1, 12 × CS0117 + 8 × CS1501. Verde: `FacilityShould` (inválidos, límites, hoy, default UTC, todos a la vez, > 4 gradas, rehidratación, `Address.Create`), handlers (sin `Save`/`Update`, 404 antes que validación), converters con claves de facility. | Demostrado |
| AC3a.10 | `KeepReadingAndUpdatingAStoredFacilityThatBreaksTheRules` y `SqlFacilityRepositoryShould.ReadAStoredFacilityThatBreaksTheCreationRulesWithoutValidatingIt` verdes en HEAD (caracterización) y ahora. PUT válido → 200 (ver decisión 1). | Demostrado |
| AC3a.8 | Rama "comparación con ayudante" (el diff toca `FacilitiesController.cs` y `ValidationStartup.cs`): volcado HEAD y actual, ambos sha256 `be49f0ab...a5a6bfe`, `cmp` exit 0. `FacilitiesControllerContractShould` sin cambios y verde. Cliente generado sin tocar. | Demostrado |
| AC3a.9 | `dotnet build gaffer-api.sln --no-incremental -v q` exit 0, 0 Advertencia(s); `dotnet test gaffer-api.sln --no-build -v q` exit 0, 574/574, 0 omitidos (413 → 574); `CancellationToken.None` en `Gaffer.Api` = 0. Tests adaptados listados en `STATUS.md`. | Demostrado |

### Decisiones sobre los puntos que dejó BUILDER

| # | Punto | Decisión |
| --- | --- | --- |
| 1 | PUT correcto → 200, no 204 | **Errata del plan, no un incumplimiento.** `FacilitiesController.Update` devuelve `Task` sin resultado (ASP.NET responde 200 vacío) desde antes de este plan; el test previo `UpdateAFacilityFieldsAndAddressKeepingItsFourStands` ya exigía 200; cambiarlo rompería AC3a.8 (contrato idéntico) y Q20 a). AC3a.4/AC3a.10 se dan por cumplidos con 200. PLANNER puede corregir el texto (y revisar 03b). |
| 2 | Dirección ausente: `Detail` genérico, mensaje en `Errors.Address` | Aceptado. Q4 y el texto de Q23 piden el 400 del dominio con `Error.Facilities.Create/Update.Validation`; el `Detail` `Address is required` de `FacilityValidationException` nunca fue observable por HTTP (producción 500; host de tests 400 de ASP.NET) y el mensaje sigue en `Errors.Address`. El converter de `FacilityValidationException` se mantiene para el límite de gradas (03b). |
| 3 | Mensaje por campo distinto entre `TestFixture` y producción | No bloqueante: AC3a.11 solo exige registrarlo y que clave y campo coincidan, lo que sí aserta la suite. El texto concreto es deducido, no aserto; queda como riesgo conocido de OPT-035 ("Obviemos este punto"). |
| 4 | Ruta `Integration/SqlFacilityRepositoryShould.cs` | Errata del plan; el fichero real es `Unit/Repositories/SqlFacilityRepositoryShould.cs` (usa la BD de Docker). Sin impacto. |
| 5 | 2 tests adaptados y `!` en `UpdateFacilityCommandHandlerShould` | Correcto. `ThrowExceptionWhenAddressIsMissing` pasa de fijar un `NullReferenceException` (defecto) a `ValidationFailedException` con clave, campo y sin `Save`; `RejectCreatingAFacilityWithMissingAddress` se endurece (400 + clave + campo). Los `!` (L76, L228) solo silencian CS8602 por la nueva nulabilidad; mismas aserciones. |

### Observaciones no bloqueantes

| # | Observación |
| --- | --- |
| 1 | Imprecisión en `STATUS.md` (fila "Rojo aceptación"): dice que los 32 casos de producción daban **500**; REVIEWER mide 18 × 500, 7 × 201 y 7 × 200. El rojo es válido igualmente y el hallazgo es más grave (en producción se aceptaban y escribían campos ausentes/`null`), cosa que este paso corrige. |
| 2 | `Address.Create` solo lo usan los tests (producción valida la dirección a través de `Facility.Create`). Es API pública sin consumidor de producto; aceptable porque el plan pide reglas "en los caminos de creación de `Facility` y `Address`". |
| 3 | Con campos inválidos y > 4 gradas a la vez solo se informa de los campos (el límite lo comprueba el constructor después). Queda para 03b. |
| 4 | Documentar el ayudante en `.github/doc/common/test-stack.md` queda pendiente para DOCUMENTER u operador (OPT-034). |

### Bloqueos

Ninguno.

## Ciclo 4 - Paso 03b (validación de dominio de gradas anidadas y del sub-recurso `/stands`)

**Veredicto: APPROVED** (con una confirmación del operador recomendada en el checkpoint; ver decisión 1, no bloqueante)

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git status` | Del plan: `Domain/Facility.cs` (`Stand.Create`/`CheckFields`, `Facility.CheckRequestedStands`, límite como campo), `Domain/DomainValidationException.cs` (`Add` sin duplicados), handlers de create/update de facility y `FacilityStandCommandHandlers.cs`, `ValidationFailedException.cs` (clave `FacilitiesStands`), `Startups/ValidationStartup.cs` (2 entradas); tests `FacilityFeaturesShould.cs`, `FacilityStandsFeaturesShould.cs`, `Unit/Domain/StandShould.cs` (nuevo), `FacilityShould.cs`, 3 de handlers, converters, `SqlFacilityRepositoryShould.cs`. Plan: `00`/`03a`/`03b`/`05` (erratas y OPT-037-039 de PLANNER), `STATUS*.md`, `OPTIMIZATION.md`, `usage/BUILDER.md`. Ajenos (preservados, no revisados): `MEMORIES.md`, `HARNESS-001/*`. |
| Fuera de alcance | Sin cambios en `TestFixture.cs`, `Startup.cs` (OPT-035), controladores, DTOs, `SqlFacilityRepository.cs`, `ExceptionToHttpResponseConverters.cs` (el converter de `ValidationFailedException` es genérico), migraciones, `nswag.json` ni cliente generado. |
| DI `TestFixture.cs` | No requerida: ningún handler nuevo (`StandValidation` es un ayudante estático interno). |
| Orden | Sub-recurso: `StandValidation.Create` antes del repositorio → 400 antes que 404 (Q21 a)). `PUT /facilities/{id}`: validación dentro del callback del lock → 404 antes que 400 y rollback. `DuplicatedStand` (lanzado por `MergeStands`) gana a los errores de grada acumulados (obs. 4 de BUILDER): sin cambio respecto a hoy. |
| Rehidratación (Q16) | Constructor primario de `Stand` intacto; `Facility.Create` no revalida las gradas que recibe; solo se validan las enviadas (`CheckRequestedStands`). |

### Evidencia por criterio (re-ejecutada por REVIEWER; `Quionis_DB` Up en `0.0.0.0:3306`)

Rojo reproducido sobre `git archive HEAD` en el scratchpad (sin worktree ni cambios en el árbol de trabajo; eliminado al terminar), con los tests nuevos copiados: aceptación exit 1, 32/199 con error (mismo recuento que BUILDER).

| AC | Evidencia REVIEWER | Estado |
| --- | --- | --- |
| AC3b.1 | Rojo HEAD 3/3 `RejectTheCreationOfAFacilityWithAnInvalidStandWithoutSavingIt` (espacios, 101, capacidad -1; un representante por regla, OPT-037). Verde: 400 `Error.Facilities.Create.Validation`, campo `Stands[1].*`, filas sin cambio. | Demostrado |
| AC3b.2 | Rojo HEAD 3/3 `RejectTheUpdateOfAFacilityWithInvalidStandsAndChangeNothing` + los 2 `RejectAnUpdateLeavingMoreThanFourStands...` (clave de create). El caso `5 stands` ya era 400 en `TestFixture` (`[MaxLength(4)]`); su equivalente de producción sí estaba en rojo (AC3b.11). Verde: clave de update, `GET` idéntico; fusión (`DiscardSilently...`) y lock en verde en la suite. | Demostrado |
| AC3b.3 | Rojo HEAD 6/6 `RejectAnInvalidStandWithoutChangingTheStands` (POST/PUT × 3) y 2/2 `RejectAnInvalidStandBeforeLookingForTheFacility`. Verde: 400 `Error.Facilities.Stands.Validation`, `GET .../stands` igual; facility inexistente → 400. | Demostrado |
| AC3b.4 | `CreateAndUpdateAFacilityWithStandsAtTheLimits` (201/200) y `CreateAndReplaceAStandWithValuesAtTheLimits` (201/204) verdes en HEAD (caracterización) y ahora; `GET` devuelve lo enviado. | Demostrado |
| AC3b.5 | Rojo HEAD: `ReportEveryInvalidField...` (sin `Stands[i].*`) y 4/4 `ReturnTheValidationFormatForStandBodiesThatCannotBeBound`. Verde: 7 campos con índice en POST y PUT, sin eco del valor; JSON mal formado y tipo incorrecto en `/stands` → formato Q4 con clave de gradas, sin `ZZZ_NOT_A_NUMBER`. | Demostrado |
| AC3b.11 | `FacilityValidationInTheProductionPipelineShould` + `DivergentStandBodies` (6) y `FacilityStandValidationInTheProductionPipelineShould` (4) sobre `StartupTestServer`. Rojo HEAD 10/10 con error. Verde: 400 con clave y campo, sin escritura. Diferencias de mensaje por host en `STATUS.md` (obs. 1). Capa limitada a los casos divergentes (OPT-037). | Demostrado |
| AC3b.6 | `RejectAStandNameLongerThanItsColumnWithAValidationError` (300 caracteres): rojo HEAD, verde 400 de gradas sin escritura. `LeaveNoRowsWhenTheStandInsertFails` y `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` sin cambios en el diff y verdes. | Demostrado |
| AC3b.7 | Rojo unitario HEAD: build exit 1, 9 × CS0117 + 3 × CS7036 (coincide con BUILDER). Verde: `StandShould` con la matriz completa (vacío, espacios, 101, `null`, -1, `int.MinValue`; 1/100 caracteres, 0, `int.MaxValue`; varios campos sin eco; prefijo; rehidratación); handlers: create/update de facility (índice, `Stands`, sin `Save`/`Update`) y POST/PUT de `/stands` (`StandName`, `StandCapacity`, título y clave, sin `AddStand`/`UpdateStand`); converters con la clave de gradas. | Demostrado |
| AC3b.10 | `KeepReadingKeepingAndDeletingAStoredStandThatBreaksTheRules` (GET 200, PUT válido 200 conservando la grada, DELETE 204) y `SqlFacilityRepositoryShould.ReadAStoredStandThatBreaksTheCreationRulesWithoutValidatingIt` verdes en HEAD y ahora; `KeepAStoredStandThatBreaksTheRulesWhenItIsNotSent` (unitario). | Demostrado |
| AC3b.8 | Rama "comparación con ayudante" (diff en `ValidationStartup.cs`): volcado REVIEWER con `ServedOpenApiDocumentDump` exit 0, 21617 bytes, sha256 `be49f0ab...a5a6bfe` = captura "antes" de BUILDER y de los ciclos 2-3. `FacilitiesControllerContractShould` + `OpenApiContractShould` exit 0, 15/15. Cliente sin tocar. | Demostrado |
| AC3b.9 | Build `--no-incremental -v q` exit 0, 0 Advertencia(s); suite exit 0, 637/637, 0 omitidos (574 → 637); `CancellationToken.None` = 0. Tests adaptados listados en `STATUS.md`. | Demostrado |

Reparto por capa (OPT-037): cumple. Matriz completa solo en `StandShould`; handlers con un caso por regla y handler; aceptación con un representante por tipo y operación (+ `5 stands` en el PUT); producción solo con los casos divergentes. OPT-038: rojo por clase como `n × estado` en `STATUS.md`, verificado. OPT-039: grep de usos registrado; sin cambios de firma ni de nulabilidad de tipos compartidos.

### Decisiones sobre los cambios de comportamiento declarados por BUILDER

| # | Cambio | Decisión |
| --- | --- | --- |
| 1 | `PUT /facilities/{id}` con > 4 gradas **enviadas** sobre una facility con 4: antes 200 con descarte silencioso (producción), ahora 400 `Error.Facilities.Update.Validation` con `Errors.Stands` | **Autorizado por el plan; no bloqueante.** (a) AC3b.2 exige "5 gradas" → 400 con clave de update, sin condicionarlo a las gradas existentes; (b) AC3b.11 exige que producción rechace "5 gradas (`[MaxLength(4)]`) en `PUT` de facility" con 400, clave, campo y sin escritura, es decir, el comportamiento de `[MaxLength(4)]`, que no depende de las gradas guardadas; (c) en TECHDEBT-003 paso 02 el orden diseñado era "(1) validación de la petición (`[MaxLength(4)]`) ... (3) clasificación/descarte R-MAX": R-MAX solo se aplica a peticiones de ≤ 4 gradas, y el 200 silencioso con > 4 era un efecto de la divergencia OPT-035 (`AddMvcCore` no evalúa DataAnnotations), no del contrato. TECHDEBT-004 Q11 a) ("conserva su fusión") se respeta: `DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands` (≤ 4 enviadas) sigue verde. **Recomendación para el checkpoint**: que ORCHESTRATOR informe al operador de que es un cambio visible en producción (un cliente que envíe 5+ gradas a una facility llena pasa de 200 a 400). DOCUMENTER debe reflejarlo en `doc/features/facility-update.md` (L17, L26) y en `doc/features/technical/facility-update.md` (L114), que hoy solo describen el descarte por grada. |
| 2 | > 4 gradas anidadas: `Detail` "Stands cannot exceed 4" sin `Errors` → `Detail` genérico + `Errors.Stands` = "Stands cannot exceed 4." | Aceptado. Q4/Q18 a) y AC3b.11 piden clave y **campo**; el texto se conserva en `Errors.Stands` y la clave de create no cambia. "Se mantiene el `Detail` actual de máximo de gradas" del contrato de 03b se refiere a `MaxStandsExceeded` del sub-recurso, que no cambia (`FacilityMaxStandsExceededException` sin tocar). La guarda del constructor (`FacilityValidationException`) sigue existiendo y cubierta. |
| 3 | `DomainValidationErrors.Add` no repite mensajes idénticos del mismo campo | Aceptado. Evita el doble "Stands cannot exceed 4." (enviadas y total fusionado). Ninguna regla de Player/Facility/Address emitía mensajes repetidos en un campo; suite verde. Cubierto por `ContainSingle` en los tests de update. |
| 4 | 8 tests adaptados y un test HTTP nuevo de 300 caracteres | Correcto. Las adaptaciones endurecen las aserciones (de "400 o 422" o excepción por mensaje a 400 + clave exacta + campo + sin escritura); el paso de clave create → update es el de Q4 previsto en AC3b.2. El test de 300 caracteres es nuevo porque TECHDEBT-004 AC4.11 no dejó uno HTTP; cubre AC3b.6. |

### Observaciones no bloqueantes

| # | Observación |
| --- | --- |
| 1 | No hay test de aceptación HTTP del caso "facility con 4 gradas + 5 enviadas" (decisión 1); solo el unitario `RejectMoreThanFourSentStandsEvenWhenTheFacilityIsFullWithoutUpdating`. El comportamiento HTTP se deduce de ese test y de AC3b.11 (facility recién creada). Basta para el plan; conviene añadirlo si el operador confirma el cambio. |
| 2 | Un elemento `null` dentro de `stands` (`"stands":[null]`) sigue dando `NullReferenceException` en el mapeo de `FacilitiesController` (L55, L85; deducido del código, no probado; preexistente, fuera de los AC de 03b). |
| 3 | Los mensajes por host de AC3b.11 se deducen del código y no se asertan (igual que en 03a; riesgo conocido de OPT-035). |

### Bloqueos

Ninguno.

## Ciclo 5 - Paso 05 (`/health/live` y `/health/ready`; se mantiene el fallback a Dummy)

Veredicto: **APPROVED**

### Alcance revisado

| Elemento | Resultado |
| --- | --- |
| `git diff --stat` + `git status` (OPT-040) | Producto del paso: `Startup.cs`, `Startups/DatabaseStartup.cs`, `Shared/ConnectionProviders/SqlConnectionProvider.cs`, nuevo `Infrastructure/DatabaseHealthCheck.cs` (diff completo leído). Tests: nuevos `Acceptance/HealthCheckShould.cs`, `Unit/HealthChecks/DatabaseHealthCheckShould.cs`; modificados `DatabaseStartupShould.cs`, `SqlConnectionProviderShould.cs`, `TestFixture.cs`, `StartupTestServer.cs` (inventario por `grep` de `[Test]`/`[TestCase`/`public .*Task`; abiertos solo los rangos de AC5.2, AC5.4 y AC5.5). Resto del diff = ciclos 2-4 ya aprobados y cambios ajenos (MEMORIES.md, HARNESS-001, ficheros de plan), no revisados |
| Sin paquete nuevo (Q11) | `Microsoft.AspNetCore.Diagnostics.HealthChecks` / `Microsoft.Extensions.Diagnostics.HealthChecks` del framework compartido; ningún `.csproj` en el diff |
| Fuera de alcance | `ci.yml` sin tocar (Q15 a)); cliente generado no tocado ni verificado |
| `TestFixture.cs` | Registra `DatabaseProviderSelection(Sql)` junto a su `SqlConnectionProvider`, igual que producción con BD alcanzable. Su `TestStartup` no llama a `AddHealthChecks`, así que allí el registro es inerte pero coherente; `TestServerDependenciesShould` en verde |

### Evidencia por criterio (re-ejecutada por REVIEWER; `Quionis_DB` Up en `0.0.0.0:3306`)

| AC | Evidencia | Resultado |
| --- | --- | --- |
| Rojo aceptación | Copia del árbol actual en scratchpad con los 3 ficheros de producto y los 3 de test del paso devueltos a `HEAD` (sus diffs solo contienen el paso 05) y sin `DatabaseHealthCheck.cs` ni `Unit/HealthChecks`; filtro `~HealthCheckShould` | exit 1; 4 × con error, todos con `404` en `/health/live` (ruta inexistente). Coincide con BUILDER |
| Rojo unitario | Misma copia con los tests del paso restaurados; `dotnet build Gaffer.Api.Tests -v q` | exit 1; `DatabaseHealthCheckShould.cs` 3 × CS0246. Coincide con BUILDER |
| Verde del paso | Filtro `HealthCheckShould`, `DatabaseHealthCheckShould`, `DatabaseStartupShould`, `SqlConnectionProviderShould`, `TestServerDependenciesShould`, `FacilitiesControllerContractShould`, `OpenApiContractShould` con `verbosity=normal` | exit 0; 73 correctas (58 del paso + 15 de contrato) |
| AC5.1 | `ReportLiveAndReadyAsHealthyWhenTheDatabaseIsReachable` (`StartupTestServer`, pipeline de producción) | `GET /health/live: 200 Healthy`; `GET /health/ready: 200 Healthy` |
| AC5.2 | `ReportReadinessAsUnhealthyWhenTheApiFellBackToTheDummyProvider...`: cadena inalcanzable, proveedor resuelto `DummyConnectionProvider` con `TestConnection()` = `true` afirmado en el propio test | `/health/live` 200 `Healthy`; `/health/ready` **503 `Unhealthy`** pese al `true` del Dummy |
| AC5.3 | `ReportReadinessAsDegradedWhenTheDatabaseIsDisabled` y `...WhenThereIsNoConnectionString` | 2 × `/health/ready: 200 Degraded`; `/health/live` 200 (Q13 a)) |
| AC5.4 | Matriz unitaria `ReportTheStatusOfTheChosenProvider`: 10 casos Dummy (5 resultados de sonda × 2 motivos; sonda nunca llamada) + 4 Sql; `ReportUnhealthyWhenTheFallbackDummyIsUsedEvenIfTheDatabaseAnswersAgain` | Fallback → `Unhealthy` con sonda correcta; configuración → `Degraded`; Sql correcta → `Healthy`; Sql fallida, con excepción o bloqueada → `Unhealthy`. Se decide por el motivo registrado, no por `TestConnection()` del proveedor, como exige el contrato |
| AC5.5 seguridad | `ShouldNotExposeAnything` (sin `Exception`, `Data` vacío, descripción sin `db-host`/`gaffer-user`/`s3cr3t`/`Pwd`/`MySqlException`) en toda la matriz, incluido Sql con excepción sensible; aserción sobre el cuerpo HTTP en AC5.2 (claves de la cadena, `Exception`, `MySql`). El escritor por defecto solo emite el estado | Cumple |
| AC5.5 tiempo y cancelación | `GiveUpAsUnhealthyWithinFiveSecondsWhenTheProbeDoesNotAnswer` (sonda que ignora el token, timeout por defecto) y `StopWithTheRequestWhenItsTokenIsCancelledDuringTheProbe` | `Unhealthy` tras **5015 ms** (BUILDER: 5009 ms) con el token de la sonda cancelado; cancelación de la petición → `OperationCanceledException` tras 100 ms (BUILDER: 102 ms). `SqlConnectionProviderShould` +2: token ya cancelado → OCE con la conexión cerrada; token vivo → `true` |
| AC5.6 | Rama "comparación con el ayudante" (el diff toca `Startup.cs` y `DatabaseStartup.cs`): `GAFFER_SWAGGER_DUMP_PATH=... dotnet test --filter ~ServedOpenApiDocumentDump` | exit 0; 21617 bytes; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe`, igual que antes (STATUS ciclo 5 y ciclos 3-4); `grep -c /health` = 0; contratos 15/15 dentro del filtro del paso |
| AC5.7 | `DatabaseStartupShould` +4 (`Sql`, `DummyByFallback`, `DummyByConfiguration` × 2); `git diff -U0` sin líneas `-` en aserciones existentes | En verde; L75/L86/L110 sin cambios |
| AC5.8 | `dotnet build gaffer-api.sln --no-incremental -v q`; `dotnet test gaffer-api.sln --no-build -v q`; `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` | build exit 0, 0 Advertencia(s); suite exit 0, 665/665 (637 → 665, +28), 0 omitidos; 0 apariciones |

### Decisiones sobre los puntos que dejó BUILDER

| # | Punto | Decisión |
| --- | --- | --- |
| 1 | Timeout interno exactamente 5 s; medido 5009/5015 ms; el test admite < 5,5 s | **Cumple.** La tabla de contrato de 05 fija el umbral como "comprobación > 5 s → 503", es decir, el corte se produce a los 5 s; Q11 fija "5 s como máximo" para la comprobación, no para la latencia total de la petición. Los 9-15 ms son la planificación del temporizador y la continuación. No hace falta bajar el límite. Si el operador quiere una cota estricta de la respuesta HTTP, sería una decisión nueva (p. ej. 4,5 s internos) |
| 2 | Ficheros fuera de la tabla de alcance | **Dentro del alcance razonable.** `Unit/HealthChecks/DatabaseHealthCheckShould.cs`: la tabla ya pide "`Unit/` (comprobación de BD)" sin fichero concreto. `SqlConnectionProvider.TestConnection(CancellationToken)`: aditiva y necesaria para que la sonda real respete el token (AC5.5); `IDatabaseConnectionProvider` y `TestConnection()` sin cambio de firma (OPT-039: 7 usos intactos); sus 2 tests en `SqlConnectionProviderShould`. `StartupTestServer.StartAsync(bool, string?)` + `Services`: infraestructura de test necesaria para AC5.2-AC5.3 con el pipeline de producción (OPT-022); `StartAsync(string)` delega y sus usos no cambian |

### Comprobaciones adicionales

| Comprobación | Resultado |
| --- | --- |
| Confirmación del operador sobre el ciclo 4 en `STATUS.md` | Presente en "Decisiones del operador sobre ciclos cerrados": literal "Apruebo el P4-1 y 2, y confirmo el 400, luego lanza el paso 5", con su alcance (PUT con > 4 gradas sobre una facility con 4 → 400) |
| Q12 | El proveedor elegido no cambia (Dummy por fallback se mantiene); readiness lo marca `Unhealthy` sin sondear |
| Q14 | Endpoints mapeados con `MapHealthChecks` fuera de `/api/v{version}`, sin metadatos de ApiExplorer; ausentes del swagger servido |

### Observaciones no bloqueantes

| # | Observación |
| --- | --- |
| 1 | `/health/ready` es anónimo y, con el motivo `Sql`, abre una conexión nueva por petición sin caché ni limitación de frecuencia (BUILDER obs. 3). Fuera del plan; candidato a deuda técnica si se expone públicamente |
| 2 | Si una sonda ignorase el token, `WaitAsync` acota la espera pero la tarea sigue en segundo plano. La sonda real (`OpenAsync`/`ExecuteScalarAsync` con token) sí lo respeta, así que no hay fuga práctica |
| 3 | Una excepción de la sonda se convierte en `Unhealthy` sin registrarse en el log; dificulta el diagnóstico aunque evita exponerla. Podría añadirse un `LogWarning` sin incluirla en el resultado |
| 4 | `DatabaseStartup.cs` pierde el BOM de la línea 1 y gana salto de línea final; cosmético |
| 5 | La celda Sql × "cancelada por el llamador" no está en la matriz `TestCaseSource`; la cubre `StopWithTheRequestWhenItsTokenIsCancelledDuringTheProbe` (propaga OCE, como hace el servicio de health checks del framework) |

### Bloqueos

Ninguno.
