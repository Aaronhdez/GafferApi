# Paso 02 (ciclo 2) - Update de Facility con gradas identificadas por su `Id` (`Guid`)

Q2 respondida (literal del operador): "Opcón A pero no borres si no viene."

Corrección posterior del operador (literal): "perdon, me equivoqué. Si cada grada tiene UUID, usa el uuid como identificador y mantene el flujo de actualización, descarta el ULID (puedes añadirlo como posible mejora), con esto puedes obviar las cuestiones relacionada co ULID."

Ajuste final del operador (R-MAX, literal): "Solo un ultimo ajuste: si una facility ya tiene el máximo de gradas (4), y llega una con un UUID nuevo no existente entre las que ya están, se deshecha."

Respuestas del operador a Q14-Q18 (literal): "Q14. Ignorarla también / Q15. Crearla / Q16. Por defecto / Q17. Por defecto / Q18. Por defecto"

Resultado: el identificador de la grada en el update es su `Id` (`Guid`) actual. Sin ULID, sin migración y sin dependencias nuevas. Q9, Q11 y Q12 retiradas; Q14-Q18 respondidas. Q3 se aplica como a) (dirección con `UPDATE` en el sitio) salvo respuesta. Q10, Q13 y Q19 (nueva, derivada de Q15 "Crearla") tienen valor por defecto (ver `00-overview-and-open-questions.md`). Ninguna pregunta bloquea este paso.

## Tabla de reglas (qué pasa con cada grada de la petición)

"Existentes" = gradas de la facility **antes** del update. "Emparejada" = grada de la petición cuyo `Id` es el de una existente.

| Grada de la petición | Facility con menos de 4 existentes | Facility con 4 existentes (R-MAX) | Origen |
| --- | --- | --- | --- |
| `Id` de una existente | Se actualiza (nombre/capacidad), conserva su `Id` | Igual | Q2 |
| Sin `Id` | Se crea con `Id` nuevo | **Se descarta** en silencio | Q2 / Q14 = b) |
| `Id` desconocido | Se crea con `Id` nuevo generado; el recibido se ignora | **Se descarta** en silencio | Q15 "Crearla" + Q19 por defecto / R-MAX |
| `Id` de una grada de otra facility | Se crea con `Id` nuevo generado; la otra facility no cambia | **Se descarta** en silencio; la otra facility no cambia | Q15 + Q19 por defecto / Q18 = a) |
| `Id` de una existente repetido en la petición | 400 `Error.Facilities.Update.DuplicatedStand`, sin cambios | Igual | Q13 por defecto (reajustada) |
| Existente que no viene (o `Stands` `null`/vacío) | Se conserva sin cambios | Igual | Q2 |
| Total resultante > 4 (existentes + creadas) | 400 `Error.Facilities.Create.Validation`, sin cambios | No puede ocurrir (no se crea nada) | Q16 = a) |
| Petición con más de 4 gradas | 400 por `[MaxLength(4)]` de la petición (sin cambios respecto a hoy) | Igual | Existente |

- Descarte: totalmente silencioso, misma respuesta que un update correcto y sin log específico (Q17 = a)).
- Orden de evaluación en el handler: (1) validación de la petición (`[MaxLength(4)]`); (2) Q13: `Id` de existente repetido → 400 `DuplicatedStand`; (3) clasificación: emparejadas / a crear / descartadas (con 4 existentes, todo lo no emparejado se descarta; con menos de 4, todo lo no emparejado se crea con `Id` nuevo); (4) invariante de 4 gradas sobre existentes + creadas → 400 `Create.Validation`; (5) `Update` del repositorio. Ninguna respuesta 400 modifica nada.
- **Clave `Error.Facilities.Update.UnknownStand`: se retira.** Tras Q15 ("Crearla") y Q18 (descartar) ningún caso la produce. La única clave nueva es `Error.Facilities.Update.DuplicatedStand` (Q13 por defecto reajustada; ver `00`).

## Objetivo

