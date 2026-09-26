# TECHDEBT-002 - Resumen, alcance y preguntas abiertas

## Petición del operador (literal)

> "genera la planificación para 1,2,4,7 y incluye además la optimización de SqlConnectionProvider, Update de Facility, Cancelación, Cambios pequeños y Warnings. Considera cada punto como un ciclo de desarrollo para poder ir viendo escalonadamente"

La petición se divide en dos planes por el límite de 6 ciclos por plan (AGENTS.md):

| Plan | Contenido | Pasos / ciclos |
| --- | --- | --- |
| **TECHDEBT-002 (este)** | Bugs: update parcial de Player, fallos de infraestructura como 400, filtro no permitido en Facilities, middleware sin log | 4 |
| TECHDEBT-003 | Optimización: SqlConnectionProvider, Update de Facility, cancelación, cambios pequeños, warnings | 5 (empieza cuando este plan esté `DONE`) |

## Objetivo

Corregir los 4 bugs sin cambiar otro comportamiento observable que el descrito en cada paso, y completar los tests que faltan (DELETE de player inexistente → 404; conversores de Facility y de BadRequest en `ExceptionToHttpResponseConvertersShould`). Línea base (dada por el operador, BUILDER la revalida en el ciclo 1): build con 0 errores; `dotnet test gaffer-api.sln` 159/159 contra Docker `Gaffer.Database` (3306).

## Secuenciación: un paso = un ciclo (requisito del operador)

| Ciclo | Paso | Fichero | Punto |
| --- | --- | --- | --- |
| 1 | 01 | `01-player-partial-update.md` | Update parcial de Player borra datos |
| 2 | 02 | `02-infrastructure-failures-5xx.md` | Fallos de infraestructura devueltos como 400; `Reason` sin asignar |
| 3 | 03 | `03-facility-filter-not-permitted-400.md` | Filtro no permitido en Facilities → 500 |
| 4 | 04 | `04-middleware-logging.md` | `ExceptionHandlingMiddleware` sin log |
| 5-6 | - | - | Reserva para `NEEDS_CHANGES` |

Reglas:

- En el ciclo N, BUILDER implementa **solo el paso N**, más las correcciones que REVIEWER haya pedido en el ciclo anterior (si las hay). No adelanta trabajo de pasos posteriores.
- En el ciclo N, REVIEWER **solo aprueba el paso N** (y verifica las correcciones del ciclo anterior). El veredicto del ciclo se refiere a ese paso.
- Los ciclos que sobran hasta 6 (5 y 6) quedan de reserva para `NEEDS_CHANGES`. Cómo se combina una corrección con el paso siguiente: Q0 = b) (default aplicado, ver tabla de preguntas).
- Cada paso sigue el doble bucle de `.github/doc/work-guidelines.md` dentro de su ciclo: aceptación (red → green) → unitario (red → green) → refactor con revalidación de la suite completa. BUILDER registra en `STATUS.md` comando y resultado de cada red y green.
- BUILDER no empieza un paso con una pregunta bloqueante abierta para ese paso; si no hay nada ejecutable en el ciclo, marca `BLOCKED`.

## Verificación de los hallazgos (PLANNER, solo lectura)

