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
