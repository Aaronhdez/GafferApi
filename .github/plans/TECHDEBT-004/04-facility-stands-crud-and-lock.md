# Paso 04 (ciclo 4) - CRUD de gradas bajo la facility y lock en transacción del máximo de 4

Decisiones del operador (literales): "CRUD para las Stands desde facility y Endpoints en como indicas en la primera opción." (`DELETE /api/v1/facilities/{id}/stands/{standId}` → 204; 404 si la facility o la grada no existe o es ajena); "Lock en transacción".

**Bloqueo**: Q9 (semántica del `PUT` de grada), Q10 (`POST` con 4 gradas) y Q12 (contrato de lectura/creación y claves 404) deben estar respondidas antes del ciclo 4. Q8 (dividir el paso), Q11, Q13, Q14, Q15 tienen valor por defecto. Skill aplicada: `api-and-interface-design` (contrato primero, errores consistentes, validación en el borde).

## Contrato (con las recomendaciones de `00`; se ajusta a las respuestas antes del ciclo 4)

Base: `/api/v1/facilities/{id}/stands`. `{id}` y `{standId}` son `Guid`. "Ajena" = grada que existe pero no está enlazada a `{id}` en `Facilities_Stands`.

| Método y ruta | Éxito | Errores | Origen |
| --- | --- | --- | --- |
| `GET .../stands` | 200, lista de `SearchFacilityStandResult` (`id`, `standName`, `standCapacity`); vacía si no hay gradas | 404 facility inexistente | Q12 |
| `GET .../stands/{standId}` | 200, `SearchFacilityStandResult` | 404 facility inexistente; 404 grada inexistente o ajena | Q12 |
| `POST .../stands` (cuerpo `{ standName, standCapacity }`) | 201 + `Location` al `GET` de la grada + `Guid` de la grada creada (generado por el servidor) | 404 facility inexistente; 400 con 4 gradas ya existentes, sin cambios (Q10) | Q10, Q12 |
| `PUT .../stands/{standId}` (cuerpo `{ standName, standCapacity }`) | 204 (Q9) | 404 facility inexistente; 404 grada inexistente o ajena (sin cambios en la otra facility) | Q9 |
| `DELETE .../stands/{standId}` | 204 | 404 facility inexistente; 404 grada inexistente o ajena (la otra facility no cambia) | Operador |

- Claves 404 (Q12, recomendación): `Error.Facilities.Stands.FacilityNotFound` y `Error.Facilities.Stands.StandNotFound`. Clave 400 del máximo (Q10, recomendación): `Error.Facilities.Stands.Create.MaxStandsExceeded`. Todas con el formato de error existente (`ExceptionToHttpResponseConverters`).
- `DELETE` borra la fila de `Facilities_Stands` y la de `Stands`, en una transacción. Borrar la última grada está permitido (Q15 por defecto).
- `PUT /facilities/{id}` conserva la semántica de fusión de TECHDEBT-003 (Q11 por defecto); solo cambia su control de concurrencia.
- Validación de campos de grada: ninguna nueva (Q13 por defecto; anotado como deuda).

## Lock en transacción (Q14 por defecto)

| Flujo | Dentro de una misma transacción del proveedor |
| --- | --- |
| `POST .../stands` | `SELECT Id FROM Facilities WHERE Id = @Id FOR UPDATE` (404 si no hay fila) → contar gradas de la facility → si ya hay 4, error Q10 sin escribir → `INSERT` de la grada y su fila en `Facilities_Stands` → commit. |
| `PUT /facilities/{id}` | `SELECT ... FOR UPDATE` de la facility → leer sus gradas → `MergeStands` (reglas de TECHDEBT-003 sin cambios) → escribir → commit. La lectura que hoy hace `UpdateFacilityCommandHandler.Handle` con `facilityRepository.Get` **fuera** de la transacción deja de ser la base de la decisión. |

- Diseño (decisión de BUILDER, registrada en `STATUS.md`): por ejemplo, un método de repositorio que abre la transacción, bloquea, lee y aplica una función de fusión que recibe del handler, o un método de repositorio específico para añadir grada con el límite. Se mantiene el reparto actual: reglas en handler/dominio, SQL en repositorio.
- Cada petición HTTP tiene su propio scope y su propio `SqlConnectionProvider`, así que dos peticiones concurrentes usan conexiones distintas y el bloqueo actúa entre ellas.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/` (nuevo `FacilityStandsController.cs` o acciones en `FacilitiesController.cs`) | 5 acciones con `CancellationToken`; DTO de petición de grada. |
| `Gaffer.Api/Commands/` y `Gaffer.Api/Queries/` | Handlers de crear/actualizar/borrar grada y de listar/obtener; `UpdateFacilityCommandHandler` (fusión dentro de la transacción con lock). |
| `Gaffer.Api/Shared/Repositories/IFacilityRepository.cs`, `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | Métodos de grada y lectura con `FOR UPDATE` dentro de `ExecuteInTransactionAsync`. |
| `Gaffer.Api/Shared/Exceptions/` y `ExceptionToHttpResponseConverters.cs` | Excepciones y claves de Q10/Q12. |
| `Gaffer.Api/Startups/ActionsStartup.cs` (y `RepositoriesStartup.cs` si procede) | Registro de handlers nuevos; `TestFixture` los recibe vía `ConfigureActions()` (lección permanente; `TestServerDependenciesShould` lo comprueba). |
| `Gaffer.Api/Domain/Facility.cs` | Solo si hace falta un método de dominio; la invariante de 4 no cambia. |
| Tests: `Acceptance/` (nuevo `FacilityStandsFeaturesShould.cs` o en `FacilityFeaturesShould.cs`), `Unit/Commands/`, `Unit/Queries/`, `Unit/Controllers/` (incl. `FacilitiesControllerContractShould`/contrato OpenAPI), `Unit/Repositories/SqlFacilityRepositoryShould.cs`, `Unit/Exceptions/ExceptionToHttpResponseConvertersShould.cs`, `Integration/TestServerDependenciesShould.cs` (solo si hace falta) | Tests. |

