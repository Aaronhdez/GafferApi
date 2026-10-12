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
