# TECHDEBT-005 - Resumen, alcance y preguntas abiertas

## Petición del operador (literal, transmitida por ORCHESTRATOR)

| Tema | Literal / dato | Paso |
| --- | --- | --- |
| Orden | "haz el 1 y luego el 2". Este plan es el "2": un plan corto antes de la siguiente feature. Lo que era el "1" no se transmitió a PLANNER y queda fuera de este plan. | - |
| Punto 1 | `IFacilityRepository.Update(Facility)` es público y no toma el bloqueo. Objetivo: que no forme parte del contrato público, sin perder la cobertura de los tests que lo usan. | 01 |
| Punto 2 | Validación de entrada en Player, Facility y gradas (`CreatePlayerRequest`, `UpdatePlayerRequest` con PUT parcial de TECHDEBT-002, `CreateFacilityRequest`/`UpdateFacilityRequest` con dirección y gradas anidadas, `FacilityStandRequest`, que quedó sin validar en TECHDEBT-004 Q13). | 02, 03a, 03b |
| Punto 3 | Fallback silencioso a `DummyConnectionProvider`: hacer visible el estado con un health check (readiness con comprobación de BD). | 05 |
| Aprobación | Plan aprobado por el operador el 2026-10-11 (transmitido por ORCHESTRATOR): "vamos con el 2". | - |

## Estado de partida (dato de ORCHESTRATOR, 2026-10-11, `main` f24e594)

- `dotnet build --no-incremental`: 0 warnings, 0 errores (`TreatWarningsAsErrors` en `Gaffer.Api`). `dotnet test`: 322/322 contra el Docker `Quionis_DB` (mariadb:latest 13.0.2, puerto 3306). CI en verde. Cliente NSwag 2.1.0.
- BUILDER revalida la línea base al inicio del ciclo 1 (build, suite, `docker ps`).

## Secuenciación: un paso = un ciclo

| Ciclo | Paso | Fichero | Contenido |
| --- | --- | --- | --- |
| 1 | 01 | `01-facility-repository-unlocked-update.md` | Retirar `Update(Facility)` del contrato público del repositorio sin perder cobertura |
| 2 | 02 | `02-player-input-validation.md` | Validación de dominio de `Player` (create y PUT parcial) + conversión común del 400 |
| 3 | 03a | `03a-facility-and-address-input-validation.md` | Ayudante `[Explicit]` de volcado del `swagger.json` (OPT-034) + validación de dominio de `Facility` y `Address` en create/update |
| 4 | 03b | `03b-stand-input-validation.md` | Validación de dominio de `Stand`: gradas anidadas en create/update de facility y sub-recurso `/stands` (`FacilityStandRequest`) |
| 5 | 05 | `05-database-health-check.md` | `/health/live` y `/health/ready`; se mantiene el fallback a `DummyConnectionProvider` |
| 6 | - | - | Reserva para `NEEDS_CHANGES` (única) |

Re-secuenciación del 2026-10-12 (OPT-036): el antiguo paso 03 se divide en 03a/03b y el antiguo 04 pasa a 05; la reserva baja de 2 a 1 ciclo. `03-facility-and-stand-input-validation.md` y `04-database-health-check.md` quedan como ficheros de redirección sin criterios (el operador los borra).

Reglas:

