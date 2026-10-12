# REVIEW - TECHDEBT-006

## Ciclo 1 - Paso 01 (REVIEWER)

**Veredicto: APPROVED**. Sin defectos bloqueantes; todos los AC del paso 01 demostrados con evidencia ejecutable.

### Alcance revisado

- `git status`: 23 ficheros (12 producto, 11 tests; 2 borrados de `Features_Refactor/`) + plan sin versionar. Ninguno fuera del alcance del paso. `Gaffer.Api.Client/`, `TestFixture.cs` y `.github/workflows/` sin cambios (`git status --short` → 0 líneas).
- Diff completo del producto leído; tests por `git diff --stat`, grep NUnit y diffs de `SqlFacilityRepositoryShould`, `DatabaseHealthCheckShould`, `HostingEnvironmentShould` y `SearchPlayersEndpointShould` (OPT-040).
- Sin registros de handlers nuevos: no aplica actualizar `TestFixture.cs`.

### Comandos del REVIEWER (`Quionis_DB`, mariadb:latest, 3306, BD `GafferDB_Test`)

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Quionis_DB` Up, `0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 errores |
| `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 688/688 correctas, 0 con error, 0 omitidas (30 s) |
| `ServedOpenApiDocumentDump` en el árbol de trabajo y en un worktree limpio de `HEAD` + `sha256sum` + `cmp` | exit 0 en ambos; sha256 `be49f0ab…a5a6bfe` idéntico; `cmp` idéntico. `CriteriaType`/`NONE` no aparecen en el documento |
| Rojo (OPT-033): `git worktree add <scratch>/red HEAD` + `git diff HEAD -- Gaffer.Api.Tests \| git -C red apply` + `dotnet build Gaffer.Api.Tests` | exit 1; 3 × CS1729 (`DatabaseHealthCheckShould`), 3 × CS1503 (`FacilityShould`), 1 × CS0266 (`UpdateFacilityCommandHandlerShould`) |
| Rojo de comportamiento: en el worktree, restaurados de `HEAD` los 3 ficheros que no compilan; `dotnet test --filter` sobre `FacilityFeaturesShould`, `FacilityValidationInTheProductionPipelineShould`, `SearchPlayersEndpointShould`, `HostingEnvironmentShould`, `DatabaseStartupShould`, `SqlFacilityRepositoryShould`, `CreateFacilityCommandHandlerShould` | exit 1; 8 × Con error: 6 × `but found InternalServerError` (`FacilityFeaturesShould` 2, `FacilityValidationInTheProductionPipelineShould` 4), 1 × `DatabaseStartupShould` (cableado del logger), 1 × `CreateFacilityCommandHandlerShould` (`NullReferenceException`). Resto en verde (caracterización: AC1.1, AC1.3, AC1.5, AC1.13) |
| `git worktree remove --force` | worktree eliminado; `git worktree list` solo el principal |
| `grep -rn "SetConfigurationProviders\|AddDevelopmentSettings" Gaffer.Api --include=*.cs` | 0 |
| `grep -rn "CriteriaType.NONE\|\bNONE\b" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 0 |
| `ls Gaffer.Api.Tests/Acceptance \| grep -c Features_Refactor` | 0 |

