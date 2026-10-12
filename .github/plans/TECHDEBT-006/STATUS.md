# STATUS - TECHDEBT-006

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 2 |
| Aprobación del plan | Aprobado por el operador ("Apruebo el plan, espera a que cree la rama"); rama `TECHDEBT-006` creada por el operador |
| Pasos completados | 01 APPROVED (ciclo 1, REVIEWER); 02 APPROVED (ciclo 2, REVIEWER) |
| Pasos bloqueados | Ninguno. Q8, Q10, Q14 y Q17 respondidas por el operador (2026-10-12); ninguna pregunta abierta bloquea un ciclo. El push tras el ciclo 2 requiere la acción del operador de Q14 (`00`, "Acciones del operador") |
| Siguiente | OPTIMIZER, ciclo 2; después, checkpoint humano (paso 03 en el ciclo 3) |
| Bloqueos | Ninguno |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 | APPROVED | AC1.1-AC1.14 (suite 665 → 688; swagger sha256 `be49f0ab…a5a6bfe` idéntico) | `db8d730` (detalle en `STATUS-archive.md`) |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 Limpieza, operación, CI, observabilidad, `null` en `stands` y test "4 gradas + 5 enviadas" (sin contrato) | APPROVED |
| 2 | 02 `ProblemDetails` en camelCase con forma RFC 7807 (contrato; MAJOR 3.0.0) | APPROVED |
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
| 2 | BUILDER | 2026-10-12T12:52:12Z | 2026-10-12T13:01:32Z |
| 2 | REVIEWER | 2026-10-12T13:03:04Z | 2026-10-12T13:07:43Z |

## Comandos y resultados

