# TECHDEBT-004 - Resumen, alcance y preguntas abiertas

## Decisiones del operador (literales, transmitidas por ORCHESTRATOR)

| Tema | Literal | Paso |
| --- | --- | --- |
| Estructura | "2 planes" (este = producto; `HARNESS-001` = propuestas OPT) | - |
| Entorno | "Leer de APNETCORE_ENVIRONMENT y si no está, usar Development en su defecto." | 02 |
| TreatWarningsAsErrors | "Solo Gaffer.Api" | 01 |
| Driver | "Sí, en TECHDEBT-004" (migrar `MySql.Data` → `MySqlConnector`) | 03 |
| Conexión tras un fallo | "Sí, try/finally" (TECHDEBT-003 Q1) | 01 |
| `.gitattributes` | Entra en TECHDEBT-004 (OPT-015, REVIEW F1 de TECHDEBT-003). Renormalización: abierta (Q2) | 01 |
| CRUD de gradas | "CRUD para las Stands desde facility y Endpoints en como indicas en la primera opción." (`DELETE /api/v1/facilities/{id}/stands/{standId}` → 204; 404 si la facility o la grada no existe o es ajena). Por extensión: CRUD completo bajo `/api/v1/facilities/{id}/stands` | 04 |
| Concurrencia del máximo de 4 | "Lock en transacción" (`SELECT ... FOR UPDATE` dentro de la transacción, en el PUT de facility y en el POST de grada, con test concurrente) | 04 |
| CI | "GH Actions + NSwag/paquete", MariaDB "latest", NSwag "Generar y fallar si difiere" | 05 |

## Estado de partida (dato del operador, 2026-09-27, `main`)

- `dotnet build --no-incremental`: 0 errores, 0 warnings. `dotnet test`: 269/269 contra Docker local `Gaffer.Database`, que es **MariaDB 12.3.2** (imagen `mariadb`, no MySQL).
- No hay `.github/workflows`. Remoto: `https://github.com/Aaronhdez/GafferApi.git`.
- BUILDER revalida la línea base al inicio del ciclo 1 (build, suite y `docker ps`).

## Secuenciación: un paso = un ciclo

| Ciclo | Paso | Fichero | Contenido |
| --- | --- | --- | --- |
| 1 | 01 | `01-warnings-as-errors-gitattributes-connection-close.md` | `TreatWarningsAsErrors` en `Gaffer.Api`; `.gitattributes` (OPT-015); cierre de conexión con `try/finally` |
| 2 | 02 | `02-hosting-environment.md` | Entorno desde la variable de entorno con `Development` por defecto |
| 3 | 03 | `03-mysqlconnector-migration.md` | `MySql.Data` → `MySqlConnector`; la cancelación aborta la consulta en curso (revisa OPT-016/OPT-019) |
| 4 | 04 | `04-facility-stands-crud-and-lock.md` | CRUD de gradas bajo la facility + lock en transacción del máximo de 4 |
| 5 | 05 | `05-ci-github-actions.md` | Workflow de GitHub Actions (el último, para validar todo lo anterior) |
| 6 | - | - | Reserva para `NEEDS_CHANGES` (única) |

Reglas:

- En el ciclo N, BUILDER implementa **solo el paso N**, más las correcciones que REVIEWER haya pedido en el ciclo anterior. No adelanta trabajo de pasos posteriores.
- En el ciclo N, REVIEWER **solo aprueba el paso N** (y verifica las correcciones del ciclo anterior).
- Un solo ciclo de reserva (el 6). Cómo se combina una corrección con el paso siguiente es Q0. Si Q8 = b), no queda reserva.
- Doble bucle de `.github/doc/work-guidelines.md` en cada paso: aceptación → unitario → refactor con revalidación. En pasos sin cambio de comportamiento observable (01 en su parte de build/EOL, 03 salvo la cancelación) la evidencia es: caracterización en verde antes y después, comprobaciones estáticas (`grep`, `git ls-files --eol`) y suite completa con recuento antes/después.
- Orden justificado: 01 y 02 son pequeños y no tocan contrato; 03 cambia el driver antes de que 04 añada SQL nuevo con `FOR UPDATE` (así el lock y su test concurrente se escriben y validan una sola vez sobre el driver definitivo); 05 va el último para que el CI valide 01-04. Se mantiene el orden propuesto por ORCHESTRATOR.

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| Entorno fijado | `Program.cs` L19 `.UseEnvironment(ProductContext.Environment ?? Development)`; `GetProductContext` L28-42 construye un `ConfigurationBuilder` (variables de entorno + `environment.json`) que no usa y fija `Environment = "Development"`; `ProductContext.cs` L6 valor por defecto `"Development"`. `launchSettings.json` ya define `ASPNETCORE_ENVIRONMENT=Development`. Tests: `TestFixture` L79 y `StartupTestServer` L30 usan `UseEnvironment("Test")`; `ApiDatabaseConfigurationShould` L36 `UseEnvironment("Development")` (no pasan por `Program.Main`). | Sí |
| `SetConfigurationProviders` | `Program.cs` L44-51: crea un `ConfigurationBuilder` (`appsettings.json`, `environment.json`, `appsettings.Development.json` si es Development) y lo descarta; no se añade al host. `AddDevelopmentSettings` (L56-60) solo se usa ahí. El host por defecto ya carga `appsettings.json` y `appsettings.{Environment}.json`; `environment.json` no llega nunca a la configuración del host. | Sí |
| TreatWarningsAsErrors | `Gaffer.Api/Gaffer.Api.csproj` L8 `false`. `Directory.Build.props` solo contiene la versión de `Gaffer.Api.Client`. | Sí |
| `.gitattributes` | Existe en la raíz con una sola regla: `*.cs text eol=lf`. `.csproj`, `.props`, `.json`, `.md`, `.yml`, `.ps1`, `.sln` no tienen atributo. | Sí |
| Conexión tras un fallo | `SqlConnectionProvider.RunOnConnectionAsync` L147-156: sin `try/finally`; su `<summary>` documenta la deuda Q1. Lo usan `ClearAsync`, `CreateAsync`, `QueryScalarAsync`, `QueryAsync`, `ExecuteAsync`. `ConnectionState` es público (L15). | Sí |
| Driver | `MySql.Data` 26.7.0 solo en `Gaffer.Api.csproj` L22 y `SqlConnectionProvider.cs` L5 (único `using` en `Gaffer.Api`; ningún test lo referencia). `Gaffer.Api.Migrations` ya usa `MySqlConnector` 2.6.1. Comentario dependiente del driver en `SqlConnectionProvider.cs` L128. Notas de driver en `dev-stack.md` L24-25 y `dev-stack-documentation.md` L13-14. | Sí |
| Máximo de 4 gradas | `UpdateFacilityCommandHandler.Handle` L37 lee la facility con `facilityRepository.Get` **fuera** de la transacción; `MergeStands` L97-117 decide con ese dato; `SqlFacilityRepository.Update` L131 abre la transacción después. Invariante en `Facility.cs` L60 ("Stands cannot exceed 4"). | Sí |
| Endpoints de facility | `FacilitiesController`: `[Route("api/v{version:apiVersion}/facilities")]`; `POST` → 201 + `CreatedAtAction` con `Guid`; `PUT {id}` y `DELETE {id}` devuelven 200 sin cuerpo (`Task`). No hay endpoints de gradas. DTO de lectura `SearchFacilityStandResult(Id, StandName, StandCapacity)`. `CreateFacilityStandRequest`/`UpdateFacilityStandRequest` sin validación de campos. Claves de error existentes: `Error.Facilities.{NotFound, Create.Validation, Update.FacilityNotFound, Update.DuplicatedStand, Delete.FacilityNotFound, ...}`. | Sí |
| NSwag en CI | `Gaffer.Api.csproj` L44: el target `NSwag` solo corre con `Configuration != Debug` **y** `$(OS) == Windows_NT`. `nswag.json`: salida en `../Gaffer.Api.Client/{OpenApiSpecs.json, GafferApiClient.cs}`, `newLineBehavior: Auto`. `Gaffer.Api.Client.csproj`: `GeneratePackageOnBuild` en Release. | Sí |
| Service containers de GitHub | Los contenedores de servicio de GitHub Actions solo funcionan en runners Linux; el target NSwag solo en Windows (ver Q17). | Dato de la plataforma (no verificado en este repo) |