### Criterios de aceptación

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC1.1 | Grep → 0; `CreateProductContext` sin `ConfigurationBuilder`. `ReadTheAppSettingsFilesFromTheContentRoot` verde en `HEAD` (worktree) y en el árbol actual (verde-verde); aserciones previas de `HostingEnvironmentShould` intactas. El builder eliminado nunca se usaba (código muerto) | Demostrado |
| AC1.2 | Q4 = a): sin código; anotado para DOCUMENTER en `STATUS.md` | n/a (conforme) |
| AC1.3 | `RemoveTheAddressStandsAndLinksThroughTheSchemaWhenOnlyTheFacilityRowIsDeleted` verde en `HEAD` (antes de cambiar `Delete`) y ahora, contra la BD real: borrado `(0,0,0,0,0)` en las 5 tablas, otro facility `(1,1,1,3,3)`. Dependencia de esquema confirmada en `M0002` (FK `ON DELETE CASCADE` en los dos enlaces) y `M0003` (`TRG_Facilities_BeforeDelete` borra `Stands` y `Addresses`). Sin huérfanos y sin efecto en otros facilities | Demostrado |
| AC1.4 | `Delete` = transacción + `DELETE FROM Facilities`; `DeleteStands`/`DeleteAddress` eliminados sin otros usos; `minimumCalls` 6 → 2 anotado; tests de borrado de repositorio y `DELETE` HTTP en verde en la suite | Demostrado |
| AC1.5 | Tabla caso → test en `STATUS.md`; 4 tests nuevos en `SearchPlayersEndpointShould` verdes; `Features_Refactor/` no existe. Decisión: **errata del plan**. El único endpoint de búsqueda es `POST /players/search` (`PlayerController` L40); un `GET` con query exigiría cambiar el contrato, lo que el paso prohíbe (AC1.10). La intención del criterio (equivalente HTTP de cada caso) se cumple | Demostrado (errata) |
| AC1.6 | Grep → 0. El valor por defecto de `SearchCriteria.CriteriaType` es `AND` y solo se usa como conector desde el segundo criterio (`i == 0 ? "WHERE"`), así que el SQL de los 4 usos con un solo criterio no cambia. Suite verde sin cambiar aserciones | Demostrado |
| AC1.7 | Q3 = c): `ci.yml` sin cambios | n/a (conforme) |
| AC1.8 | Matriz de Q5 a) cubierta: lanza → `Warning` con excepción; `false` y timeout → `Warning` sin excepción; `Healthy`/`Degraded`/fallback → nada; mensaje sin fragmentos sensibles; `ShouldNotExposeAnything` intacto. Cableado rojo → verde (worktree). Decisión sobre el punto 3 del BUILDER: **cumple**. Q5 a) y AC1.8 definen precisamente que `false` se registre sin excepción; que `SqlConnectionProvider.TestConnection` convierta `DbException`/`TimeoutException`/`SocketException` en `false` es comportamiento previo fuera del alcance. Se pierde el tipo/mensaje del error de BD en el log (ver observación N2), pero el plan no lo exigía | Demostrado |
| AC1.9 | Timeout inyectado de 200 ms; `DefaultTimeout == 5 s` afirmado sin esperar; clase 5,323 s → 0,733 s (`STATUS.md`) | Demostrado |
| AC1.10 | sha256 del `swagger.json` servido idéntico en `HEAD` limpio y en el árbol actual (reproducido por el REVIEWER); `OpenApiContractShould` y `FacilitiesControllerContractShould` verdes en la suite | Demostrado |
| AC1.11 | Suite 688/688 (665 − 5 + 28, recuento de `STATUS.md` coherente); build 0 warnings; `TestServerDependenciesShould` verde; `TestFixture.cs`, `Gaffer.Api.Client/` y CI sin cambios | Demostrado |
| AC1.12 | Rojo reproducido en el worktree: 6 × 500 (producción 4, `TestFixture` 2). Verde ahora con 400 y `Errors["Stands[i]"]`, sin guardado/cambio | Demostrado (rojo → verde) |
| AC1.13 | Ambos tests (TestFixture y producción) verdes en `HEAD` y ahora: caracterización, origen del 400 distinto por pipeline anotado | Demostrado (caracterización) |
| AC1.14 | Rojo de compilación reproducido (3 × CS1503, 1 × CS0266) y `CreateFacilityCommandHandlerShould` 1 × NRE; matriz posición 0/1/2 + combinación con otros errores y un caso por handler verdes | Demostrado (rojo → verde) |

### Punto 4 del BUILDER: cambio de tipos de `Stands`

