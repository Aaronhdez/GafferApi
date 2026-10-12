# TECHDEBT-006 - Resumen, alcance y preguntas abiertas

## Petición del operador (literal, transmitida por ORCHESTRATOR)

| Tema | Literal / dato | Paso |
| --- | --- | --- |
| Petición | "Planifica TECHDEBT-006, intenta juntar cambios en ciclos para ahorrar. Añde un ciclo final para incluir un .editorconfig de C#" | Todos |
| Agrupación (ORCHESTRATOR) | Menor número posible de ciclos; no mezclar en un ciclo cambios de contrato con cambios sin contrato; al menos 1 ciclo de reserva; máximo 6 ciclos. | - |
| Origen del alcance | Deuda que el operador dejó "para el final" en TECHDEBT-005 (`00-*.md` de ese plan, "Fuera de alcance") y el último análisis. | - |
| Fuera de alcance (operador) | N+1 en `SqlFacilityRepository.Get` y paginación: se reservan para la feature. | - |
| Respuestas (2026-10-12) | Q8 "Dejarlo como está"; Q10/Q11 "Formato estándar RFC 7807"; Q14 "MAJOR 3.0.0"; Q17 "Formato completo + CI". Las demás preguntas, con su valor por defecto. Ver "Preguntas resueltas". | 01-03 |
| Aprobación | Pendiente: la da el operador en el checkpoint del plan. | - |

## Estado de partida (dato de ORCHESTRATOR, 2026-10-12, `main` e44c896)

- `dotnet build --no-incremental`: 0 warnings (`TreatWarningsAsErrors` solo en `Gaffer.Api`). `dotnet test`: 665/665 contra `Quionis_DB` (mariadb:latest, puerto 3306). Cliente NSwag 2.1.1 (`Directory.Build.props` L9).
- BUILDER revalida la línea base al inicio del ciclo 1 (`docker ps`, build y suite).

## Secuenciación: un paso = un ciclo

| Ciclo | Paso | Fichero | Contenido | ¿Contrato? |
| --- | --- | --- | --- | --- |
| 1 | 01 | `01-cleanup-operations-and-observability.md` | Limpieza (4, 6, 8, 9) + operación, CI y observabilidad (7, 5, log del health check, test de 5 s) + elemento `null` en `stands` + test HTTP "4 gradas + 5 enviadas" | No (OpenAPI igual; un 500 pasa a 400) |
| 2 | 02 | `02-problem-details-camel-case.md` | `ProblemDetails` en camelCase con forma RFC 7807 (10) | **Sí** (formato de error en el cable) |
| 3 | 03 | `03-csharp-editorconfig.md` | `.editorconfig` de C# + `dotnet format` completo + verificación en CI (ciclo final pedido por el operador) | No |
| 4-6 | - | - | Reserva para `NEEDS_CHANGES` (3 ciclos) | - |

Re-secuenciación del 2026-10-12 (respuestas del operador): con Q8 "Dejarlo como está" desaparece la alineación de `TestFixture`; el `null` en `stands` y el test 4+5 se funden en el paso 01, y los antiguos 03/04 pasan a 02/03. `02-test-production-pipeline-alignment.md`, `03-problem-details-camel-case.md` y `04-csharp-editorconfig.md` quedan como ficheros de redirección sin criterios (el operador los borra). Correspondencia: antiguo AC2.0-AC2.3 → AC1.12-AC1.14; antiguo AC2.4-AC2.6 → retirados (Q8); antiguo AC3.N → AC2.N; antiguo AC4.N → AC3.N (reescritos para Q17).

Justificación de la agrupación:

- El paso 01 reúne todo lo que no cambia el contrato antes del formato: los grupos orientativos 1 y 2 de ORCHESTRATOR y lo que quedó del antiguo 02 ("intenta juntar cambios en ciclos para ahorrar"). **Se mantiene la fusión**: sin la alineación de `TestFixture`, el `null` en `stands` es un cambio de mapeo en un controlador más tests, y el 4+5 es un test; ningún punto depende de otro salvo AC1.3 → AC1.4. Riesgo (OPT-036): 10 puntos y el paso más ancho del plan; si REVIEWER devuelve `NEEDS_CHANGES`, la reserva de 3 ciclos lo cubre.
- 02 es el único con cambio de contrato y va solo, después de 01 para que los tests nuevos de AC1.12-AC1.13 también pasen a camelCase en ese ciclo.
- 03 (`.editorconfig` + formato) es el último, como pidió el operador, para que el reformateo no choque con los diffs de 01-02.