| Punto | Evidencia en el código | Confirmado |
| --- | --- | --- |
| 01 | `PlayerController.cs` L83-90: `UpdatePlayerRequest` con campos públicos, `DateTime BirthDate` y `Gender Gender` no anulables. L77-78: `request.BirthDate.ToString(InvariantCulture)` y `request.Gender.ToString()` siempre no vacíos. `UpdatePlayerCommandHandler.UpdatePlayer` L59-67 solo omite valores vacíos → sobrescribe fecha (0001-01-01) y género (`MALE`, valor 0). `PlayerFeaturesShould.UpdateAPlayer` L106-121 solo comprueba `FirstName`. Además, `GafferApiClientTest.UpdateAsync` serializa el objeto completo con Newtonsoft, así que hoy el test **envía** `BirthDate` y `Gender` por defecto (no los omite). | Sí |
| 02 | `DeletePlayerCommandHandler` L16-24, `UpdatePlayerCommandHandler` L16-24, `UpdateFacilityCommandHandler` L37-57 y `DeleteFacilityCommandHandler` L16 capturan `Exception` y la envuelven en `CannotDelete*/CannotUpdate*Exception`; `ExceptionToHttpResponseConverters` las mapea a 400 con clave `$"Error.<X>.<Op>.{Reason}"`. `Reason` es `{ get; }` sin asignar en las 4 clases → clave `Error.Players.Delete.` (con punto final). La validación de `UpdateFacility` (`BuildUpdatedFacility`) queda fuera del `try` y sigue dando 400. | Sí |
| 03 | `SqlFacilityRepository.BuildFilters` L49-52 lanza `ArgumentException`, que no tiene conversor → 500 "Unkwnown error". `PlayerSearchFilterNotPermittedException` (`IPlayerRepository.cs` L45) → 400 `Error.Search.FilterNotPermitted`. **Ningún endpoint HTTP expone hoy filtros de Facility**: `FacilitiesController.GetBy` añade solo el filtro `Id` (permitido) y `GetAll` no añade filtros; no existe `POST facilities/search`. El test `NotMapGenericArgumentExceptionsSoFacilitySearchKeepsItsBehaviour` fija que `ArgumentException` no se mapea. | Sí |
| 04 | `ExceptionHandlingMiddleware.cs` L14 `logger` sin asignar (CS0169), L24 `//logger.LogError(...)`, L59 `"Unkwnown error"`. Registrado en `Startup.cs` L66 y `TestFixture.cs` (`TestStartup`) L145. No hay ningún test de la rama "excepción desconocida → 500". | Sí |
| Tests que faltan | `ExceptionToHttpResponseConvertersShould` tiene 4 tests. El diccionario `PlayerConverters` tiene **12** entradas hoy (no 13): 6 de Player (incluido `PlayerSearchFilterNotPermittedException`) y 6 de Facility (`FacilityValidation`, `FacilityNotFound`, `CannotDelete/UpdateFacility[NotFound]`). Tras el paso 03 serán 13. `PlayerFeaturesShould` no tiene DELETE de player inexistente (sí existe para Facility: `ReturnNotFoundWhenDeletingAnUnknownFacility`). | Sí (recuento corregido) |
| Fallo de BD reproducible | `TestConfiguration.UnreachableDatabaseConnectionString` (puerto 1) ya existe y se usa en `SqlConnectionProviderShould`. Permite levantar un `TestServer` aparte con la BD inalcanzable para provocar un fallo real de MySQL por HTTP. | Sí |

## No verificado

- Qué excepción exacta lanza `MySql.Data` con la BD inalcanzable (`MySqlException` esperado). BUILDER lo registra en el paso 02.
- Cómo publica NSwag/OpenAPI los tipos anulables del paso 01 (`nullable: true` / no `required`). Se comprueba con un test sobre `/swagger/v1/swagger.json`, como `OpenApiContractShould` en TECHDEBT-001.

## Preguntas abiertas