- `CreateFacilityCommand.Stands` / `UpdateFacilityCommand.Stands` pasan a `IReadOnlyList<T?>?`; son tipos internos de comando, no DTO HTTP: el OpenAPI no cambia (sha256 idéntico).
- Usos adaptados (OPT-039): controlador (2), handlers (`OfType<T>()` antes de crear `Stand`/fusionar; `ToRequestedStand` conserva el `null` para el dominio), `FacilityShould`, `BuildCommand`. Build 0 warnings con nullable, sin conversiones pendientes.
- `CheckRequestedStands` con `null` → `Stands[i] is required.`; los `null` cuentan para el máximo de 4. Coherente con Q9 a).

### Observaciones no bloqueantes

| ID | Observación |
| --- | --- |
| N1 | AC1.5 dice "`GET` y filtros de query"; es una errata del plan (el endpoint es `POST /players/search`). Recomendable corregir el texto si PLANNER vuelve a tocar el paso. |
| N2 | Un fallo real de BD llega al health check como `false` y se registra sin excepción ni causa (`SqlConnectionProvider.TestConnection` la descarta). Si el operador quiere la causa en el log, sería un cambio nuevo fuera de este plan (p. ej. registrar en `TestConnection` o propagar la excepción en `SqlDatabaseProbe`). |
| N3 | `ReturnEveryPlayerWhenSearchingWithoutFilters` compara con `BeEquivalentTo` los players creados; depende de que la limpieza por test de la BD siga como hasta ahora (lo está: suite verde). |

## Ciclo 2 - Paso 02 (REVIEWER)

**Veredicto: APPROVED**. Sin defectos bloqueantes. AC2.1-AC2.7 están demostrados con evidencia ejecutable, incluido el cuerpo que sirve producción (`StartupTestServer`) antes y después.

### Alcance revisado

- `git status`: 1 fichero de producto (`Shared/Exceptions/ExceptionToHttpResponseConverters.cs`, +37/−2); 7 tests modificados y 2 nuevos (`TestShared/ProblemJson.cs`, `Unit/Startups/ValidationStartupShould.cs`). El resto son ficheros del harness (plan, `MEMORIES.md`, agentes BUILDER) que no forman parte de la revisión de producto. `ValidationStartup.cs`, `ExceptionHandlingMiddleware.cs`, `TestFixture.cs`, `Directory.Build.props`, `nswag.json` y `Gaffer.Api.Client/` no cambian (`git status --short` → 0 líneas).
- Diff completo del producto leído. Los tests se revisaron con `git diff --stat`, grep NUnit y la lectura de `ProblemJson.cs` y `ValidationStartupShould` (OPT-040).
- No hay handlers nuevos, así que `TestFixture.cs` no aplica.
- Commit `db8d730`: autor y committer `Aaronhdez <aaronhdezalv@hotmail.com>` (operador), a las `2026-10-12T13:50:10+01:00` (= 12:50:10Z). Queda entre el `Fin` del REVIEWER del ciclo 1 (12:47:05Z) y el `Inicio` del BUILDER del ciclo 2 (12:52:12Z), y no lleva trailer `Co-Authored-By`/Claude. Lo hizo el operador, no un agente.