- En el ciclo N, BUILDER implementa **solo el paso N** más las correcciones que REVIEWER pidió en el ciclo anterior. REVIEWER solo aprueba el paso N (y verifica esas correcciones).
- Doble bucle de `.github/doc/work-guidelines.md` en cada paso: aceptación → unitario → refactor con revalidación. En el paso 01 (sin cambio observable por HTTP) la aceptación es caracterización en verde antes y después.
- Orden justificado: 01 es pequeño, sin contrato y sin preguntas bloqueantes; 02 fija el mecanismo y el formato de error que reutilizan 03a/03b; 03a crea el ayudante que usan 03b y 05; 05 es independiente. Q2 = a) con la división de OPT-036: orden fijo 01 → 02 → 03a → 03b → 05; el 05 no se adelanta.

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| `Update(Facility)` público sin lock | `IFacilityRepository.cs` L9 `Task Update(Facility, CancellationToken)`; L16 la sobrecarga `Update(Guid, Func<Facility,Facility>, CancellationToken)`. `SqlFacilityRepository.cs` L121 (público, abre su propia transacción sin `FOR UPDATE`); L204-214 la sobrecarga bloquea (`LockFacility`, L275-277 `SELECT Id ... FOR UPDATE`), lee y llama a `Update(buildUpdated(facility))` en L212. | Sí |
| Usos de `Update(Facility)` | Producción: solo L212 de `SqlFacilityRepository`. `UpdateFacilityCommandHandler` L36 usa la sobrecarga con lock. Tests: `SqlFacilityRepositoryShould` L254 (`UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`), L308 (`LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails`), L430 (`PassItsTokenToTheProviderWhenUpdating`, `minimumCalls: 7`). Los campos del test son del tipo concreto `SqlFacilityRepository` (L15, L382). Ningún sustituto NSubstitute de `IFacilityRepository` configura `Update(Facility)`. | Sí |
| Validación actual | `[ApiController]` en `PlayerController`, `FacilitiesController` y `FacilityStandsController`. Solo hay `[Required]` en `Address` y `[MaxLength(4)]` en `Stands` (`FacilitiesController.cs` L98-102, L129-133). Dominio: `Facility.cs` L52 (dirección obligatoria) y L58-60 (máx. 4) lanzan `FacilityValidationException` → 400 `Error.Facilities.Create.Validation` (`ExceptionToHttpResponseConverters.cs` L57-61, también en el update). `Player.cs` sin validación (propiedades con `set` público y métodos `Set*` L26-79). Sin `InvalidModelStateResponseFactory` ni `SuppressModelStateInvalidFilter`. `Startup` usa `AddMvcCore()` (no `AddControllers()`). | Sí |
| Construcción del dominio | Creación: `CreatePlayerCommandHandler` L21, `CreateFacilityCommandHandler` L33-46, `UpdateFacilityCommandHandler` L46-65 y L91-94 (dentro del lock), `FacilityStandCommandHandlers` L21 y L40 (antes de llamar al repositorio). PUT de player: `UpdatePlayerCommandHandler` L13-14 lee el player (404 si no existe) y aplica `Set*` L37-61. **Rehidratación**: `SqlPlayerRepository` L22 (`QueryAsync<Player>`, Dapper usa el constructor principal) y `SqlFacilityRepository` L26, L73, L83 (`new Facility/Address/Stand`). | Sí |
| PUT parcial de Player hoy (TECHDEBT-002) | `UpdatePlayerCommandHandler` L39-47: `FirstName`/`LastName` vacíos se **ignoran** (no cambian). L49-50: `SecondLastName` ausente, `null` o `""` **lo borra** (tests `ReplaceTheSecondLastNameAndKeepTheRest` `[TestCase("", null)]`, `[TestCase(null, null)]` y `KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent`). Un cuerpo sin campos hoy **no** es un no-op: borra `SecondLastName`. | Sí (corrige lo que decía Q6 b) en la versión anterior de este fichero: "hoy no se puede borrar" era incorrecto). Q17 lo cambia. |
| OpenAPI de los 400 | Ningún controlador declara `ProducesResponseType` 400 (solo 201/204). | Sí |
| Tests de 400 existentes | `FacilityFeaturesShould` L118 y L152 aceptan `BadRequest` **o** `UnprocessableEntity` (dirección ausente, 5 gradas); L525-531 leen `Extensions.ErrorTranslationKey` (PascalCase). `FacilitiesControllerContractShould` L76-81 comprueba `[MaxLength(4)]` por reflexión. | Sí |
| Tamaño de columnas | Migraciones `M0001`/`M0002`: todos los textos son `AsString()` sin longitud (VARCHAR(255) por defecto de FluentMigrator; TECHDEBT-004 AC4.11 confirma que 300 caracteres fallan en BD). Capacidades `AsInt32()`, fechas `AsDateTime()`. | Sí (255 inferido del valor por defecto) |
| Fallback a Dummy | `DatabaseStartup.cs` L24-36: Dummy si `Enabled` es falso o no hay cadena, y también si la sonda de arranque falla (log de aviso). Registro definitivo: la API no se recupera aunque la BD vuelva. `DummyConnectionProvider.TestConnection()` devuelve **`true`** (L5) y el resto lanza `NotImplementedException`. Tests: `Integration/DatabaseStartupShould.cs` L75, L86, L110 fijan el fallback actual. | Sí |
| Health checks | Ninguna referencia a `AddHealthChecks`/`MapHealthChecks` en `Gaffer.Api`. `Startup.Configure` mapea solo controladores (L72-73). | Sí |
| CI | `ci.yml` job `client-and-package` (Windows) con `DatabaseConfig__Enabled: "false"`, build Release que regenera el cliente y `git diff --exit-code` sobre `GafferApiClient.cs` y `OpenApiSpecs.json`. | Sí |