| Ciclo | Paso | Comando / acción | Resultado |
| --- | --- | --- | --- |
| 2 | Inicio | Ciclo 1 colapsado en el índice; su detalle (comandos + sección BUILDER) movido literalmente a `STATUS-archive.md` (OPT-026, OPT-029) | - |
| 2 | Preflight | `docker ps` | `Quionis_DB` Up, `0.0.0.0:3306->3306/tcp`; tests contra `GafferDB_Test` |
| 2 | Línea base | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro OPT-006) | exit 0; 0 Advertencia(s), 0 Errores |
| 2 | Línea base | `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 688/688 (30 s) |
| 2 | AC2.6 antes | `GAFFER_SWAGGER_DUMP_PATH=<scratch>/swagger-before.json dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` (antes de editar) | exit 0; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` |
| 2 | Resumen del rojo (OPT-041, definido una vez para el ciclo 2) | `red.sh "<F>" --no-build` (scratchpad): `dotnet test Gaffer.Api.Tests --no-build --filter "<F>" --logger "console;verbosity=normal" > red.out; echo exit=$?; grep -oE "^ +(Correctas\|Con error\|Omitidas) [A-Za-z0-9_]+\|error CS[0-9]+" red.out \| sed (solo el estado) \| sort \| uniq -c`, una ejecución por clase (`<F>` = `FullyQualifiedName~<Clase>`); causa con `grep -oE 'Expected members {...} to contain' red.out \| uniq -c` | Reutilizado en cada rojo/verde de abajo |
| 2 | AC2.1/AC2.2 rojo | `red.sh` por clase tras adaptar los tests (antes de tocar `Serialize`) | exit 1 en las 6 clases: `PlayerFeaturesShould` 31 × Con error / 17 × Correctas; `FacilityFeaturesShould` 91 / 21; `FacilityValidationInTheProductionPipelineShould` 44 / 0 (incluye el test nuevo AC2.2); `FacilityStandsFeaturesShould` 16 / 9; `SearchPlayersEndpointShould` 4 / 7; `DatabaseFailureShould` 1 / 5. Causa única en las 187: `Expected members {"Type", "Title", "Status", "Detail", "Instance", "Extensions"} to contain {"type", ...}` (187 × ) |
| 2 | AC2.3/AC2.4 rojo | `red.sh` por clase | exit 1: `ExceptionToHttpResponseConvertersShould` 5 × Con error (los 5 tests de `Serialize` que leen el JSON; `LeaveTheProblemDetailsInMemoryUnchangedWhenSerializing` verde = caracterización), 52 × Correctas; `ExceptionHandlingMiddlewareShould` 4 × Con error, 6 × Correctas; `ValidationStartupShould` 1 × Con error (misma causa: miembros PascalCase + `Extensions`) |
| 2 | AC2.3/AC2.4 verde | `red.sh "...ExceptionToHttpResponseConvertersShould\|...ExceptionHandlingMiddlewareShould\|...ValidationStartupShould"` | exit 0; 68 × Correctas |
| 2 | AC2.1/AC2.2 verde | `red.sh` por clase | exit 0: 48, 112, 44, 25, 11 y 6 × Correctas |
| 2 | AC2.3 grep | `grep -rn "JsonConvert.SerializeObject" Gaffer.Api --include=*.cs \| wc -l`; `grep -rn "ExceptionToHttpResponseConverters.Serialize(" Gaffer.Api --include=*.cs \| wc -l` | 0; 2 (middleware y `ValidationStartup`, sin cambios en ambos) |
| 2 | AC2.7 build | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro OPT-006) | exit 0; 0 Advertencia(s), 0 Errores |
| 2 | AC2.6 después | volcado `swagger-after.json` + `sha256sum` + `cmp` | exit 0; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` idéntico; `cmp` exit 0. Rama OPT-030 **no** se activa |
| 2 | AC2.7 suite | `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 697/697 (30 s); 688 + 9 nuevos |
| 2 | AC2.7 git | `git status --short Gaffer.Api.Client Gaffer.Api.Tests/TestFixture.cs Directory.Build.props nswag.json \| wc -l` | 0 (cliente, `TestFixture`, versión y `nswag.json` sin cambios) |
| 2 | Impacto externo | `ls` + `grep -rlE "ErrorTranslationKey\|errorTranslationKey\|Extensions\|ProblemDetails\|Gaffer.Api.Client"` en `quionis-bff-api` y `quionis-frontend` (sin `node_modules`) | `quionis-bff-api`: directorio vacío, no verificable. `quionis-frontend`: 0 ficheros. Sin ediciones |
| 2 | REVIEWER | `docker ps`; build `-v q`; `dotnet test Gaffer.Api.Tests --no-build` | `Quionis_DB` Up 3306; exit 0, 0 Advertencia(s); exit 0, 697/697 |
| 2 | REVIEWER rojo (OPT-033) | worktree de `HEAD` + diff de tests + 2 ficheros nuevos; `dotnet test --filter` de las 9 clases del paso | exit 1; 197 × Con error (192 por miembros PascalCase/`Extensions`); worktree eliminado |
| 2 | REVIEWER sonda | `StartupTestServer` (BD real e inalcanzable), 8 peticiones, con el producto de `HEAD` y del diff | Forma RFC 7807 camelCase en 400 validación/binding/filtro, 404 y 500; valores idénticos (detalle en `REVIEW.md`) |
| 2 | REVIEWER AC2.6 | `ServedOpenApiDocumentDump` en `HEAD` y en el árbol + `sha256sum` + `cmp` | sha256 `be49f0ab…a5a6bfe` en los dos; `cmp` exit 0 |
| 2 | REVIEWER veredicto | `REVIEW.md`, ciclo 2 | APPROVED; 4 hallazgos no bloqueantes |

## Ciclo 2 - Paso 02 (BUILDER)

Decisiones: Q10 RFC 7807 (raíz, sin `extensions`); Q11 a) camelCase por segmento; Q12 a) `null` conservado; Q13 a) `application/json`; Q14: sin tocar `GlobalPackageVersion`, `nswag.json` ni `Gaffer.Api.Client` (acción del operador). `TestFixture.cs` sin cambios (Q8).

Diseño: un único punto, `ExceptionToHttpResponseConverters.Serialize`, construye un `JObject` (Newtonsoft, sin paquetes nuevos): `type`, `title`, `status`, `detail`, `instance` y cada extensión en la raíz con nombre camelCase; un valor `IReadOnlyDictionary<string, string[]>` (`Errors`) se escribe con cada clave en camelCase por segmento (primera letra en minúscula de cada segmento separado por `.`). El `ProblemDetails` en memoria no cambia (claves `ErrorTranslationKey`/`Errors` y campos PascalCase del dominio y de `ValidationStartup.FieldName`), así que `ValidationStartup.cs`, `ExceptionHandlingMiddleware.cs`, `FieldName` y `DomainValidationErrors` no se tocan. Rama de contrato (OPT-032 + OPT-034): el paso nombra `ValidationStartup.cs`, así que se volcó el swagger antes y después aunque al final no se editó (sha256 idéntico).

Grep de usos (OPT-039):

