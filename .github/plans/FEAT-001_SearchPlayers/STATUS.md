# STATUS - FEAT-001_SearchPlayers

| Campo | Valor |
| --- | --- |
| Estado | DONE |
| Ciclo | 3 |
| Plan | Creado como `TECHDEBT-007`; renombrado por el operador a `FEAT-001_SearchPlayers` |
| Aprobación del plan | **Aprobado por el operador** ("apruebo el plan, ... procede"). Decisiones: Q1 b), Q3 b), Q4 b), Q5 b2); Q0, Q2, Q6-Q11 por defecto (no respondidas explícitamente). Ver `00-overview-and-open-questions.md` |
| Rama | `FEAT-001_SearchPlayers` (`git branch --show-current`, BUILDER ciclo 1, 2026-10-14T15:50:39Z) |
| Pasos completados | Paso 01 APPROVED (ciclo 1). Paso 02 APPROVED (ciclo 2). Paso 03 APPROVED (ciclo 3); suite completa en verde (754 antes y después) |
| Pasos bloqueados | Ninguno |
| Siguiente | Cerrado: documentación completada; el operador regenera el cliente y hace commit |
| Bloqueos | Ninguno abierto. **B1** (Docker/MySQL caído) resuelto en la reanudación 2026-10-14T16:03:06Z: `Quionis_DB` arriba (`0.0.0.0:3306->3306/tcp`). Mensaje original de B1: `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`; `OneTimeSetUp: MySqlConnector.MySqlException : Unable to connect to any of the specified MySQL hosts.` |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |
| 1 | 01 `ValueCriteria`, `WHERE `, `AddValueCriteria()`, refactor LINQ | APPROVED | AC1.1-AC1.12 (detalle verbatim en `STATUS-archive.md`) | `f773fcd` |
| 2 | 02 `IntervalCriteria<T>` (`BETWEEN`), `OrderCriteria` (`ORDER BY`) | APPROVED | AC2.1-AC2.9 (detalle en "Ciclo 2 - detalle", ciclo anterior) | sin commit (operador) |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 `ValueCriteria`, prefijo `WHERE ` y `AddValueCriteria()` | APPROVED |
| 2 | 02 `IntervalCriteria` (`BETWEEN`) y `OrderCriteria` (`ORDER BY`) | APPROVED |
| 3 | 03 Refactor: criterios agnósticos en su propia carpeta | APPROVED |
| 4 | Reserva | - |
| 5 | Reserva | - |
| 6 | Reserva | - |

## Tiempos por agente (ISO 8601 UTC)

| Ciclo | Agente | Inicio | Fin | Observaciones |
| --- | --- | --- | --- | --- |
| 0 | PLANNER | n/a | n/a | PLANNER sin herramienta de reloj (sin `Bash`); fecha 2026-10-14 |
| 0 | PLANNER | n/a | n/a | Registro de decisiones del operador y renombrado a `FEAT-001_SearchPlayers`; sin reloj; fecha 2026-10-14 |
| 1 | BUILDER | 2026-10-14T15:50:39Z | 2026-10-14T15:57:26Z | Paso 01 implementado; suite no ejecutable por B1 (Docker/MySQL caído) |
| 1 | BUILDER (reanudación) | 2026-10-14T16:03:06Z | 2026-10-14T16:06:45Z | Operador: "Ya está arrnacado,docker." B1 resuelto; suite ejecutada; refactor LINQ (D-LINQ, AC1.12); `READY_FOR_REVIEW` |
| 1 | REVIEWER | 2026-10-14T16:07:54Z | 2026-10-14T16:11:06Z | Revisión ciclo 1 (paso 01): APPROVED |
| 2 | BUILDER | 2026-10-14T16:18:01Z | 2026-10-14T16:23:44Z | Paso 02 implementado; `READY_FOR_REVIEW` |
| 2 | REVIEWER | 2026-10-14T16:25:06Z | 2026-10-14T16:27:54Z | Revisión ciclo 2 (paso 02): APPROVED |
| 3 | BUILDER | 2026-10-14T16:30:31Z | 2026-10-14T16:34:50Z | Paso 03 (refactor sin cambio de comportamiento); compactación del ciclo 1 a `STATUS-archive.md`; `READY_FOR_REVIEW` |
| 3 | REVIEWER | 2026-10-14T16:35:19Z | 2026-10-14T16:37:23Z | Revisión ciclo 3 (paso 03): APPROVED |