## No verificado

- Si con `AddMvcCore()` los DataAnnotations se evalúan hoy y qué produce exactamente el 400 de "dirección ausente" (atributo `[Required]`, nulabilidad implícita o un error posterior), con qué cuerpo. Igual para JSON mal formado y valores de enum inválidos (texto o número fuera de rango). **BUILDER lo caracteriza al inicio del ciclo 2** (estado y cuerpo por caso, en `STATUS.md`) antes de cambiar nada.
- Si Newtonsoft acepta números de enum no definidos (p. ej. `"Gender": 99`) y los pasa al dominio, o si el model binding los rechaza. **AC2.0 lo caracteriza** (afecta a Q19).
- Con Q3 = b) (solo dominio) no se añaden atributos a los DTO ni se pide documentar el 400, así que no se espera ningún cambio en el OpenAPI. Se confirma comparando el `swagger.json` servido antes y después (AC2.6, AC3.7). Si hay diferencias, se aplica Q10.
- Si hay filas guardadas que incumplen las reglas nuevas (nombres de 101-255 caracteres, players menores de 16 años, capacidades negativas, fechas fuera de rango). Con Q16 se siguen leyendo; un PUT de player que no toque el campo inválido no lo valida, y un PUT de facility (reemplazo con fusión) sí valida los campos enviados.
- Si `MapHealthChecks` aparece en el documento de NSwag (normalmente no, porque no pasa por ApiExplorer). Se comprueba en el paso 05.
- El efecto de reglas nuevas en consumidores (`quionis-bff-api`, `quionis-frontend`): fuera del repositorio; lo valora el operador.

## Preguntas resueltas (2026-10-11)

Origen: "operador" = opción literal elegida por el operador (transmitida por ORCHESTRATOR); "por defecto" = sin respuesta, se aplica la recomendación de PLANNER.

