# STATUS - FEAT-001_SearchPlayers

| Campo | Valor |
| --- | --- |
| Estado | APPROVED |
| Ciclo | 1 |
| Plan | Creado como `TECHDEBT-007`; renombrado por el operador a `FEAT-001_SearchPlayers` |
| Aprobación del plan | **Aprobado por el operador** ("apruebo el plan, ... procede"). Decisiones: Q1 b), Q3 b), Q4 b), Q5 b2); Q0, Q2, Q6-Q11 por defecto (no respondidas explícitamente). Ver `00-overview-and-open-questions.md` |
| Rama | `FEAT-001_SearchPlayers` (`git branch --show-current`, BUILDER ciclo 1, 2026-10-14T15:50:39Z) |
| Pasos completados | Paso 01 APPROVED por REVIEWER (ciclo 1). Paso 01 implementado, incluido el refactor LINQ (D-LINQ, AC1.12); suite completa en verde (723) |
| Pasos bloqueados | Ninguno |
| Siguiente | OPTIMIZER ciclo 1; checkpoint humano antes del ciclo 2 (paso 02) |
| Bloqueos | Ninguno abierto. **B1** (Docker/MySQL caído) resuelto en la reanudación 2026-10-14T16:03:06Z: `Quionis_DB` arriba (`0.0.0.0:3306->3306/tcp`). Mensaje original de B1: `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`; `OneTimeSetUp: MySqlConnector.MySqlException : Unable to connect to any of the specified MySQL hosts.` |

## Índice resumido de ciclos APPROVED

| Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash |
| --- | --- | --- | --- | --- |

## Secuenciación

| Ciclo | Paso | Estado |
| --- | --- | --- |
| 1 | 01 `ValueCriteria`, prefijo `WHERE ` y `AddValueCriteria()` | APPROVED |
| 2 | 02 `IntervalCriteria` (`BETWEEN`) y `OrderCriteria` (`ORDER BY`) | PLANNED |
| 3 | 03 Refactor: criterios agnósticos en su propia carpeta | PLANNED |
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

## Comandos y resultados