Fuera: migraciones (sin cambio de esquema: `Stands` y `Facilities_Stands` ya existen); salida generada de `Gaffer.Api.Client` (la regenera el operador, Q16/Q19).

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC4.1 | Aceptación `GET` lista y detalle: facility con 2 gradas → lista con esos 2 `Id`/valores (iguales a los del `GET /facilities/{id}`); detalle de cada una → 200 con sus valores; facility sin gradas → lista vacía. |
| AC4.2 | Aceptación `POST`: facility con 3 gradas → 201, `Location` apunta al `GET` de la grada, el `Guid` devuelto es el de la nueva grada y el `GET` de la facility muestra 4. Con 4 gradas → respuesta y clave de Q10, y el `GET` no cambia. |
| AC4.3 | Aceptación `PUT` de grada según Q9: cambia nombre y capacidad, conserva el `Id`; las demás gradas y la facility no cambian. |
| AC4.4 | Aceptación `DELETE`: 204; el `GET` ya no la muestra; no quedan filas huérfanas en `Stands`/`Facilities_Stands` (repositorio). |
| AC4.5 | 404 (Q12): facility inexistente en las 5 operaciones; grada inexistente en `GET` detalle/`PUT`/`DELETE`; grada **ajena** (de otra facility B) en `GET` detalle/`PUT`/`DELETE` → 404 y el `GET` de B no cambia. |
| AC4.6 | Concurrencia, determinista (repositorio, contra Docker): la transacción A bloquea la facility con 3 gradas (`FOR UPDATE`) y la mantiene abierta; un `POST` de grada en paralelo (otro proveedor/scope) **no termina** mientras A está abierta (comprobado con una espera acotada, p. ej. 500 ms); A inserta la 4ª grada y hace commit; entonces el `POST` termina con el error de Q10 y la facility queda con 4. Red: sin `FOR UPDATE` el `POST` termina de inmediato y la facility queda con 5. |
| AC4.7 | Concurrencia en el `PUT /facilities/{id}`, mismo patrón que AC4.6 con A bloqueando y un `PUT` de facility que añade una grada sin `Id`: el `PUT` espera y, al leer las 4 gradas tras el commit de A, aplica las reglas de TECHDEBT-003 (descarte silencioso con 4); nunca quedan 5. Red: sin lock quedan 5. |
| AC4.8 | Concurrencia extremo a extremo (aceptación HTTP): facility con 3 gradas; N `POST` simultáneos (p. ej. 2 con barrera de inicio, repetido 10 veces con facilities nuevas) → exactamente un 201 y el resto con el error de Q10; la facility nunca pasa de 4. Complementa AC4.6-AC4.7 (no es la única evidencia de red, porque una carrera puede no reproducirse). |
| AC4.9 | Contrato: el OpenAPI servido (`StartupTestServer`) declara las 5 operaciones con sus códigos (200/201/204/400/404) y los esquemas de petición (`standName`, `standCapacity`, sin `id`). El OpenAPI de las operaciones existentes no cambia (comparación antes/después del documento servido, salvo las rutas nuevas). La salida generada del cliente no se toca. |
| AC4.10 | Unitario: handlers de grada (404 de facility/grada, grada ajena, límite de Q10, 204/201 de los casos felices) y conversores de las claves nuevas. `UpdateFacilityCommandHandlerShould`: las reglas de fusión de TECHDEBT-003 siguen en verde (se permiten cambios de preparación de los dobles por el nuevo método de repositorio, registrados sin debilitar aserciones). |
| AC4.11 | Atomicidad: si falla el `INSERT` de una grada nueva (p. ej. nombre de 300 caracteres), no queda la fila en `Stands` ni en `Facilities_Stands` y el recuento no cambia. |
| AC4.12 | Regresión: `FacilityFeaturesShould` (incluidas R-MAX y `DuplicatedStand` de TECHDEBT-003), `TestServerDependenciesShould` (resuelve los handlers nuevos), `JsonContractShould` y la suite completa en verde; build 0 warnings; `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` → 0. |

## Doble bucle

1. **Aceptación**: AC4.1-AC4.5 y AC4.8 (red: 404 de ruta inexistente / carrera) → green.
2. **Unitario**: AC4.6-AC4.7 y AC4.11 en repositorio; AC4.9-AC4.10. Red → green.
3. **Refactor**: separar reglas y SQL; nombres. Revalidar suite y build.

## Cómo demostrarlo

- Filtro de los tests del paso (nombres en `STATUS.md`): red y green.
- Para AC4.6-AC4.7: tiempos observados (espera de B mientras A mantiene el lock; fin de B tras el commit) y recuento final de gradas, en `STATUS.md`.
- Para AC4.8: número de repeticiones y reparto 201/400 por repetición.
- Comparación del `swagger.json` servido antes/después (como AC4.7 de TECHDEBT-003) para AC4.9.
- Suite completa y build `--no-incremental -v q`.

## Notas

- Cambio de contrato aditivo (5 operaciones, claves de error nuevas): el operador regenera el cliente y sube MINOR (Q16) antes del ciclo 5 (Q19).
- Si Q8 = b) o c), PLANNER divide este fichero antes del ciclo afectado (04a: AC4.1-AC4.5, AC4.9-AC4.10 parcial; 04b: lock AC4.6-AC4.8, AC4.11).