| ID | Paso | Respuesta | Origen |
| --- | --- | --- | --- |
| Q0 | Todos | b) Corregir y avanzar si la corrección es pequeña (≤ 1 fichero de producto, sin cambio de contrato); si no, el ciclo siguiente solo corrige. Se confirma en cada checkpoint con `NEEDS_CHANGES`. | por defecto |
| Q1 | 01 | a) Se quita de la interfaz y pasa a `private` en `SqlFacilityRepository`; los 3 tests usan `Update(id, _ => updated, ...)` con las mismas aserciones. | por defecto |
| Q2 | 02-04 | a) 02 Player, 03 Facility + gradas, 04 health; 2 ciclos de reserva; sin adelantar el 04. | por defecto (confirmado por ORCHESTRATOR) |
| Q3 | 02, 03 | **"Solo dominio"**: los constructores y métodos de `Player`, `Facility` y `Stand` lanzan excepciones que convierte el middleware (como `FacilityValidationException`); **sin DataAnnotations en los DTO**. El PUT parcial de Player modifica el dominio a través de setters; el sub-recurso `/stands` crea `Stand` directamente. Sustituye a la recomendación d). | operador |
| Q4 | 02, 03 | **"ProblemDetails + clave"**: el mismo formato que el middleware. `Extensions.ErrorTranslationKey` por operación: `Error.Players.Create.Validation`, `Error.Players.Update.Validation`, `Error.Facilities.Create.Validation`, `Error.Facilities.Update.Validation`, `Error.Facilities.Stands.Validation`; `Extensions.Errors` con los mensajes por campo. Los mensajes no repiten el valor recibido. | operador |
| Q5 | 02 | **"Con edad mínima"**: `FirstName`, `LastName` de 1 a 100 caracteres, sin aceptar solo espacios; `SecondLastName` opcional, pero no vacío (ni solo espacios) y ≤ 100 si llega; `BirthDate` entre 1900-01-01 y hoy (UTC); `Gender` con un valor definido del enum; **edad mínima de 16 años** (el operador no indicó otra cifra). | operador |
| Q6 | 02 | **"Reglas de Q5 en lo enviado"**: solo se validan los campos presentes; cadena vacía o solo espacios → 400; cuerpo sin campos = no-op. Con Q17 el no-op es real también para `SecondLastName`. | operador |
| Q7 | 02, 03 | a) Validar sin modificar (sin `Trim`). | por defecto |
| Q8 | 03 | **"Sí, tal cual"**: `Name` 1-100; `BuildDate` entre 1800-01-01 y hoy (UTC); `Type` con un valor definido; `Capacity` ≥ 0 sin máximo; dirección: todos los campos obligatorios y no vacíos, `StreetNumber` y `PostalCode` ≤ 20, el resto ≤ 100, sin formato por país. | operador |
| Q9 | 03 | **"Solo forma"**: `StandName` 1-100; `StandCapacity` ≥ 0; sin unicidad de nombres ni regla de suma. | operador |
| Q10 | 02, 03 | a) MINOR **solo si el contrato cambia**. Revisión con Q3 = b): no se añaden atributos ni `ProducesResponseType(400)` (no se pidió documentar el 400), así que se espera un OpenAPI y un cliente idénticos y **ninguna regeneración**. El prerrequisito OPT-030 (regenerar y versionar antes del push) solo se activa si la comparación de AC2.6/AC3.7 detecta diferencias o si se elige Q20 b). | por defecto (revisada) |
| Q11 | 04 | **"live + ready, texto"**: `/health/live` (200 sin dependencias) y `/health/ready` (comprueba la BD: 200 o 503), cuerpo de texto `Healthy`/`Degraded`/`Unhealthy`, 5 s como máximo, sin paquete nuevo. | operador |
| Q12 | 04 | **"Mantener Dummy"**: se mantiene el fallback a `DummyConnectionProvider` si la sonda de arranque falla; readiness informa `Unhealthy` (503). Sustituye a la recomendación c). | operador |
| Q13 | 04 | a) Con `Enabled = false` o sin cadena (Dummy intencionado): `Degraded` con 200. | por defecto |
| Q14 | 04 | a) `/health/*` fuera del OpenAPI y del cliente NSwag. | por defecto |
| Q15 | 04 | a) Sin cambios en CI. | por defecto |
| Q16 | 02, 03 | **"Validar solo al crear/actualizar"**: las reglas se aplican en los caminos de creación y modificación (constructor o factoría que usan los handlers y métodos `Set*`). La rehidratación desde la BD (Dapper en `SqlPlayerRepository` L22; `SqlFacilityRepository` L26, L73, L83) **no valida**: los datos antiguos que incumplan las reglas se siguen leyendo sin error (AC2.9, AC3.10). | operador (2026-10-11) |
| Q17 | 02 | **"Ausente/null no cambia"**. Texto literal de la opción: "Coherente con el resto del PUT parcial: omitirlo no lo toca; \"\" → 400. Entonces no habría forma de borrarlo (cambia el contrato de TECHDEBT-002)." `SecondLastName` ausente o `null` = no cambia; `""` o solo espacios → 400. **No se añade ninguna forma nueva de borrarlo.** Cambio de comportamiento respecto a TECHDEBT-002: ver la sección siguiente. | operador (2026-10-11) |
| Q23 | 03a | **"Sí, incluirlo"**. Texto literal de la opción: "Solo el mapeo tolera la dirección ausente (sin atributos, rutas ni DTO nuevos); el dominio devuelve 400 Error.Facilities.Create/Update.Validation." `FacilitiesController.cs` entra en el alcance de 03a con esa limitación; el caso "dirección ausente" de AC3a.1/AC3a.2/AC3a.11 deja de estar bloqueado. | operador (2026-10-12) |

## Cambio de comportamiento respecto a TECHDEBT-002 (Q17)

| Caso `PUT /players/{id}` | Hoy (TECHDEBT-002) | Tras el paso 02 |
| --- | --- | --- |
| `secondLastName` ausente | Lo borra | No cambia |
| `secondLastName: null` | Lo borra | No cambia |
| `secondLastName: ""` o solo espacios | Lo borra (`""`) / lo guarda (espacios) | 400 `Error.Players.Update.Validation` |
| Cuerpo sin campos | Borra `secondLastName` | No-op real |
| Borrar el segundo apellido | Posible (ausente/`null`/`""`) | **Ya no es posible** por la API (decisión del operador) |

Tests actuales que fijan el comportamiento anterior (se adaptan en el ciclo 2 y se listan en `STATUS.md`):