### Comandos del REVIEWER (`Quionis_DB`, MariaDB, 3306, BD `GafferDB_Test`)

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Quionis_DB` Up, `0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 Errores |
| `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 697/697 correctas (31 s) |
| Rojo (OPT-033): `git worktree add <scratch>/red HEAD` + `git diff HEAD -- Gaffer.Api.Tests \| git -C red apply` + copia de los 2 ficheros nuevos; `dotnet test --filter` sobre las 9 clases del paso | exit 1; 197 × Con error, igual que BUILDER (187 de aceptación/producción + 5 de `Serialize` + 4 de middleware + 1 de `ValidationStartupShould`). 192 fallan por `Expected members {"Type", "Title", "Status", "Detail", "Instance", "Extensions"} to contain`; el resto, por comparaciones exactas de cadena en `Serialize` |
| Sonda de producción con un fichero temporal `ReviewerProbe.cs` que solo existe en el worktree: `StartupTestServer` con la BD real y con la BD inalcanzable, 8 peticiones, ejecutada con el producto del diff y con el de `HEAD` | Ver la tabla de cuerpos servidos |
| `ServedOpenApiDocumentDump` en el worktree (`HEAD`) y en el árbol de trabajo + `sha256sum` + `cmp` | exit 0 en ambos; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe` en los dos; `cmp` exit 0 |
| `git worktree remove --force` | Eliminado; `git worktree list` → solo el principal |
| `grep -rn "JsonConvert.SerializeObject" Gaffer.Api --include=*.cs \| wc -l` | 0 |

### Cuerpos servidos por producción (`StartupTestServer`), antes (`HEAD`) → después

En todos los casos, antes y después, `Content-Type: application/json` (Q13 a)). Los ids se sustituyen por `<id>`.

| Caso | Antes | Después |
| --- | --- | --- |
| 400 de validación player (`POST /players` con nombres vacíos) | `{"Type":"","Title":"Cannot create player","Status":400,"Detail":"One or more fields are not valid.","Instance":"/api/v1.0/players","Extensions":{"ErrorTranslationKey":"Error.Players.Create.Validation","Errors":{"FirstName":[...],"LastName":[...]}}}` | `{"type":"","title":"Cannot create player","status":400,"detail":"One or more fields are not valid.","instance":"/api/v1.0/players","errorTranslationKey":"Error.Players.Create.Validation","errors":{"firstName":[...],"lastName":[...]}}` |
| 400 de validación facility anidada | Claves `Stands[1].StandName`, `Name`, `BuildDate`, `Address.Street`, `Address.StreetNumber`, ... dentro de `Extensions.Errors` | `errors` en la raíz con `stands[1].standName`, `name`, `buildDate`, `address.street`, `address.streetNumber`, ...; mensajes idénticos |
| 400 de model binding, JSON mal formado (`{"name":"Arena",`) | `...,"Extensions":{"ErrorTranslationKey":"Error.Facilities.Create.Validation","Errors":{"Name":["The value is not valid."]}}}` | `...,"errorTranslationKey":"Error.Facilities.Create.Validation","errors":{"name":["The value is not valid."]}}` |
| 400 de model binding, enum no válido | `"Errors":{"Gender":[...]}` | `"errors":{"gender":[...]}` |
| 404 sin clave (`GET /players/<id>`) | `{"Type":null,"Title":null,"Status":404,"Detail":null,"Instance":"...","Extensions":{}}` | `{"type":null,"title":null,"status":404,"detail":null,"instance":"..."}` |
| 404 con clave (`PUT /players/<id>`) | `...,"Detail":"Player <id> not found",...,"Extensions":{"ErrorTranslationKey":"Error.Players.Update.PlayerNotFound"}}` | `...,"detail":"Player <id> not found",...,"errorTranslationKey":"Error.Players.Update.PlayerNotFound"}` |
| 400 de filtro no permitido (`POST /players/search` con `Gender`) | `{"Type":"","Title":"Error.Search.FilterNotPermitted","Status":400,"Detail":"FilterNotPermitted",...,"Extensions":{"ErrorTranslationKey":"Error.Search.FilterNotPermitted"}}` | `{"type":"","title":"Error.Search.FilterNotPermitted","status":400,"detail":"FilterNotPermitted",...,"errorTranslationKey":"Error.Search.FilterNotPermitted"}` |
| 500 desconocido (BD inalcanzable) | `{"Type":null,"Title":"Unknown error","Status":500,"Detail":null,"Instance":"...","Extensions":{}}` | `{"type":null,"title":"Unknown error","status":500,"detail":null,"instance":"..."}` |