### Preguntas originales (texto original conservado)

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloqueaba |
| --- | --- | --- | --- | --- | --- |
| Q0 | Todos | ¿Cómo se combina un `NEEDS_CHANGES` del paso N con el paso N+1? | a) El ciclo siguiente solo corrige el paso N; el paso N+1 se desplaza un ciclo (consume reserva). b) Corregir primero y avanzar al paso N+1 en el mismo ciclo solo si la corrección es pequeña (p. ej. ≤ 1 fichero de producto y sin cambio de contrato); si no, a). c) Siempre corregir y avanzar en el mismo ciclo. | b). Lo decide el operador en el checkpoint del ciclo con `NEEDS_CHANGES`. | No bloquea el ciclo 1 |
| Q1 | 01 | ¿Se debe poder borrar `SecondLastName` explícitamente con un update? Hoy `null` y `""` se ignoran, así que no se puede borrar. | a) No: `null`, ausente y `""` conservan el valor (comportamiento actual). b) `""` lo borra; `null`/ausente lo conserva. c) `null` explícito lo borra y ausente lo conserva (exige distinguir "ausente" de `null`: JSON Merge Patch o similar; más diseño). | a) o b). a) es el cambio mínimo; b) es explícito y no necesita distinguir ausente/`null`. | Solo AC1.5 |
| Q2 | 01 | El update parcial por `PUT` no sigue la semántica HTTP (PUT = reemplazo). ¿Se mantiene `PUT` con semántica parcial o se cambia a `PATCH`? | a) Mantener `PUT /players/{id}` con semántica parcial (sin cambio de ruta). b) Añadir `PATCH` y deprecar `PUT`. | a): la petición es corregir la pérdida de datos, no rediseñar la ruta. | No (se aplica a) salvo respuesta antes del ciclo 1) |
| Q3 | 02 | Código HTTP para fallos de infraestructura (MySQL) en Delete/Update. | a) 500: los handlers dejan de envolver excepciones no de negocio; el middleware da 500 "Unknown error". b) 503: nueva excepción/conversor para fallos de BD (qué tipos cuentan: `MySqlException`, `DbException`, `TimeoutException`...). | a): sin tipos nuevos y coherente con `GetBy`, que ya da 500 ante el mismo fallo. b) si los clientes necesitan distinguir "reintentable". | Sí, paso 02 |
| Q4 | 02 | ¿Qué pasa con `Reason` y con `CannotDelete*/CannotUpdate*Exception` genéricas (no `NotFound`)? Si el fallo de infraestructura deja de envolverse, ningún código las lanza. | a) Borrar las 4 excepciones genéricas y sus 4 conversores (código muerto). b) Mantenerlas para futuras reglas de negocio y asignar `Reason` en el constructor (valor a definir, p. ej. `"Unknown"`), de modo que la clave nunca acabe en `.`. c) Borrar solo `Reason` y fijar una clave constante (`Error.Players.Delete.CannotDelete`...). | a) si Q3 = a); b) o c) si el operador prevé reglas de negocio que impidan borrar/actualizar. | Sí, paso 02 |
| Q5 | 03 | Clave de traducción y título del 400 de Facilities. | a) Igual que Players: `Title` y clave `Error.Search.FilterNotPermitted`, `Detail` `FilterNotPermitted`. b) Clave propia `Error.Facilities.Search.FilterNotPermitted`. | a): "alinear" con Players. | No (se aplica a) salvo respuesta antes del ciclo 3) |
| Q6 | 04 | Nivel de log para excepciones **mapeadas** (400/404). Las no mapeadas (500) se registran con `LogError` y la excepción. | a) No registrar las mapeadas. b) `Debug`. c) `Information`. d) `Warning`. | b) o c): dejan rastro sin ruido en `Error`; 404 y 400 son respuestas esperadas. | Solo AC4.3 |

### Respuestas y estado

| ID | Respuesta del operador (literal) | Decisión aplicada al plan | Estado |
| --- | --- | --- | --- |
| Q0 | (sin respuesta) | b). Default aplicado, sin objeción del operador. | Resuelta |
| Q1 | 1ª respuesta: "Si, se puede borrar, hay personas con un solo apellido". 2ª respuesta (al aprobar el plan, **sustituye** a la deducción b): "Q1, el second last name puede ser null, si viene en la actualización, actualiza al valor, si no, dejalo null." | `secondLastName` tiene semántica de **reemplazo** (distinta del resto de campos, que son parciales): si el cuerpo trae un valor, se guarda ese valor; si no lo trae (ausente o `null`), se guarda `null`. `""`: se mantiene la equivalencia actual de `""` con `null` (hoy `string.IsNullOrEmpty`), así que `""` también deja `null`; no se añade recorte (`Trim`) porque hoy no existe. | Resuelta |
| Q2 | (sin respuesta) | a) `PUT` parcial. Default aplicado, sin objeción del operador. | Resuelta |
| Q3 | "a" | a) 500: los handlers dejan de envolver excepciones no de negocio. | Resuelta |
| Q4 | "Mantenlas, pero pon un reason que indique error interno en." (texto aparentemente truncado) | b) Se mantienen las 4 excepciones genéricas y sus 4 conversores; `Reason` se asigna siempre con un valor que indica error interno. Valor exacto: Q4.1. | Resuelta (valor en Q4.1) |
| Q4.1 | "Q4.1. \"Internal Server Error\" as reason" | `Reason = "Internal Server Error"` (literal, con espacios) en las 4 excepciones genéricas. Con los conversores actuales (`$"Error.<X>.<Op>.{Reason}"`) las claves quedan `Error.Players.Delete.Internal Server Error`, `Error.Players.Update.Internal Server Error`, `Error.Facilities.Delete.Internal Server Error`, `Error.Facilities.Update.Internal Server Error`. `Title`/`Detail` no usan `Reason` y no cambian. Estado HTTP: sigue 400 (el operador no pidió 500 para ellas). | Resuelta |
| Q5 | (sin respuesta) | a) Misma clave y título que Players. Default aplicado, sin objeción del operador. | Resuelta |
| Q6 | "400 Error, 404 no se registran" | Excepciones mapeadas a 400 → `LogError`; mapeadas a 404 → no se registran. Hoy (y tras el paso 03) el diccionario solo mapea a 400 y 404, así que no hay otros códigos mapeados que decidir. Las no mapeadas (500) siguen con `LogError` + excepción. | Resuelta |

