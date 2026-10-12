# STATUS - TECHDEBT-005

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 5 |
| Aprobación del plan | Aprobado por el operador el 2026-10-11 (transmitido por ORCHESTRATOR): "vamos con el 2" |
| Pasos completados | 01 (APPROVED en ciclo 1); 02 (APPROVED en ciclo 2); 03a (APPROVED en ciclo 3); 03b (APPROVED en ciclo 4); 05 (APPROVED en ciclo 5) |
| Pasos bloqueados | Ninguno (Q23 respondida 2026-10-12; Q18-Q21 con valor por defecto a)) |
| Siguiente | Ninguno: plan cerrado (DOCUMENTER, 2026-10-12; aprobación del operador: "aplica la veriosnes permanentes y lanza el documenter") |
| Bloqueos | Ninguno |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | AC1.1-AC1.6 (rojo 2/2 → verde; suite 322 → 324; swagger idéntico) | sin commit registrado (rama `TECHDEBT-005`); detalle en `STATUS-archive.md` |
| 2 | 02 Validación de dominio de Player (Q17 `SecondLastName`) | APPROVED | AC2.0-AC2.9 (rojo aceptación 33/76 + rojo unitario por compilación → verde; suite 324 → 413; swagger vía `StartupTestServer` idéntico) | sin commit registrado (rama `TECHDEBT-005`, `e28e545` contiene el paso); detalle en `STATUS-archive.md` |
| 3 | 03a Ayudante `swagger.json` (OPT-034) + validación de dominio de Facility y dirección | APPROVED | AC3a.0-AC3a.11 (rojo aceptación 106/132, producción 32/32: 18 × 500, 7 × 201, 7 × 200; rojo unitario 12 × CS0117, 8 × CS1501 → verde; suite 413 → 574; swagger sha256 `be49f0ab...` idéntico) | sin commit registrado (rama `TECHDEBT-005`, `7a22e5c` contiene el paso); detalle en `STATUS-archive.md` |
| 4 | 03b Validación de dominio de gradas (anidadas + sub-recurso `/stands`) | APPROVED | AC3b.1-AC3b.11 (rojo aceptación 32/199, producción del sub-recurso 6/6: 1 × 201, 1 × 204, 2 × 500, 2 × 404; rojo unitario 9 × CS0117 + 3 × CS7036 → verde; suite 574 → 637; swagger sha256 `be49f0ab...` idéntico) | sin commit registrado (rama `TECHDEBT-005`); detalle en `STATUS-archive.md` |

## Decisiones del operador sobre ciclos cerrados

| Ciclo | Decisión (literal) | Alcance |
| --- | --- | --- |
| 4 | "Apruebo el P4-1 y 2, y confirmo el 400, luego lanza el paso 5" | "confirmo el 400": acepta el cambio visible señalado por REVIEWER en el ciclo 4 (`REVIEW.md`, decisión 1): `PUT /facilities/{id}` con más de 4 gradas enviadas sobre una facility que ya tiene 4 devuelve 400 (`Errors.Stands`) en vez del 200 con descarte silencioso de antes |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED |
| 2 | 02 Validación de dominio de Player (incluye el cambio de `SecondLastName` de Q17 respecto a TECHDEBT-002) | APPROVED |
| 3 | 03a Ayudante de volcado del `swagger.json` (OPT-034) + validación de dominio de Facility y dirección (create/update) | APPROVED |
| 4 | 03b Validación de dominio de gradas (anidadas + sub-recurso `/stands`) | APPROVED |
| 5 | 05 Health check (se mantiene el fallback a Dummy) | APPROVED |
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
| 4 | BUILDER | 2026-10-12T02:01:23Z | 2026-10-12T02:13:13Z |
| 4 | REVIEWER | 2026-10-12T02:13:56Z | 2026-10-12T02:17:58Z |
| 5 | BUILDER | 2026-10-12T10:54:55Z | 2026-10-12T11:01:47Z |
| 5 | REVIEWER | 2026-10-12T11:03:14Z | 2026-10-12T11:07:16Z |