## Reglas del plan (todas las fases)

- En el ciclo N, BUILDER implementa **solo el paso N** más las correcciones que REVIEWER pidió en el ciclo anterior. REVIEWER solo aprueba el paso N (y verifica esas correcciones).
- Doble bucle de `.github/doc/work-guidelines.md` en cada paso: aceptación → unitario → refactor con revalidación. Si un cambio no tiene efecto observable (código muerto, borrados redundantes), la aceptación es **caracterización en verde antes y después**.
- **Contrato/swagger (OPT-032 + OPT-034)**: si el diff no toca `Gaffer.Api/Controllers/`, DTOs de petición/respuesta, `Program.cs`, `Startup.cs` ni `Startups/*.cs`, bastan `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde. Si los toca, se compara el `swagger.json` servido volcado con `Acceptance/ServedOpenApiDocumentDump.cs` (`GAFFER_SWAGGER_DUMP_PATH`, `test-stack.md`) **antes de editar** y después (sha256 o `cmp`); nunca `dotnet run` ni sondas temporales. `STATUS.md` indica qué rama se aplicó y por qué.
- **Reparto por capa (OPT-037)**: en criterios de validación, matriz completa en dominio/unitario, un caso por regla en handlers, y en aceptación un representante por tipo de regla y operación; los casos divergentes (OPT-035) se prueban en el pipeline de producción (`StartupTestServer`).
- **Rojo cuantitativo (OPT-038)**: el rojo se registra en `STATUS.md` como `n × estado` por clase de test (p. ej. `3 × 500, 1 × 200`) o `n × CSxxxx` si es de compilación; nunca "todos".
- **Comando de resumen del rojo (OPT-041)**: BUILDER define una sola vez en `STATUS.md` el comando de resumen (`... | grep -oE ... | sort | uniq -c` por clase) y lo reutiliza en cada rojo del plan.
- **Grep de usos (OPT-039)**: antes de cambiar la firma, nulabilidad o miembros de un tipo compartido (p. ej. `CriteriaType`, `DatabaseHealthCheck`, `ExceptionToHttpResponseConverters.Serialize`, `TestFixture`), `grep -rn "<Tipo o miembro>" Gaffer.Api Gaffer.Api.Tests --include=*.cs`, adaptar todos los usos en el mismo paso y anotar comando y número de usos.
- **Revisión acotada (OPT-040)**: REVIEWER parte de `git diff --stat`, `git status` y `grep -n "\[Test\|\[TestCase"` sobre los ficheros de test cambiados; abre por rangos solo lo que justifica un AC; diff completo solo para producto.
- Build: `dotnet build gaffer-api.sln --no-incremental -v q` con el filtro de `CLAUDE.md` (consola localizada). BUILDER y REVIEWER no ejecutan `-c Release` (regenera el cliente).

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| (4) Código muerto en `Program.cs` | L66-73 `SetConfigurationProviders` crea un `ConfigurationBuilder` y lo descarta (L31 lo invoca); `ConfigurationBuilderExtensions.AddDevelopmentSettings` (L76-83) solo se usa ahí. L52-55 `config` de `CreateProductContext` no se usa. El host por defecto ya carga `appsettings.json` y `appsettings.{Environment}.json`; `environment.json` nunca llega a la configuración (TECHDEBT-004 `00`, fila `SetConfigurationProviders`). En TECHDEBT-004 Q4 quedó en c) (se mantuvo). | Sí |
| (6) Borrados redundantes | `SqlFacilityRepository.Delete` L306-316: en una transacción llama a `DeleteStands` (L386-394) y `DeleteAddress` (L396-404) y luego `DELETE FROM Facilities`. Esquema: FK en cascada `FK_Facilities_Addresses_Facilities`, `FK_Facilities_Stands_Facilities`, `FK_Facilities_Stands_Stands` (M0002 L45-63) y `FK_Facilities_Addresses_Addresses` (M0003 L22-25); trigger `TRG_Facilities_BeforeDelete` **recreado en M0003** (L43-57) con `Addresses` (el de M0002 usaba `Adresses`), borra `Stands` y `Addresses` del facility. Test afectado: `Unit/Repositories/SqlFacilityRepositoryShould.PassItsTokenToTheProviderWhenDeleting` L488-496 (`minimumCalls: 6`). | Sí (lectura); comportamiento real pendiente de AC1.3 |
| (8) `Features_Refactor/` | `PlayerFeatureShould.cs` (base con `TearDown`) y `SearchPlayersFeatureShould.cs` (5 tests: por id, sin filtros, filtro Id, FirstName, "SecondName"). Por HTTP existen `PlayerFeaturesShould.GetAPlayer` (L34) y `SearchPlayersEndpointShould` (inyección, campo no permitido, comilla, mayúsculas), **no** búsqueda sin filtros ni por FirstName/LastName/Id. Repositorio: `SqlPlayerRepositoryShould` L142-226 cubre esos filtros. | Sí |
| (9) `CriteriaType.NONE` | Enum en `Shared/Repositories/IPlayerRepository.cs` L26-31. Usos: `DeleteFacilityCommandHandler` L28, `DeletePlayerCommandHandler` L28, `UpdatePlayerCommandHandler` L31, `SqlFacilityRepository` L214. Los 4 añaden un **único** criterio; el primer criterio usa siempre `WHERE` (`SqlPlayerRepository` L40, `SqlFacilityRepository` L55), así que `NONE` nunca llega al SQL. No aparece en controladores ni DTO. Tests: solo `AND`/`OR` (`SqlPlayerRepositoryShould` L229-230). | Sí |
| (7) MariaDB en CI | `.github/workflows/ci.yml` L17 `image: mariadb:latest`. `dev-stack.md` L25 y `test-stack.md` L36 citan `mariadb:latest`. | Sí |
| (5) Entorno por defecto | `Program.ResolveEnvironmentName` L39-42 (`Development` si falta o está en blanco); `Startup.Configure` L58-61 activa `UseDeveloperExceptionPage` en Development. Documentado en `doc/features/technical/hosting-environment.md`. | Sí |
| Log del health check | `DatabaseHealthCheck` (L29-67) no recibe `ILogger`; los `catch` (L59-66) devuelven `Unhealthy` sin registrar nada. Se construye en `DatabaseStartup.AddDatabaseHealthCheck` L66-73 con una factoría que tiene el `IServiceProvider`. | Sí |
| Test de 5 s | `Unit/HealthChecks/DatabaseHealthCheckShould.GiveUpAsUnhealthyWithinFiveSecondsWhenTheProbeDoesNotAnswer` L88-103: espera real con `DefaultTimeout` (5 s) y exige `< 5.5 s`. El resto de la clase usa `ShortTimeout` (200 ms, L12). | Sí |
| `/health/ready` | Anónimo (no hay autenticación en `Gaffer.Api`: 0 usos de `AddAuthentication`/`Authorize`); `SqlDatabaseProbe` abre un `SqlConnectionProvider` nuevo por comprobación (L15-19). | Sí |
| Divergencia (OPT-035) | `TestFixture.cs` L112 `services.AddControllers()`; `Startup.cs` L31-39 `AddMvcCore()` + `AddNewtonsoftJson`. Ambos llaman a `ConfigureValidationResponses()`. DTO con `[Required]` (`Address`) y `[MaxLength(4)]` (`Stands`) en `FacilitiesController.cs` L98-102, L129-133: solo se evalúan en tests. | Sí |
| `null` en `stands` | `FacilitiesController.cs` L55 y L85: `request.Stands?.Select(stand => new ...(stand.StandName, ...))` sin comprobar `stand` → `NullReferenceException` probable (500). | Sí (lectura; el 500 lo caracteriza AC1.12) |
| "4 gradas + 5 enviadas" | Unitario existente: `UpdateFacilityCommandHandlerShould.RejectMoreThanFourSentStandsEvenWhenTheFacilityIsFullWithoutUpdating` L275. Por HTTP no hay test (decisión del operador en TECHDEBT-005 ciclo 4: "confirmo el 400"). | Sí |
| (10) `ProblemDetails` PascalCase | `ExceptionToHttpResponseConverters.cs` L150 `Serialize => JsonConvert.SerializeObject(problemDetails)` (sin resolver camelCase), usado por `ExceptionHandlingMiddleware` L78 y `ValidationStartup` L64. Newtonsoft no reconoce el `[JsonExtensionData]` de System.Text.Json de `ProblemDetails`: hoy las extensiones salen **anidadas** en `"Extensions": {"ErrorTranslationKey": ..., "Errors": {...}}`. Tests: 28 lecturas con índice `JObject` sensible a mayúsculas (`body["Extensions"]!["ErrorTranslationKey"]`, etc.) en 7 ficheros (detallados en el paso 02), que fallarán con el cambio; otros deserializan con `TestJson.Options` (`JsonSerializerDefaults.Web`, sin distinguir mayúsculas) y no detectan el casing. Documentos con la forma actual: `doc/features/technical/error-handling.md` L107-111 y otros 10 en `doc/features/` (paso 02). | Sí |
| Versionado | `Directory.Build.props` L9 `GlobalPackageVersion` 2.1.1; `Gaffer.Api/nswag.json` L35 `infoVersion` **2.0.0** (desalineado; `dev-stack.md` L47 pide mantenerlos iguales). | Sí (observación para el operador, no se corrige en este plan) |
| `.editorconfig` | No existe ninguno en el repositorio. `.gitattributes` fija `*.cs` a LF. Estilos mezclados (p. ej. `Program.cs` namespace con bloque; `Startup.cs` file-scoped; llaves en la misma línea en `DatabaseStartup.cs` L10, L41). | Sí |
| Consumidores (10) | `quionis-frontend/src`: sin referencias a `ProblemDetails`/`errorTranslationKey` (solo `App.vue` de andamiaje). `quionis-bff-api`: `Glob` no devolvió ficheros. | No concluyente (ver "No verificado") |

## No verificado

- Que borrar solo la fila de `Facilities` deja a 0 `Stands`, `Facilities_Stands`, `Addresses` y `Facilities_Addresses` del facility en `Quionis_DB` y en CI (migraciones M0001-M0003 aplicadas). **AC1.3 lo caracteriza antes de quitar nada.**
- Qué devuelve hoy producción con un elemento `null` en `stands` (create y update). **AC1.12 lo caracteriza** con `StartupTestServer`.
- Si el OpenAPI servido declara el esquema `ProblemDetails` (no hay `ProducesResponseType` 400; probablemente no). Lo resuelve AC2.6; activa o no el prerrequisito OPT-030.
- Uso real del formato de error en `quionis-bff-api` y `quionis-frontend`: fuera del repositorio; lo valora el operador.
- Qué cambios produce `dotnet format style` con las reglas en `suggestion` y la severidad `info` (paso 03): se miden en AC3.4.
- Si `SqlConnectionProvider.TestConnection` captura excepciones y devuelve `false` (afecta a qué se puede registrar en el log, Q6): BUILDER lo comprueba al inicio del paso 01.

## Preguntas resueltas (2026-10-12)

Origen: "operador" = opción literal elegida por el operador (transmitida por ORCHESTRATOR); "por defecto" = sin respuesta, se aplica la columna "Sin respuesta" de la tabla siguiente.

| ID | Paso | Respuesta literal | Texto de la opción | Efecto en el plan | Origen |
| --- | --- | --- | --- | --- | --- |
| Q8 | 01 (antes 02) | "Dejarlo como está" | "Se mantiene la divergencia; el ciclo 2 solo arregla el null en stands y añade el test HTTP 4+5." | No se cambian `TestFixture` ni el registro MVC; OPT-035 sigue sin abordarse. Los casos divergentes se demuestran en producción con `StartupTestServer`, como en TECHDEBT-005. Lo que quedaba del paso 02 se funde con el 01 (AC1.12-AC1.14). | operador |
| Q10 | 02 (antes 03) | "Formato estándar RFC 7807" | "camelCase y extensiones aplanadas al nivel raíz (errorTranslationKey, errors), como hace ASP.NET; cambia más el contrato." | Propiedades estándar en camelCase y `errorTranslationKey`/`errors` en la raíz, sin objeto `extensions` (paso 02, tabla de contrato). | operador |
| Q11 | 02 | La misma respuesta que Q10 ("Formato estándar RFC 7807") | La opción no fija las claves **dentro** de `errors`. | Queda abierta como pregunta **no bloqueante** (Q11 en la tabla siguiente); sin respuesta, a) camelCase por segmento. | operador (parcial) |
| Q14 | 02 | "MAJOR 3.0.0" | a) MAJOR (3.0.0) igualmente, por ser incompatible para quien lea los nombres en PascalCase. | Acción del operador antes del push (tabla de acciones): regenerar el cliente si cambia el OpenAPI (OPT-030), `GlobalPackageVersion` 3.0.0 e `infoVersion` de `nswag.json` alineado (hoy 2.0.0 frente a 2.1.1). | operador |
| Q17 | 03 (antes 04) | "Formato completo + CI" | "dotnet format completo (whitespace + estilo) y verificación en CI; diff más grande." | El paso 03 aplica `dotnet format whitespace` + `style` a la solución sin `Gaffer.Api.Client` y añade `--verify-no-changes` al CI; los AC exigen build a 0 warnings (también con `-warnaserror`), suite en verde y swagger idéntico tras el formato. | operador |
| Q0-Q7, Q9, Q12, Q13, Q15, Q16, Q18 | - | Sin respuesta | - | Se aplica su valor por defecto ("Sin respuesta" en la tabla siguiente). | por defecto |

## Preguntas abiertas

Origen de la respuesta por defecto: si no hay respuesta antes del ciclo indicado, se aplica la columna "Sin respuesta". La recomendación de PLANNER no es vinculante. Ninguna pregunta abierta bloquea un ciclo.

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER | Sin respuesta | Bloquea |
| --- | --- | --- | --- | --- | --- | --- |
| Q0 | Todos | Política de correcciones con `NEEDS_CHANGES`. | a) Solo corregir en el ciclo siguiente. b) Corregir y avanzar al paso siguiente si la corrección es pequeña (≤ 1 fichero de producto, sin contrato). | b), como en TECHDEBT-005; con 3 ciclos de reserva hay margen. | b) | No |
| Q1 | 01 (8) | Destino de `Features_Refactor/`. | a) Llevar a HTTP (en `Acceptance/SearchPlayersEndpointShould.cs`) los casos sin equivalente HTTP (sin filtros, filtro `Id`, `FirstName`, `LastName`) y borrar la carpeta. b) Borrar sin migrar (la cobertura queda en `SqlPlayerRepositoryShould` y `PlayerControllerShould`). | a): hoy no hay aceptación HTTP de esos filtros. | a) | No |
| Q2 | 01 (9) | `CriteriaType.NONE`. | a) Quitar el miembro `NONE` y las 4 asignaciones (el criterio único queda con el valor por defecto `AND`, SQL idéntico porque el primero usa `WHERE`). b) Mantenerlo. | a). `AND`=0 y `OR`=1 no cambian de valor. | a) | No |
| Q3 | 01 (7) | Imagen de MariaDB en CI. | a) `mariadb:13.0.2` (igual que el local). b) `mariadb:13.0` (parches automáticos). c) Mantener `mariadb:latest` (decisión de TECHDEBT-004). | a): CI reproducible e igual al local; el operador sube ambas a la vez. | c) (sin cambio en `ci.yml`) | No |
| Q4 | 01 (5) | Arranque en `Development` sin `ASPNETCORE_ENVIRONMENT` (decisión del operador en TECHDEBT-004, no se cambia). | a) Solo documentar (DOCUMENTER, al cierre). b) Además, un aviso en el log de arranque (`Warning`) cuando la variable falta o está en blanco, sin cambiar el entorno resultante. c) Otra salvaguarda que cambie el comportamiento (p. ej. no arrancar): requiere replanificar. | b): hace visible el valor por defecto sin cambiar la decisión. | a) | No |
| Q5 | 01 | Log de errores del health check. | a) `Warning` con la excepción cuando la sonda lanza; `Warning` sin excepción cuando agota el tiempo o devuelve `false`; nada en `Healthy`/`Degraded`/fallback (el fallback ya se avisa al arrancar). Sin limitar la frecuencia. b) Igual con nivel `Error`. c) a) + limitar la frecuencia (p. ej. un log por minuto). | a): sin estado nuevo; el cuerpo HTTP sigue sin datos (AC5.5 de TECHDEBT-005). | a) | No |
| Q6 | 01 | Test del límite de 5 s. | a) La medición de reloj usa un timeout inyectado corto (como `ShortTimeout`) y `DefaultTimeout == 5 s` se comprueba aparte, sin esperar. b) Ampliar el margen (p. ej. `< 6 s`) manteniendo la espera real. c) Marcarlo `[Explicit]`. | a): elimina la dependencia de tiempos y 5 s de suite. | a) | No |
| Q7 | - | `/health/ready` anónimo y una conexión por petición. | a) Dejarlo como está en este plan. b) Cachear el resultado unos segundos (`IMemoryCache` ya registrado). c) Exigir autenticación (la API no tiene autenticación: alcance nuevo). | a); b) o c) serían un paso nuevo (usaría reserva y replanificación). | a) | No |
| Q8 | 01 (antes 02) | Dirección de la alineación tests/producción (OPT-035). | a) `TestFixture` usa el **mismo registro MVC que producción** (extraer a `Startups/` una extensión compartida que llamen `Startup` y `TestFixture`, en lugar de `AddControllers()`); los DataAnnotations dejan de evaluarse también en tests. b) Producción pasa a `AddControllers()` (evalúa DataAnnotations: cambio de comportamiento en producción, posibles 400 distintos). c) Mantener la divergencia. | a) | **Resuelta: c) "Dejarlo como está"** | - |
| Q9 | 01 | Respuesta ante un elemento `null` en `stands` (create y update). | a) 400 con la clave de la operación (`Error.Facilities.Create.Validation` / `Error.Facilities.Update.Validation`) y `Errors["Stands[i]"]` con mensaje genérico, como TECHDEBT-005 Q4/Q18. b) Ignorar los `null` en silencio. c) 400 de model binding con el formato de `ConfigureValidationResponses`. | a): mismo formato que el resto de errores de gradas. | a) | No |
| Q10 | 02 | Forma del cuerpo de error. | - | - | **Resuelta: "Formato estándar RFC 7807"** | - |
| Q11 | 02 | Claves **dentro** de `errors` (`FirstName`, `Address.Street`, `Stands[1].StandName`); la respuesta a Q10 no las fija. | a) camelCase por segmento (`firstName`, `address.street`, `stands[1].standName`), como los nombres JSON de la petición. b) Se mantienen como hoy. Nota: el resolver camelCase de Newtonsoft procesa por defecto las claves de diccionario y solo el primer carácter (`address.Street`): con b) hay que desactivarlo y con a) completarlo. | a): coherente con la forma RFC/ASP.NET elegida (ASP.NET serializa con camelCase también las claves de `errors` en `ValidationProblemDetails`) y con los nombres que envía el cliente. | a) | No |
| Q12 | 02 | Propiedades sin valor (`type`, `detail`, `instance`). | a) Se mantienen con `null` (como hoy). b) Se omiten (como el `ProblemDetails` nativo de ASP.NET). | a): cambio mínimo. | a) | No |
| Q13 | 02 | `Content-Type` de los errores. | a) `application/json` (como hoy). b) `application/problem+json` (como ASP.NET). | a). | a) | No |
| Q14 | 02 | Versión del cliente. | - | - | **Resuelta: "MAJOR 3.0.0"** | - |
| Q15 | 03 | Convenciones del `.editorconfig`. | a) Derivarlas del código actual: BUILDER inventaría con `grep` las convenciones mayoritarias (namespaces, llaves, `var`, campos privados, `using`, indentación 4, LF, UTF-8, salto final) y las registra en `STATUS.md` con recuentos. b) Plantilla estándar de .NET (estilo `dotnet/runtime`). | a). | a) | No |
| Q16 | 03 | Severidades. | a) Reglas de estilo como `suggestion`, sin `EnforceCodeStyleInBuild` (sin efecto en el build ni en el CI). b) `warning` en un subconjunto + `EnforceCodeStyleInBuild`: con `TreatWarningsAsErrors` en `Gaffer.Api` y `-warnaserror` en CI rompe el build hasta corregir todo. c) `error`. | a). | a) | No |
| Q17 | 03 | Reformateo y verificación en CI. | - | - | **Resuelta: "Formato completo + CI"** | - |
| Q18 | 03 | Exclusión del cliente generado. | a) Sección `[Gaffer.Api.Client/**.cs]` con `generated_code = true` en el `.editorconfig` raíz + `--exclude Gaffer.Api.Client` en `dotnet format`; ningún fichero dentro de `Gaffer.Api.Client/`. b) Un `.editorconfig` propio dentro de `Gaffer.Api.Client/`. | a): no toca la carpeta del cliente. | a) | No |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Aprobar el plan y crear la rama. Opcional: Q0-Q7, Q9. Borrar los ficheros de redirección `02-test-production-pipeline-alignment.md`, `03-problem-details-camel-case.md` y `04-csharp-editorconfig.md` (PLANNER no puede borrar). |
| Antes del ciclo 2 | Opcional: Q11, Q12, Q13. Avisar a `quionis-bff-api`/`quionis-frontend` del cambio de formato (forma RFC 7807, versión 3.0.0). |
| Antes de subir la rama / abrir PR (tras el ciclo 2) | **Prerrequisito del job `client-and-package` (OPT-030)**, con Q14 = "MAJOR 3.0.0": (1) si AC2.6 detecta cambios en el OpenAPI, regenerar `Gaffer.Api.Client` (build Release) y versionar `GafferApiClient.cs` y `OpenApiSpecs.json`; si no, el CI falla en "Fail if the generated client differs"; (2) en cualquier caso, `GlobalPackageVersion` = 3.0.0 en `Directory.Build.props` e `infoVersion` = 3.0.0 en `Gaffer.Api/nswag.json` (hoy 2.0.0 frente a 2.1.1); como `infoVersion` entra en el documento generado, cambiarlo exige regenerar y versionar el cliente igualmente. |
| Tras el push | Si Q3 = a) o b): comprobar el job `build-and-test` con la imagen fijada. Tras el ciclo 3: comprobar que el paso `dotnet format --verify-no-changes` pasa en GitHub Actions (BUILDER solo verifica el YAML y la ejecución local). |
| En cada checkpoint con `NEEDS_CHANGES` | Confirmar Q0. |

## Fuera de alcance

- N+1 en `SqlFacilityRepository.Get` y paginación (reservado para la feature).
- Hacer efectivo `environment.json` (hoy nunca se carga; quitar el código muerto conserva ese comportamiento).
- Cambiar el entorno por defecto `Development` (decisión del operador en TECHDEBT-004).
- Autenticación o caché de `/health/*` (Q7 a)).
- Corregir el desalineado `infoVersion` 2.0.0 / `GlobalPackageVersion` 2.1.1 (acción del operador con Q14: ambos a 3.0.0).
- Alinear el registro MVC de `TestFixture` con producción (Q8 "Dejarlo como está"; OPT-035 sigue `pendiente`).
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release`.
- Tests con BD contra el Docker local `Quionis_DB`; nunca se omiten. Si no está disponible: `BLOCKED` con el error exacto.
- DI: cualquier registro nuevo o cambiado (p. ej. logger del health check) se refleja en `TestFixture` en el mismo incremento; `TestServerDependenciesShould` en verde. El registro MVC de `TestFixture` no se cambia (Q8).
- Sin paquetes nuevos. Sin operaciones de Git (salvo `git worktree` de REVIEWER según `work-guidelines.md`). Máximo 6 ciclos.
- Documentación afectada (DOCUMENTER, solo tras `APPROVED`): `doc/features/technical/hosting-environment.md` (Q4), `doc/features/technical/ci-github-actions.md`, `dev-stack.md` L25 y `test-stack.md` L36 (Q3), health check en `doc/features/technical/` (paso 01), `doc/features/error-handling.md`, `doc/features/technical/error-handling.md` y los demás documentos con ejemplos de error listados en el paso 02, `doc/features/technical/api-client-versioning.md` (3.0.0), y `.editorconfig`/`dotnet format` en `dev-stack.md` y `ci-github-actions.md` (paso 03).
