# REVIEW - TECHDEBT-006

## Ciclo 1 - Paso 01 (REVIEWER)

**Veredicto: APPROVED**. Sin defectos bloqueantes; todos los AC del paso 01 demostrados con evidencia ejecutable.

### Alcance revisado

- `git status`: 23 ficheros (12 producto, 11 tests; 2 borrados de `Features_Refactor/`) + plan sin versionar. Ninguno fuera del alcance del paso. `Gaffer.Api.Client/`, `TestFixture.cs` y `.github/workflows/` sin cambios (`git status --short` → 0 líneas).
- Diff completo del producto leído; tests por `git diff --stat`, grep NUnit y diffs de `SqlFacilityRepositoryShould`, `DatabaseHealthCheckShould`, `HostingEnvironmentShould` y `SearchPlayersEndpointShould` (OPT-040).
- Sin registros de handlers nuevos: no aplica actualizar `TestFixture.cs`.

### Comandos del REVIEWER (`Quionis_DB`, mariadb:latest, 3306, BD `GafferDB_Test`)

| Comando | Resultado |
| --- | --- |
| `docker ps` | `Quionis_DB` Up, `0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --no-incremental -v q` | exit 0; 0 Advertencia(s), 0 errores |
| `dotnet test Gaffer.Api.Tests --no-build` | exit 0; 688/688 correctas, 0 con error, 0 omitidas (30 s) |
| `ServedOpenApiDocumentDump` en el árbol de trabajo y en un worktree limpio de `HEAD` + `sha256sum` + `cmp` | exit 0 en ambos; sha256 `be49f0ab…a5a6bfe` idéntico; `cmp` idéntico. `CriteriaType`/`NONE` no aparecen en el documento |
| Rojo (OPT-033): `git worktree add <scratch>/red HEAD` + `git diff HEAD -- Gaffer.Api.Tests \| git -C red apply` + `dotnet build Gaffer.Api.Tests` | exit 1; 3 × CS1729 (`DatabaseHealthCheckShould`), 3 × CS1503 (`FacilityShould`), 1 × CS0266 (`UpdateFacilityCommandHandlerShould`) |
| Rojo de comportamiento: en el worktree, restaurados de `HEAD` los 3 ficheros que no compilan; `dotnet test --filter` sobre `FacilityFeaturesShould`, `FacilityValidationInTheProductionPipelineShould`, `SearchPlayersEndpointShould`, `HostingEnvironmentShould`, `DatabaseStartupShould`, `SqlFacilityRepositoryShould`, `CreateFacilityCommandHandlerShould` | exit 1; 8 × Con error: 6 × `but found InternalServerError` (`FacilityFeaturesShould` 2, `FacilityValidationInTheProductionPipelineShould` 4), 1 × `DatabaseStartupShould` (cableado del logger), 1 × `CreateFacilityCommandHandlerShould` (`NullReferenceException`). Resto en verde (caracterización: AC1.1, AC1.3, AC1.5, AC1.13) |
| `git worktree remove --force` | worktree eliminado; `git worktree list` solo el principal |
| `grep -rn "SetConfigurationProviders\|AddDevelopmentSettings" Gaffer.Api --include=*.cs` | 0 |
| `grep -rn "CriteriaType.NONE\|\bNONE\b" Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 0 |
| `ls Gaffer.Api.Tests/Acceptance \| grep -c Features_Refactor` | 0 |

### Criterios de aceptación

| AC | Evidencia | Estado |
| --- | --- | --- |
| AC1.1 | Grep → 0; `CreateProductContext` sin `ConfigurationBuilder`. `ReadTheAppSettingsFilesFromTheContentRoot` verde en `HEAD` (worktree) y en el árbol actual (verde-verde); aserciones previas de `HostingEnvironmentShould` intactas. El builder eliminado nunca se usaba (código muerto) | Demostrado |
| AC1.2 | Q4 = a): sin código; anotado para DOCUMENTER en `STATUS.md` | n/a (conforme) |
| AC1.3 | `RemoveTheAddressStandsAndLinksThroughTheSchemaWhenOnlyTheFacilityRowIsDeleted` verde en `HEAD` (antes de cambiar `Delete`) y ahora, contra la BD real: borrado `(0,0,0,0,0)` en las 5 tablas, otro facility `(1,1,1,3,3)`. Dependencia de esquema confirmada en `M0002` (FK `ON DELETE CASCADE` en los dos enlaces) y `M0003` (`TRG_Facilities_BeforeDelete` borra `Stands` y `Addresses`). Sin huérfanos y sin efecto en otros facilities | Demostrado |
| AC1.4 | `Delete` = transacción + `DELETE FROM Facilities`; `DeleteStands`/`DeleteAddress` eliminados sin otros usos; `minimumCalls` 6 → 2 anotado; tests de borrado de repositorio y `DELETE` HTTP en verde en la suite | Demostrado |
| AC1.5 | Tabla caso → test en `STATUS.md`; 4 tests nuevos en `SearchPlayersEndpointShould` verdes; `Features_Refactor/` no existe. Decisión: **errata del plan**. El único endpoint de búsqueda es `POST /players/search` (`PlayerController` L40); un `GET` con query exigiría cambiar el contrato, lo que el paso prohíbe (AC1.10). La intención del criterio (equivalente HTTP de cada caso) se cumple | Demostrado (errata) |
| AC1.6 | Grep → 0. El valor por defecto de `SearchCriteria.CriteriaType` es `AND` y solo se usa como conector desde el segundo criterio (`i == 0 ? "WHERE"`), así que el SQL de los 4 usos con un solo criterio no cambia. Suite verde sin cambiar aserciones | Demostrado |
| AC1.7 | Q3 = c): `ci.yml` sin cambios | n/a (conforme) |
| AC1.8 | Matriz de Q5 a) cubierta: lanza → `Warning` con excepción; `false` y timeout → `Warning` sin excepción; `Healthy`/`Degraded`/fallback → nada; mensaje sin fragmentos sensibles; `ShouldNotExposeAnything` intacto. Cableado rojo → verde (worktree). Decisión sobre el punto 3 del BUILDER: **cumple**. Q5 a) y AC1.8 definen precisamente que `false` se registre sin excepción; que `SqlConnectionProvider.TestConnection` convierta `DbException`/`TimeoutException`/`SocketException` en `false` es comportamiento previo fuera del alcance. Se pierde el tipo/mensaje del error de BD en el log (ver observación N2), pero el plan no lo exigía | Demostrado |
| AC1.9 | Timeout inyectado de 200 ms; `DefaultTimeout == 5 s` afirmado sin esperar; clase 5,323 s → 0,733 s (`STATUS.md`) | Demostrado |
| AC1.10 | sha256 del `swagger.json` servido idéntico en `HEAD` limpio y en el árbol actual (reproducido por el REVIEWER); `OpenApiContractShould` y `FacilitiesControllerContractShould` verdes en la suite | Demostrado |
| AC1.11 | Suite 688/688 (665 − 5 + 28, recuento de `STATUS.md` coherente); build 0 warnings; `TestServerDependenciesShould` verde; `TestFixture.cs`, `Gaffer.Api.Client/` y CI sin cambios | Demostrado |
| AC1.12 | Rojo reproducido en el worktree: 6 × 500 (producción 4, `TestFixture` 2). Verde ahora con 400 y `Errors["Stands[i]"]`, sin guardado/cambio | Demostrado (rojo → verde) |
| AC1.13 | Ambos tests (TestFixture y producción) verdes en `HEAD` y ahora: caracterización, origen del 400 distinto por pipeline anotado | Demostrado (caracterización) |
| AC1.14 | Rojo de compilación reproducido (3 × CS1503, 1 × CS0266) y `CreateFacilityCommandHandlerShould` 1 × NRE; matriz posición 0/1/2 + combinación con otros errores y un caso por handler verdes | Demostrado (rojo → verde) |

### Punto 4 del BUILDER: cambio de tipos de `Stands`

- `CreateFacilityCommand.Stands` / `UpdateFacilityCommand.Stands` pasan a `IReadOnlyList<T?>?`; son tipos internos de comando, no DTO HTTP: el OpenAPI no cambia (sha256 idéntico).
- Usos adaptados (OPT-039): controlador (2), handlers (`OfType<T>()` antes de crear `Stand`/fusionar; `ToRequestedStand` conserva el `null` para el dominio), `FacilityShould`, `BuildCommand`. Build 0 warnings con nullable, sin conversiones pendientes.
- `CheckRequestedStands` con `null` → `Stands[i] is required.`; los `null` cuentan para el máximo de 4. Coherente con Q9 a).

### Observaciones no bloqueantes

| ID | Observación |
| --- | --- |
| N1 | AC1.5 dice "`GET` y filtros de query"; es una errata del plan (el endpoint es `POST /players/search`). Recomendable corregir el texto si PLANNER vuelve a tocar el paso. |
| N2 | Un fallo real de BD llega al health check como `false` y se registra sin excepción ni causa (`SqlConnectionProvider.TestConnection` la descarta). Si el operador quiere la causa en el log, sería un cambio nuevo fuera de este plan (p. ej. registrar en `TestConnection` o propagar la excepción en `SqlDatabaseProbe`). |
| N3 | `ReturnEveryPlayerWhenSearchingWithoutFilters` compara con `BeEquivalentTo` los players creados; depende de que la limpieza por test de la BD siga como hasta ahora (lo está: suite verde). |