En los 8 casos solo cambian los nombres y la posición de los miembros. Estados, `title`, `detail`, `instance`, claves `Error.*` y mensajes de `errors` son idénticos byte a byte.

### Criterios de aceptación

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC2.1 | `ProblemJson.Parse` lee el JSON crudo con `JObject` (distingue mayúsculas). Exige los 5 miembros estándar y que todos los miembros estén en {5 estándar, `errorTranslationKey`, `errors`}, lo que excluye `extensions`/`Extensions` y el PascalCase; además, ningún segmento de clave de `errors` puede empezar por mayúscula. Lo usan los helpers de aceptación: rojo 187 → verde en la suite. La sonda muestra la forma exacta en el 400 de validación player/facility, el 404 con y sin clave, el 400 de filtro y el 500 | Demostrado |
| AC2.2 | En producción, `FacilityValidationInTheProductionPipelineShould` (44 casos con `ProblemJson`, incluido el nuevo `AnswerAMalformedBodyWithTheRfc7807ValidationProblem`) pasa de rojo a verde. La sonda sobre `StartupTestServer` da la forma RFC 7807 en el 400 de dominio y en el 400 de JSON mal formado | Demostrado |
| AC2.3 | 6 tests nuevos de `Serialize`: cada miembro, `null`, sin extensiones, `errorTranslationKey` sola, `errors` con clave simple, con punto, con índice y `Stands[0]`, y el `ProblemDetails` en memoria intacto. 5 fallan en `HEAD` y ahora pasan. `JsonConvert.SerializeObject` en `Gaffer.Api` → 0; hay un único punto (`Serialize`), que usan el middleware y `ValidationStartup` | Demostrado |
| AC2.4 | Middleware: la excepción convertida (`WriteAConvertedValidationFailureWithItsKeyAndErrorsAtTheRoot`, nuevo, y el 400 de filtro) y el 500 desconocido, que conserva `title`/`status` sin `errorTranslationKey`/`errors`. `ValidationStartupShould` (nuevo) usa la factoría real y obtiene `firstName`, `address.street` y `body`. Rojo 4 + 1 → verde. La sonda muestra la misma forma en el 500 y en el 400 de binding de producción | Demostrado |
| AC2.5 | No se borra ningún test: los 6 `[TestCase]` retirados se sustituyen 1 a 1 por la misma entrada con el campo en camelCase, y se añaden 9 tests (688 → 697). Valores: la sonda antes/después es idéntica salvo la forma. La tabla de tests ajustados está en `STATUS.md` | Demostrado |
| AC2.6 | El sha256 del documento servido es idéntico en `HEAD` y en el árbol (`be49f0ab…a5a6bfe`) y `cmp` da exit 0, así que OPT-030 no se activa. `Gaffer.Api.Client/` no se toca (su contenido no se verifica, está fuera de alcance) | Demostrado |
| AC2.7 | Build 0/0; suite 697/697; `git status` sin cambios en `Gaffer.Api.Client/`, `TestFixture.cs`, `Directory.Build.props` ni `nswag.json` | Demostrado |

### Hallazgos (no bloqueantes)

| # | Hallazgo | Severidad |
| --- | --- | --- |
| 1 | Los mensajes de `errors` conservan la ruta en PascalCase (`"stands[1].standName": ["Stands[1].StandName must not be empty ..."]`). Es lo que exige AC2.5 (valores sin cambios), pero ahora la clave y el texto no coinciden; DOCUMENTER debería mencionarlo en los ejemplos. | Info |
| 2 | `Serialize` solo convierte las claves cuando el valor de la extensión es `IReadOnlyDictionary<string, string[]>`. Cualquier otra extensión pasa por `JToken.FromObject` con los nombres por defecto de Newtonsoft (PascalCase en objetos anidados). Hoy solo existen `string` y ese diccionario (`BuildValidationProblemDetails` tipa así el parámetro), y `ProblemJson` lo detectaría en aceptación. | Menor |
| 3 | `CamelCase` solo baja la primera letra (`ID` → `iD`). Hoy no hay segmentos en mayúsculas completas, y BUILDER ya lo anota. | Menor |
| 4 | Impacto externo: `quionis-bff-api` no se puede verificar en este equipo. Queda pendiente del operador, junto con la versión 3.0.0 (Q14). | Info (operador) |

