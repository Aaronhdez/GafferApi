# TECHDEBT-003 - Resumen, alcance y preguntas abiertas

## Petición del operador (literal)

> "genera la planificación para 1,2,4,7 y incluye además la optimización de SqlConnectionProvider, Update de Facility, Cancelación, Cambios pequeños y Warnings. Considera cada punto como un ciclo de desarrollo para poder ir viendo escalonadamente"

Este plan cubre la parte de optimización ("SqlConnectionProvider, Update de Facility, Cancelación, Cambios pequeños y Warnings"). Los bugs (1, 2, 4, 7) están en `.github/plans/TECHDEBT-002/`. Se divide en dos planes por el límite de 6 ciclos por plan.

## Dependencia

**Este plan empieza cuando TECHDEBT-002 esté `DONE`.** Varios pasos tocan los mismos ficheros (`UpdatePlayerCommandHandler`, `ExceptionHandlingMiddleware`, `*CommandHandler`, `TestFixture`, conversores). BUILDER revalida la línea base al inicio del ciclo 1 **sobre el resultado de TECHDEBT-002** (el 159/159 dado por el operador es anterior y el recuento cambiará).

## Objetivo

Aplicar las 5 optimizaciones sin cambiar el comportamiento HTTP observable salvo donde un paso lo diga (IDs de grada estables en el paso 02; respuesta a peticiones canceladas según Q4 en el paso 03).

## Secuenciación: un paso = un ciclo (requisito del operador)

| Ciclo | Paso | Fichero | Punto |
| --- | --- | --- | --- |
| 1 | 01 | `01-sql-connection-provider-lifecycle.md` | `SqlConnectionProvider`: cerrar sin liberar, apertura asíncrona |
| 2 | 02 | `02-facility-update-stable-stand-ids.md` | Update de Facility con IDs de grada estables |
| 3 | 03 | `03-request-cancellation.md` | Propagar el `CancellationToken` de la petición hasta Dapper |
| 4 | 04 | `04-small-changes.md` | Cambios pequeños (TestFixture, `IGafferApiClient`, `nswag.json`, código comentado, `Player`, versionado) |
| 5 | 05 | `05-build-warnings.md` | 0 warnings (va el último: los pasos anteriores tocan los mismos ficheros) |
| 6 | - | - | Reserva para `NEEDS_CHANGES` |

Reglas:

- En el ciclo N, BUILDER implementa **solo el paso N**, más las correcciones que REVIEWER haya pedido en el ciclo anterior (si las hay). No adelanta trabajo de pasos posteriores.
- En el ciclo N, REVIEWER **solo aprueba el paso N** (y verifica las correcciones del ciclo anterior).
- Solo queda **un** ciclo de reserva (el 6). Cómo se combina una corrección con el paso siguiente es Q0 (abierta). Con un único ciclo de reserva, dos `NEEDS_CHANGES` que desplacen pasos dejarían el paso 05 fuera del límite: el operador lo decide en el checkpoint.
- Doble bucle de `.github/doc/work-guidelines.md` en cada paso: aceptación → unitario → refactor con revalidación. En pasos sin cambio de comportamiento (01, 04, 05) la evidencia es: tests de caracterización en verde antes y después, comprobaciones estáticas (`grep`) y suite completa en verde con recuento antes/después.

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| SqlConnectionProvider | `SqlConnectionProvider.cs`: `ClearAsync` L63, `CreateAsync` L77, `QueryScalarAsync` L88, `QueryAsync` L100, `ExecuteAsync` L112 y el `finally` de `ExecuteInTransactionAsync` L144 llaman `CloseAsync()` + `DisposeAsync()` sobre `_sqlConnection` (readonly, se reutiliza después). `ClearAsync` L54 y `CreateAsync` L71 usan `Open()` síncrono. Además: `QueryScalarAsync`/`QueryAsync`/`ExecuteAsync` no usan `try/finally`, así que una consulta que falla deja la conexión abierta hasta el fin del scope (ver Q1). Tests de BUGFIX-001: `SqlConnectionProviderShould` (5 tests) y `DatabaseStartupShould`. | Sí |
| Update de Facility | `UpdateFacilityCommandHandler.BuildUpdatedFacility` L85-87: `new Stand(Guid.NewGuid(), ...)`. La dirección conserva `existing.Address.Id` (L77). `SqlFacilityRepository.Update` L143-146: `DeleteStands` + `DeleteAddress` + `InsertAddress` + `InsertStands` en la misma transacción. `UpdateFacilityStandRequest`/`UpdateFacilityStandCommand` no llevan `Id`. `GET /facilities/{id}` sí devuelve el `Id` de cada grada (`SearchFacilityStandResult(Guid Id, ...)`), así que el cambio es observable por HTTP. | Sí |
| Cancelación | `PlayerController` (5 llamadas) y `FacilitiesController` (5 llamadas) usan `CancellationToken.None`. Handlers y repositorios reciben el token pero `IDatabaseConnectionProvider` no tiene parámetro de token: ningún repositorio puede pasarlo. Implementaciones: `SqlConnectionProvider`, `DummyConnectionProvider`. | Sí |
| TestFixture | `TestFixture.cs` L109-120: 11 `AddScoped` + `AddHttpContextAccessor` (12 registros). Equivalentes en producción: `DatabaseStartup` L38, `RepositoriesStartup` L12-13, `ActionsStartup` L18-29. `AddHttpContextAccessor` no se registra en producción. TestFixture también duplica el bloque de versionado (L91-106) con dos `new GafferApiVersioningDefinition()`. | Sí |
| `IGafferApiClient` | `Gaffer.Api/Shared/Clients/IGafferApiClient.cs`; usos: solo `Gaffer.Api.Tests` (`TestFixture`, `GafferApiClientTest`, 4 clases de test). Sin uso en `Gaffer.Api.Client`. | Sí |
| `nswag.json` | L33 `"infoTitle": "My Title"`. `Program.cs` L36: `ProductContext.Name = "Gaffer Api"`. | Sí |
| Código comentado | `Startup.cs` L52-55 (4 líneas); `SwaggerStartup.cs` L27-44 (bloque OAuth2 de IdentityServer). | Sí |
| `Player.cs` | `SetLastName(string commandFirstName)`, `SetSecondLastName(string commandFirstName)`, `SetBirthDateName(string commandFirstName)`, `SetGender(string commandFirstName)`, `GetBirthDateName()`. Puede cambiar en TECHDEBT-002 paso 01; el paso 04 parte del estado resultante. | Sí (estado previo a TECHDEBT-002) |
| Versionado | `VersioningStartup.cs` L12 y L20: dos `new GafferApiVersioningDefinition()` (el segundo oculta al primero). | Sí |
| Warnings | Dato del operador: 70 warnings en `dotnet build --no-incremental` (recuentos por código suman 140 porque la salida de MSBuild repite cada warning). `Gaffer.Api.csproj` L8 `TreatWarningsAsErrors = false`. | Parcial (dato del operador; BUILDER lo mide) |

