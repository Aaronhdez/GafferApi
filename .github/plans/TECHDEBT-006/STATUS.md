# STATUS - TECHDEBT-006

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 3 |
| Aprobación del plan | Aprobado por el operador ("Apruebo el plan, espera a que cree la rama"); rama `TECHDEBT-006` creada por el operador |
| Pasos completados | 01 APPROVED (ciclo 1, REVIEWER); 02 APPROVED (ciclo 2, REVIEWER); 03 APPROVED (ciclo 3, REVIEWER) |
| Pasos bloqueados | Ninguno. Q8, Q10, Q14 y Q17 respondidas por el operador (2026-10-12); ninguna pregunta abierta bloquea un ciclo. El push tras el ciclo 2 requiere la acción del operador de Q14 (`00`, "Acciones del operador") |
| Siguiente | Acciones pendientes del operador (ver "Documentación modificada al cierre") |
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
| 3 | 03 `.editorconfig` de C# + `dotnet format` completo + verificación en CI | APPROVED |
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
| 3 | BUILDER | 2026-10-12T13:23:02Z | 2026-10-12T13:32:29Z |
| 3 | REVIEWER | 2026-10-12T13:33:04Z | 2026-10-12T16:36:55Z (REVIEW.md escrito a las 13:40Z; la ejecución se cortó por un límite de la API y se reanudó solo para cerrar STATUS/usage) |

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
| 3 | Inicio | Ciclo 1 ya colapsado (ciclo 2 = anterior, se mantiene en detalle) | Sin compactación nueva |
| 3 | Preflight | `docker ps`; `dotnet --version` | `Quionis_DB` Up, `0.0.0.0:3306->3306/tcp`; SDK 10.0.302. `pwsh` no está en `PATH`: el script se ejecuta con `powershell.exe -NoProfile -ExecutionPolicy Bypass -File` |
| 3 | AC3.1 build | `dotnet build gaffer-api.sln --no-incremental -v q` y `... -warnaserror -v q` (filtro OPT-006) | exit 0 / 0 Advertencia(s), 0 Errores; exit 0 / 0 Advertencia(s) |
| 3 | AC3.1 suite | `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 697/697, 0 omitidos |
| 3 | AC3.1 swagger | `GAFFER_SWAGGER_DUMP_PATH=<scratch>/swagger-before.json dotnet test ... --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` + `sha256sum` | exit 0; `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` |
| 3 | AC3.1 recuentos | Comandos de AC3.6/AC3.7 (ver nota de regex abajo) | declaraciones `private` `_x`: 78 (grep corregido; 0 con el del plan); identificadores `_x`: 816 (43 nombres distintos, 31 ficheros); `const` 121, `static readonly` 47; BOM 40; namespaces de bloque 3 (1 con el grep del plan); `[Migration(...)]` 202608071730 / 202608131010 / 202609261200 |
| 3 | Rojo (resumen OPT-041, definido una vez) | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/ > out; echo exit=$?; grep -oE ": (error\|warning\|info) [A-Za-z]+[0-9]*" out \| sort \| uniq -c` (con el `.editorconfig` del plan, antes de aplicar) | **exit 2**; 107 ficheros `.cs`; 1270 × WHITESPACE, 950 × ENDOFLINE, 43 × FINALNEWLINE, 40 × CHARSET, 3 × IMPORTS, 87 × IDE1006, 42 × IDE0022, 3 × IDE0161, 3 × IDE0290, 1 × IDE0021, 1 × IDE0011, 8 × NUnit1028, CA no decididas: 5 × CA1859, 5 × CA2211, 4 × CA1822, 2 × CA1816, 2 × CA1861, 2 × CA2016, 1 × CA1068, 1 × CA1846, 1 × CA1873, 1 × CA2263; 0 líneas de `Gaffer.Api.Client` |
| 3 | Aplicar 1 | `.editorconfig` + 11 desactivaciones (Q19 (i) a)); `dotnet format gaffer-api.sln --severity info --exclude Gaffer.Api.Client/`; verify (mismo resumen) | exit 0; 108 ficheros en `git status`; verify exit 2: 87 × IDE1006, 3 × WHITESPACE (línea en blanco tras namespace file-scoped recién convertido), 2 × CHARSET (ficheros reescritos por IDE0290) → resueltos en la 2.ª pasada |
| 3 | Renombrado manual | `sed -E "s/\b_<n>\b/<n>/g"` por fichero, guiado por la tabla `fichero / nombre / nº declaraciones / nº usos del nombre sin prefijo` (OPT-039); `this.` donde había conflicto | Detalle en la sección del ciclo 3 |
| 3 | Aplicar 2 + AC3.3 | Comando de aplicar; `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | exit 0; **verify exit 0**, 0 diagnósticos |
| 3 | AC3.4 | `dotnet build gaffer-api.sln --no-incremental -v q`; `... -warnaserror -v q` + `grep -E "CS9124\|CS9107\|CS1717"` | exit 0 / 0 Advertencia(s), 0 Errores; exit 0 / 0 Advertencia(s), 0 Errores; 0 × CS9124/CS9107/CS1717 |
| 3 | AC3.5 | `dotnet test Gaffer.Api.Tests --no-build`; `--filter "...DatabaseSchemaShould\|...SqlConnectionProviderShould\|...TestServerDependenciesShould"` | exit 0; 697/697 (30 s), 0 omitidos; filtro: exit 0, 28/28 |
| 3 | AC3.6 | Mismos comandos que AC3.1 | declaraciones `_x` 0 (ambos grep); identificadores `_x` 0; `const` 121 y `static readonly` 47 (iguales) |
| 3 | AC3.7 | Mismos comandos que AC3.1 | BOM 0 (antes 40); namespaces de bloque 0 (antes 3) |
| 3 | AC3.8 | volcado `swagger-after.json` + `sha256sum` + `cmp` | exit 0; `be49f0ab…a5a6bfe` idéntico; `cmp` exit 0 |
| 3 | AC3.9 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./scripts/run-migrations.ps1` (solo `GafferDB_Test`); `grep -rn "\[Migration(" Gaffer.Api.Migrations`; `git diff --word-diff=porcelain -w` y `git diff -w` de `Migrations/` | exit 0 ("Migrations for GafferDB_Test finished"); atributos idénticos; word-diff: solo BOM de `using`, llaves K&R y `Down() => Delete.Table("Players");` (M0001, IDE0022); 0 cambios en literales SQL/nombres; `DatabaseSchemaShould` verde (fila AC3.5) |
| 3 | AC3.10 | `git status --porcelain Gaffer.Api.Client \| wc -l`; `grep -c Gaffer.Api.Client` en las salidas de verify | 0; 0 |
| 3 | AC3.11 | `git diff --numstat .github/workflows/ci.yml`; `git diff -U0` | `1 0`; única línea añadida tras el build `-warnaserror`, antes de migraciones/tests. Run en GitHub Actions: **no demostrado** (acción del operador tras el push) |
| 3 | Resumen git | `git status --short`; `git diff --numstat -- '*.cs'` | 107 `.cs` + `ci.yml` modificados, `.editorconfig` nuevo; +2125 / −3357 en `.cs` |
| 3 | REVIEWER | `docker ps`; build `-v q` y `-warnaserror`; verify; `dotnet test --no-build` | `Quionis_DB` Up 3306; exit 0 / 0 Advertencia(s) en los dos builds; verify exit 0; 697/697 |
| 3 | REVIEWER rojo/AC3.12 (OPT-033) | worktree `HEAD` + `.editorconfig`: verify → aplicar ×2 → verify; comparación por fichero con el árbol | rojo exit 2; tras aplicar, solo 87 × IDE1006; 65 ficheros idénticos y 42 que difieren solo en renombrados (0 pares que no sean renombrado); worktree eliminado |
| 3 | REVIEWER AC3.8/AC3.9 | volcado del swagger + `sha256sum`/`cmp`; `run-migrations.ps1` (powershell.exe); `[Migration(` | sha256 `be49f0ab…a5a6bfe` idéntico; exit 0; atributos idénticos |
| 3 | REVIEWER veredicto | `REVIEW.md`, ciclo 3 | APPROVED; 5 puntos de BUILDER decididos; 4 hallazgos no bloqueantes |

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

