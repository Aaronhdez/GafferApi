# Paso 03b (ciclo 4) - Validación de dominio de gradas (anidadas y sub-recurso `/stands`)

Objetivo: las gradas anidadas en `POST`/`PUT /api/v1/facilities[/{id}]` y `POST`/`PUT /api/v1/facilities/{id}/stands[/{standId}]` (`FacilityStandRequest`) rechazan con 400 los datos inválidos sin escribir en la BD. Las reglas viven en los caminos de creación de `Stand` (Q3, Q16), reutilizando lo del paso 02 y 03a. Cierra la deuda de TECHDEBT-004 Q13. Skills: `api-and-interface-design`, `security-and-hardening`.

Origen: división del antiguo paso 03 (OPT-036). La corrección de la divergencia `AddControllers`/`AddMvcCore` no se aplica (OPT-035, "Obviemos este punto").

**Bloqueo**: paso 03a `APPROVED`. Q9, Q16 respondidas; Q18-Q21 con valor por defecto (a)).

## Contrato

| Objeto de dominio (petición) | Campo | Regla (Q8/Q9) |
| --- | --- | --- |
| `Facility` (`CreateFacilityRequest`, `UpdateFacilityRequest`) | `Stands` | ≤ 4 (ya lo exige el dominio, `Facility.cs` L58-60); cada elemento con las reglas de `Stand` |
| `Stand` (gradas anidadas y `FacilityStandRequest`) | `StandName` | Obligatorio; 1-100; no solo espacios |
| | `StandCapacity` | ≥ 0 |

- Sin unicidad de nombres de grada ni regla de suma de capacidades (Q9 "Solo forma").
- Claves (Q4): `Error.Facilities.Create.Validation` (POST de facility), `Error.Facilities.Update.Validation` (PUT de facility; **cambia** respecto a hoy para más de 4 gradas, que devuelve la clave de create), `Error.Facilities.Stands.Validation` (sub-recurso). La misma regla de `Stand` produce una clave distinta según la operación: la aporta el handler o la excepción (diseño de BUILDER).
- `Errors` (Q18 a)): gradas anidadas con índice (`Stands[1].StandName`), junto con los campos de facility/dirección de 03a. El índice lo añade quien construye la lista, porque `Stand` no lo conoce.
- Orden (Q21 a)): `POST`/`PUT .../stands` construyen `Stand` antes de llamar al repositorio (`FacilityStandCommandHandlers` L21, L40) → 400 antes que 404 y antes que el 400 de máximo de gradas. `PUT /facilities/{id}`: como en 03a (404 antes que 400).
- Las invariantes y excepciones actuales (`DuplicatedStand`, `MaxStandsExceeded`) no cambian; se mantiene el `Detail` actual de máximo de gradas.
- Rehidratación (Q16): no valida. En el `PUT /facilities/{id}`, las gradas guardadas que no se envían y se conservan por la fusión de TECHDEBT-003 no se revalidan.
- Divergencia conocida no corregida (OPT-035): el host de `TestFixture` evalúa `[MaxLength(4)]` y el required implícito de `StandName`; producción no.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Domain/Facility.cs` | Reglas de `Stand` en los caminos de creación (Q16). |
| `Gaffer.Api/Commands/CreateFacilityCommandHandler.cs`, `UpdateFacilityCommandHandler.cs`, `FacilityStandCommandHandlers.cs` | Operación de la clave e índice de las gradas anidadas. |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs`, `ValidationFailedException.cs` | Clave `Error.Facilities.Stands.Validation`. |
| `Gaffer.Api/Startups/ValidationStartup.cs` | Registrar `FacilityStandsController` (POST/PUT) para el formato de Q19 a) (explicitado: lo exige AC3b.5). |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | Solo si hace falta un camino de rehidratación sin validar (Q16). |
| Tests: `Acceptance/FacilityFeaturesShould.cs`, `Acceptance/FacilityStandsFeaturesShould.cs`, `Unit/Domain/` (Stand), `Unit/Commands/` (handlers de facility y gradas), `Unit/Exceptions/`, `Unit/Repositories/SqlFacilityRepositoryShould.cs` (Q16; errata del ciclo 3: antes decía `Integration/`), `Acceptance/OpenApiContractShould.cs`; builders/valores de test | `FacilityFeaturesShould` L152 (5 gradas) pasa a exigir 400 con la clave exacta. |