| Tipo / miembro | Comando | Usos | Adaptados |
| --- | --- | --- | --- |
| `Serialize`, `BuildValidationProblemDetails`, `FieldName`, `DomainValidationErrors`, `JsonConvert.SerializeObject` | `grep -rn "Serialize(\|BuildValidationProblemDetails\|FieldName\|DomainValidationErrors\|JsonConvert.SerializeObject" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | `Serialize` 2 (middleware, `ValidationStartup`); `BuildValidationProblemDetails` 2; `FieldName` 1 en `ValidationStartup` (+2 propiedades homónimas de excepciones de filtro, sin relación); `DomainValidationErrors` 12 producto + 6 tests | Firma de `Serialize` sin cambios: 0 usos que adaptar |

| AC | Evidencia | Resultado |
| --- | --- | --- |
| AC2.1 | Helper nuevo `TestShared/ProblemJson.cs`: parsea el JSON crudo con `JObject` (sensible a mayúsculas) y exige los 5 miembros estándar en camelCase, que todos los miembros estén en {`type`,`title`,`status`,`detail`,`instance`,`errorTranslationKey`,`errors`} (por tanto sin `extensions`/`Extensions` ni PascalCase) y claves de `errors` sin mayúscula inicial en ningún segmento. Lo usan los helpers `ShouldBeAValidationProblem` (player y facility), `AssertRejectedWithoutChanges`, `AssertProblem` y los tests de 404/400 filtro/500. Representantes: 400 validación player, facility y `/stands`; 404 con clave (player update/delete); 404 sin clave (`ReturnNotFoundWhenGettingAnUnknownPlayer`: sin `errorTranslationKey` ni `errors`); 400 filtro no permitido; 500 BD (`DatabaseFailureShould`: `title`, `status` 500, sin `errorTranslationKey`) | Rojo (187 × Con error) → verde |
| AC2.2 | Producción (`StartupTestServer`): 400 de dominio = casos de `FacilityValidationInTheProductionPipelineShould` (`DivergentBodies`, `DivergentStandBodies`, `NullStands`, 4+5 gradas) con el helper nuevo; 400 de model binding = test nuevo `AnswerAMalformedBodyWithTheRfc7807ValidationProblem` (`{"name":"Arena",` → `title`, `status` 400, `instance`, `errorTranslationKey`, `errors` no vacío) | Rojo (44 × Con error) → verde |
| AC2.3 | Matriz de `Serialize` en `ExceptionToHttpResponseConvertersShould` (6 tests): cada miembro estándar con valor (cadena exacta), todos `null` (cadena exacta `{"type":null,"title":null,"status":null,"detail":null,"instance":null}`), sin extensiones (exactamente 5 miembros), `errorTranslationKey` sola, `errors` con `FirstName`→`firstName`, `Address.Street`→`address.street`, `Stands[1].StandName`→`stands[1].standName`, `Stands[0]`→`stands[0]` (mensajes intactos), y `ProblemDetails` en memoria sin cambios. `JsonConvert.SerializeObject` en `Gaffer.Api` → 0 | Rojo (5 × Con error) → verde |
| AC2.4 | Middleware: excepción convertida (`ReturnABadRequestWhenAFacilitySearchUsesAFilterThatIsNotPermitted` y nuevo `WriteAConvertedValidationFailureWithItsKeyAndErrorsAtTheRoot`), desconocida 500 (`ReturnAnUnknownErrorForAnUnmappedException`: `title`, `status`, sin `errorTranslationKey`/`errors`). Model binding: nuevo `Unit/Startups/ValidationStartupShould` (registros reales de `Startup`, `InvalidModelStateResponseFactory` con `request.firstName`, `request.address.street`, `request` → `firstName`, `address.street`, `body`; sin eco del mensaje del binder) | Rojo (4 + 1 × Con error) → verde |
| AC2.5 | Valores sin cambios: claves `Error.*`, estados, `title`/`detail` idénticos (solo cambia la ruta de lectura y los nombres de campo esperados). Ningún test borrado; los nombres visibles de los casos parametrizados se conservan (`display` en `AddressTextCases`/`DivergentBodies`). Tabla abajo | Verde |
| AC2.6 | sha256 `be49f0ab…a5a6bfe` antes y después, `cmp` idéntico; OPT-030 no se activa. `Gaffer.Api.Client/` sin tocar | Verde |
| AC2.7 | Suite 688 → 697 (+9: `Serialize` 6, middleware 1, `ValidationStartupShould` 1, producción 1); build 0/0; `git status` 0 líneas en cliente/`TestFixture`/versión/`nswag.json` | Verde |

Tests ajustados (AC2.5; `git diff --numstat` +/−):

| Fichero | Cambio | Antes → después |
| --- | --- | --- |
| `Acceptance/PlayerFeaturesShould.cs` (+43/−42) | 404 sin clave (5 aserciones), 404/400 con clave (3), helper (4), campos esperados de 21 casos + 2 tests de varios campos + 2 `TestCase` | `["Status"]`/`["Extensions"]["ErrorTranslationKey"]`/`"FirstName"` → `["status"]`/`errorTranslationKey`/`"firstName"` |
| `Acceptance/FacilityFeaturesShould.cs` (+62/−47) | `AssertRejectedWithoutChanges`, helper `ShouldBeAValidationProblem`, `Errors` de model binding, campos esperados de `InvalidBodies`, `DivergentBodies`, `InvalidNestedStands`, `TooManyStands`, `DivergentStandBodies`, `NullStands`, `SeveralInvalidFieldNames` y 5 llamadas; + test nuevo AC2.2 | `"Address.Street"`/`"Stands[1].StandName"`/`"Stands[0]"` → `"address.street"`/`"stands[1].standName"`/`"stands[0]"` |
| `Acceptance/FacilityStandsFeaturesShould.cs` (+10/−10) | `AssertProblem`, `Errors` de model binding, campos de 3 llamadas, 2 `TestCase`, `InvalidStands` | `"StandName"` → `"standName"` |
| `Acceptance/SearchPlayersEndpointShould.cs` (+6/−6) | 5 aserciones del 400 de filtro | PascalCase → camelCase |
| `Acceptance/DatabaseFailureShould.cs` (+4/−2) | `title` + `status` 500 + sin `errorTranslationKey` | `["Title"]` → `["title"]` |
| `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` (+32/−12) | `ReadBody` con `ProblemJson.Parse`; 10 aserciones a camelCase; 500 sin clave; + test nuevo | PascalCase → camelCase |
| `Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs` (+81/−0) | Solo tests nuevos; los que leen `problemDetails.Extensions[...]` en C# no cambian | - |

Cuerpos de ejemplo (sin datos sensibles):

| Error | Antes | Después |
| --- | --- | --- |
| 500 desconocido | `{"Type":null,"Title":"Unknown error","Status":500,"Detail":null,"Instance":"/api/v1.0/players/<id>","Extensions":{}}` | `{"type":null,"title":"Unknown error","status":500,"detail":null,"instance":"/api/v1.0/players/<id>"}` |
| 400 validación | `{"Type":"","Title":"Cannot create player","Status":400,"Detail":"One or more fields are not valid.","Instance":"/api/v1.0/players","Extensions":{"ErrorTranslationKey":"Error.Players.Create.Validation","Errors":{"FirstName":["..."]}}}` | `{"type":"","title":"Cannot create player","status":400,"detail":"One or more fields are not valid.","instance":"/api/v1.0/players","errorTranslationKey":"Error.Players.Create.Validation","errors":{"firstName":["..."]}}` |
| 404 con clave | `{..., "Extensions":{"ErrorTranslationKey":"Error.Players.Update.PlayerNotFound"}}` | `{..., "errorTranslationKey":"Error.Players.Update.PlayerNotFound"}` |
| 404 sin clave | `{"Type":null,"Title":null,"Status":404,"Detail":null,"Instance":"/api/v1.0/players/<id>","Extensions":{}}` | `{"type":null,"title":null,"status":404,"detail":null,"instance":"/api/v1.0/players/<id>"}` |

Observaciones para REVIEWER: las claves de `errors` se convierten solo en la serialización (primera letra de cada segmento); un segmento en mayúsculas completas se convertiría solo en su primera letra (hoy no existe ninguno). Para el 400 de JSON mal formado la clave es la que da el binder (p. ej. `$` o `body`), igual que antes salvo la inicial. Impacto externo: `quionis-frontend` sin lecturas del formato de error; `quionis-bff-api` vacío en este equipo (no verificable). Acciones pendientes del operador (Q14): `GlobalPackageVersion` e `infoVersion` a 3.0.0 y publicación del cliente; el OpenAPI no cambia, así que no hace falta regenerar por contrato.

## Documentación modificada al cierre

Pendiente (DOCUMENTER). Notas del ciclo 1 para después de APPROVED: Q4 a) solo documentación en `doc/features/technical/hosting-environment.md`; log `Warning` del health check (Q5 a)); `null` en `stands` → 400 (Q9 a)); borrado de facility apoyado en FK en cascada + trigger.