- `PUT /facilities/{id}`: cada grada de la petición lleva un `Id` (`Guid`) opcional y anulable, con el tratamiento de la tabla de reglas.
- El repositorio deja de borrar y reinsertar gradas y dirección en el update: `UPDATE` de dirección y gradas existentes, `INSERT` de las nuevas, sin `DELETE`, todo en la misma transacción.
- OPT-016 (aprobado por el operador para este ciclo): documentar en `dev-stack.md` el comportamiento de `MySql.Data` con `DisposeAsync`/`Component.Disposed`.

## Consecuencias derivadas de las respuestas (no son requisitos nuevos)

| Tema | Consecuencia |
| --- | --- |
| Borrado de gradas | Con "no borres si no viene", el `PUT` ya no permite retirar gradas (hoy sí: reemplaza la lista). No se planifica un endpoint de borrado de grada (Q10). |
| Límite de 4 gradas | La invariante existente de `Facility` ("Stands cannot exceed 4") se aplica a existentes + creadas. 3 existentes + 2 sin `Id` (o con `Id` desconocido/ajeno) → 400 `Error.Facilities.Create.Validation` (Q16). Con 4 existentes nunca se crea nada (R-MAX + Q14), así que la invariante no puede fallar en ese caso. |
| Facility de 4 gradas | Con 4 existentes, el `PUT` solo puede modificar campos de la facility, dirección y gradas emparejadas; cualquier otra grada enviada se ignora sin aviso. |
| `Id` enviado por el cliente | Nunca se usa como `Id` de una grada nueva (Q19 por defecto): el cliente no puede fijar identificadores ni chocar con la clave primaria de `Stands`. |
| `UpdateAFacilityReplacingFieldsAddressAndStands` | Su nombre describe "reemplazar" gradas. Si alguna aserción depende del borrado de las gradas de creación, cambia por decisión del operador (Q2); BUILDER lo registra en `STATUS.md` (test y aserción) y lo renombra si procede. No se debilitan aserciones sin registrarlo. |
| Clientes actuales | Un cliente que hoy envía las gradas sin `Id` para "reemplazarlas" pasará a **añadir** gradas (y puede chocar con el límite de 4) o, con 4 gradas, verá que no cambian. Cambio de comportamiento a comunicar al regenerar el cliente. |

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Commands/UpdateFacilityCommandHandler.cs` | `UpdateFacilityStandCommand` con `Guid? Id`; `BuildUpdatedFacility` aplica la tabla de reglas en el orden indicado: rechaza `Id` existente repetido (Q13); actualiza emparejadas; con menos de 4 existentes crea el resto con `Guid.NewGuid()` (sin `Id`, desconocido o ajeno); con 4 existentes descarta todo lo no emparejado; conserva las no mencionadas. |
| `Gaffer.Api/Controllers/FacilitiesController.cs` | `Guid? Id` opcional en `UpdateFacilityStandRequest` y su mapeo. `[MaxLength(4)]` de la petición se mantiene. |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | `Update`: `UPDATE` de dirección y gradas existentes, `INSERT` de nuevas (y de su fila en `Facilities_Stands`), sin `DELETE`. `Save` y `Delete` no cambian. |
| `Gaffer.Api/Shared/Exceptions/` (nueva excepción) y `ExceptionToHttpResponseConverters.cs` | Excepción de grada repetida → 400 `Error.Facilities.Update.DuplicatedStand`. No se crea excepción ni clave `UnknownStand`. |
| `Gaffer.Api/Domain/Facility.cs` | Solo si BUILDER necesita un método de dominio para la fusión; la invariante de 4 gradas no cambia. |
| DI (`*Startup.cs`) y `Gaffer.Api.Tests/TestFixture.cs` | Solo si BUILDER introduce un servicio inyectable: se registra en producción y en `TestFixture` en el mismo incremento (lección permanente de `AGENTS.md`). |
| `.github/doc/common/dev-stack.md` | Nota OPT-016 junto a la mención de `MySql.Data` (L24). |
| Tests: `Acceptance/FacilityFeaturesShould.cs`, `Unit/Commands/UpdateFacilityCommandHandlerShould.cs`, `Unit/Repositories/SqlFacilityRepositoryShould.cs`, `Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs`, `Unit/Controllers/FacilitiesControllerContractShould.cs` | Tests. |

Fuera de alcance: artefactos generados de `Gaffer.Api.Client` (los regenera el operador); migraciones (no hay cambio de esquema: `Stands.Id` ya existe y es estable); ULID por grada (posible mejora, ver `00-overview-and-open-questions.md`); el N+1 de `SqlFacilityRepository.Get`. Sin paquetes nuevos.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Aceptación: crear una facility con 2 gradas; update que envía las 2 gradas con su `Id` (tomado del `GET`) y nombre/capacidad cambiados → el `GET` devuelve los **mismos** `Id` con los valores nuevos. Red hoy: `Id` distintos. |
| AC2.2 | Aceptación: update que envía una grada existente (con `Id`) y una sin `Id`, omitiendo la otra existente → el `GET` devuelve 3 gradas: la enviada actualizada (mismo `Id`), la omitida **sin cambios** (mismo `Id` y valores) y la nueva con un `Id` distinto de los anteriores. Un update con `Stands` `null` o vacío deja las gradas igual. |
| AC2.3 | Aceptación, facility con **menos de 4** gradas (Q15 + Q19 por defecto): crear facility A con 2 gradas y facility B con al menos 1; update de A que envía una grada con un `Guid` que no existe y otra con el `Id` de una grada de B → misma respuesta que un update correcto; el `GET` de A devuelve 4 gradas: las 2 originales sin cambios y 2 nuevas con los nombres/capacidades enviados y `Id` **distintos** de los enviados y de los existentes; el `GET` de B no cambia. Red hoy: no existe `Id` en la petición / se reemplazan las gradas. |
| AC2.4 | Aceptación, Q16: update de una facility con menos de 4 gradas que dejaría más de 4 en total → 400 con la clave existente `Error.Facilities.Create.Validation` y el `GET` no muestra cambios (campos, dirección ni gradas). Dos casos: 2 existentes + 3 sin `Id`; 3 existentes + 1 sin `Id` + 1 con `Guid` desconocido (las creadas por Q15 cuentan para el total). |
| AC2.11 | Aceptación, R-MAX + Q14 + Q18 + Q17: facility A con 4 gradas y facility B con al menos 1; update de A que cambia un campo de la facility, envía una grada existente con su `Id` y valores nuevos, otra con un `Guid` nuevo que no existe, otra sin `Id` y otra con el `Id` de una grada de B → misma respuesta que un update correcto (sin indicación del descarte); el `GET` de A devuelve las mismas 4 gradas (mismos `Id`), la enviada actualizada, las otras 3 sin cambios, ninguna grada con el `Guid` nuevo ni con los nombres de las descartadas, y el campo de la facility actualizado; el `GET` de B no cambia. Red hoy: no existe `Id` en la petición / se reemplazan las gradas. |
| AC2.12 | Aceptación, Q13 por defecto: update que repite el `Id` de una grada existente de la facility → 400 con `ErrorTranslationKey` = `Error.Facilities.Update.DuplicatedStand`; el `GET` no muestra cambios. Se demuestra con una facility de menos de 4 gradas y con una de 4 (la comprobación va antes del descarte R-MAX). |
| AC2.5 | El `Id` de la dirección no cambia tras el update y sus campos se actualizan (Q3 = a). Repositorio: tras el update no hay filas huérfanas ni duplicadas en `Stands`, `Facilities_Stands`, `Addresses`, `Facilities_Addresses`. |
| AC2.6 | Atomicidad: si falla una operación del update, no cambia nada (test de repositorio con fallo provocado, p. ej. nombre de grada que viola la longitud de columna, o el mecanismo que BUILDER registre). |
| AC2.7 | Contrato: el OpenAPI servido declara `UpdateFacilityStandRequest.id` opcional y anulable (formato `uuid`); `SearchFacilityStandResult` sin cambios. `FacilitiesControllerContractShould` lo cubre. La salida generada del cliente no se toca. |
| AC2.8 | Unitario (`UpdateFacilityCommandHandlerShould`): conserva el `Id` de las emparejadas y actualiza sus valores; conserva las no mencionadas; con menos de 4 existentes crea con `Id` nuevo las gradas sin `Id`, con `Id` desconocido y con `Id` ajeno (el `Id` creado nunca es el recibido, Q19); con 4 existentes descarta las gradas sin `Id`, con `Id` desconocido y con `Id` ajeno y llama a `Update` con las 4 gradas originales (las emparejadas actualizadas) y el resto de campos; rechaza `Id` existente repetido (Q13) sin llamar a `Update`, con menos de 4 y con 4 existentes; rechaza el total > 4 (Q16) sin llamar a `Update`. `ExceptionToHttpResponseConvertersShould` cubre la clave `Error.Facilities.Update.DuplicatedStand`. |
| AC2.9 | Regresión: `UpdateAFacilityReplacingFieldsAddressAndStands` (según la consecuencia registrada arriba), `ReturnNotFoundWhenUpdatingAnUnknownFacility`, `RejectUpdatingAFacilityWithMoreThanFourStands`, `JsonContractShould` y la suite completa en verde. Si hay servicio inyectable nuevo, `TestFixture` lo registra (sin él los tests de aceptación fallarían al resolver el handler). |
| AC2.10 | OPT-016: `.github/doc/common/dev-stack.md` incluye, junto a la mención de `MySql.Data` (L24), la nota: con `MySql.Data` 26.7.0, `MySqlConnection.DisposeAsync()` no lanza `Component.Disposed` ni libera el `Component` de una conexión ya cerrada (se puede reabrir); solo `Dispose()` síncrono ejecuta esa liberación; los tests sobre liberación al cerrar el scope se basan en `Dispose()`. Fuente: `STATUS.md` ciclo 1, fila "AC1.2: hallazgo del driver". Solo esa nota; el resto de `dev-stack.md` sin cambios. |

## Doble bucle

1. **Aceptación**: AC2.1-AC2.4, AC2.11 y AC2.12 en `FacilityFeaturesShould`. Red → green.
2. **Unitario**: AC2.8 (todas las filas de la tabla de reglas) en `UpdateFacilityCommandHandlerShould` y `ExceptionToHttpResponseConvertersShould`; AC2.5-AC2.6 en `SqlFacilityRepositoryShould`; AC2.7 en `FacilitiesControllerContractShould`. Red → green.
3. **Refactor**: separar el cálculo de la fusión (emparejadas/a crear/conservadas/descartadas) de la ejecución SQL. Revalidar la suite completa. AC2.10 (documentación) se hace al final del ciclo.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~FacilityFeaturesShould|FullyQualifiedName~UpdateFacilityCommandHandler|FullyQualifiedName~SqlFacilityRepository|FullyQualifiedName~ExceptionToHttpResponseConverters|FullyQualifiedName~FacilitiesControllerContract"`: red y green en `STATUS.md`.
- Suite completa en verde con recuento antes/después (línea base: 220/220 tras el ciclo 1).
- `STATUS.md` registra, por cada fila de la tabla de reglas, el test de aceptación o unitario que la cubre (nombre, red y green). Para AC2.3 y AC2.11, además, el `GET` comparado (`Id` antes/después; ningún `Id` creado igual a un `Id` enviado; `GET` de la otra facility sin cambios).
- Q19: BUILDER registra en `STATUS.md` que aplica el valor por defecto a) (o la respuesta del operador si llega antes del ciclo 2; en ese caso PLANNER ajusta AC2.3 y AC2.8 antes de que empiece BUILDER).
- `grep -n "UnknownStand" -r Gaffer.Api Gaffer.Api.Tests`: sin resultados (la clave se retira).
- `grep -n "DELETE" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs`: ninguna sentencia `DELETE` alcanzable desde `Update` (solo las de `Delete`).
- AC2.10: `grep -n "DisposeAsync" .github/doc/common/dev-stack.md` muestra la nota; `git diff --stat .github/doc/common/dev-stack.md` solo con esas líneas.

## Notas

- Cambio de contrato aditivo (`id` opcional en la petición; nueva clave de error `DuplicatedStand`), pero con cambio de comportamiento (el `PUT` ya no reemplaza gradas; con 4 gradas ignora las no emparejadas sin error): el operador regenera el cliente al cerrar.
- Marcar OPT-016 como aplicada en `MEMORIES.md` corresponde a OPTIMIZER, no a BUILDER.