Fuera: reglas de `Facility`/`Address` (03a); `TestFixture.cs` y `Startup.cs` por la divergencia (OPT-035); migraciones; datos guardados; cliente generado.

## Criterios de aceptación

Numeración: `AC3b.N` conserva el sufijo del antiguo `AC3.N` (correspondencia en `00`).

### Reparto por capa (OPT-037, pirámide de pruebas)

Ningún criterio se elimina; solo cambia cuántos casos exige cada capa. Las reglas siguen cubiertas por completo en el dominio.

| Capa | Criterios | Casos exigidos |
| --- | --- | --- |
| Dominio (`Unit/Domain/`, `Stand`) | AC3b.7 | **Matriz completa**: cada caso inválido (`StandName` vacío, solo espacios, 101 caracteres, `null`; `StandCapacity` negativa), límites aceptados (1 y 100 caracteres, `StandCapacity = 0`) y varios campos inválidos a la vez. |
| Handlers (`Unit/Commands/`) | AC3b.7 | **Un caso por regla** (`StandName`, `StandCapacity`, más de 4 gradas) y por handler (create y update de facility, `POST`/`PUT .../stands`): no escriben y aportan la clave/índice de la operación. |
| Aceptación con `TestFixture` | AC3b.1-AC3b.5 | **Un representante por tipo de regla y operación**: texto vacío o solo espacios, máximo + 1, rango (`StandCapacity` negativa) y, en el `PUT` de facility, número de elementos (5 gradas). No se repite la matriz del dominio. |
| Producción (`StartupTestServer`) | AC3b.11 | **Solo los casos divergentes de OPT-035** (5 gradas; `StandName` ausente o `null`), sin cambios respecto a la versión anterior de este criterio. |