| Test | Hoy espera | Pasa a esperar |
| --- | --- | --- |
| `Unit/Commands/UpdatePlayerCommandHandlerShould.KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent` (L66-81) | `SecondLastName == null` | El valor original (y el nombre del test se ajusta) |
| `Unit/Commands/UpdatePlayerCommandHandlerShould.ReplaceTheSecondLastNameAndKeepTheRest` (L83-101) | `("", null)` y `(null, null)` | `""` → excepción de validación sin llamar a `Update`; `null` → valor original; `("Nuevo", "Nuevo")` sin cambios |
| `Acceptance/PlayerFeaturesShould.UpdateAPlayer` (L102-116) | `SecondLastName` nulo tras `{"firstName":"X"}` (L115) | El valor original |
| `Acceptance/PlayerFeaturesShould.ReplaceTheSecondLastNameOnUpdate` (L150-161) | `null` → nulo (L151); `""` → nulo (L152) | `null` → valor original; `""` → 400 y sin cambios |

Documentación afectada (para DOCUMENTER, solo tras `APPROVED`): `doc/features/player-update.md` (L5, L17, L24, L30 describen el borrado y la excepción de `secondLastName`); `doc/features/technical/player-update.md` (L5, L29, L46, L56, L69, L80, L106: diagrama, tabla de métodos, ejemplos y lista de tests); `doc/features/technical/api-client-versioning.md` L57 (nota histórica sobre el borrado al omitir; revisar si necesita una referencia al cambio). Es un cambio de comportamiento visible para el cliente sin cambio de esquema: con Q10 a) el cliente generado no cambia, pero el operador puede tenerlo en cuenta al versionar la siguiente publicación.

## Preguntas abiertas nuevas

Surgen al aplicar las respuestas. Q16 y Q17 ya están resueltas (tabla anterior). Q18-Q22 no bloquean: sin respuesta se aplica la recomendación a). Q23 resuelta el 2026-10-12 ("Sí, incluirlo"); ya no bloquea.

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER | Bloquea |
| --- | --- | --- | --- | --- | --- |
| Q18 | 02, 03 | Contenido de `Extensions.Errors`. | a) Todos los campos inválidos de la petición en una sola respuesta (el dominio comprueba todos los campos antes de lanzar; en el PUT parcial, el handler comprueba todos los campos enviados antes de aplicar ninguno). Clave = nombre del campo del DTO (`FirstName`, `Address.Street`, `Stands[1].StandName`). b) Solo el primer campo inválido. | a): Q4 habla de mensajes por campo, y el frontend puede marcar todos a la vez. | No (sin respuesta: a)) |
| Q19 | 02, 03 | Errores de model binding (JSON mal formado, enum como texto desconocido, tipo incorrecto y, si AC2.0 lo muestra, campos obligatorios no anulables): no llegan al dominio. | a) Configurar `InvalidModelStateResponseFactory` (configuración de la respuesta, no son DataAnnotations) para devolver el formato de Q4 con la clave de la operación y mensajes genéricos por campo (los mensajes de Newtonsoft pueden incluir el valor recibido). b) Dejar el 400 automático de ASP.NET tal cual (otro formato y posible reflejo de la entrada). c) Formato de Q4 con una clave genérica `Error.Validation`. | a): la opción Q4 que eligió el operador ya decía "Aplica también a JSON mal formado y enums inválidos". Los números de enum no definidos que lleguen al dominio (AC2.0) los rechaza la regla de Q5/Q8. | No (sin respuesta: a)) |
| Q20 | 03 | Atributos existentes `[Required]` (`Address`) y `[MaxLength(4)]` (`Stands`) en los DTO de facility, frente a "sin DataAnnotations en los DTO". | a) Se mantienen (solo no se añaden nuevos): el OpenAPI no cambia y `FacilitiesControllerContractShould` L76-81 sigue igual. b) Se quitan: el OpenAPI pierde `required`/`maxItems`, el cliente cambia y se aplica Q10 (regeneración y versión). | a): el dominio ya aplica ambas reglas y quitarlos cambia el contrato sin beneficio. | No (sin respuesta: a)) |
| Q21 | 02, 03 | Orden entre 400 de validación y 404. Al validar en el dominio, el orden lo marca el handler: `PUT /players/{id}` y `PUT /facilities/{id}` leen primero (404 antes que 400); `POST`/`PUT .../stands` construyen `Stand` antes de ir al repositorio (400 antes que 404). | a) Aceptar el orden natural de cada handler y fijarlo en los tests. b) Forzar siempre 400 antes que 404 (en el PUT de player habría que validar los valores enviados antes de leer, fuera de los setters). | a): b) duplica las reglas fuera de los setters, contra Q3. | No (sin respuesta: a)) |
| Q22 | 02 | Cálculo de la edad mínima. | a) Se acepta si `BirthDate.AddYears(16) ≤ hoy (UTC)` (el nacido el 29 de febrero cumple el 28 de febrero, como `AddYears` de .NET); junto con la mínima de 1900-01-01. En el PUT se aplica cuando llega `BirthDate`. b) Otra regla de cómputo. | a). | No (sin respuesta: a)) |
| Q23 (resuelta, ver tabla de resueltas) | 03a | Pregunta consolidada (OPT-035). Con la divergencia sin corregir ("Obviemos este punto"), en producción un `POST`/`PUT /facilities` sin dirección falla con `NullReferenceException` en `FacilitiesController.cs` L49/L79 (mapeo `request.Address.Street`...) **antes** de llegar al dominio. Q3/Q4/Q8 y AC3a.1/AC3a.2/AC3a.11 exigen 400 desde el dominio con `Address`. Para eso hay que tocar el controlador, que no estaba en el alcance de ficheros del antiguo paso 03. | a) Incluir en 03a `FacilitiesController.cs` solo para el mapeo sin NRE cuando falta la dirección (pasar la ausencia al handler/dominio; sin atributos, rutas ni DTO nuevos). Activa la rama de comparación de AC3a.8 con el ayudante. b) No tocar el controlador: el caso "dirección ausente" en producción sigue en 500; se retira ese caso de AC3a.1/AC3a.2/AC3a.11 y queda como limitación conocida (los demás casos no cambian). | a): es la única forma de cumplir Q3/Q4/Q8 en producción sin cambiar `TestFixture`/`Startup`, y el diff es mínimo. | **Sí, bloquea el ciclo 3 parcialmente**: sin respuesta, BUILDER no toca el controlador y el caso "dirección ausente" de AC3a.1/AC3a.2/AC3a.11 queda `BLOCKED`; el resto de 03a puede avanzar. |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Crear la rama. (Plan aprobado: "vamos con el 2".) |
| Antes del ciclo 2 | Opcional: Q18, Q19, Q21, Q22 (Q16 y Q17 respondidas). |
| Antes del ciclo 3 | Q23 respondida (2026-10-12). Opcional: Q20. Borrar los ficheros de redirección `03-facility-and-stand-input-validation.md` y `04-database-health-check.md` (PLANNER no puede borrar). |
| Al cerrar el plan | Documentar el ayudante de AC3a.0 en `.github/doc/common/test-stack.md` (OPT-034) si DOCUMENTER no lo hace como cambio de herramienta de test. |
| Antes de subir la rama / abrir PR | **Solo si AC2.6/AC3a.8/AC3b.8 detectan cambios en el OpenAPI o se elige Q20 b)** (prerrequisito del job `client-and-package`, OPT-030): regenerar `Gaffer.Api.Client` (build Release), subir `GlobalPackageVersion` e `infoVersion` de `nswag.json` según Q10 y versionar el resultado. Si no, el CI falla en "Fail if the generated client differs". Si no hay cambios, no hace falta nada. |
| En cada checkpoint con `NEEDS_CHANGES` | Confirmar Q0. |

