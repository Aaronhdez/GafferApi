# REVIEW - FEAT-001_SearchPlayers

## Ciclo 1 - Paso 01 (`ValueCriteria`, `WHERE `, `AddValueCriteria()`, refactor LINQ D-LINQ)

**Veredicto: APPROVED**

Revisor: REVIEWER, 2026-10-14. Skills: `code-review-and-quality`, `doubt-driven-development` (cambio de firma de repositorio), `security-and-hardening` (construcción de SQL con entrada del llamador).

### Comprobaciones del revisor (ejecutadas)

| # | Comando | Resultado |
| --- | --- | --- |
| R1 | `docker ps` | `Quionis_DB Up 10 minutes 0.0.0.0:3306->3306/tcp` |
| R2 | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | `0 Advertencia(s)`, `0 Errores` |
| R3 | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | exit 0 |
| R4 | `dotnet test gaffer-api.sln --no-build --verbosity quiet` (árbol de trabajo) | exit 0; `Superado: 723, Total: 723`, 0 con error |
| R5 | Línea base: `git worktree add <scratchpad>/red HEAD`; build + `dotnet test --verbosity quiet` | Build 0 errores, 4 avisos (NU1902/NU1903, auditoría NuGet en restauración limpia, paquetes de `HEAD`, no de código); exit 0; `Superado: 699, Total: 699` |
| R6 | Rojo (OPT-033): `git diff HEAD -- Gaffer.Api.Tests \| git -C red apply` + copia de `SqlPlayerRepositoryQueryShould.cs` (no rastreado); build | exit 1; `SearchPlayersQueryHandlerShould` 1 × CS0246; `SqlPlayerRepositoryQueryShould` 3 × CS0103, 7 × CS0246; `SqlPlayerRepositoryShould` 5 × CS0246. Idéntico a la fila "Rojo (compilación)" de BUILDER |
| R7 | `git worktree remove --force` (falló por ruta larga) → `rm -rf` + `git worktree prune` | `git worktree list` solo muestra el árbol principal; `git status` sin cambios ajenos |
| R8 | `grep -cE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | `0` |
| R9 | `git diff --stat -- '*.csproj' Gaffer.Api/Controllers '*Facility*' Gaffer.Api.Tests/Acceptance Gaffer.Api.Tests/TestFixture.cs Gaffer.Api.Client` | vacío |
| R10 | `grep -rn PlayerSearchCriteria Gaffer.Api Gaffer.Api.Tests --include=*.cs` | 0 usos; `SearchCriteria`/`CriteriaType`/`FacilitySearchCriteria` solo en Facility y en su definición conservada |
| R11 | `stat` de ficheros de test vs `SqlPlayerRepository.cs` | Tests modificados por última vez 15:55Z; repositorio 16:05Z (refactor LINQ, tras la reanudación 16:03Z): los tests no se tocaron durante el refactor |

### Criterios de aceptación

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC1.1 | Demostrado (con observación O1) | R1, R5: `HEAD` sin editar 699/699 en verde con Docker arriba; build de código sin avisos (R2 y fila BUILDER de línea base) |
| AC1.2 | Demostrado | Fila de grep OPT-039 en `STATUS.md`; R9, R10 |
| AC1.3 | Demostrado | 3 tests nuevos en `WhenGettingPlayers` (unión `IN`, intersección, `AND`+`OR`) + 7 adaptados; rojo R6, verde R4 |
| AC1.4 | Demostrado | `BuildTheExactQueryWithOneParameterPerValue` (11 casos) coincide literal con cada fila `IN` de la tabla y comprueba ausencia de `WHERE AND`/`WHERE OR` y nombres/valores de parámetro; rojo R6, verde R4 |
| AC1.5 | Demostrado | `ReturnPlayersWhoseFieldContains...` (1 y varios valores), `RestrictWithAnd...`, `MatchAnUnderscore...Literally` (`P_u` no encuentra a `Paul`), `MatchAPercentSign...Literally`; verde contra MariaDB R4 |
| AC1.6 | Demostrado | Casos LIKE de AC1.4 (primer criterio 1/N valores, tras otro criterio 1/N, escape `%50\%\_a\\b%`); `KeepEveryCallerValueOutOfTheQueryText(LIKE)` |
| AC1.7 | Demostrado | `WhenFilteringPlayersWithUntrustedInput` con los mismos casos (`SecondLastName`, `Gender`, `BirthDate`, inyección en nombre de campo, comilla, mayúsculas) sin aserciones eliminadas; unitario `KeepEveryCallerValueOutOfTheQueryText` × 3 |
| AC1.8 | Demostrado | `ThrowWithoutQueryingWhenACriterionHasNoValues`/`...NullValues`: `ArgumentException` y `DidNotReceiveWithAnyArgs().QueryAsync` |
| AC1.9 | Demostrado | `Arg.Is<CriteriaSet>` con `Name`, `Type == AND`, `Values.Count == 1` y valor, en `SearchPlayersQueryHandlerShould`, `UpdatePlayerCommandHandlerShould`, `DeletePlayerCommandHandlerShould`; verde R4 |
| AC1.10 | Demostrado | Clases de aceptación y `OpenApiContractShould` sin diff (R9) y en verde dentro de R4; sin diff en `Controllers/` |
| AC1.11 | Demostrado | R2, R3, R4: 723 = 699 + 24 nuevos |
| AC1.12 | Demostrado | R8; `Select` (con índice para `@v{i}_{j}`), `SelectMany`, `Aggregate`, `string.Join` en el diff; sin diff de `*.csproj` (R9); 723 antes/después (filas BUILDER) y tests intactos durante el refactor (R11) |

### Revisión del código de producto

- `SqlPlayerRepository`: solo columnas de la lista blanca, palabras fijas y nombres de parámetro llegan al texto SQL; cada valor es parámetro. Validación (lista blanca, luego valores) materializada con `ToList()` antes de construir SQL y de consultar. Índices de parámetro sobre la secuencia filtrada (sin nulos): nombres únicos, `ToDictionary` sin colisiones. Escape de `LIKE` con `\` primero: correcto y probado contra BD.
- Handlers: mapeo 1:1 al `ValueCriteria` `AND` de un valor; comportamiento HTTP igual (R4, aceptación).
- `TestFixture.cs`: sin handlers nuevos; no requiere registro DI.
- Facility, controladores, DTO, `*.csproj` y cliente generado: sin cambios.

### Hallazgos

Bloqueantes: ninguno.

| ID | Tipo | Observación |
| --- | --- | --- |
| O1 | No bloqueante (proceso) | AC1.1 pide la línea base "antes de editar"; se obtuvo después, sobre una copia de `HEAD` (`git archive`), por B1. El revisor la reprodujo de forma independiente sobre un `git worktree` de `HEAD` (R5, 699/699). El árbol medido es exactamente el de partida, así que la línea base es equivalente; se acepta. Los 4 avisos NU1902/NU1903 de la copia son de auditoría NuGet en restauración limpia de los mismos paquetes de `HEAD`, no avisos de compilación. |
| O2 | No bloqueante | El rojo registrado es de compilación (tipos inexistentes), que la tabla de AC admite (`n × CSxxxx`); no hay rojo de aserción previo para los casos nuevos. |
| O3 | Informativo | Precedencia `AND`/`OR` sin paréntesis entre criterios (`A IN (...) OR B IN (...) AND C ...`), conforme a la especificación literal; a tener en cuenta en el paso 02/03. |

## Ciclo 2 - Paso 02 (`IntervalCriteria<T>` `BETWEEN`, `OrderCriteria` `ORDER BY`)

**Veredicto: APPROVED**

Revisor: REVIEWER, 2026-10-14. Skills: `code-review-and-quality`, `doubt-driven-development` (abstracción `object`, paréntesis `OR`), `security-and-hardening` (columnas y sentido en texto SQL). Decisiones aplicadas: Q3 b), Q4 b), Q5 b2), D-LINQ a); Q12-Q14 a) por defecto.

### Comprobaciones del revisor (ejecutadas)

| # | Comando | Resultado |
| --- | --- | --- |
| R1 | `docker ps` | `Quionis_DB Up 27 minutes 0.0.0.0:3306->3306/tcp` |
| R2 | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | `0 Advertencia(s)`, `0 Errores` |
| R3 | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | exit 0 |
| R4 | `dotnet test gaffer-api.sln --no-build --verbosity quiet` (árbol de trabajo, contra `Quionis_DB`) | exit 0; `Superado: 754, Total: 754`, 0 con error |
| R5 | Rojo 1 (OPT-033): `git worktree add --detach <scratchpad>/wt2 HEAD` + `git diff -- Gaffer.Api.Tests/` aplicado; build | exit 1; `SqlPlayerRepositoryQueryShould` 5 × CS0246, `SqlPlayerRepositoryShould` 5 × CS0246 (= BUILDER) |
| R6 | Rojo 2: mismo worktree + solo `IPlayerRepository.cs` del árbol de trabajo (repositorio de `HEAD`); `dotnet test --filter "FullyQualifiedName~SqlPlayerRepository" --logger trx` | exit 1; 27 × Failed: `BuildTheExactQueryWithIntervalAndOrderCriteria` 10, `...IntervalFieldIsNotPermitted` 4, `...OrderFieldIsNotPermitted` 4, `...OrderDirectionIsUndefined` 1, `KeepEveryIntervalBoundOutOfTheQueryText` 1, 7 tests BD nuevos; 4 nuevos Passed esperados (`..._ValuesWithOrWithoutInterval`, `RejectAMissing{Minimum,Maximum}Bound`, `...BirthDateIsUsedAsAValueCriterion`); todos los existentes Passed. Idéntico a "Rojo 2" de BUILDER. Build del worktree: 4 avisos NU1902/NU1903 (auditoría NuGet en restauración limpia, como en ciclo 1) |
| R7 | `git worktree remove --force` (ruta larga) → `rm -rf` + `git worktree prune` | `git worktree list` solo el árbol principal |
| R8 | `grep -cE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | `0` |
| R9 | `git diff --stat -- '*.csproj' Gaffer.Api/Controllers/ Gaffer.Api.Tests/TestFixture.cs` | vacío |
| R10 | `grep -rn "class IntervalCriteria" Gaffer.Api` | `IPlayerRepository.cs:26: public class IntervalCriteria<T> : IIntervalCriteria where T : IComparable<T>` |
| R11 | `git status --short` | 4 ficheros de producto/test + `STATUS.md`, `usage/BUILDER.md`, `OPTIMIZATION.md` (plan); sin cliente generado, Facility ni controladores |