| Ciclo | Agente | Comando | Resultado |
| --- | --- | --- | --- |
| 1 | BUILDER | `git branch --show-current` | `FEAT-001_SearchPlayers` |
| 1 | BUILDER | `docker ps` | Exit ≠ 0: `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine` (B1) |
| 1 | BUILDER | `exec 3<>/dev/tcp/127.0.0.1/3306` | `Connection refused` (B1) |
| 1 | BUILDER | Línea base: `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | Exit 0; `0 Advertencia(s)`, `0 Errores` |
| 1 | BUILDER | Línea base: `dotnet test gaffer-api.sln --no-build --logger trx` | Exit 1; `Con error: 700, Superado: 0, Total: 700`; causa única `OneTimeSetUp: MySqlConnector.MySqlException : Unable to connect to any of the specified MySQL hosts.` (B1). AC1.1 **no demostrado** |
| 1 | BUILDER | Grep de usos OPT-039 (antes de editar): `grep -rn "<Tipo>" Gaffer.Api Gaffer.Api.Tests --include=*.cs \| wc -l` | `PlayerSearchCriteria` 43 (9 ficheros, todos Player); `SearchCriteria` 134 (incluye `PlayerSearchCriteria`/`FacilitySearchCriteria`); `CriteriaType` 6; `IPlayerRepository` 15. Tras el cambio: `PlayerSearchCriteria` 0. Facility (`SearchCriteria`, `CriteriaType`, `FacilitySearchCriteria`) sin tocar |
| 1 | BUILDER | Rojo (compilación), comando de resumen único: `dotnet build gaffer-api.sln -v q 2>&1 \| grep -oE "[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error CS[0-9]+" \| sort -u \| sed -E 's/\([0-9]+,[0-9]+\)//' \| sort \| uniq -c` | `SearchPlayersQueryHandlerShould` 1 × CS0246; `SqlPlayerRepositoryQueryShould` 3 × CS0103, 7 × CS0246; `SqlPlayerRepositoryShould` 5 × CS0246 |
| 1 | BUILDER | Verde (compilación): `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | Exit 0; `0 Advertencia(s)`, `0 Errores` |
| 1 | BUILDER | `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` | 1ª: exit 2, 4 × WHITESPACE (inicializadores en una línea en los helpers `Value` de los dos tests); refactor; 2ª: exit 0 |
| 1 | BUILDER | `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~SqlPlayerRepositoryQueryShould"` | Exit 1; 16 tests descubiertos, 16 × Failed por `OneTimeSetUp` (B1), no por aserciones. Verde **no demostrado** |
| 1 | BUILDER | Sonda ad hoc fuera del repo (consola en scratchpad con `ProjectReference` a `Gaffer.Api`, proveedor falso que captura `query` y `DynamicParameters`), `dotnet run \| grep PROBE` | Exit 0; SQL y parámetros idénticos a las 10 filas de "Forma SQL esperada" (p. ej. `` WHERE `LastName` IN (@v0_0) AND (`FirstName` LIKE @v1_0 OR `FirstName` LIKE @v1_1); `` con `%Pa%`,`%Xa%`; escape `%50\%\_a\\b%`); `firstname` → `` `FirstName` ``; valor de inyección solo en parámetro; `Values=[]` y `Values=null` → `ArgumentException`, 0 consultas; `Gender` → `PlayerSearchFilterNotPermittedException`, 0 consultas. Evidencia complementaria, **no sustituye** a los tests |
| 1 | BUILDER | `git status --short` / `git diff --stat` | 9 ficheros modificados + 1 nuevo (`SqlPlayerRepositoryQueryShould.cs`); sin Facility, sin `Gaffer.Api/Controllers/`, sin cliente generado |
| 1 | BUILDER (reanudación) | `docker ps` / `exec 3<>/dev/tcp/127.0.0.1/3306` | `Quionis_DB Up 4 minutes 0.0.0.0:3306->3306/tcp`; puerto abierto. B1 resuelto |
| 1 | BUILDER (reanudación) | `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`) | Exit 0; `0 Advertencia(s)`, `0 Errores` |
| 1 | BUILDER (reanudación) | AC1.1 línea base sin editar: `git archive HEAD \| tar -x` en el scratchpad (árbol de trabajo intacto), `dotnet build` + `dotnet test gaffer-api.sln --no-build --verbosity quiet` en esa copia | Exit 0; **`Superado: 699, Total: 699`, 0 con error**. Build de la copia: 0 errores, 4 avisos NU1902/NU1903 (auditoría NuGet de `System.Security.Cryptography.Pkcs` 9.0.0 en la restauración limpia; mismos paquetes que `HEAD`, no son avisos de código). El "700" de la fila de línea base anterior era el recuento con `OneTimeSetUp` fallido; el recuento real de `HEAD` es 699 |
| 1 | BUILDER (reanudación) | Verde (paso 3 de "Reanudación"): `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~SqlPlayerRepository\|...~PlayerCommandHandlerShould\|...~SearchPlayersQueryHandlerShould\|...~SearchPlayersEndpointShould\|...~PlayerFeaturesShould\|...~OpenApiContractShould"` | Exit 0; `Superado: 141, Total: 141`, 0 con error (AC1.3-AC1.10) |
| 1 | BUILDER (reanudación) | Suite completa **antes** del refactor LINQ: `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Exit 0; `Superado: 723, Total: 723` (= 699 base + 8 BD + 16 sin BD; la estimación previa de 724 partía del 700 erróneo) |
| 1 | BUILDER (reanudación) | Refactor LINQ (D-LINQ): solo `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs`; ningún test editado | `dotnet build --no-incremental -v q`: exit 0, `0 Advertencia(s)`, `0 Errores`; `dotnet format ... --verify-no-changes --severity info --exclude Gaffer.Api.Client/`: exit 0 (1ª pasada) |
| 1 | BUILDER (reanudación) | Suite completa **después** del refactor LINQ: `dotnet test gaffer-api.sln --no-build --verbosity quiet` | Exit 0; `Superado: 723, Total: 723` (mismo recuento que antes) |
| 1 | BUILDER (reanudación) | `grep -cE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` / `grep -nE "\.Select\(\|string.Join\|\.Aggregate\(\|SelectMany" ...` / `git diff --stat -- '*.csproj'` | `0`; líneas 34 (`Select`), 40 (`Aggregate`), 42 (`SelectMany`), 66 (`Select` con índice), 70, 76 (`string.Join`), 81; diff de `*.csproj` vacío. `System.Linq` llega por `ImplicitUsings` (sin `using` explícito, que `dotnet format` marcaría como innecesario) |
| 1 | BUILDER (reanudación) | `git status --short` / `git diff --stat` | Mismos 9 modificados + 1 nuevo; sin Facility, `Controllers/`, `*.csproj` ni cliente generado |
| 1 | REVIEWER | `docker ps`; build `--no-incremental -v q`; `dotnet format --verify-no-changes`; `dotnet test gaffer-api.sln --no-build --verbosity quiet` | `Quionis_DB` arriba; 0 avisos/0 errores; format exit 0; exit 0, `Superado: 723, Total: 723` |
| 1 | REVIEWER | Línea base independiente en `git worktree` de `HEAD`: build + suite | exit 0, `Superado: 699, Total: 699` (4 avisos NU1902/NU1903 de auditoría NuGet en restauración limpia, no de código) |
| 1 | REVIEWER | Rojo en el mismo worktree con el diff de tests aplicado (OPT-033) | exit 1; 1 × CS0246 `SearchPlayersQueryHandlerShould`, 3 × CS0103 + 7 × CS0246 `SqlPlayerRepositoryQueryShould`, 5 × CS0246 `SqlPlayerRepositoryShould` (= BUILDER); worktree eliminado y podado |
| 1 | REVIEWER | Veredicto | **APPROVED**; AC1.1-AC1.12 demostrados; sin bloqueantes; observaciones O1-O3 en `REVIEW.md` |

## Ciclo 1 - detalle (BUILDER)

Decisiones de implementación:

| Tema | Decisión |
| --- | --- |
| Tipos (Q9 a)) | En `IPlayerRepository.cs` (namespace `Gaffer.Api.Shared.Repositories`): `CriteriaSet { List<ValueCriteria> ValueCriteria }`, `ValueCriteria { Name, List<string> Values, ValueCriteriaType Type = AND }`, `enum ValueCriteriaType { AND, OR, LIKE }`. `PlayerSearchCriteria` eliminado (0 usos); `SearchCriteria`/`CriteriaType` conservados para Facility (Q8 a)) |
| Parámetros | `@v{i}_{j}` (recomendados por el plan) |
| `AddValueCriteria()` | Tras D-LINQ: `ToValueCriteriaClause(criterio, índice)` valida (lista blanca, luego valores) y devuelve conector/expresión/parámetros (`Select` con índice → `@v{i}_{j}`); `BuildFilters` materializa con `ToList()` (valida todos en orden antes de construir SQL) y hace `clauses.Aggregate("WHERE ", AddValueCriteria)`, donde `AddValueCriteria(statement, clause)` antepone ` AND `/` OR ` solo si la sentencia ya tiene más que `WHERE `; parámetros con `new DynamicParameters(diccionario)`. `LIKE` usa conector `AND`. Antes del refactor: `StringBuilder` + bucles `for` |
| Q6 a) | `Values` nulo o vacío → `ArgumentException` antes de consultar (tras validar la lista blanca) |
| Valor `null` dentro de la lista | Sin regla nueva: `IN` lo pasa como parámetro (como hoy con `=`, 0 filas); `LIKE` pasa `null` (0 filas), sin `NullReferenceException` |
| Criterios `null` en la lista | Se ignoran, como hacía el código anterior |
| `TestFixture.cs` | Sin handlers nuevos: no requiere cambio de DI |
| Contrato (Q7 a)) | Sin cambios en `Controllers/` ni DTO: rama OPT-032 "solo `OpenApiContractShould`" |

Tests añadidos/adaptados:

| Clase | Cambio |
| --- | --- |
| `SqlPlayerRepositoryShould.WhenGettingPlayers` (BD) | Adaptados 7 existentes; nuevos: `ReturnPlayersMatchingAnyValueOfAnAndCriterion`, `ReturnTheIntersectionOfTwoAndCriteriaWithSeveralValues`, `ReturnTheUnionOfAnAndCriterionAndAnOrCriterionWithSeveralValues` (AC1.3); `ReturnPlayersWhoseFieldContainsTheLikeValue`, `ReturnPlayersWhoseFieldContainsAnyOfTheLikeValues`, `RestrictWithAndALikeCriterionThatFollowsAnotherCriterion`, `MatchAnUnderscoreInALikeValueLiterally`, `MatchAPercentSignInALikeValueLiterally` (AC1.5) |
| `SqlPlayerRepositoryShould` resto (BD) | `WhenFilteringPlayersWithUntrustedInput` (AC1.7), cancelación, guardar, borrar, actualizar: adaptados al tipo nuevo, mismos casos |
| `SqlPlayerRepositoryQueryShould` (nuevo, sin BD) | `BuildTheExactQueryWithOneParameterPerValue` × 11 (10 filas de la tabla + `firstname`) (AC1.4, AC1.6); `KeepEveryCallerValueOutOfTheQueryText` × 3 (AND/OR/LIKE) (AC1.7); `ThrowWithoutQueryingWhenACriterionHasNoValues`, `...HasNullValues` (AC1.8) |
| Handlers (AC1.9) | `SearchPlayersQueryHandlerShould.MapEveryQueryFilterToRepositorySearchCriteria`, `UpdatePlayerCommandHandlerShould` y `DeletePlayerCommandHandlerShould.DeleteAPlayerWhileBeingExecuted` comprueban `ValueCriteria` `AND` de un valor |

Estado de AC (BUILDER, pendiente de REVIEWER):

| AC | Estado |
| --- | --- |
| AC1.1 | Hecho: Docker arriba; build 0 warnings; suite de `HEAD` (copia sin editar) 699/699 en verde. Nota: la línea base se ejecutó en una copia de `HEAD` porque el paso ya estaba editado cuando cayó B1 |
| AC1.2 | Hecho (fila de grep OPT-039; `git diff --stat` sin Facility) |
| AC1.3, AC1.5, AC1.7 (BD), AC1.10 | Hecho: rojo de compilación (fila "Rojo (compilación)"); verde 141/141 en el filtro dirigido y 723/723 en la suite |
| AC1.4, AC1.6, AC1.8 | Hecho: rojo de compilación registrado; verde en `SqlPlayerRepositoryQueryShould` (incluido en 141/141 y 723/723) |
| AC1.9 | Hecho: verde (141/141) |
| AC1.11 | Hecho: build 0 warnings, `dotnet format` exit 0, suite 723 ≥ 699 + 24 nuevos |
| AC1.12 | Hecho: 0 bucles; `Select`/`SelectMany`/`Aggregate`/`string.Join`; sin diff en `*.csproj`; tests sin tocar en el refactor; 723 antes y 723 después |

Reanudación (tras levantar Docker/`Quionis_DB`), en este orden (ejecutada el 2026-10-14T16:03:06Z; resultados en "Comandos y resultados"):

1. `docker ps` (contenedor arriba) y `dotnet build gaffer-api.sln --no-incremental -v q` (filtro `CLAUDE.md`).
2. Rojo: el registrado es de compilación (tipos nuevos inexistentes); no se repite.
3. `dotnet test gaffer-api.sln --no-build --filter "FullyQualifiedName~SqlPlayerRepository\|FullyQualifiedName~PlayerCommandHandlerShould\|FullyQualifiedName~SearchPlayersQueryHandlerShould\|FullyQualifiedName~SearchPlayersEndpointShould\|FullyQualifiedName~PlayerFeaturesShould\|FullyQualifiedName~OpenApiContractShould"`.
4. `dotnet test gaffer-api.sln --verbosity quiet` (suite completa; recuento = 700 base + 8 BD nuevos + 16 sin BD = 724 esperado).

## Documentación modificada al cierre

Pendiente (DOCUMENTER, tras `APPROVED` y aprobación humana).
