# Paso 01 (ciclo 1) - `Update(Facility)` fuera del contrato público del repositorio

Objetivo: que ninguna feature pueda escribir una facility sin el `SELECT ... FOR UPDATE` que protege el máximo de 4 gradas. `IFacilityRepository.Update(Facility, CancellationToken)` deja de ser público; la única vía de actualización es `Update(Guid, Func<Facility, Facility>, CancellationToken)`. Sin cambio de comportamiento HTTP ni de contrato OpenAPI.

**Bloqueo**: ninguno. Q1 tiene valor por defecto a) (método `private` y tests a través de la sobrecarga con lock).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Repositories/IFacilityRepository.cs` | Quitar `Task Update(Facility, CancellationToken)` (L9). |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | El método de L121 deja de ser público (según Q1; nombre propio a criterio de BUILDER, registrado en `STATUS.md`); L212 lo sigue llamando dentro de la transacción con lock. |
| `Gaffer.Api.Tests/Unit/Repositories/SqlFacilityRepositoryShould.cs` | L254, L308, L430: invocar la vía con lock (Q1 a)) conservando las aserciones; test nuevo de contrato (reflexión). |

Fuera: handlers, controladores, OpenAPI, migraciones, cliente generado.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC1.1 | Contrato (unitario, reflexión): `IFacilityRepository` no declara ningún método de escritura de facility que reciba `Facility` (solo `Save(Facility, ...)` para creación); `SqlFacilityRepository` no tiene ningún método público que actualice una facility sin recibir su `Guid` y la función de construcción. Rojo antes del cambio, verde después. |
| AC1.2 | Cobertura conservada: `UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent`, `LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails` y `PassItsTokenToTheProviderWhenUpdating` siguen existiendo y en verde, con las mismas aserciones de estado; en el diff solo cambia la invocación (y el comentario del recuento si procede). `minimumCalls` no baja de 7. Con Q1 = b) los tests no cambian. |
| AC1.3 | Ninguna llamada de producción a la escritura sin lock fuera de la transacción de `Update(Guid, ...)`: `grep -n "Update(" Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` y `grep -rn "\.Update(" Gaffer.Api --include=*.cs` lo muestran (resultado en `STATUS.md`). |
| AC1.4 | Concurrencia sin regresión: `MakeAConcurrentStandCreationWaitForTheLockHolderAndThenRespectTheLimit`, `MakeAConcurrentFacilityUpdateWaitForTheLockHolderAndThenRespectTheLimit`, `UpdateFacilityCommandHandlerShould`, `FacilityFeaturesShould` y `FacilityStandsFeaturesShould` en verde. |
| AC1.5 | Sin cambio de contrato HTTP: `OpenApiContractShould` y `FacilitiesControllerContractShould` en verde; el `swagger.json` servido (vía `StartupTestServer`) es idéntico antes y después. |
| AC1.6 | Suite completa en verde con recuento ≥ 322 + tests nuevos (antes/después en `STATUS.md`); build `--no-incremental` con 0 warnings; `grep -rn "CancellationToken.None" Gaffer.Api --include=*.cs` → 0. |

## Doble bucle

1. **Aceptación** (caracterización, sin cambio observable): `FacilityFeaturesShould` y `FacilityStandsFeaturesShould` en verde antes y después (AC1.4).
2. **Unitario**: AC1.1 en rojo → cambio de interfaz y visibilidad → AC1.1 y AC1.2 en verde.
3. **Refactor**: nombre del método privado y `<summary>` de la sobrecarga; revalidar AC1.3-AC1.6.

## Cómo demostrarlo

- Filtro `SqlFacilityRepositoryShould` y el test de contrato: rojo y verde (código de salida como criterio, OPT-005).
- Salida de los `grep` de AC1.3 y comparación del `swagger.json` servido (AC1.5).
- Suite completa y `dotnet build gaffer-api.sln --no-incremental -v q` (resumen de warnings, OPT-006).