## No verificado

- Que `MySqlConnector` aborte de verdad una consulta en curso contra **MariaDB 12.3.2** al cancelar el token (el paso 03 lo mide).
- Diferencias de mapeo entre drivers (p. ej. `Guid` sobre las columnas de `Id`, fechas, variables de usuario `@x` en SQL, excepción concreta en fallos de conexión/consulta). El paso 03 lo cubre con la suite completa y registra el tipo de excepción observado (insumo de OPT-012, ver `HARNESS-001`).
- Si el comportamiento de `DisposeAsync`/`Component.Disposed` (nota OPT-016) se mantiene con `MySqlConnector`; afecta a `ReleaseTheConnectionOnlyWhenItsScopeIsDisposed` (paso 03).
- Si extraer un método de construcción del host en `Program` cambia cómo NSwag (`aspNetCoreToOpenApi`) arranca la app al generar el cliente. BUILDER no puede comprobarlo (no ejecuta Release sobre `Gaffer.Api`, ver Restricciones); lo detecta el job de NSwag del paso 05 o la regeneración del operador.
- Si el build Release de Windows que genera el cliente necesita base de datos (hoy hay un fallback a `DummyConnectionProvider` si no conecta). Lo revela el paso 05.

## Preguntas abiertas

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloquea |
| --- | --- | --- | --- | --- | --- |
| Q0 | Todos | ¿Cómo se combina un `NEEDS_CHANGES` del paso N con el paso N+1? | a) El ciclo siguiente solo corrige; N+1 se desplaza (consume la reserva). b) Corregir y avanzar a N+1 en el mismo ciclo si la corrección es pequeña (≤ 1 fichero de producto, sin cambio de contrato); si no, a). c) Siempre corregir y avanzar. | b) (fue la respuesta del operador en TECHDEBT-003, pero se pregunta de nuevo para este plan). | No bloquea el ciclo 1; se decide en cada checkpoint con `NEEDS_CHANGES` |
| Q1 | 01 | ¿Qué extensiones cubre `.gitattributes` además de `*.cs`? | a) Solo `*.cs` (ya existe; el paso solo renormaliza). b) Añadir `text eol=lf` para `*.csproj`, `*.props`, `*.json`, `*.md`, `*.yml`/`*.yaml`, `*.ps1`, y `* text=auto` como red de seguridad; `*.sln` con `eol=crlf`. c) Otra lista. | b): cierra el hueco de OPT-015 para los ficheros que tocan los pasos 01-05 (`.csproj`, `.yml` del CI, `.md` de plan). | No (sin respuesta: a), y BUILDER lo registra) |
| Q2 | 01 | Renormalización de los ficheros ya versionados (`SqlConnectionProvider.cs` quedó en CRLF, REVIEW F1 de TECHDEBT-003). | a) Mismo paso: BUILDER ajusta `.gitattributes`, lista con `git ls-files --eol` los ficheros con `i/crlf` o sin atributo, y convierte a LF en el árbol de trabajo solo los ficheros de producto/tests/config que no sean salida generada; el operador ejecuta `git add --renormalize .` y hace el commit (`chore:` separado). b) Solo `.gitattributes`; la renormalización la hace el operador cuando quiera. c) No renormalizar. | a) (recomendación de ORCHESTRATOR). `Gaffer.Api.Client/GafferApiClient.cs` y `OpenApiSpecs.json` quedan fuera de BUILDER; si también hay que renormalizarlos, lo decide y hace el operador. | No (sin respuesta: b); BUILDER solo lista) |
| Q3 | 02 | La instrucción dice "APNETCORE_ENVIRONMENT". ¿Qué variable se lee exactamente? | a) `ASPNETCORE_ENVIRONMENT` (se asume errata). b) `ASPNETCORE_ENVIRONMENT` y, si falta, `DOTNET_ENVIRONMENT`, y si falta, `Development`. c) Literalmente `APNETCORE_ENVIRONMENT`. Además: ¿valor vacío o solo espacios = ausente? | a), con vacío/espacios = ausente → `Development`. Es la variable que ya usa `launchSettings.json`. | **Sí, el paso 02** (basta con confirmar la recomendación al aprobar el plan) |
| Q4 | 02 | ¿Entra `SetConfigurationProviders` (construye un `ConfigurationBuilder` y lo descarta) en el paso 02? | a) Sí: se elimina junto con `AddDevelopmentSettings` (código muerto; sin cambio observable porque el host ya carga `appsettings.json` y `appsettings.{Environment}.json`). b) Sí, pero haciéndolo efectivo (añadir `environment.json` a la configuración del host): comportamiento nuevo. c) No; se deja. | a). b) sería un requisito nuevo. `GetProductContext` también construye un `ConfigurationBuilder` sin usarlo: con a) se simplifica en el mismo paso. | No (sin respuesta: c), anotado) |
| Q5 | 03 | Versión de `MySqlConnector` en `Gaffer.Api`. | a) 2.6.1, la misma que `Gaffer.Api.Migrations` (una sola versión en la solución). b) Última estable y alinear también Migrations. | a). | No (sin respuesta: a)) |
| Q6 | 03 | Umbral para "la cancelación aborta la consulta". | a) `SELECT SLEEP(5)` cancelado a los ~100 ms lanza `OperationCanceledException` (o derivada) en < 2 s, la consulta ya no aparece en `information_schema.PROCESSLIST` y la conexión del proveedor sigue usable. b) Otro umbral. | a). | No (sin respuesta: a)) |
| Q7 | 03 | ¿Se revisa en este plan la nota OPT-016 (`MySql.Data` `DisposeAsync`) y la propuesta OPT-019 (cancelación de `MySql.Data`) de `dev-stack.md`? | a) Sí, al cierre: DOCUMENTER actualiza `dev-stack.md`/`dev-stack-documentation.md` por el cambio de paquete (AGENTS.md lo permite), retira o sustituye la nota OPT-016 y deja OPT-019 sin aplicar por obsoleta, apoyándose en la evidencia del paso 03. b) Lo hace el operador. | a). Marcar OPT-016/OPT-019 en `MEMORIES.md` (aplicada/revertida) es de OPTIMIZER con aprobación. | No |
| Q8 | 04 | ¿El paso 04 cabe en un ciclo? (5 endpoints, lock en 2 flujos, tests concurrentes, contrato.) | a) Un ciclo (queda la reserva). b) Dividir en 04a (CRUD) y 04b (lock + tests concurrentes); CI pasa al ciclo 6; **sin reserva**. c) Dividir 04 y fusionar 01+02 en el ciclo 1 para conservar la reserva (01+02 son pequeños y sin contrato). | c) si el operador prefiere ciclos más pequeños; a) si acepta un ciclo grande. Con b) cualquier `NEEDS_CHANGES` deja el CI fuera del límite. Si se elige b) o c), PLANNER renumera antes del ciclo afectado. | **Solo si elige c): antes del ciclo 1.** Si no, antes del ciclo 4 |
| Q9 | 04 | Semántica de `PUT /api/v1/facilities/{id}/stands/{standId}`. | a) Reemplazo completo de `StandName` y `StandCapacity` (ambos en el cuerpo; el `Id` va en la ruta, no en el cuerpo); respuesta 204. b) Igual que a) con 200 sin cuerpo (como el `PUT` de facility actual). c) Parcial (campos anulables; solo cambian los enviados). | a): coherente con el 204 elegido por el operador para el `DELETE`. | **Sí, el paso 04** (contrato) |
| Q10 | 04 | `POST .../stands` con la facility ya en 4 gradas. | a) 400 con clave nueva `Error.Facilities.Stands.Create.MaxStandsExceeded` (nombre propuesto) y sin cambios. b) 400 con la clave existente `Error.Facilities.Create.Validation`. c) 409 Conflict. | a) (recomendación de ORCHESTRATOR: 400 con clave `Error.Facilities...`). | **Sí, el paso 04** (contrato) |
| Q11 | 04 | Relación con el `PUT /facilities/{id}`, que hoy fusiona gradas (con `Id` actualiza, sin `Id` crea, las que no vienen se conservan, se descartan las que superan el máximo; paso 02 de TECHDEBT-003). | a) Sin cambios de semántica; solo se añade el lock en transacción. b) El `PUT` de facility deja de aceptar gradas (cambio incompatible; las gradas solo por el sub-recurso). c) Otra. | a). b) sería un cambio incompatible (MAJOR del cliente). | No (sin respuesta: a)) |
| Q12 | 04 | Contrato de lectura/creación y claves 404. | a) `GET .../stands` → 200 con lista de `SearchFacilityStandResult` (vacía si no hay gradas); `GET .../stands/{standId}` → 200 con `SearchFacilityStandResult`; `POST .../stands` → 201 + `Location` al `GET` de la grada + `Guid` en el cuerpo (como el `POST` de facility); cuerpo de `POST`/`PUT` = `{ standName, standCapacity }`. 404: facility inexistente → clave nueva `Error.Facilities.Stands.FacilityNotFound`; grada inexistente o ajena → clave nueva `Error.Facilities.Stands.StandNotFound`. b) Otra forma o reutilizar `Error.Facilities.NotFound` para la facility. | a). | **Sí, el paso 04** (contrato) |
| Q13 | 04 | Validación de campos de grada en `POST`/`PUT` (nombre vacío, nombre más largo que la columna, capacidad negativa). | a) Igual que hoy en creación/update de facility: sin validación nueva (un nombre demasiado largo acaba en error de BD → respuesta de fallo de infraestructura). Se anota como deuda. b) Validar y devolver 400 (claves nuevas). | a): no hay requisito; b) sería nuevo. | No (sin respuesta: a)) |
| Q14 | 04 | Qué filas bloquea el `SELECT ... FOR UPDATE`. | a) La fila de la facility (`SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE`) y, después, lectura y recuento de sus gradas en la misma transacción: serializa todas las escrituras de gradas de esa facility aunque tenga 0 gradas. b) Las filas de `Facilities_Stands` de la facility (`FOR UPDATE`): con 0 filas depende de los bloqueos de hueco del índice y del nivel de aislamiento. | a). Cumple "leer y contar con `SELECT ... FOR UPDATE` dentro de la transacción". | No (sin respuesta: a)) |
| Q15 | 04 | ¿Se puede borrar la última grada (facility con 0 gradas)? | a) Sí (hoy una facility puede crearse sin gradas). b) No (400). | a). | No (sin respuesta: a)) |
| Q16 | 04 | Versionado del cliente tras el CRUD. | a) Cambio aditivo → MINOR (`2.1.0`), el operador regenera y versiona al cerrar (y antes del ciclo 5, ver Q19). b) Otra. | a) (regla SemVer de `dev-stack.md`). | No (acción del operador) |
| Q17 | 05 | Conflicto de runners: los contenedores de servicio (`mariadb:latest`) solo existen en runners Linux y el target NSwag solo corre en `Windows_NT`. | a) Dos jobs: `ubuntu-latest` (restore, build con 0 warnings, migraciones, tests contra el servicio `mariadb:latest`) y `windows-latest` (build Release que genera el cliente, comparación con el cliente versionado, empaquetado y artefacto), el segundo con `needs:` del primero. b) Un job Linux cambiando la condición del target NSwag en `Gaffer.Api.csproj` para Linux. c) Un job Windows instalando MariaDB a mano (sin contenedor de servicio). | a): no cambia el build del proyecto. b) cambia cómo se genera el cliente (fin de línea, runtime) sin poder verificarlo en local. | **Sí, el paso 05** |
| Q18 | 05 | Cómo se demuestra la ejecución en GitHub (los agentes no hacen push). | a) BUILDER valida en local todo lo que no toca la salida generada (YAML parseable; `actionlint` si está instalado; los comandos del job Linux uno a uno contra el Docker local; `act` para el job Linux si está disponible); después, **antes de REVIEWER**, el operador sube la rama (o abre PR) y anota en `STATUS.md` la URL del run y su resultado. b) Solo validación local; el AC del run real queda "no demostrado" (el paso no puede quedar `APPROVED`). c) Solo `act` (no cubre el job Windows). | a). Sin evidencia del operador, REVIEWER no aprueba el AC del run real. | **Sí, el paso 05** |
| Q19 | 05 | El CI falla si el cliente generado difiere del versionado. Tras el paso 04 el contrato cambia, así que el cliente versionado quedará desfasado. | a) El operador regenera y versiona `Gaffer.Api.Client` (y `infoVersion`/`GlobalPackageVersion`, Q16) entre el ciclo 4 y el 5, antes del run de Q18. b) El primer run falla a propósito como demostración del control y el operador regenera después (se registran ambos runs). | a); b) sirve además como red del control "fallar si difiere". Ambos son compatibles si el operador hace dos pushes. | **Sí, el paso 05** (acción del operador) |
| Q20 | 05 | "build (con 0 warnings)" en CI: ¿en toda la solución? | a) `dotnet build gaffer-api.sln -warnaserror` en el job Linux (toda la solución, hoy 0). b) Solo lo que impone `Gaffer.Api` (`TreatWarningsAsErrors`, paso 01). | a): es la lectura literal. | No (sin respuesta: a)) |
| Q21 | 05 | Credenciales de la BD del CI. | a) Valores fijos solo para CI en el YAML (contenedor efímero, sin acceso externo), con comentario. b) Secretos del repositorio en GitHub (los crea el operador). | a); b) si el operador prefiere no tener ninguna contraseña en claro. | No (sin respuesta: a)) |
| Q22 | 05 | Cómo se lanzan las migraciones en CI. | a) `pwsh ./scripts/run-migrations.ps1` (por defecto `GafferDB_Test`, códigos de salida documentados en `test-stack.md`) con `ConnectionStrings__GafferDB_Test`. b) `dotnet run --project Gaffer.Api.Migrations -- --Target GafferDB_Test`. | a). | No (sin respuesta: a)) |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Aprobar el plan; crear la rama; confirmar Q3 (o dejarlo para antes del ciclo 2); decidir Q8 si elige c). Opcionales: Q1, Q2. |
| Tras el ciclo 1 | Si Q2 = a): `git add --renormalize .` y commit separado. |
| Antes del ciclo 4 | Q9, Q10, Q12 (contrato). Q8 si no se decidió antes. |
| Entre el ciclo 4 y el 5 | Regenerar y versionar `Gaffer.Api.Client` (Q16, Q19). Responder Q17, Q18. |
| Durante el ciclo 5 (entre BUILDER y REVIEWER) | Push de la rama o PR y registro del run (Q18 = a)). |
| En cada checkpoint con `NEEDS_CHANGES` | Decidir Q0. |