## No verificado

- Si `MySql.Data` respeta de verdad el token en `OpenAsync`/`ExecuteAsync` (hay versiones que solo comprueban el token al inicio). El paso 03 lo mide con un token ya cancelado y, si se puede, con una consulta larga (`SELECT SLEEP(n)`).
- Qué proyectos emiten los warnings y si alguno sale de la salida generada de `Gaffer.Api.Client` (fuera de alcance de BUILDER). El paso 05 lo mide primero.
- Si corregir anulabilidad (CS8618/CS8625...) en DTOs cambia el OpenAPI (NSwag lee la anulabilidad). El paso 05 lo controla con AC5.3.

## Preguntas abiertas

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloquea |
| --- | --- | --- | --- | --- | --- |
| Q0 | Todos | ¿Cómo se combina un `NEEDS_CHANGES` del paso N con el paso N+1? | a) El ciclo siguiente solo corrige; el paso N+1 se desplaza (consume la única reserva). b) Corregir primero y avanzar al paso N+1 en el mismo ciclo solo si la corrección es pequeña (p. ej. ≤ 1 fichero de producto y sin cambio de contrato); si no, a). c) Siempre corregir y avanzar en el mismo ciclo. | b). Con una sola reserva, a) solo cabe una vez. Lo decide el operador en cada checkpoint con `NEEDS_CHANGES`. | No bloquea el ciclo 1 |
| Q1 | 01 | ¿Entra cerrar la conexión también cuando la consulta falla (`try/finally` en `QueryScalarAsync`/`QueryAsync`/`ExecuteAsync`)? Hoy queda abierta hasta que termina el scope. | a) Sí, mismo paso (mismo método, mismo objetivo "cerrar sin liberar"). b) No; se anota como deuda. | a). | No (sin respuesta, BUILDER no lo hace y lo deja anotado) |
| Q2 | 02 | ¿Cómo se identifica una grada existente en la petición de update? El contrato no lleva el `Id` de la grada. | a) `Id` opcional en `UpdateFacilityStandRequest`: con `Id` de una grada de esa facility → se actualiza; sin `Id` → se crea; las existentes que no vienen → se borran; `Id` desconocido o de otra facility → 400 (clave a definir). Cambio de contrato. b) Emparejar por `StandName` (exige que el nombre sea único por facility; hoy no hay restricción verificada). c) Emparejar por posición en la lista. | a): es explícito y no depende de nombres ni del orden. b) sin cambio de contrato pero ambiguo con nombres repetidos o renombres. | Sí, paso 02 |
| Q3 | 02 | La dirección ya conserva su `Id`, pero se borra y reinserta. ¿Se pasa a `UPDATE` en el sitio? | a) Sí (mismo paso: el repositorio deja de borrar/insertar dirección y gradas en el update). b) No; solo gradas. | a): la petición cita gradas y dirección. | No (sin respuesta se aplica a)) |
| Q4 | 03 | ¿Qué respuesta y qué log produce una petición cancelada por el cliente (`OperationCanceledException` con `RequestAborted` cancelado)? Hoy el middleware la trataría como desconocida → 500 + `LogError` (tras TECHDEBT-002). | a) El middleware no escribe respuesta (el cliente ya no escucha) y registra en `Information`/`Debug`, sin 500 ni `LogError`. b) 499 (convención no estándar de nginx). c) Dejarlo como 500. | a). | Solo AC3.5 |
| Q5 | 04 | ¿El paso de TestFixture incluye también el bloque de MVC/versionado duplicado (L82-108, dos `new GafferApiVersioningDefinition()`), llamando a `ConfigureVersioning()`? | a) Sí: TestFixture reutiliza `ConfigureVersioning` además de `ConfigureRepositories`/`ConfigureActions`. b) No: solo los 12 registros pedidos. | a): resuelve a la vez la duplicación de `GafferApiVersioningDefinition` en tests. | No (sin respuesta se aplica b)) |
| Q6 | 04 | Valor de `infoTitle` en `nswag.json`. | a) `"Gaffer Api"` (`ProductContext.Name`). b) `"Gaffer API"`. c) Otro. | a). Afecta al título del cliente generado (lo regenera el operador). | Solo AC4.5 |
| Q7 | 05 | ¿Se activa `TreatWarningsAsErrors` al terminar? ¿En qué proyectos? | a) Sí, en `Gaffer.Api` (hoy `false`). b) Sí, en todos los proyectos no generados (`Gaffer.Api`, `Gaffer.Api.Tests`, `Gaffer.Api.Migrations`), p. ej. en `Directory.Build.props` con condición. c) No; solo 0 warnings. | a) o b), solo si el paso 05 llega a 0. Con b), cuidado con `Directory.Build.props` (hoy condicionado a `Gaffer.Api.Client`, TECHDEBT-001 Q16). | Solo AC5.5 |
| Q8 | 05 | Si hay warnings que solo se quitan cambiando el contrato OpenAPI (p. ej. hacer anulable una propiedad de un DTO) o que vienen de la salida generada del cliente, ¿qué se hace? | a) No cambiar el contrato: usar `= null!`/`default!`/`required` solo si no altera el OpenAPI, o suprimir el código en el `.csproj` con justificación; la salida generada queda fuera (el operador decide). b) Aceptar cambios de contrato y regenerar el cliente. | a). | No (se aplica a) salvo respuesta) |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Confirmar TECHDEBT-002 `DONE`; crear la rama del plan; responder Q1 si quiere a). |
| Antes del ciclo 2 | Responder Q2 (bloqueante); Q3 opcional. |
| Antes del ciclo 3 / 4 / 5 | Q4 / Q5 y Q6 / Q7 y Q8. |
| En cada checkpoint con `NEEDS_CHANGES` | Decidir Q0 para ese ciclo. |
| Al cerrar (tras `DONE`) | Regenerar `Gaffer.Api.Client`: cambia el contrato si Q2 = a (`Id` de grada) y el título si cambia `infoTitle`. Versión según la regla SemVer de TECHDEBT-001. |