## Ciclo 3 - Paso 03 (REVIEWER)

Veredicto: **APPROVED**

### Alcance revisado

- `git status`: 107 `.cs` modificados (Api, Tests, Migrations), `.github/workflows/ci.yml` modificado y `.editorconfig` nuevo. El resto son ficheros del harness y del plan. `Gaffer.Api.Client/` sin cambios.
- Leídos completos (OPT-040): `.editorconfig` (diff contra el bloque `ini` del paso), `ci.yml`, el diff `-w` de M0001-M0003, los tres tipos IDE0290 y los cambios manuales. El resto del `.cs` se ha validado de forma mecánica (ver abajo), sin leerlo línea a línea.

### Comandos del REVIEWER (`Quionis_DB`, MariaDB, 3306, BD `GafferDB_Test`)

| Comando | Resultado |
| --- | --- |
| `docker ps`; `dotnet --version` | `Quionis_DB` Up, 3306; SDK 10.0.302 |
| `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| `dotnet build gaffer-api.sln --no-incremental -warnaserror -v q` + `grep CS9124\|CS9107\|CS1717` | exit 0; 0 Advertencia(s); 0 coincidencias |
| `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` (árbol de trabajo) | **exit 0**; salida vacía |
| `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 697/697, 0 omitidos |
| Worktree `HEAD` + solo `.editorconfig`: verify (rojo) | **exit 2**: 1270 × WHITESPACE, 87 × IDE1006, 43 × FINALNEWLINE, 42 × IDE0022, 40 × CHARSET, 3 × IDE0290, 3 × IDE0161, 3 × IMPORTS, 1 × IDE0021, 1 × IDE0011; 0 líneas de `Gaffer.Api.Client`. Sin ENDOFLINE: el worktree recién creado ya sale en LF por `.gitattributes` (`*.cs text eol=lf`). Las 950 de BUILDER vienen de su copia de trabajo en CRLF; no hay ningún CR en el índice. |
| Mismo worktree: aplicar ×2 y verify | apply exit 0 / 0; verify exit 2 con **solo 87 × IDE1006** (los renombrados manuales) |
| Comparación por fichero entre el worktree formateado y el árbol de trabajo (`cmp` + `git diff --no-index --word-diff=porcelain`) | 65 de 107 ficheros idénticos byte a byte; 42 difieren. Un script que normaliza cada par `-`/`+` (quita `_` y `this.` y pasa a minúsculas) da **0 diferencias que no sean renombrados**: ni pares sueltos ni tokens nuevos |
| Diff de tokens `HEAD` → worktree formateado, ignorando espacios, `{}`, `return`, `;` y `=>` | Solo aparecen el orden de 3 `using` (`PlayerShould`, `Program`, `Startup`), BOM y los 3 IDE0290 (`FacilityNotFoundException`, `PlayerNotFoundException`, `GafferApiVersioningDefinition`: el constructor pasa a primary constructor con inicializadores de propiedad, mismo orden y mismos valores) |
| Comprobación de ocultación (awk sobre `HEAD`: método con local o parámetro del nombre nuevo que usaba `_nombre`) | 1 caso real, `OpenApiContractShould` L56 → `this.document` (correcto). Los constructores de `EnvironmentVariableScope` y `StartupTestServer` usan `this.`. En `DatabaseHealthCheck`, los inicializadores usan el parámetro y los métodos usan el campo: compila sin CS8602/CS9124 con `-warnaserror` y `CancelAfter(timeout)` no admitiría el `TimeSpan?` del parámetro. `DomainValidationErrors.errors` no tiene parámetro homónimo |
| `GAFFER_SWAGGER_DUMP_PATH=<scratch>/swagger-rev.json dotnet test ... ServedOpenApiDocumentDump` + `sha256sum` + `cmp` | exit 0; `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe`, el mismo de `HEAD` (ciclos 1-2) y de la línea base de BUILDER; `cmp` exit 0 |
| `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./scripts/run-migrations.ps1` | exit 0, "Migrations for GafferDB_Test finished" (BD al día: transacción vacía) |
| `grep "\[Migration("` en `HEAD` y en el árbol | 202608071730 / 202608131010 / 202609261200 en ambos |
| `git diff -U0 -w Gaffer.Api.Migrations` con filtro de líneas que tienen `"` o `Migration(` | Solo `Delete.Table("Players")`, que pasa a `Down() => Delete.Table("Players");` (IDE0022, mismo literal) |
| `git status --porcelain Gaffer.Api.Client \| wc -l`; `grep -c Gaffer.Api.Client` en la salida del rojo | 0; 0 |
| Worktree eliminado (`git worktree remove --force`) | `git worktree list`: solo el árbol principal |

