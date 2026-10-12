# STATUS - TECHDEBT-005

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 3 |
| Aprobación del plan | Aprobado por el operador el 2026-10-11 (transmitido por ORCHESTRATOR): "vamos con el 2" |
| Pasos completados | 01 (APPROVED en ciclo 1); 02 (APPROVED en ciclo 2); 03a (APPROVED en ciclo 3) |
| Pasos bloqueados | Ninguno (Q23 respondida 2026-10-12; Q18-Q21 con valor por defecto a)) |
| Siguiente | OPTIMIZER ciclo 3; checkpoint humano (ciclo 4: paso 03b) |
| Bloqueos | Ninguno |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | AC1.1-AC1.6 (rojo 2/2 → verde; suite 322 → 324; swagger idéntico) | sin commit registrado (rama `TECHDEBT-005`); detalle en `STATUS-archive.md` |
| 2 | 02 Validación de dominio de Player (Q17 `SecondLastName`) | APPROVED | AC2.0-AC2.9 (rojo aceptación 33/76 + rojo unitario por compilación → verde; suite 324 → 413; swagger vía `StartupTestServer` idéntico) | sin commit registrado (rama `TECHDEBT-005`, `e28e545` contiene el paso); detalle en `STATUS-archive.md` |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED |
| 2 | 02 Validación de dominio de Player (incluye el cambio de `SecondLastName` de Q17 respecto a TECHDEBT-002) | APPROVED |
| 3 | 03a Ayudante de volcado del `swagger.json` (OPT-034) + validación de dominio de Facility y dirección (create/update) | APPROVED |
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
| 3 | BUILDER | 2026-10-12T01:36:12Z | 2026-10-12T01:48:33Z |
| 3 | REVIEWER | 2026-10-12T01:49:01Z | 2026-10-12T01:52:22Z |

## Comandos y resultados

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

## Documentación cambiada

Ninguna todavía.