### Criterios de aceptación

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC2.1 | Demostrado | `ReturnOnlyThePlayersBornWithinTheIntervalIncludingBothBounds`: 1990-01-01 y 1999-12-31 (límites) e interior devueltos; 1989-12-31 y los 3 jugadores del `SetUp` (2000-01-01) excluidos. Rojo R6, verde R4 |
| AC2.2 | Demostrado | `ReturnTheIntersectionOfAValueCriterionAndAnInterval`; `RestrictBothSidesOfAnOrWithTheInterval` (Paul, 2000-01-01, cumple solo el primer lado del `OR` y no se devuelve). Rojo R6, verde R4 |
| AC2.3 | Demostrado | `ReturnThePlayersIn{Ascending,Descending}Order`, `BreakTiesWithTheNextOrderCriterion` (3 empatados en 2000-01-01 desempatados por `FirstName ASC`), `ApplyValueIntervalAndOrderCriteriaTogether`, todos con `Equal`. Sin `OrderCriteria` no hay `ORDER BY` (casos `NoCriteria`/`ValuesWithOrWithoutInterval`) |
| AC2.4 | Demostrado | `BuildTheExactQueryWithIntervalAndOrderCriteria` × 11: las 8 filas de "Forma SQL esperada" literales + 3 adicionales; reutiliza la aserción del paso 01 (`NotContain("WHERE AND"/"WHERE OR")`, nombres exactos, `BeOfType(DateTime/int)` por parámetro) |
| AC2.5 | Demostrado | `...IntervalFieldIsNotPermitted` × 4, `...OrderFieldIsNotPermitted` × 4 (incluye `"FirstName; DROP TABLE Players"`), `...BirthDateIsUsedAsAValueCriterion`, `KeepEveryIntervalBoundOutOfTheQueryText` (inyección y comilla solo en parámetros); todos con `DidNotReceiveWithAnyArgs().QueryAsync` |
| AC2.6 | Demostrado (con O5) | `RejectAMissing{Minimum,Maximum}Bound` → `ArgumentNullException` en el constructor (antes de cualquier consulta); R10 muestra `IComparable<T>` |
| AC2.7 | Demostrado | R4 incluye los 141 del ciclo 1 y las clases de aceptación/`OpenApiContractShould`; R9 sin diff en `Controllers/` |
| AC2.8 | Demostrado | R2, R3, R4 (754 = 723 + 31 nuevos) |
| AC2.9 | Demostrado | R8; `Aggregate`, `Concat`, `SelectMany`, `Skip`/`Any`, `string.Join` en el diff; R9 sin `*.csproj` |