## Ciclo 3 - Paso 03 (BUILDER)

Decisiones: Q15/Q16/Q18 (contenido literal del paso); Q19 sin respuesta → (i) a) lista blanca y (ii) a). Doble bucle: aceptación = AC3.1 + rojo de `--verify-no-changes`; unitario: **no aplica** (sin lógica nueva); refactor = aplicar + renombrar + revalidar.

Cambios en `.editorconfig` respecto al bloque del paso (AC3.2): ninguna corrección de sintaxis; solo se añaden, tras el comentario de collection expressions, 11 líneas `dotnet_diagnostic.<regla>.severity = none` (Q19 (i) a)): CA1068, CA1816, CA1822, CA1846, CA1859, CA1861, CA1873, CA2016, CA2211, CA2263 y **NUnit1028** (analizador NUnit, info, 8 avisos "Only test methods should be public"; no es `CA*` pero es igual de no decidido, mismo criterio).

Regex del plan (nota para REVIEWER): el grep de declaraciones de AC3.6 (`[A-Za-z0-9_<>,?\[\] ]+`) no coincide nunca en GNU grep (en una expresión entre corchetes POSIX `\` es literal y el `]` cierra la clase), por eso da 0 también antes; se usa además `grep -rnE "^\s*private\s.*\s_[a-z]\w*\s*[;=]"` (78 → 0). El grep de namespaces de AC3.7 no ve los de bloque con `{` en la misma línea; se usa además `"^namespace [A-Za-z0-9_.]+\s*\{?\s*$"` (3 → 0). El de identificadores `\b_[a-z]...` es fiable (816 → 0).

Renombrado manual (OPT-039): tabla previa `fichero / nombre / declaraciones / usos del nombre sin prefijo` en los tres proyectos; con 0 usos sin prefijo, `sed` directo (64 pares); con usos, revisión por ámbito (solo comentarios, cadenas o locales en otro método salvo los tres casos con `this.`). `const`/`static readonly` no tocados (ninguno tenía `_`).

| Fichero | Campos `_x` → `x` (privados) |
| --- | --- |
| `ApiShould.cs`, `JsonContractShould.cs`, `PlayerFeaturesShould.cs` | `_client` |
| `FacilityFeaturesShould.cs`, `FacilityStandsFeaturesShould.cs` | `_client`, `_server` |
| `DatabaseFailureShould.cs` | `_client`, `_logs`, `_server` |
| `OpenApiContractShould.cs` | `_document` (**`this.document`** en L51: `var document = ...FromJsonAsync(this.document.ToString())`), `_server` |
| `SearchPlayersEndpointShould.cs` | `_client`, `_createdPlayers` |
| `EnvironmentVariableScope.cs` | `_name` (**`this.name = name;`**), `_previousValue` |
| `StartupTestServer.cs` | `_host` (**`this.host = host;`**) |
| `DatabaseSchemaShould.cs` | `_provider` |
| `Create/Delete{Facility,Player}CommandHandlerShould.cs` | `_handler`, `_repository` |
| `UpdateFacilityCommandHandlerShould.cs` | `_facilityId`, `_handler`, `_repository`, `_updatedFacility` |
| `UpdatePlayerCommandHandlerShould.cs` | `_handler`, `_playerId`, `_repository` |
| `FacilityStandCommandHandlersShould.cs` | `_facilityId`, `_repository`, `_standId` |
| `FacilityStandQueryHandlersShould.cs` | `_facilityId`, `_repository` |
| `SearchFacilitiesQueryHandlerShould.cs` | `_emptySearchCriteria`, `_facilities`, `_handler`, `_repository` |
| `SearchPlayersQueryHandlerShould.cs` | `_emptySearchCriteria`, `_handler`, `_players`, `_repository` |
| `FacilitiesControllerShould.cs` | `_cancellation`, `_controller`, `_create/_delete/_updateFacilityCommandHandler`, `_searchFacilitiesQueryHandler` |
| `PlayerControllerShould.cs` | `_controller`, `_create/_delete/_updatePlayerCommandHandler`, `_searchPlayersQueryHandler` |
| `SqlFacilityRepositoryShould.cs` | `_cancellation`, `_competitorProvider`, `_databaseConnectionProvider`, `_holderProvider`, `_recordedRepository`, `_recorder`, `_repository` |
| `SqlPlayerRepositoryShould.cs` | `_birthDate`, `_cancellation`, `_recordedRepository`, `_recorder`, `_repository` |
| `SqlTest.cs` | `_connectionProvider`, `_dbConnection` |
| `PlayerShould.cs` | `_player` |
| `TokenRecordingConnectionProvider.cs` | `_calls` |
| `SqlConnectionProvider.cs` (producto) | `_currentTransaction`, `_sqlConnection`; cadena de reflexión `GetField("_sqlConnection", ...)` → `"sqlConnection"` en `SqlConnectionProviderShould.cs` (L385 tras el reformateo; L424 antes) |
| `DatabaseHealthCheck.cs` (producto) | `_logger`, `_timeout` → `private readonly ILogger logger = logger ?? ...;` (campo inicializado con el parámetro homónimo del primary constructor; el parámetro no se captura: sin CS9124) |
| `DomainValidationException.cs` (producto, `DomainValidationErrors`) | `_errors` |

Cambios manuales fuera de "campos privados `_camelCase`" (imprescindibles para AC3.3/AC3.6 con las reglas decididas; **a valorar por REVIEWER**, todos verificados por compilador, suite y sha256 del swagger):

| Fichero(s) | Cambio | Motivo |
| --- | --- | --- |
| `SqlTest.cs` (+ `SqlFacilityRepositoryShould.cs`, `SqlPlayerRepositoryShould.cs`) | `protected _tablesToDelete` → `TablesToDelete` | AC3.6 exige 0 `_x`; al ser `protected` aplica `types_and_members_pascal`, no `private_fields_camel` |
| `SqlPlayerRepositoryShould.cs` | `public _databaseConnectionProvider` → `DatabaseConnectionProvider` | Igual (campo `public` de test) |
| `PlayerFeaturesShould.cs` | campos privados en PascalCase `PlayerId`, `BirthDate`, `Player1-3` → `playerId`, `birthDate`, `player1-3` (no se tocan `player.BirthDate`, cadenas SQL ni el miembro anónimo `BirthDate =`) | IDE1006 `private_fields_camel`; sin ellos verify exit 2 |
| `IFacilityRepository.cs`, `IPlayerRepository.cs` + 35 usos (producto y tests) | propiedad pública `criteriaList` → `CriteriaList` (`FacilitySearchCriteria`, `PlayerSearchCriteria`) | IDE1006 `types_and_members_pascal`; sin él verify exit 2. Tipos internos de repositorio, no expuestos en el OpenAPI (sha256 idéntico) ni en `OpenApiSpecs.json`. Alternativa si el operador no lo acepta: excluir IDE1006 de la verificación o suprimirlo en esas dos propiedades (cambio de decisión → checkpoint) |

Lo aplicado por `dotnet format` (no manual): K&R, whitespace, LF, newline final, charset sin BOM, orden de `using` (3 ficheros), IDE0161 (3 namespaces), IDE0022/IDE0021 (43 cuerpos de expresión), IDE0011 (1 `if`), IDE0290 (3 tipos: `GafferApiVersioningDefinition`, `FacilityNotFoundException`, `PlayerNotFoundException`). Sin CS9124/CS9107 → Q19 (ii) a) no requirió constructores clásicos. Migraciones: solo formato (fila AC3.9). `TestFixture.cs`: solo formato.

Para AC3.12 (REVIEWER): copiar `.editorconfig` y aplicar los renombrados de las dos tablas anteriores; el resto lo produce el comando de aplicar (dos pasadas).

## Documentación modificada al cierre (DOCUMENTER, 2026-10-12)

Base: código aprobado, `STATUS.md`, `STATUS-archive.md` y `REVIEW.md` (ciclos 1-3). Sin documentos creados: todo se hizo actualizando los existentes. Sin paquetes nuevos (`dotnet format` viene con el SDK).

| Tipo | Documento | Cambio |
| --- | --- | --- |
| Directa | `doc/features/technical/hosting-environment.md` | Q4 a): el entorno por defecto es `Development` (aviso al operador, sin código); `ConfigurationBuilderExtensions`/`SetConfigurationProviders` eliminados; test `ReadTheAppSettingsFilesFromTheContentRoot` |
| Directa | `doc/features/health-checks.md`, `doc/features/technical/health-checks.md` | Log `Warning` del health check (con/sin excepción), constructor con `timeout` y `logger` inyectables, matriz de tests, `DefaultTimeout` sin esperar |
| Directa | `doc/features/technical/facility-stands.md`, `doc/features/facility-stands.md` | `Delete` de facility = una transacción + `DELETE FROM Facilities` apoyado en FK en cascada (M0002) y trigger (M0003) |
| Directa | `doc/features/technical/player-search-and-persistence.md` | Fuera `CriteriaType.NONE` y `Features_Refactor/` (sustituida por `SearchPlayersEndpointShould`); `CriteriaList`; ejemplos 400/404 en RFC 7807 |
| Directa | `doc/features/error-handling.md`, `doc/features/technical/error-handling.md` | Nuevo formato RFC 7807 (`Serialize`, camelCase, `errorTranslationKey`/`errors` en la raíz, sin `extensions`), ejemplos de 400/404/500 |
| Directa | `doc/features/input-validation.md`, `doc/features/technical/input-validation.md` | Formato de respuesta, claves de `errors` en camelCase por segmento (`stands[1].standName`), mensajes en PascalCase (observación del REVIEWER), `null` en `stands` → 400 con `stands[i]`, firma de `CheckRequestedStands` |
| Directa | `doc/features/technical/api-client-versioning.md` | Cambio incompatible del formato de error (MAJOR; 3.0.0 planeado, Q14); no publicado, acciones pendientes del operador |
| Directa | `doc/features/technical/ci-github-actions.md` | Paso `dotnet format --verify-no-changes`, sección "Code style (.editorconfig)" (Q15, Q16, Q18, Q19, camelCase sin `_`), límites |
| Indirecta | `doc/features/player-search.md`, `player-update.md`, `facility-update.md`, `api-contract-and-access.md` | Nombres de miembros del cuerpo de error (`errorTranslationKey`, `errors`) en vez de `Extensions.*`; 404 desnudo con `type`/`title`/`detail` en `null` |
| Indirecta | `doc/features/technical/facility-update.md`, `player-update.md` | Mismos nombres en el cuerpo de error |
| Indirecta | `.github/doc/common/dev-stack.md`, `.github/doc/common/test-stack.md` | `dotnet format` como paso de CI (herramienta del SDK), `.editorconfig`, formato de error RFC 7807, entorno `Development` por defecto |

Revisado sin cambios: `doc/features/database-connection-reliability.md`, `technical/database-connection-lifecycle.md` (cubren `DatabaseStartup`, no el health check), `technical/api-contract-and-access.md`, `database-configuration*.md`. `.github/doc/features/facility-crud.md` (fuera del alcance de DOCUMENTER) sigue describiendo el borrado con `DELETE Stands/Address` en el diagrama; pendiente de revisión por el operador.

Acciones pendientes del operador:

| # | Acción |
| --- | --- |
| 1 | `GlobalPackageVersion` e `infoVersion` de `Gaffer.Api/nswag.json` a 3.0.0 (Q14, MAJOR por el formato de error) |
| 2 | Regenerar y versionar `Gaffer.Api.Client` y publicarlo (el OpenAPI no cambia, solo la versión) |
| 3 | Commit del reformateo del paso 03 por separado (opcional: `.git-blame-ignore-revs`) |
| 4 | Push y primer run real del CI con el paso `dotnet format` (AC3.11 solo demostrado por el diff) |
| 5 | Verificar `quionis-bff-api` (no verificable en este equipo) frente al nuevo formato de error |
| 6 | Revisar `.github/doc/features/facility-crud.md` (borrado de facility) |