Notas sobre Q1 (2ª respuesta):

- La deducción anterior (b: `""` borra, `null`/ausente conserva) queda **sustituida** por la respuesta literal del operador.
- Consecuencia observable: un `PUT` que no incluya `secondLastName` lo deja a `null`, aunque el resto de campos sí sean parciales. Esto afecta a clientes (BFF/frontend) que envíen updates parciales sin `secondLastName`; se registra como efecto del contrato, no como pregunta.
- No hace falta distinguir "ausente" de `null` explícito: ambos llevan a `null`. Compatible con Q2 = a) (`PUT` sin JSON Merge Patch).
- `""` → `null`: no lo dice la respuesta; se conserva la equivalencia actual `""` ≡ `null` del handler (`string.IsNullOrEmpty`). Valores con solo espacios se guardan tal cual (hoy no hay recorte). Si el operador prefiere guardar `""` literal, puede indicarlo en el checkpoint del ciclo 1; no bloquea.

### Preguntas abiertas nuevas

Ninguna. Q4.1 respondida (ver tabla de respuestas).

Nota sobre Q4: los 4 conversores genéricos mantienen el estado 400 (el operador no pidió 500 al responder Q4.1). Con Q3 = a) ningún código de producto lanza hoy esas excepciones, así que el 400 no es observable por HTTP.

## Aprobación

| Fecha (UTC) | Texto del operador (literal) |
| --- | --- |
| 2026-09-26 | "I approve the plan" |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Crear la rama del plan (hecho: `TECHDEBT-002`). |
| En cada checkpoint con `NEEDS_CHANGES` | Q0 = b) por defecto; el operador puede decidir otra cosa en ese checkpoint. |
| Al cerrar (tras `DONE`) | Regenerar `Gaffer.Api.Client` con NSwag: cambia el contrato de `UpdatePlayerRequest` (paso 01; `secondLastName` con semántica de reemplazo, Q1) y las respuestas de error (Q3 = a: 500 en Delete/Update ante fallos de infraestructura; Q4.1: claves con `Reason = "Internal Server Error"`). Decidir la versión SemVer según la regla de TECHDEBT-001 paso 10. |

## Fuera de alcance (anotado, no se planifica)

- Todo lo de TECHDEBT-003 (SqlConnectionProvider, Update de Facility, cancelación, cambios pequeños, warnings).
- Deuda ya anotada y no pedida: N+1 en `SqlFacilityRepository.Get`, paginación, entorno fijado a `"Development"` en `Program`/`ProductContext`, fallback silencioso a `DummyConnectionProvider`/health checks, validación de entrada, carpeta `Features_Refactor` duplicada, `CriteriaType.NONE`, camelCase de ProblemDetails (Q5 de TECHDEBT-001).
- `PlayerFeaturesShould.ThrowExceptionIfPlayerIsNotFound` sin `await` (anotado en TECHDEBT-001); solo se toca si el paso 01 lo necesita para su evidencia.
- Añadir filtros de búsqueda HTTP a Facilities (no se pide; ver paso 03).
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release` sobre `Gaffer.Api` ni sobre la solución.
- Los tests con MySQL se ejecutan contra Docker local (`Gaffer.Database`); nunca se omiten. Si Docker o la BD no están disponibles, `BLOCKED` con el error exacto. La BD inalcanzable a propósito (puerto 1) es parte del diseño de los tests, no una exclusión.
- Cambios de DI o de pipeline en `Gaffer.Api` se reflejan en el mismo incremento en `Gaffer.Api.Tests/TestFixture.cs`.
- Sin paquetes nuevos salvo necesidad justificada en `STATUS.md` (si se añade alguno, DOCUMENTER actualiza `.github/doc/common/test-stack.md`).
- Sin operaciones de Git por parte de los agentes. Máximo 6 ciclos.