### Revisión del código de producto

- Inyección: en el texto SQL solo entran columnas canónicas de las listas blancas (`OrdinalIgnoreCase`, valor del diccionario, no del llamador), las palabras fijas `BETWEEN`/`ASC`/`DESC` y nombres de parámetro `@i{k}_min/max` generados por índice. `Min`/`Max` (`object`) van siempre a `DynamicParameters`; nunca se interpolan. Seguro aunque `IIntervalCriteria` sea pública.
- Tipado: `DynamicParameters(diccionario)` conserva el tipo en tiempo de ejecución (`DateTime`/`int`), comprobado con `BeOfType` y contra MySQL en los tests BD.
- Paréntesis (O3 del ciclo 1): `GroupValuesBeforeIntervals` agrupa el bloque de valores solo si hay intervalos y algún conector `OR` emitido (desde el 2.º criterio); el `LIKE` multivalor ya llega entre paréntesis del paso 01. Sin intervalos, SQL del paso 01 intacto (Q12 a)). Bloques mixtos `A AND B OR C` quedan como `(A AND B OR C) AND intervalo`, coherente con Q4 b).
- Validación completa (valores → intervalos → orden) antes de `QueryAsync`; nulos en listas filtrados; índices de parámetro sin colisión (`v`/`i`).
- `TestFixture.cs`: sin handlers nuevos; no requiere DI.