### Criterios de aceptación

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC3.1 | Línea base de `STATUS.md`, registrada antes de editar: 697, 0/0 warnings, sha256 `be49f0ab…`, 40 BOM, 78 declaraciones `_x`. Coincide con lo que he medido en `HEAD`: 78 declaraciones (grep corregido), 40 CHARSET, 121 `const` / 47 `static readonly` | Demostrado |
| AC3.2 | `diff` del bloque `ini` del paso contra `.editorconfig`: solo se añaden 11 líneas `= none` (Q19 (i) a), ver punto 3). Ninguna decisión cambia. Sin BOM | Demostrado |
| AC3.3 | Verify en el árbol de trabajo: exit 0. Rojo reproducido en el worktree: exit 2 | Demostrado (rojo → verde) |
| AC3.4 | Build 0/0; `-warnaserror` exit 0; 0 × CS9124/CS9107/CS1717 | Demostrado |
| AC3.5 | 697/697, el mismo recuento. `SqlConnectionProviderShould` (cadena de reflexión `"sqlConnection"`) y `TestServerDependenciesShould` forman parte de la suite en verde | Demostrado |
| AC3.6 | En el árbol: grep del plan 0, grep corregido 0, identificadores `_x` 0, campos `_x` de cualquier visibilidad 0. `const` 121 = 121 y `static readonly` 47 = 47; en `HEAD` no había ningún `const`/`static readonly` con `_` | Demostrado |
| AC3.7 | BOM 40 → 0; namespaces de bloque 3 → 0 (grep corregido) | Demostrado |
| AC3.8 | sha256 idéntico y `cmp` exit 0 | Demostrado |
| AC3.9 | `DatabaseSchemaShould` en verde (suite); script exit 0 con `powershell.exe`; atributos idénticos; ningún literal SQL ni nombre cambiado | Demostrado |
| AC3.10 | Cliente sin cambios y ausente de la salida de `dotnet format` | Demostrado |
| AC3.11 | `git diff ci.yml`: una sola línea añadida, igual a AC3.3, después del build `-warnaserror` y antes de migraciones y tests. El AC deja el run en GitHub Actions como acción del operador | Demostrado (diff) |
| AC3.12 | Worktree `HEAD` + `.editorconfig` + aplicar ×2: 65 ficheros idénticos; en los 42 restantes la diferencia son solo los renombrados manuales que lista `STATUS.md` (las dos tablas). Con ellos aplicados, el árbol de trabajo da verify exit 0 | Demostrado |

Doble bucle: aceptación = rojo de verify (exit 2) → verde (exit 0). Unitario: no aplica, sin lógica nueva (lo registra `STATUS.md`). Refactor revalidado con suite, build, swagger y migraciones.

### Puntos que planteó BUILDER