## Comandos y resultados

| Ciclo | Comando | Resultado |
| --- | --- | --- |
| 5 | Compactación (OPT-026/OPT-029) | Ciclo 4 colapsado en el índice; sus comandos y su detalle movidos tal cual a `STATUS-archive.md` |
| 5 | `docker ps` (preflight) | `Quionis_DB` Up, `0.0.0.0:3306->3306` |
| 5 | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 5 | Línea base: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 637/637, 0 omitidos |
| 5 | AC5.6 antes: `GAFFER_SWAGGER_DUMP_PATH="<scratchpad>/c5/swagger-before.json" dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~ServedOpenApiDocumentDump"` | exit 0; 21617 bytes; sha256 `be49f0abd26c4c6fe3bd95a16691c6734012310127dd1521fddb98092a5a6bfe`; `grep -c /health` 0 |
| 5 | Resumen del rojo (OPT-041), definido una vez y reutilizado: `dotnet test Gaffer.Api.Tests --no-build --filter "$F" --logger "console;verbosity=normal" > red.txt; grep -oE "Con error [A-Za-z]+\|but found [A-Za-z]+\.[A-Za-z]+( \{value: [0-9]+\})?" red.txt \| sort \| uniq -c`; para compilación: `dotnet build Gaffer.Api.Tests -v q > red.txt; grep -oE "[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+" red.txt \| sort -u` y `uniq -c` por fichero y código | - |
| 5 | Rojo aceptación (solo tests; producto sin tocar): filtro `FullyQualifiedName~HealthCheckShould` | exit 1; `HealthCheckShould` 4/4 con error: 4 × 404 (ruta inexistente) |
| 5 | Rojo unitario/integración: `dotnet build Gaffer.Api.Tests -v q` | exit 1; `DatabaseHealthCheckShould.cs` 3 × CS0246 (`IDatabaseProbe`, `DatabaseProviderReason` ×2). La fase de declaración detiene la compilación: los usos de `DatabaseProviderSelection` en `DatabaseStartupShould` y de `TestConnection(CancellationToken)` en `SqlConnectionProviderShould` no llegan a informarse |
| 5 | OPT-039: `grep -rn "TestConnection(" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l` y `grep -rn "StartupTestServer.StartAsync" Gaffer.Api.Tests --include=*.cs` | 7 usos / 7 usos. Sin cambio de firma: `IDatabaseConnectionProvider.TestConnection()` intacto (se añade una sobrecarga `TestConnection(CancellationToken)` solo en `SqlConnectionProvider`); `StartupTestServer.StartAsync(string)` intacto (delegado en la sobrecarga nueva `StartAsync(bool enabled, string? connectionString)`). Ningún uso adaptado |
| 5 | Verde, filtro del paso: `...~HealthCheckShould\|...~DatabaseHealthCheckShould\|...~DatabaseStartupShould\|...~SqlConnectionProviderShould\|...~TestServerDependenciesShould` | exit 0; 58/58 al primer intento. AC5.5 tiempos: sonda bloqueada (timeout por defecto 5 s) → `Unhealthy` tras 5009 ms; cancelación de la petición a los 100 ms → `OperationCanceledException` tras 102 ms |
| 5 | AC5.8: `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s) |
| 5 | AC5.8: `dotnet test gaffer-api.sln --no-build -v q` | exit 0; 665/665 (637 + 28), 0 omitidos |
| 5 | AC5.6 después: mismo comando del ayudante con `swagger-after.json` + `cmp swagger-before.json swagger-after.json` + `grep -c /health` | exit 0; 21617 bytes; sha256 `be49f0ab...a5a6bfe`; `cmp` exit 0; 0 rutas `/health` |
| 5 | AC5.6: `dotnet test Gaffer.Api.Tests --no-build --filter "FullyQualifiedName~FacilitiesControllerContractShould\|FullyQualifiedName~OpenApiContractShould"` | exit 0; 15/15 |
| 5 | AC5.8: `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs \| wc -l` | 0 |
| 5 | REVIEWER: rojo en copia del árbol con los ficheros del paso en `HEAD` (scratchpad) | aceptación exit 1, 4 × 404; build de tests exit 1, 3 × CS0246 en `DatabaseHealthCheckShould.cs` |
| 5 | REVIEWER: build `--no-incremental -v q` + suite completa | build exit 0, 0 Advertencia(s); suite exit 0, 665/665, 0 omitidos |
| 5 | REVIEWER: filtro del paso + contratos (`verbosity=normal`) | exit 0; 73/73; sonda bloqueada → `Unhealthy` en 5015 ms; cancelación en 100 ms; `/health/ready` 200 Healthy / 503 Unhealthy (fallback) / 2 × 200 Degraded |
| 5 | REVIEWER: ayudante `ServedOpenApiDocumentDump` + `grep -c /health` + `CancellationToken.None` | exit 0; 21617 bytes; sha256 `be49f0ab...a5a6bfe` igual; 0 `/health`; 0 `CancellationToken.None` |
| 5 | REVIEWER: veredicto | APPROVED (`REVIEW.md` ciclo 5): timeout de 5 s exactos aceptado; los 4 ficheros fuera de la tabla, dentro del alcance razonable |

## Ciclo 5 (BUILDER) - detalle (paso 05)

### Diseño

| Pieza | Fichero | Detalle |
| --- | --- | --- |
| Motivo del proveedor | `Startups/DatabaseStartup.cs` | `enum DatabaseProviderReason { Sql, DummyByConfiguration, DummyByFallback }` y `record DatabaseProviderSelection(Reason)`, singleton registrado en cada rama. El proveedor elegido no cambia. En la rama Sql se registra además `IDatabaseProbe` = `SqlDatabaseProbe(connectionString)`. |
| Registro de la comprobación | `Startups/DatabaseStartup.cs` | `AddHealthChecks().Add(HealthCheckRegistration("database", factoría, tags: ["ready"]))`; la factoría resuelve la selección y la sonda opcional. Sin paquete nuevo (framework compartido de ASP.NET Core). |
| Comprobación | `Infrastructure/DatabaseHealthCheck.cs` (nuevo) | `DummyByConfiguration` → `Degraded`; `DummyByFallback` → `Unhealthy` sin sondear (Q12); `Sql` → sonda con token enlazado al de la petición + `CancelAfter(5 s)` + `WaitAsync` (acota aunque la sonda ignore el token): `true` → `Healthy`; `false`, excepción o tiempo agotado → `Unhealthy`; token de la petición cancelado → `OperationCanceledException`. Resultados sin `Exception`, sin `Data` y con descripciones fijas. |
| Sonda | `Infrastructure/DatabaseHealthCheck.cs` + `Shared/ConnectionProviders/SqlConnectionProvider.cs` | `SqlDatabaseProbe` crea un `SqlConnectionProvider` por comprobación y llama a la sobrecarga nueva `TestConnection(CancellationToken)` (`OpenAsync`/`ExecuteScalarAsync` con token; token ya cancelado → excepción sin tocar la conexión). `TestConnection()` delega con `default`; la interfaz no cambia. |
| Rutas | `Startup.cs` | `MapHealthChecks("/health/live", Predicate = _ => false)` y `MapHealthChecks("/health/ready", Predicate = etiqueta "ready")`; escritor y códigos por defecto del framework (texto `Healthy`/`Degraded`/`Unhealthy`; 200/200/503). Registro MVC sin tocar (OPT-035). |
| `TestFixture.cs` | `Gaffer.Api.Tests/TestFixture.cs` | Registra `DatabaseProviderSelection(Sql)` junto al `SqlConnectionProvider` que ya registra (mismo estado que producción con BD alcanzable). Su `TestStartup` no mapea `/health`: la aceptación usa `StartupTestServer`. `TestServerDependenciesShould` en verde sin cambios. |
| `StartupTestServer` | `TestShared/StartupTestServer.cs` | Sobrecarga `StartAsync(bool enabled, string? connectionString)` y propiedad `Services`; `StartAsync(string)` delega (7 usos intactos). |

### Tests nuevos por criterio (reparto OPT-037)

| AC | Capa | Tests |
| --- | --- | --- |
| AC5.1 | Aceptación (`StartupTestServer`) | `HealthCheckShould.ReportLiveAndReadyAsHealthyWhenTheDatabaseIsReachable` |
| AC5.2 + AC5.5 | Aceptación | `ReportReadinessAsUnhealthyWhenTheApiFellBackToTheDummyProviderWithoutExposingTheConnection` (proveedor resuelto `DummyConnectionProvider`, su `TestConnection()` = `true`, `/health/live` 200, `/health/ready` 503 `Unhealthy`; cuerpo sin `Server`/`Uid`/`Pwd`/`Database`/`Exception`/`MySql`) |
| AC5.3 | Aceptación | `ReportReadinessAsDegradedWhenTheDatabaseIsDisabled`, `ReportReadinessAsDegradedWhenThereIsNoConnectionString` (live 200 `Healthy`, ready 200 `Degraded`) |
| AC5.4 + AC5.5 | Unitario | `DatabaseHealthCheckShould.ReportTheStatusOfTheChosenProvider`: matriz motivo × sonda (correcta, fallida, excepción con datos sensibles, bloqueada, cancelada): 10 casos Dummy (sonda nunca llamada) + 4 Sql; sin `Exception`/`Data`/datos sensibles. Además: fallback con BD que responde → `Unhealthy`; Sql sin sonda → `Unhealthy`; cancelación de la petición durante la sonda → `OperationCanceledException` en 102 ms y token de la sonda cancelado; sonda bloqueada con el timeout por defecto (5 s) → `Unhealthy` en 5009 ms y token de la sonda cancelado |
| AC5.7 | Integración | `DatabaseStartupShould` (+4: `Sql`, `DummyByFallback`, `DummyByConfiguration` × 2 configuraciones); las aserciones existentes (L75, L86, L110) sin cambios |
| AC5.5 (sonda) | Integración (BD) | `SqlConnectionProviderShould` (+2: token ya cancelado → `OperationCanceledException` con la conexión cerrada; token vivo → `true`) |

### AC5.6 (OPT-032 + OPT-034)

Rama aplicada: **comparación con el ayudante**, porque el diff toca `Startup.cs` y `Startups/DatabaseStartup.cs`. Antes y después: 21617 bytes, sha256 `be49f0ab...a5a6bfe`, `cmp` exit 0, 0 apariciones de `/health`. Contratos 15/15. Cliente generado sin tocar; sin build Release.

### Observaciones

| # | Observación |
| --- | --- |
| 1 | AC5.5 "≤ 5 s": el timeout es exactamente 5 s; la medida con sonda bloqueada fue 5009 ms (5 s + 9 ms de planificación) y el test admite < 5,5 s. Si REVIEWER interpreta "≤ 5 s" como cota estricta del total, habría que bajar el timeout interno; decisión no tomada por BUILDER. |
| 2 | Ficheros fuera de la tabla "Alcance de ficheros" de 05, necesarios para el diseño: `Shared/ConnectionProviders/SqlConnectionProvider.cs` (sobrecarga con token para respetar la cancelación; la interfaz no cambia), `Unit/ConnectionProviders/SqlConnectionProviderShould.cs` (sus tests), `TestShared/StartupTestServer.cs` (hosts deshabilitado/sin cadena/inalcanzable para AC5.2-AC5.3) y `Unit/HealthChecks/DatabaseHealthCheckShould.cs` (el plan pide `Unit/` sin fichero concreto). |
| 3 | Con el motivo `Sql`, `/health/ready` abre una conexión nueva por petición (sin caché); el endpoint es anónimo. No se añadió limitación de frecuencia (fuera del plan). |
| 4 | Una excepción de la sonda se resuelve como `Unhealthy` sin registrar la excepción (el resultado no la lleva para no exponerla). |
| 5 | Refactor: la comprobación está separada del registro desde el verde (fichero propio; `AddDatabaseHealthCheck` privado en `DatabaseStartup`); no hubo cambios adicionales tras el verde. Cambios ajenos (MEMORIES.md, HARNESS-001, ficheros de plan) sin tocar. |

## Documentación cambiada

DOCUMENTER, 2026-10-12. Base: código aprobado, `STATUS.md`/`STATUS-archive.md` y `REVIEW.md`. Sin paquetes nuevos (no cambia ninguna referencia de paquete).

| Tipo | Documento | Contenido |
| --- | --- | --- |
| Creado | `doc/features/input-validation.md` | Funcional: reglas por campo, formato 400 (`ErrorTranslationKey` por operación, `Errors` por campo), errores de model binding, rehidratación sin validar (Q16), orden 404/400, límites |
| Creado | `doc/features/technical/input-validation.md` | Técnico: `DomainValidationErrors`, `ValidationFailedException`, `ValidationStartup`, `Create`/`Set*`, handlers, diagrama, tabla, pruebas |
| Creado | `doc/features/health-checks.md` | Funcional: `/health/live`, `/health/ready`, estados, códigos, 5 s, límites |
| Creado | `doc/features/technical/health-checks.md` | Técnico: `DatabaseProviderSelection`, `DatabaseHealthCheck`, `IDatabaseProbe`, `SqlConnectionProvider.TestConnection(CancellationToken)`, rutas, diagrama |
| Directa | `doc/features/player-update.md` | Q17: `secondLastName` ausente/null no cambia, `""` → 400; ya no se puede borrar; validación de lo enviado |
| Directa | `doc/features/technical/player-update.md` | Q17: resumen, diagrama, tabla, ejemplos, tests |
| Directa | `doc/features/facility-update.md` | Más de 4 gradas enviadas → 400 (confirmado: "confirmo el 400"); clave `Error.Facilities.Update.Validation`; validación; 404 antes que 400 |
| Directa | `doc/features/technical/facility-update.md` | Resumen, diagrama, tabla, dependencias, ejemplo del 400; `Update(Facility)` ya no forma parte del contrato (paso 01) |
| Directa | `doc/features/facility-stands.md` | 400 de validación de `/stands` y sustitución de la limitación "sin validar" |
| Directa | `doc/features/technical/facility-stands.md` | `StandValidation`/`Stand.Create`, clave `Error.Facilities.Stands.Validation`, ejemplo |
| Indirecta | `doc/features/error-handling.md`, `doc/features/technical/error-handling.md` | Entrada del convertidor `ValidationFailedException`; 14 → 18 entradas de `PlayerConverters` |
| Indirecta | `doc/features/technical/api-client-versioning.md` | Nota histórica de `secondLastName`; validación y health sin cambio de OpenAPI (sin regenerar el cliente) |
| Indirecta | `doc/features/database-connection-reliability.md`, `doc/features/technical/database-connection-lifecycle.md` | Readiness visible sobre el fallback; `DatabaseProviderSelection`, sonda y sobrecarga `TestConnection(CancellationToken)` |
| Indirecta (pila) | `.github/doc/common/test-stack.md` | Ayudante `ServedOpenApiDocumentDump` (`[Explicit]`, `GAFFER_SWAGGER_DUMP_PATH`) y sobrecarga de `StartupTestServer` |

Fuera de la matriz de DOCUMENTER (acción del operador): `.github/doc/features/facility-crud.md` (L39 y L73) aún describe `Update(Facility)` en `IFacilityRepository`, retirado del contrato en el paso 01.