| ID | Criterio |
| --- | --- |
| AC3b.1 | Aceptación `POST /facilities` (OPT-037: un representante por tipo de regla): un caso de `StandName` vacío o solo espacios, uno de 101 caracteres y uno de `StandCapacity` negativa → 400 `Error.Facilities.Create.Validation` con el campo con índice; `GET /facilities` no cambia. El resto de la matriz va en AC3b.7. |
| AC3b.2 | Aceptación `PUT /facilities/{id}`: los mismos representantes que AC3b.1 con `Error.Facilities.Update.Validation`, más 5 gradas (hoy clave de create); el `GET` de la facility (campos, dirección y gradas) no cambia. Fusión de TECHDEBT-003 y lock de TECHDEBT-004 en verde. |
| AC3b.3 | Aceptación del sub-recurso (OPT-037: un representante por tipo de regla y operación): en `POST` y en `PUT` de grada, un caso de `StandName` vacío o solo espacios, uno de 101 caracteres y uno de `StandCapacity` negativa → 400 `Error.Facilities.Stands.Validation`; el `GET .../stands` no cambia. Una petición inválida con facility inexistente → 400 (Q21 a)). |
| AC3b.4 | Límites aceptados: `StandName` de 100 caracteres exactos, `StandCapacity = 0`, en anidadas y en el sub-recurso → `POST /facilities` y `POST .../stands` 201; `PUT /facilities/{id}` 200 (errata del ciclo 3: antes decía 204); `PUT .../stands/{standId}` 204 (TECHDEBT-004 Q9); el `GET` devuelve lo enviado. |
| AC3b.5 | Formato y seguridad: varios campos inválidos (facility, dirección y gradas anidadas) → todos en `Errors` con índice; el cuerpo no contiene el valor rechazado; errores de model binding en los endpoints de gradas → formato de Q4 con `Error.Facilities.Stands.Validation` (Q19 a)). |
| AC3b.11 | (OPT-035) Evidencia de producción en los casos divergentes: 5 gradas (`[MaxLength(4)]`) en `POST` y `PUT` de facility, y `StandName` ausente o `null` (required implícito en el host de `TestFixture`) en gradas anidadas y en el sub-recurso. Se demuestran con una prueba de aceptación sobre `StartupTestServer` (pipeline de producción) o sobre el host real: 400 con la clave y el campo de Q3/Q4/Q9, nunca 500, y sin escritura. Los tests con `TestFixture` solos **no** bastan. Diferencias de mensaje entre hosts se registran en `STATUS.md`; clave y campo deben coincidir. Esta capa no replica los casos no divergentes (OPT-037). |
| AC3b.6 | Sustitución de TECHDEBT-004 AC4.11: el caso de nombre de grada de 300 caracteres por HTTP ahora es 400 de validación. Los tests de atomicidad del repositorio (`LeaveNoRowsWhenTheStandInsertFails`, `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails`) siguen demostrando la atomicidad: construyen la grada de 300 caracteres por el camino sin validar (Q16), sin debilitar la aserción; si cambia su construcción, se registra en `STATUS.md`. |
| AC3b.7 | Unitario: reglas de `Stand` con la matriz completa de la tabla "Reparto por capa" (válidos, inválidos y límites); handlers, un caso por regla y handler: con datos inválidos no escriben (no llaman a `AddStand`/`UpdateStand`; el update de facility deshace la transacción); conversión con `Error.Facilities.Stands.Validation`. |
| AC3b.10 | (Q16) Datos antiguos: se inserta por SQL una facility con una grada con `StandCapacity` negativa y: `GET .../stands` → 200 con los valores guardados; `SqlFacilityRepository` la lee sin excepción; un `PUT /facilities/{id}` con todos los campos válidos que no envía la grada inválida → 200 (errata del ciclo 3: antes decía 204) y la grada se conserva sin revalidar; `DELETE .../stands/{standId}` de la grada inválida → 204. |
| AC3b.8 | Contrato: igual que AC3a.8 (Q20 a); `FacilitiesControllerContractShould` L76-81 sin cambios; Q10 si hay diferencias; cliente sin tocar). **Procedimiento (OPT-032 + OPT-034)**: sin cambios en controladores, DTOs ni `Startup.cs`/`Startups/*Startup.cs` → tests de contrato en verde; con cambios (previsto: `Startups/ValidationStartup.cs`) → volcado con el ayudante de AC3a.0 antes y después; sin `dotnet run` ni sonda temporal. `STATUS.md` indica la rama. |
| AC3b.9 | Regresión: suite completa en verde (recuento antes/después, tests adaptados listados en `STATUS.md`), build 0 warnings, `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` → 0. |

## Doble bucle

1. **Aceptación**: AC3b.1-AC3b.5 y AC3b.11 rojo → verde; AC3b.10 caracterización (verde antes y después).
2. **Unitario**: AC3b.6-AC3b.7 rojo → verde; AC3b.8 comparación.
3. **Refactor**: reglas de texto compartidas con `Player`, `Facility` y `Address`; revalidar suite y build.

## Cómo demostrarlo

- Rojo en `STATUS.md` como `n × estado` por clase de test (OPT-038) y `grep` de usos antes de cambiar firmas o nulabilidad de tipos compartidos (OPT-039): reglas del plan en `00`.
- Filtro de los tests del paso en rojo y verde; AC3b.11 con su filtro propio sobre `StartupTestServer` (sin `dotnet run`).
- AC3b.8 con el ayudante de AC3a.0.
- Suite completa y build `--no-incremental -v q`.