### Hallazgos

Bloqueantes: ninguno.

| ID | Tipo | Observación |
| --- | --- | --- |
| O4 | No bloqueante (alcance) | `ArgumentOutOfRangeException` para `OrderDirection` no definido no está en el plan. Es la rama por defecto obligada de un `switch` exhaustivo sobre enum, evita que `ToString()` de un valor arbitrario llegue al SQL y está probada (`ThrowWithoutQueryingWhenTheOrderDirectionIsUndefined`). Se acepta como endurecimiento mínimo. |
| O5 | No bloqueante | AC2.6: el rojo de los tests de límites es solo de compilación (R5); la validación vive en el constructor del tipo nuevo, así que no hay estado intermedio con rojo de aserción razonable. |
| O6 | Informativo | `IIntervalCriteria` es pública: una implementación ajena puede devolver `Min`/`Max` `null` sin pasar por `ThrowIfNull`; el resultado sería `BETWEEN NULL ...` (lista vacía), no inyección. Considerar en el paso 03 (p. ej. validar en `ToIntervalCriteriaClause` o restringir la interfaz) si el operador lo pide. |

## Ciclo 3 - Paso 03 (criterios agnósticos en `Gaffer.Api/Shared/Criteria/`, sin cambio de comportamiento)

Veredicto: **APPROVED**. Decisiones por defecto Q8 a), Q9 a), Q10 a); AC3.7 N/A.

Revisor: 2026-10-14T16:35:19Z - 2026-10-14T16:37:23Z.