## Fuera de alcance (anotado, no se planifica)

- N+1 en `SqlFacilityRepository.Get`, paginación, entorno fijado a `"Development"` en `Program`/`ProductContext`, fallback silencioso a `DummyConnectionProvider`/health checks, validación de entrada, carpeta `Features_Refactor` duplicada, `CriteriaType.NONE`, camelCase de ProblemDetails (Q5 de TECHDEBT-001).
- Cambiar de driver (`MySql.Data` → `MySqlConnector`).
- `AddHttpContextAccessor` de TestFixture: no duplica nada de producción; se deja salvo que el paso 03 lo haga innecesario (se registra).
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client` (sí `nswag.json`, que es configuración). BUILDER no ejecuta `dotnet build -c Release` sobre `Gaffer.Api` ni sobre la solución.
- Los tests con MySQL se ejecutan contra Docker local (`Gaffer.Database`); nunca se omiten. Si Docker o la BD no están disponibles, `BLOCKED` con el error exacto.
- Cambios de DI en `Gaffer.Api` se reflejan en el mismo incremento en `Gaffer.Api.Tests/TestFixture.cs` (lección permanente de AGENTS.md; el paso 04 la hace estructural).
- Sin paquetes nuevos salvo necesidad justificada en `STATUS.md`. Sin operaciones de Git. Máximo 6 ciclos.
