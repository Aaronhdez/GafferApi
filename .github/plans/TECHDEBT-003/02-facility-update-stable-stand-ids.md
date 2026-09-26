# Paso 02 (ciclo 2) - Update de Facility con IDs de grada estables

Prerequisito: Q2 respondida (bloqueante: cómo se identifica una grada existente). Q3: se aplica a) (dirección con `UPDATE` en el sitio) salvo respuesta.

## Objetivo

Actualizar una facility no cambia el `Id` de las gradas que se conservan ni el de la dirección. El repositorio deja de borrar y reinsertar gradas y dirección en el update: actualiza las existentes, inserta las nuevas y borra las que ya no vienen, todo en la misma transacción.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Controllers/FacilitiesController.cs` | Solo si Q2 = a: `Id` opcional (`Guid?`) en `UpdateFacilityStandRequest` y su mapeo. |
| `Gaffer.Api/Commands/UpdateFacilityCommandHandler.cs` | `BuildUpdatedFacility` conserva los `Id` de las gradas existentes según Q2; nuevo `Guid` solo para gradas nuevas. Si Q2 = a, error para `Id` ajeno a la facility. |
| `Gaffer.Api/Infrastructure/SqlFacilityRepository.cs` | `Update`: `UPDATE` de dirección y gradas existentes, `INSERT` de nuevas, `DELETE` de las retiradas. |
| `Gaffer.Api/Shared/Exceptions/ExceptionToHttpResponseConverters.cs` | Solo si Q2 = a necesita un 400 nuevo (con su test en `ExceptionToHttpResponseConvertersShould`, que cubre todas las claves desde TECHDEBT-002). |
| `Gaffer.Api.Tests/Acceptance/FacilityFeaturesShould.cs`, `Unit/Commands/UpdateFacilityCommandHandlerShould.cs`, `Unit/Repositories/SqlFacilityRepositoryShould.cs` | Tests. |

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| AC2.1 | Aceptación: crear una facility con 2 gradas, leer sus `Id`, actualizar cambiando el nombre/capacidad de una e identificándolas según Q2: el `GET` devuelve las **mismas** 2 `Id` con los valores nuevos. Red hoy: `Id` distintos. |
| AC2.2 | Aceptación: un update que añade una grada nueva y retira otra devuelve la conservada con su `Id`, la nueva con un `Id` nuevo, y la retirada ya no aparece (ni queda huérfana en `Stands`/`Facilities_Stands`; comprobación en el test de repositorio). |
| AC2.3 | El `Id` de la dirección no cambia tras el update y sus campos se actualizan. |
| AC2.4 | (Si Q2 = a) Un `Id` de grada que no pertenece a la facility → 400 con la clave acordada; nada se modifica. |
| AC2.5 | Atomicidad: si falla una operación del update, no cambia nada (test de repositorio con un fallo provocado, p. ej. una grada con nombre que viola la longitud de columna, o el mecanismo que BUILDER registre). |
| AC2.6 | Regresión: `UpdateAFacilityReplacingFieldsAddressAndStands`, `ReturnNotFoundWhenUpdatingAnUnknownFacility`, `RejectUpdatingAFacilityWithMoreThanFourStands` y la suite completa en verde. |
| AC2.7 | (Si Q2 = a) El OpenAPI servido declara `UpdateFacilityStandRequest.id` opcional y anulable. |

## Doble bucle

1. **Aceptación**: AC2.1-AC2.4 en `FacilityFeaturesShould`. Red → green.
2. **Unitario**: `UpdateFacilityCommandHandlerShould` (conserva `Id`, crea nuevos, rechaza ajenos) y `SqlFacilityRepositoryShould` (AC2.2 huérfanas, AC2.3, AC2.5). Red → green.
3. **Refactor**: separar en el repositorio el cálculo de diferencias (conservadas/nuevas/retiradas) de la ejecución SQL. Revalidar la suite.

## Cómo demostrarlo

- `dotnet test gaffer-api.sln --filter "FullyQualifiedName~FacilityFeaturesShould|FullyQualifiedName~UpdateFacilityCommandHandler|FullyQualifiedName~SqlFacilityRepository"`: red (IDs distintos) y green en `STATUS.md`.
- Suite completa en verde.

## Notas

- El N+1 de `SqlFacilityRepository.Get` sigue fuera de alcance.
- Si Q2 = a, es un cambio de contrato aditivo (campo opcional): el cliente lo regenera el operador.