### Comprobaciones del revisor (ejecutadas)

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | Modificados: 4 producto Player + `IPlayerRepository.cs` + 5 tests Player + ficheros del plan; nuevos: `Shared/Criteria/` (7), `Shared/Repositories/SearchCriteria.cs`, `STATUS-archive.md`. 0 coincidencias de Facility, `Controllers`, `TestFixture`, `csproj`, `Client`, `Middleware` |
| `docker ps` | `Quionis_DB Up 37 minutes 0.0.0.0:3306->3306/tcp` |
| `dotnet build gaffer-api.sln --no-incremental -v q` | Exit 0; `0 Advertencia(s)`, `0 Errores` |
| `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | Exit 0 |
| `dotnet test gaffer-api.sln --no-build --verbosity quiet` (MySQL en Docker local) | Exit 0; `Superado: 754, Total: 754` (= REVIEWER ciclo 2 y AC3.1 de BUILDER) |
| Filtro Player (`SqlPlayerRepository`, `PlayerCommandHandlerShould`, `SearchPlayersQueryHandlerShould`, `SearchPlayersEndpointShould`, `PlayerFeaturesShould`, `OpenApiContractShould`) + `~Facilit` | Exit 0; `Superado: 538, Total: 538` |
| `--filter ~OpenApiContractShould` | Exit 0; `Superado: 6, Total: 6` |
| Grep de aislamiento (AC3.2) sobre `Shared/Criteria` incl. `^using` | `0`; 7 ficheros, todos `namespace Gaffer.Api.Shared.Criteria;` |
| Definiciones únicas: `grep "public (class\|enum\|interface) <tipo>"` en `Gaffer.Api` | Cada tipo de criterio definido una sola vez (7 en `Shared/Criteria/`, `SearchCriteria`/`CriteriaType` en `Shared/Repositories/SearchCriteria.cs`) |
| Comparación de bloques con `HEAD` (independiente, AC3.4): `SearchCriteria` + `CriteriaType` de `HEAD:IPlayerRepository.cs` vs `SearchCriteria.cs`; `ValueCriteria` + `ValueCriteriaType` de `HEAD` vs ficheros nuevos | `legacy-identical`; `value-identical` |
| Bucles / `*.csproj` / `Controllers` + `TestFixture.cs` | `0` / `0` / `0` |

Rojo: el único rojo de un refactor de movimiento es de compilación (falta del `using`), registrado por BUILDER (`SqlPlayerRepository.cs` 6 × CS0246, 1 × CS0535). No se reproduce en worktree: el estado "antes" (diff del ciclo 2 sin commit) no existe en Git y la evidencia de aceptación del bucle de refactor es la caracterización en verde antes (754, REVIEWER ciclo 2 sobre el mismo árbol) y después (754, esta revisión).

### Criterios de aceptación

| AC | Estado | Evidencia |
| --- | --- | --- |
| AC3.1 | Demostrado | 754/754 antes de editar (fila BUILDER ciclo 3); coincide con la ejecución independiente del REVIEWER ciclo 2 sobre el árbol aprobado |
| AC3.2 | Demostrado | 7 ficheros en `Gaffer.Api/Shared/Criteria/`; grep de dependencias y `using` = 0 |
| AC3.3 | Demostrado | `PlayerSearchCriteria` = 0 (ya ausente en `HEAD`); `IPlayerRepository.cs` solo `interface IPlayerRepository` (`Get(CriteriaSet, ...)`), `PlayerNotFoundException`, `PlayerSearchFilterNotPermittedException` |
| AC3.4 | Demostrado | Sin ficheros Facility en status/diff; bloques heredados idénticos a `HEAD`. `git diff -M` no aplica (ficheros nuevos sin seguimiento); la comparación de bloques es la evidencia equivalente |
| AC3.5 | Demostrado | Diff de tests frente a `HEAD`: el único `using` añadido es `+using Gaffer.Api.Shared.Criteria;` (5 ficheros); los 3 de handlers solo tienen esa línea; 754 = AC3.1 |
| AC3.6 | Demostrado | `OpenApiContractShould` 6/6; sin diff en `Controllers/`; build 0 avisos; format exit 0; suite 754/754 |
| AC3.7 | N/A, Q10 a) | Constructor SQL sigue privado en `SqlPlayerRepository` |
| AC3.8 | Demostrado | 0 bucles en `SqlPlayerRepository.cs` y `Shared/Criteria/`; sin diff en `*.csproj` |

### Revisión del código de producto

- Contenido de los 7 tipos coincide con lo aprobado en ciclos 1-2 (`ValueCriteria`/`ValueCriteriaType` verificados byte a byte frente a `HEAD`; `IntervalCriteria<T>`, `IIntervalCriteria`, `OrderCriteria`, `OrderDirection`, `CriteriaSet` coinciden con el diff aprobado en el ciclo 2).
- Handlers, `SqlPlayerRepository.cs` e `IPlayerRepository.cs`: el cambio del ciclo 3 es solo `using Gaffer.Api.Shared.Criteria;` (y la eliminación de las definiciones en `IPlayerRepository.cs`). Cada fichero con el `using` nuevo usa al menos un tipo de criterio.
- `TestFixture.cs`: sin handlers nuevos; no requiere DI.

### Hallazgos

Bloqueantes: ninguno.

| ID | Tipo | Observación |
| --- | --- | --- |
| O7 | No bloqueante (evidencia) | Ciclos 1-2 sin commit: en `SqlPlayerRepository.cs`, `SqlPlayerRepositoryShould.cs` y `SqlPlayerRepositoryQueryShould.cs` el diff frente a `HEAD` mezcla ciclo 2 y ciclo 3; no se puede aislar el ciclo 3 con Git. Mitigado: el contenido coincide con el diff aprobado en ciclo 2 más la línea `using`, y el recuento 754 es idéntico. Recomendación al operador: commit por ciclo aprobado. |
| O8 | Informativo (preexistente) | `IPlayerRepository.cs` conserva `using Gaffer.Api.Commands;`, aparentemente sin uso. Fuera del alcance de AC3.2 (que solo restringe `Shared/Criteria/`); `dotnet format` no lo marca. |
| O6 | Sigue abierto (informativo) | No abordado en el paso 03 (no solicitado por el operador). |