| # | Punto | Decisión |
| --- | --- | --- |
| 1 | Renombrados fuera de "campos privados `_camelCase`": `criteriaList` → `CriteriaList` (propiedad pública de `FacilitySearchCriteria`/`PlayerSearchCriteria`), `protected _tablesToDelete` → `TablesToDelete`, `public _databaseConnectionProvider` (test) → `DatabaseConnectionProvider` y los campos privados en PascalCase de `PlayerFeaturesShould` → camelCase | **Caben en la decisión del operador.** Q15 dice "resto: Sí, estándar .NET" (tipos y miembros en PascalCase, campos privados en camelCase), y las reglas `types_and_members_pascal` y `private_fields_camel` son literales del paso. Sin estos cambios no se cumplirían AC3.3 (verify exit 0) ni AC3.6 (0 `_x`). Comprobado: son renombrados puros (script de pares), `CriteriaList` no aparece en controladores, en `Gaffer.Api.Client` ni en ningún literal o `nameof`, y el swagger es idéntico. Hay una tensión literal con la línea "Fuera: renombrados distintos de los campos privados" del paso, pero esa línea va contra sus propios AC. No bloquea; conviene dejarlo **visible en el checkpoint** como información (la alternativa, suprimir IDE1006, sí cambiaría una decisión) |
| 2 | Falsos negativos de los grep de AC3.6/AC3.7 | Confirmado en `HEAD`: el grep de declaraciones del plan da 0 y el corregido 78; el de namespaces del plan da 1 y el corregido 3 (`Program.cs`, `SwaggerStartup.cs {`, `ApiVersioning.cs {`). Los corregidos detectan los casos y en el árbol dan 0. Diferencia menor: el grep de identificadores da 721 líneas en `HEAD` (`git grep`) frente a las 816 de BUILDER (probablemente ocurrencias); ambos terminan en 0 |
| 3 | 11 líneas `= none` | Encajan con Q19 (i) a): las 10 `CA*` son justo las que BUILDER listó en el rojo con su recuento (fila "Rojo" de `STATUS.md`) y se desactivan una a una. NUnit1028 no es `CA*`, pero es una regla de analizador de nivel info sin decidir, así que aplica el mismo criterio de lista blanca. Ninguna reactiva una regla de estilo ni cambia una decisión |
| 4 | `pwsh` no está en el `PATH` local | Anotado. Localmente se usó `powershell.exe` (Windows PowerShell 5.1), con exit 0. En el CI (ubuntu) el script se ejecuta con `pwsh`, sin cambios en este paso |
| 5 | Run real del paso nuevo del CI | **No bloquea.** AC3.11 exige el contenido del diff de `ci.yml` y declara el run en GitHub "acción del operador". OPT-027 / `harness-run` piden un checkpoint previo solo cuando un AC *depende* del run, y este no depende. Queda para el operador después del push |

### Hallazgos (no bloqueantes)

| # | Hallazgo | Severidad |
| --- | --- | --- |
| 1 | Los renombrados del punto 1 conviene mencionarlos en el checkpoint: cambian nombres públicos de tipos internos del repositorio (no tocan el contrato HTTP) | Info (operador) |
| 2 | Las copias de trabajo en Windows hechas antes de `.gitattributes` (como la de BUILDER) pueden tener `.cs` en CRLF, y el verify local daría ENDOFLINE. Con un checkout nuevo, o con `git add --renormalize`, desaparece; el CI (ubuntu, checkout nuevo) no se ve afectado | Info |
| 3 | El paso del CI depende de que el SDK de `global.json` sea el mismo localmente y en el CI. Si `rollForward` permite otra versión del parche, los analizadores podrían cambiar y el verify fallar sin que haya cambios en el código | Menor |
| 4 | Recomendación del paso (operador): hacer un commit de reformateo separado y, opcionalmente, añadirlo a `.git-blame-ignore-revs` | Info (operador) |