## Dependencias con HARNESS-001

- OPT-015 (`.gitattributes`) se absorbe en el paso 01 de este plan.
- OPT-016 y OPT-019 (comportamiento de `MySql.Data`) se revisan aquí tras el paso 03 (Q7). OPT-012 (tipo de excepción del driver) depende de la evidencia del paso 03.
- Los cambios de harness de `HARNESS-001` (CLAUDE.md, AGENTS.md, agentes) los aplica el operador; pueden entrar entre ciclos de este plan (ver `HARNESS-001/00`).

## Fuera de alcance (anotado, no se planifica)

- N+1 en `SqlFacilityRepository.Get`, paginación, fallback silencioso a `DummyConnectionProvider`/health checks, validación de entrada general (salvo Q13 b)), carpeta `Features_Refactor`, `CriteriaType.NONE`, camelCase de ProblemDetails.
- `.github/doc/features/facility-crud.md` (formato legado, desfasado respecto al `PUT` de facility; aviso de DOCUMENTER en TECHDEBT-003): lo corrige o retira el operador.
- Publicar el paquete (el CI solo lo deja como artefacto).
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release` sobre `Gaffer.Api` ni sobre la solución (regeneraría la salida). El workflow del paso 05 solo compara; si hay que regenerar, lo hace el operador.
- Tests con BD contra el Docker local `Gaffer.Database` (MariaDB 12.3.2); nunca se omiten. Si no está disponible: `BLOCKED` con el error exacto.
- Cambios de DI: `TestFixture` reutiliza `ConfigureRepositories`/`ConfigureActions` (TECHDEBT-003 paso 04) y `TestServerDependenciesShould` detecta registros que falten; se revalida en el mismo incremento.
- Cambio de dependencia (paso 03): BUILDER y REVIEWER leen `.github/doc/common/dev-stack-documentation.md` y la documentación oficial enlazada. Actualizar `dev-stack.md`/`dev-stack-documentation.md` es del cierre (DOCUMENTER).
- Sin operaciones de Git. Máximo 6 ciclos.