## Comandos y resultados

| Ciclo | Agente | Comando | Resultado |
| --- | --- | --- | --- |
| 2 | BUILDER | Preflight: `docker ps` | `Quionis_DB Up 20 minutes 0.0.0.0:3306->3306/tcp` |
| 2 | BUILDER | Línea base antes de editar (`HEAD` `f773fcd`): `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) + `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Build exit 0, `0 Advertencia(s)`, `0 Errores`; tests exit 0, `Superado: 723, Total: 723` |
| 2 | BUILDER | Grep de usos OPT-039: `grep -rn "CriteriaSet" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l` | 48 usos en 10 ficheros; solo se añaden propiedades con valor por defecto (`[]`), sin cambio de firma: ningún uso requiere adaptación |
| 2 | BUILDER | Comando de resumen de rojo único (OPT-041), script en scratchpad `red.sh`: `build` = `dotnet build -v q \| grep -oE "<fichero>.cs(l,c): error CSxxxx" \| sort -u \| sed \| uniq -c`; `test` = `dotnet test --no-build --filter "FullyQualifiedName~SqlPlayerRepository" --logger trx` + `uniq -c` de `outcome` por método | - |
| 2 | BUILDER | Rojo 1 (compilación; tests escritos antes que los tipos): `red.sh build` | Exit 1; `SqlPlayerRepositoryQueryShould` 5 × CS0246; `SqlPlayerRepositoryShould` 5 × CS0246 |
| 2 | BUILDER | Rojo 2 (aserción; solo tipos en `IPlayerRepository.cs`, repositorio sin cambios): `red.sh test` | Exit 1. `SqlPlayerRepositoryQueryShould`: 20 × Failed (`BuildTheExactQueryWithIntervalAndOrderCriteria` 10, `ThrowWithoutQueryingWhenTheIntervalFieldIsNotPermitted` 4, `...OrderFieldIsNotPermitted` 4, `...OrderDirectionIsUndefined` 1, `KeepEveryIntervalBoundOutOfTheQueryText` 1), 4 × Passed nuevos esperados (`..._ValuesWithOrWithoutInterval`: SQL del paso 01 sin cambios, Q12 a); `RejectAMissingMinimumBound`/`...MaximumBound`: el constructor del tipo ya valida; `ThrowWithoutQueryingWhenBirthDateIsUsedAsAValueCriterion`: regla del paso 01). `SqlPlayerRepositoryShould`: 7 × Failed (los 7 nuevos de `WhenGettingPlayers`). Existentes: todos Passed |
| 2 | BUILDER | Verde: `red.sh build` + `red.sh test` | Build exit 0; tests exit 0, 0 Failed |
| 2 | BUILDER | Refactor/revalidación: `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`); `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | Exit 0, `0 Advertencia(s)`, `0 Errores`; format exit 0 (1ª pasada) |
| 2 | BUILDER | AC2.7: `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~SqlPlayerRepository\|...~PlayerCommandHandlerShould\|...~SearchPlayersQueryHandlerShould\|...~SearchPlayersEndpointShould\|...~PlayerFeaturesShould\|...~OpenApiContractShould" --verbosity quiet` | Exit 0; `Superado: 172, Total: 172` (141 del ciclo 1 + 31 nuevos) |
| 2 | BUILDER | Suite completa: `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Exit 0; `Superado: 754, Total: 754` (= 723 + 24 sin BD + 7 BD) |
| 2 | BUILDER | AC2.9/AC2.6: `grep -cE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs`; `git diff --stat -- '*.csproj'`; `grep -n "class IntervalCriteria" -r Gaffer.Api` | `0`; vacío; `IPlayerRepository.cs:26: public class IntervalCriteria<T> : IIntervalCriteria where T : IComparable<T>` |
| 2 | BUILDER | `git status --short` / `git diff --stat` | 4 ficheros de producto/test modificados (`IPlayerRepository.cs`, `SqlPlayerRepository.cs`, `SqlPlayerRepositoryQueryShould.cs`, `SqlPlayerRepositoryShould.cs`) + `STATUS.md`; `OPTIMIZATION.md` ya modificado antes del ciclo (OPTIMIZER). Sin `Controllers/`, DTO, handlers, Facility, `TestFixture.cs`, `*.csproj` ni cliente generado |
| 2 | REVIEWER | `docker ps`; build `--no-incremental -v q`; `dotnet format --verify-no-changes`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | `Quionis_DB` arriba; 0 avisos/0 errores; format exit 0; exit 0, `Superado: 754, Total: 754` |
| 2 | REVIEWER | Rojo en `git worktree` de `HEAD` (OPT-033): diff de tests → build; + `IPlayerRepository.cs` → `dotnet test --filter ~SqlPlayerRepository` | Rojo 1: exit 1, 5 + 5 × CS0246; rojo 2: exit 1, 27 × Failed y 4 nuevos Passed esperados (= BUILDER); worktree eliminado y podado |
| 2 | REVIEWER | Veredicto | **APPROVED**; AC2.1-AC2.9 demostrados; sin bloqueantes; observaciones O4-O6 en `REVIEW.md` |
| 3 | BUILDER | Compactación (AGENTS.md, OPT-013/026/029): filas `\| 1 \|` de esta tabla y sección "Ciclo 1 - detalle" movidas verbatim (`awk`/`sed`) | `STATUS-archive.md` nuevo (77 líneas); `STATUS.md` 173 → 105 líneas antes de las filas del ciclo 3 |
| 3 | BUILDER | Preflight: `docker ps` | `Quionis_DB Up 32 minutes 0.0.0.0:3306->3306/tcp` |
| 3 | BUILDER | AC3.1 caracterización antes de editar (árbol con el diff del ciclo 2 aprobado): `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) + `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Build exit 0, `0 Advertencia(s)`, `0 Errores`; tests exit 0, **`Superado: 754, Total: 754`**, 0 con error |
| 3 | BUILDER | Grep de usos OPT-039 (antes de mover): `grep -rlE "\b(CriteriaSet\|ValueCriteria\|ValueCriteriaType\|IIntervalCriteria\|IntervalCriteria\|OrderCriteria\|OrderDirection)\b" Gaffer.Api Gaffer.Api.Tests --include=*.cs`; ídem `SearchCriteria\|CriteriaType` | Criterios nuevos: 10 ficheros (4 producto Player + `IPlayerRepository.cs` + 5 tests Player). Heredados: 7 ficheros (Facility + `ExceptionHandlingMiddlewareShould.cs`), todos en namespace `Gaffer.Api.Shared.Repositories`, que se conserva: ninguno requiere cambio |
| 3 | BUILDER | Rojo (compilación, tras mover tipos y antes de añadir `using`), comando de resumen único (OPT-041) del ciclo 1: `dotnet build gaffer-api.sln -v q 2>&1 \| grep -oE "[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+" \| sort -u \| sed -E 's/\([0-9]+,[0-9]+\)//' \| sort \| uniq -c` | `SqlPlayerRepository.cs` 6 × CS0246, 1 × CS0535 (el proyecto de tests no llega a compilar al fallar `Gaffer.Api`) |
| 3 | BUILDER | Verde: `using Gaffer.Api.Shared.Criteria;` en 9 ficheros (`sed` antes del primer `using Gaffer.Api.Shared.(Exceptions\|Repositories);`, orden ordinal); `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | Exit 0, `0 Advertencia(s)`, `0 Errores` |
| 3 | BUILDER | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | Exit 0 (1ª pasada) |
| 3 | BUILDER | AC3.5/AC3.6 suite completa **después**: `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Exit 0; **`Superado: 754, Total: 754`** (= AC3.1) |
| 3 | BUILDER | AC3.5/AC3.6 filtro dirigido: `dotnet test gaffer-api.sln --no-build --verbosity quiet --filter "FullyQualifiedName~SqlPlayerRepository\|...~PlayerCommandHandlerShould\|...~SearchPlayersQueryHandlerShould\|...~SearchPlayersEndpointShould\|...~PlayerFeaturesShould\|...~OpenApiContractShould"` | Exit 0; `Superado: 172, Total: 172` (= ciclo 2) |
| 3 | BUILDER | AC3.2: `ls Gaffer.Api/Shared/Criteria`; `grep -rnE "Gaffer\.Api\.(Domain\|Infrastructure\|Shared\.Repositories\|Commands\|Queries)\|Dapper\|Player\|Facility" Gaffer.Api/Shared/Criteria \| wc -l`; `grep -rn "^using" Gaffer.Api/Shared/Criteria \| wc -l` | 7 ficheros (`CriteriaSet`, `IIntervalCriteria`, `IntervalCriteria`, `OrderCriteria`, `OrderDirection`, `ValueCriteria`, `ValueCriteriaType`); `0`; `0` (ningún `using`) |
| 3 | BUILDER | AC3.3: `grep -rn "PlayerSearchCriteria" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l`; `grep -n "class \|enum \|interface " Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` | `0`; solo `interface IPlayerRepository` (l. 7), `PlayerNotFoundException` (l. 14), `PlayerSearchFilterNotPermittedException` (l. 19) |
| 3 | BUILDER | AC3.4: `git diff --stat HEAD -- <5 ficheros Facility> Gaffer.Api/Controllers Gaffer.Api.Tests \| grep -i facilit \| wc -l`; `git status --short \| grep -i facilit \| wc -l`; `diff <(git show HEAD:.../IPlayerRepository.cs \| sed -n '/^public class SearchCriteria/,/^}/p;/^public enum CriteriaType/,/^}/p') <(ídem sobre Shared/Repositories/SearchCriteria.cs)` | `0`; `0`; sin diferencias (`legacy-identical`). `SearchCriteria.cs` es fichero nuevo sin seguimiento, por lo que `git diff -M` no puede mostrar el movimiento: la comparación de bloques sustituye esa evidencia |
| 3 | BUILDER | AC3.5 (tests solo `using`): `git diff --numstat HEAD -- Gaffer.Api/Commands Gaffer.Api/Queries Gaffer.Api.Tests/Unit/Commands Gaffer.Api.Tests/Unit/Queries`; `git diff -U0 HEAD -- Gaffer.Api/Commands Gaffer.Api/Queries Gaffer.Api.Tests \| grep -E "^[+-]using"` | 6 ficheros `1 0` (solo la línea `using`); 8 × `+using Gaffer.Api.Shared.Criteria;`, ningún otro `using` añadido/quitado. En `SqlPlayerRepositoryShould.cs` y `SqlPlayerRepositoryQueryShould.cs` el diff frente a `HEAD` incluye el diff del ciclo 2 (sin commit); el ciclo 3 solo inserta esa línea (mismo `sed`) |
| 3 | BUILDER | AC3.6/AC3.8: `git diff --stat HEAD -- Gaffer.Api/Controllers \| wc -l`; `grep -rnE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs Gaffer.Api/Shared/Criteria \| wc -l`; `git diff --stat HEAD -- '*.csproj' \| wc -l` | `0`; `0`; `0` |
| 3 | BUILDER | `git status --short` | Modificados: 4 producto (`DeletePlayerCommandHandler.cs`, `UpdatePlayerCommandHandler.cs`, `SearchPlayersQueryHandler.cs`, `SqlPlayerRepository.cs`) + `IPlayerRepository.cs` + 5 tests Player; nuevos: `Gaffer.Api/Shared/Criteria/` (7), `Gaffer.Api/Shared/Repositories/SearchCriteria.cs`, `STATUS-archive.md`. Sin Facility, `Controllers/`, DTO, `TestFixture.cs`, `*.csproj` ni cliente generado |
| 3 | REVIEWER | `docker ps`; build `--no-incremental -v q`; `dotnet format --verify-no-changes`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | `Quionis_DB` arriba; 0 avisos/0 errores; format exit 0; exit 0, `Superado: 754, Total: 754` |
| 3 | REVIEWER | Filtro Player + `OpenApiContractShould` + `~Facilit`; `--filter ~OpenApiContractShould` | Exit 0, 538/538; exit 0, 6/6 |
| 3 | REVIEWER | Greps AC3.2/AC3.3/AC3.8, definiciones únicas, comparación de bloques `HEAD` vs ficheros nuevos (heredados y `ValueCriteria`) | Todos `0`; `legacy-identical`, `value-identical`; rojo de compilación no reproducido (estado "antes" sin commit; ver `REVIEW.md`) |
| 3 | REVIEWER | Veredicto | **APPROVED**; AC3.1-AC3.6, AC3.8 demostrados; AC3.7 N/A (Q10 a)); sin bloqueantes; O7-O8 en `REVIEW.md` |

## Ciclo 2 - detalle (BUILDER)

Decisiones aplicadas: Q3 b), Q4 b), Q5 b2), D-LINQ a) (operador); Q12 a), Q13 a), Q14 a) por defecto (no respondidas; ORCHESTRATOR indicó aplicar la opción a) por defecto; el operador solo dijo "Prosigue con el ciclo 2").

| Tema | Decisión |
| --- | --- |
| Tipos (Q5 b2), Q9 a)) | En `IPlayerRepository.cs`: `CriteriaSet` gana `List<IIntervalCriteria> IntervalCriteria` y `List<OrderCriteria> OrderCriteria` (ambas `= []`). **Abstracción no genérica elegida: interfaz `IIntervalCriteria { string Name; object Min; object Max }`**, implementada explícitamente por `IntervalCriteria<T> where T : IComparable<T>` (inmutable: constructor `(name, min, max)`, propiedades `T Min`/`T Max`). `OrderCriteria { Name, OrderDirection Direction = ASC }`, `enum OrderDirection { ASC, DESC }` |
| Límites obligatorios (AC2.6) | `ArgumentNullException.ThrowIfNull(min/max)` en el constructor: el error ocurre al construir, antes de cualquier consulta. Q13 a): sin validar mín > máx. Q14 a): sin validar `T` frente al tipo de columna (caso `IntervalCriteria<int>` sobre `BirthDate` solo en el test sin BD, parámetros `int`) |
| Parámetros | `@i{k}_min`/`@i{k}_max` (los recomendados); `k` = índice en la lista de intervalos sin nulos. Valores en `DynamicParameters` como `object` con su tipo `T` (test: `BeOfType(DateTime/int)`) |
| Listas blancas (Q3 b)) | `AllowedFilterColumns` (valor, sin cambios), `AllowedIntervalColumns` {`BirthDate`}, `AllowedOrderColumns` {`FirstName`, `LastName`, `BirthDate`}; `OrdinalIgnoreCase`, columna canónica. Validación en orden valores → intervalos → orden, todas materializadas (`ToList()`) antes de consultar |
| Combinación (Q4 b), Q12 a)) y nota O3 | `valueClauses.Aggregate("WHERE ", AddCriteria)`; luego `GroupValuesBeforeIntervals`: si hay al menos un intervalo y algún criterio de valor **a partir del segundo** tiene conector `OR` (el `OR` del primero no se emite), el bloque tras `WHERE ` se envuelve en paréntesis; luego `intervalClauses.Aggregate(..., AddCriteria)` con conector ` AND ` y la misma regla de "solo `WHERE `" (un intervalo solo no emite `AND`). O3 de REVIEWER (precedencia `AND`/`OR` sin paréntesis) queda resuelta para los intervalos por Q4 b) y demostrada contra BD (`RestrictBothSidesOfAnOrWithTheInterval`); entre criterios de valor se mantiene el SQL literal del paso 01 (Q12 a)) |
| `ORDER BY` | `BuildOrder`: `Select(ToOrderTerm)` + `string.Join(", ", ...)` tras un único ` ORDER BY `; sin criterios, sufijo vacío. Sentido por `switch` sobre el enum → `"ASC"`/`"DESC"`; valor de enum no definido (p. ej. `(OrderDirection)42`) → `ArgumentOutOfRangeException` sin consultar (endurecimiento: el texto nunca sale de `ToString()` de un valor arbitrario) |
| Renombrado interno | `ValueCriteriaClause` → `CriteriaClause` (privado), compartido por valores e intervalos; `AddValueCriteria` → `AddCriteria`; constantes `AndConnector`/`OrConnector` |
| `TestFixture.cs` | Sin handlers nuevos: no requiere cambio de DI |
| OPT-047 (tabla "antes") | n/a: sin cambio de cuerpo de respuesta HTTP (Q7 a)) |

Tests añadidos (31; ningún test existente modificado salvo el tipo de `expectedParameters` de `BuildTheExactQueryWithOneParameterPerValue`, `string` → `object` con aserción `BeOfType`, mismos 11 casos y nombres):

| Clase | Tests | AC |
| --- | --- | --- |
| `SqlPlayerRepositoryQueryShould` (sin BD) | `BuildTheExactQueryWithIntervalAndOrderCriteria` × 11: las 8 filas de "Forma SQL esperada" + `FirstValueWithOrThenInterval` (OR del primer criterio no cuenta), `IntegerInterval` (Q14 a), `T = int`), `CanonicalColumnNamesForCaseInsensitiveIntervalAndOrderFields` | AC2.4 |
| `SqlPlayerRepositoryQueryShould` | `ThrowWithoutQueryingWhenTheIntervalFieldIsNotPermitted` × 4 (`FirstName`, `Id`, `LastName`, inyección), `...OrderFieldIsNotPermitted` × 4 (`Id`, `Gender`, `SecondLastName`, `"FirstName; DROP TABLE Players"`), `ThrowWithoutQueryingWhenBirthDateIsUsedAsAValueCriterion`, `KeepEveryIntervalBoundOutOfTheQueryText`, `ThrowWithoutQueryingWhenTheOrderDirectionIsUndefined` | AC2.5 |
| `SqlPlayerRepositoryQueryShould` | `RejectAMissingMinimumBound`, `RejectAMissingMaximumBound` (`IntervalCriteria<string>` con `null` → `ArgumentNullException`) | AC2.6 |
| `SqlPlayerRepositoryShould.WhenGettingPlayers` (BD) | `ReturnOnlyThePlayersBornWithinTheIntervalIncludingBothBounds` (1990-01-01, 1995-06-15, 1999-12-31 dentro; 1989-12-31 y 2000-01-01 fuera) | AC2.1 |
| `SqlPlayerRepositoryShould.WhenGettingPlayers` (BD) | `ReturnTheIntersectionOfAValueCriterionAndAnInterval`, `RestrictBothSidesOfAnOrWithTheInterval` (Paul, 2000, cumple solo el primer lado del `OR` y no se devuelve) | AC2.2 |
| `SqlPlayerRepositoryShould.WhenGettingPlayers` (BD) | `ReturnThePlayersInAscendingOrder`, `ReturnThePlayersInDescendingOrder`, `BreakTiesWithTheNextOrderCriterion` (`BirthDate DESC, FirstName ASC`), `ApplyValueIntervalAndOrderCriteriaTogether` (comparación con `Equal`) | AC2.3 |

Estado de AC (BUILDER, pendiente de REVIEWER):

| AC | Estado |
| --- | --- |
| AC2.1, AC2.2, AC2.3 | Hecho: rojo 1 (CS0246) y rojo 2 (7 × Failed en `SqlPlayerRepositoryShould`); verde contra `Quionis_DB` (172/172, 754/754). Sin `OrderCriteria` no se emite `ORDER BY` (`NoCriteria`, `ValuesWithOrWithoutInterval`); los tests existentes no dependen del orden |
| AC2.4 | Hecho: 10 × Failed → verde; `ValuesWithOrWithoutInterval` en verde desde el rojo 2 por diseño (fila "Valores con `OR`, sin intervalo" = SQL del paso 01) |
| AC2.5 | Hecho: 10 × Failed → verde; `BirthDate` como valor sigue rechazado (test nuevo + `WhenFilteringPlayersWithUntrustedInput` del paso 01) |
| AC2.6 | Hecho: 2 tests (verdes desde que existe el tipo: el rojo es el CS0246 del rojo 1); restricción `IComparable<T>` visible en el grep |
| AC2.7 | Hecho: filtro dirigido 172/172; sin diff en `Gaffer.Api/Controllers/` |
| AC2.8 | Hecho: build 0 avisos, `dotnet format` exit 0, suite 754/754 |
| AC2.9 | Hecho: 0 bucles; `Select`, `Aggregate`, `Concat`, `SelectMany`, `Skip`/`Any`, `string.Join`; sin diff en `*.csproj` |

## Ciclo 3 - detalle (BUILDER)

Decisiones aplicadas: Q8 a), Q9 a), Q10 a) por defecto (no respondidas; ORCHESTRATOR indicó aplicar la opción por defecto; el operador dijo "Proced con el ciclo 3"). O6 de REVIEWER y P2-1 de OPTIMIZER no se abordan (no solicitados/aprobados).

| Tema | Decisión |
| --- | --- |
| Carpeta (Q9 a)) | `Gaffer.Api/Shared/Criteria/`, namespace `Gaffer.Api.Shared.Criteria`, un fichero por tipo; contenido de cada tipo copiado sin cambios desde `IPlayerRepository.cs` (incluido el comentario de `IIntervalCriteria`). Ningún `using`: solo BCL vía `ImplicitUsings` |
| Heredados (Q8 a)) | `SearchCriteria` + `CriteriaType` movidos sin cambios a `Gaffer.Api/Shared/Repositories/SearchCriteria.cs` (mismo namespace: Facility y `ExceptionHandlingMiddlewareShould` sin diff). Un solo fichero para ambos, como estaban juntos |
| `IPlayerRepository.cs` | Solo la interfaz (`Get(CriteriaSet, ...)`) y las dos excepciones; añade `using Gaffer.Api.Shared.Criteria;`. Se conservan los `using` existentes (`Gaffer.Api.Commands`, `Gaffer.Api.Domain`); `dotnet format` no los marca |
| Q10 a) | El constructor SQL sigue privado en `SqlPlayerRepository`; sin extracción. AC3.7: **N/A, Q10 a)** |
| Bucle de refactor | Sin tests nuevos ni aserciones cambiadas: la caracterización 754/754 antes y después es la evidencia; el único rojo es de compilación (falta de `using`) |
| `TestFixture.cs` | Sin handlers nuevos: sin cambio de DI |
| OPT-047 | n/a: sin cambio de cuerpo de respuesta |

Estado de AC (BUILDER, pendiente de REVIEWER):

| AC | Estado |
| --- | --- |
| AC3.1 | Hecho: 754/754 antes de editar, build 0 avisos |
| AC3.2 | Hecho: 7 ficheros en `Shared/Criteria/`; grep de dependencias `0`; sin `using` |
| AC3.3 | Hecho: `PlayerSearchCriteria` 0; `IPlayerRepository.cs` solo interfaz + excepciones; `Get(CriteriaSet, ...)` con `CriteriaSet` de `Gaffer.Api.Shared.Criteria` |
| AC3.4 | Hecho: 0 ficheros Facility en diff/status; bloques heredados idénticos a `HEAD` |
| AC3.5 | Hecho: tests solo `+using Gaffer.Api.Shared.Criteria;`; 754/754 = AC3.1; filtro dirigido 172/172 |
| AC3.6 | Hecho: `OpenApiContractShould` en el filtro dirigido (verde); sin diff en `Controllers/`; build 0 avisos; format exit 0; suite 754/754 |
| AC3.7 | N/A, Q10 a) |
| AC3.8 | Hecho: 0 bucles en `SqlPlayerRepository.cs` y `Shared/Criteria/`; sin diff en `*.csproj` |

## Documentación modificada al cierre

| Tipo | Documento |
| --- | --- |
| Actualizado (directo) | `doc/features/player-search.md` (capacidades de criterios: valor AND/OR/LIKE, intervalo, orden; listas blancas; regla de paréntesis; HTTP sin cambios) |
| Actualizado (directo) | `doc/features/technical/player-search-and-persistence.md` (CriteriaSet y tipos de `Shared/Criteria/`, constructor SQL con LINQ, formas SQL, listas blancas, ejemplos, tests) |
| Actualizado (indirecto) | `doc/features/technical/player-update.md` (diagrama: `Get(CriteriaSet)`) |
| Actualizado (indirecto) | `doc/features/technical/database-connection-lifecycle.md` (ejemplo de `SqlPlayerRepository.Get` con `CriteriaSet` y `BuildOrder`) |
| Creado | Ninguno (se actualizó lo existente) |
| Sin cambio | `.github/doc/common/*-stack*.md` (sin cambios de paquetes ni herramientas de test; `*.csproj` sin diff); documentación de Facility (sigue con `SearchCriteria` heredado) |

DOCUMENTER: Inicio 2026-10-14T16:45:00Z, Fin 2026-10-14T17:00:00Z (aproximados).