## Optimizaciones aplicadas al plan (2026-10-11, tras el ciclo 1)

Aprobación del operador (literal, transmitida por ORCHESTRATOR): "Aprueba y aplica las dos propuestas, y luego ejecuta el ciclo 2".

| ID | Aplicada por | Dónde | Efecto en el plan |
| --- | --- | --- | --- |
| OPT-032 | PLANNER | AC2.6 (paso 02), AC3.8 (paso 03), AC4.6 (paso 04) y su "Cómo demostrarlo" | Cada criterio de "contrato/swagger sin cambios" fija el procedimiento: sin cambios en controladores, DTOs ni `Startup.cs`/`Startups/*Startup.cs` → `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde; con cambios → comparación del `swagger.json` servido con `StartupTestServer` (OPT-022), nunca `dotnet run`. No cambia ningún otro criterio ni el alcance. |
| OPT-033 | Operador, a mano | `.github/doc/work-guidelines.md` (receta estándar del rojo para REVIEWER) | Ningún paso depende de ella para sus criterios. Dependencia del operador: si no está aplicada antes del ciclo 2, REVIEWER reproduce el rojo como en el ciclo 1 (`STATUS.md`). |

Nota de PLANNER (sin resolver): "No verificado", Q10 y "Acciones del operador" citan "AC3.7" como la comparación de contrato del paso 03; en `03-*.md` ese criterio es **AC3.8** (AC3.7 es el unitario). Se lee AC3.8 donde dice AC3.7 en ese sentido; el operador confirma si hace falta corregir el texto. Tras OPT-036, AC3.8 = AC3a.8 y AC3b.8.

## Optimizaciones aplicadas al plan (2026-10-12, tras el ciclo 2)

Aprobación del operador (literal, transmitida por ORCHESTRATOR): "Aprueba las propuestas P2-1 a 3 y preguntame lo que necesites". Para P2-3 eligió "Facility / gradas" (texto de la opción: "03a: Facility + dirección (create/update) y la corrección de la divergencia. 03b: gradas anidadas en create/update + sub-recurso /stands."). Para P2-2: "Obviemos este punto".

| ID | Aplicada por | Dónde | Efecto en el plan |
| --- | --- | --- | --- |
| OPT-036 | PLANNER | Pasos 03a/03b/05, secuenciación, `STATUS.md` (tabla de ciclos) | Paso 03 dividido; health check renumerado a 05; reserva de 1 ciclo. La parte "y la corrección de la divergencia" de la opción elegida **no se aplica** (P2-2 "Obviemos este punto"). Ningún criterio se pierde (correspondencia abajo). Incluye la nota de calcular fechas de calendario antes de escribir tests (03a). |
| OPT-034 | PLANNER (tarea) / BUILDER (ciclo 3) | AC3a.0; AC3a.8, AC3b.8, AC5.6 | Ayudante `[Explicit]` permanente de volcado del `swagger.json` con `StartupTestServer`, en lugar de una sonda temporal. Documentarlo en `test-stack.md`: DOCUMENTER u operador (fuera de la matriz de BUILDER). |
| OPT-035 | No abordada (operador) | AC3a.11, AC3b.11; Q23 | No se cambian `TestFixture` ni `Startup`. **Riesgo**: `TestFixture` (`AddControllers`) evalúa DataAnnotations y el required implícito; producción (`AddMvcCore`) no, así que un test verde con `TestFixture` puede ocultar un 500 en producción (caso conocido: facility sin dirección, `STATUS.md` ciclo 2, AC2.0). Mitigación dentro del alcance: en los casos divergentes, la evidencia debe venir del pipeline de producción (`StartupTestServer` o host real), no solo de `TestFixture`. |

### Correspondencia de criterios (antiguo paso 03 → 03a/03b; antiguo 04 → 05)

| Antiguo | Nuevo | Reparto |
| --- | --- | --- |
| - | AC3a.0 | Nuevo (OPT-034): ayudante de volcado |
| AC3.1 | AC3a.1 + AC3b.1 | 03a: reglas de facility y dirección (incluida dirección ausente); 03b: gradas anidadas con índice |
| AC3.2 | AC3a.2 + AC3b.2 | 03a: facility, dirección ausente, 404, fusión/lock; 03b: gradas anidadas y 5 gradas con clave de update |
| AC3.3 | AC3b.3 | Sub-recurso `/stands` |
| AC3.4 | AC3a.4 + AC3b.4 | 03a: 100/20, `Capacity = 0`, `BuildDate` límites; 03b: `StandName` 100, `StandCapacity = 0` |
| AC3.5 | AC3a.5 + AC3b.5 | 03a: varios campos, sin eco, model binding de facility; 03b: con gradas indexadas y model binding de `/stands` |
| AC3.6 | AC3b.6 | Sustitución de TECHDEBT-004 AC4.11 (grada de 300) |
| AC3.7 | AC3a.7 + AC3b.7 | 03a: `Facility`/`Address`, handlers create/update, claves; 03b: `Stand`, `AddStand`/`UpdateStand`, clave de `/stands` |
| AC3.8 | AC3a.8 + AC3b.8 | Contrato con el ayudante (OPT-032 + OPT-034) |
| AC3.9 | AC3a.9 + AC3b.9 | Regresión |
| AC3.10 | AC3a.10 + AC3b.10 | 03a: facility y dirección antiguas (GET, búsqueda, repositorio, PUT válido); 03b: grada antigua (`GET .../stands`, PUT que la conserva, DELETE) |
| - | AC3a.11 + AC3b.11 | Nuevo (OPT-035): evidencia de producción en casos divergentes |
| AC4.1-AC4.8 | AC5.1-AC5.8 | Mismo texto; AC5.6 usa el ayudante de AC3a.0 |

## Optimizaciones aplicadas al plan (2026-10-12, tras el ciclo 3)

Aprobación del operador (literal, transmitida por ORCHESTRATOR): "Apruebo P3-1 a 3 y luego lanza el ciclo 4". Detalle en `MEMORIES.md` (OPT-037 a OPT-039) y en `OPTIMIZATION.md`, ciclo 3.

| ID | Propuesta | Aplicada por | Dónde | Efecto en el plan |
| --- | --- | --- | --- | --- |
| OPT-037 | P3-1 | PLANNER | `03b-*.md` y `05-*.md`, sección "Reparto por capa" y AC3b.1-AC3b.4, AC3b.7, AC3b.11 | Pirámide de pruebas: la matriz completa va en dominio/unitario; los handlers, un caso por regla; la aceptación con `TestFixture`, un representante por tipo de regla y operación; la capa de producción (`StartupTestServer`) conserva solo los casos divergentes de OPT-035. No se elimina ningún criterio. |
| OPT-038 | P3-2 | PLANNER (regla del plan) | Sección siguiente | Aplicada como regla para los ciclos 4-5. La aplicación permanente en `.claude/agents/builder.md`/`work-guidelines.md` la hace el operador. |
| OPT-039 | P3-3 | PLANNER (regla del plan) | Sección siguiente | Aplicada como regla para los ciclos 4-5. La aplicación permanente la hace el operador. |

### Reglas del plan para los ciclos 4-5 (OPT-038, OPT-039)

- **Rojo cuantitativo (OPT-038)**: BUILDER registra el rojo en `STATUS.md` como `n × estado` por clase de test, agrupando el resultado obtenido en cada test (p. ej. `18 × 500, 7 × 201, 7 × 200`), o `n × CSxxxx` si el rojo es de compilación. Nunca "todos" ni un resultado extrapolado de una muestra.
- **Grep de usos antes de cambiar tipos compartidos (OPT-039)**: antes de cambiar la firma o la nulabilidad de un tipo compartido (comando, DTO, entidad de dominio, interfaz o clase de `Startups/`), BUILDER ejecuta `grep -rn "<Tipo o miembro>" Gaffer.Api Gaffer.Api.Tests --include=*.cs` y adapta todos los usos de producto y tests en el mismo paso. Anota en `STATUS.md` el comando y el número de usos.

### Errata detectada por REVIEWER en el ciclo 3 (corregida en el texto)

| Dónde | Antes | Ahora | Evidencia |
| --- | --- | --- | --- |
| AC3a.4, AC3a.10, AC3b.4, AC3b.10 | `PUT /facilities/{id}` correcto → 204 | → **200** (sin cambio de contrato) | `FacilitiesController.Update` devuelve `Task` (sin `NoContent`); los tests ya exigían 200 (`REVIEW.md` ciclo 3). `PUT .../stands/{standId}` sigue en **204** (TECHDEBT-004 Q9) y `DELETE .../stands/{standId}` en 204: no cambian. |
| Alcance de tests de 03a y 03b | `Integration/SqlFacilityRepositoryShould.cs` | `Unit/Repositories/SqlFacilityRepositoryShould.cs` | Ubicación real del fichero (`Glob`). El paso 05 no lo cita. |

## Fuera de alcance (decisión del operador: para el final)

- N+1 y paginación; código muerto en `Program.cs`; entorno por defecto `Development`; borrados redundantes con cascada/trigger; `mariadb:latest` en CI; `Features_Refactor/`; `CriteriaType.NONE`; ProblemDetails en camelCase.
- `IPlayerRepository.Update(Player)` (no protege ninguna invariante con lock; no se pidió).
- El título "Cannot create facility" de `FacilityValidationException`. Q4 sí cambia la **clave** en el update (`Error.Facilities.Update.Validation` en lugar de `Error.Facilities.Create.Validation`); el título no se pidió y solo cambia si BUILDER lo necesita para separar las operaciones.
- Documentar el 400 en el OpenAPI (`ProducesResponseType`): no se pidió.
- Una forma nueva de borrar `SecondLastName` por el PUT (Q17: no se añade).
- Migración o limpieza de datos ya guardados que incumplan las reglas nuevas.
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release` sobre `Gaffer.Api` ni sobre la solución.
- Tests con BD contra el Docker local `Quionis_DB`; nunca se omiten. Si no está disponible: `BLOCKED` con el error exacto.
- DI: registros nuevos (p. ej. health checks, estado del fallback) se reflejan en `TestFixture` en el mismo incremento (`ConfigureRepositories`/`ConfigureActions`; `TestServerDependenciesShould`). Rutas servidas (OpenAPI, `/health/*`) se comprueban con `StartupTestServer` (OPT-022).
- Sin paquetes nuevos (Q3 = b), Q11). Sin operaciones de Git. Máximo 6 ciclos.
