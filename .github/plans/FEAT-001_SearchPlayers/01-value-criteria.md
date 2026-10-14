# Paso 01 (ciclo 1) - `ValueCriteria`, prefijo `WHERE ` y `AddValueCriteria()`

Objetivo: sustituir en la búsqueda de Player el criterio actual (`SearchCriteria` con un único `Value` y `=`) por `ValueCriteria` (nombre del campo, lista de valores `string`, tipo de añadido `AND`/`OR`/`LIKE`), generando el filtro con un método `AddValueCriteria()` en `SqlPlayerRepository`: prefijo `WHERE ` si hay al menos un criterio, `AND|OR` + campo + `IN (...)` por criterio, y sin prefijo de acción cuando la sentencia solo contiene `WHERE ` (nunca `WHERE AND`/`WHERE OR`). Sin cambio de contrato HTTP (Q7 a)). Skills de PLANNER aplicadas: `spec-driven-development`, `planning-and-task-breakdown`.

**Decisiones aplicadas** (ver `00-overview-and-open-questions.md`): Q1 b) (operador); Q2 a), Q6 a), Q7 a), Q9 a), Q11 a) por defecto (no respondidas explícitamente); D-LINQ a) (operador: "La opcion a") en la fase de refactor. Sin bloqueos.

**Refactor LINQ (D-LINQ)**: tras el verde de aceptación y unitario, `BuildFilters`, `AddValueCriteria()` y los auxiliares (`LikeExpression`…) se reescriben con `System.Linq` (`Select` con índice para `@v{i}_{j}`, `string.Join` para `, `/` OR `, etc.) sin bucles `for`/`foreach`/`while` para construir cláusulas o parámetros. Sin paquetes nuevos; mismo SQL exacto y mismos parámetros. Los tests no se modifican en esta fase.

`LIKE` (Q1 b)): conector `AND`; un `` `Campo` LIKE @p `` por valor; si hay varios valores, unidos con `OR` y el grupo entre paréntesis; el repositorio envuelve cada valor en `%valor%` y antes escapa con `\` los caracteres `%`, `_` y `\` del valor (`\` → `\\`, `%` → `\%`, `_` → `\_`; `\` se escapa primero para no duplicar los escapes añadidos). Sin cláusula `ESCAPE` (el carácter de escape por defecto de MariaDB es `\`); AC1.5 lo demuestra contra la BD.

## Forma SQL esperada (Q1 b), Q2 a), Q9 a), Q11 a))

Nombres de parámetro: `@v{i}_{j}` (criterio `i`, valor `j`), recomendados; si BUILDER elige otros, los fija en `STATUS.md` y los tests los usan tal cual.

| Criterios | SQL exacto |
| --- | --- |
| Ninguno | ``SELECT * FROM `Players`;`` (sin `WHERE`, como hoy) |
| `AND FirstName [Paul, Xavi]` | ``SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0, @v0_1);`` |
| `AND FirstName [Paul]`, `AND LastName [Gascoigne]` | ``SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) AND `LastName` IN (@v1_0);`` |
| `AND FirstName [Paul]`, `OR LastName [Martel, Hernandez]` | ``SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) OR `LastName` IN (@v1_0, @v1_1);`` |
| `OR LastName [Martel]` (primer criterio con `OR`) | ``SELECT * FROM `Players` WHERE `LastName` IN (@v0_0);`` |
| `LIKE FirstName [au]` (primer criterio) | ``SELECT * FROM `Players` WHERE `FirstName` LIKE @v0_0;`` con `@v0_0 = "%au%"` |
| `LIKE FirstName [au, av]` (primer criterio, varios valores) | ``SELECT * FROM `Players` WHERE (`FirstName` LIKE @v0_0 OR `FirstName` LIKE @v0_1);`` con `"%au%"`, `"%av%"` |
| `AND LastName [Gascoigne]`, `LIKE FirstName [Pa]` | ``SELECT * FROM `Players` WHERE `LastName` IN (@v0_0) AND `FirstName` LIKE @v1_0;`` con `@v1_0 = "%Pa%"` |
| `AND LastName [Gascoigne]`, `LIKE FirstName [Pa, Xa]` | ``SELECT * FROM `Players` WHERE `LastName` IN (@v0_0) AND (`FirstName` LIKE @v1_0 OR `FirstName` LIKE @v1_1);`` |
| `LIKE FirstName [50%_a\b]` (escape) | ``SELECT * FROM `Players` WHERE `FirstName` LIKE @v0_0;`` con `@v0_0 = "%50\%\_a\\b%"` |

- El nombre de columna en el SQL es el canónico de la lista blanca (`firstname` → `` `FirstName` ``), no el texto del llamador.
- Precedencia `AND`/`OR` entre criterios: sin paréntesis, igual que hoy (literal de la especificación); el único paréntesis del paso 01 es el del grupo `LIKE` de varios valores (Q1 b)).
- Lista blanca de valor sin cambios (`Id`, `FirstName`, `LastName`; Q3 b)) también para `LIKE`.

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` | Tipos nuevos (`ValueCriteria`, `ValueCriteriaType`, contenedor según Q9) junto a los actuales; firma de `Get`. `SearchCriteria`/`CriteriaType` **se conservan** (los usa Facility, Q8). El paso 03 los moverá. |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | `BuildFilters` → prefijo `WHERE ` + `AddValueCriteria()`; parámetros por valor; lista blanca sin cambios (`Id`, `FirstName`, `LastName`). |
| `Gaffer.Api/Queries/SearchPlayersQueryHandler.cs`, `Gaffer.Api/Commands/UpdatePlayerCommandHandler.cs`, `Gaffer.Api/Commands/DeletePlayerCommandHandler.cs` | Construyen `ValueCriteria` `AND` de un valor (mismo resultado que hoy). |
| Tests: `Unit/Repositories/SqlPlayerRepositoryShould.cs` (BD), nuevo `Unit/Repositories/SqlPlayerRepositoryQueryShould.cs` (SQL sin BD), `Unit/Queries/SearchPlayersQueryHandlerShould.cs`, `Unit/Commands/UpdatePlayerCommandHandlerShould.cs`, `Unit/Commands/DeletePlayerCommandHandlerShould.cs`, y los que aparezcan en el grep de usos (OPT-039) | Adaptación al tipo nuevo y casos nuevos. |

Fuera: `Gaffer.Api/Controllers/`, DTO, Facility (`SqlFacilityRepository`, `IFacilityRepository`, `DeleteFacilityCommandHandler` y sus tests), cliente generado, `IntervalCriteria`/`OrderCriteria` (paso 02).

## Criterios de aceptación

| AC | Criterio verificable | Capa | Cómo demostrarlo |
| --- | --- | --- | --- |
| AC1.1 | Línea base: Docker `Quionis_DB` arriba, build con 0 warnings y suite completa en verde **antes** de editar; recuento de tests anotado. | - | `docker ps`, build con el filtro de `CLAUDE.md`, `dotnet test gaffer-api.sln --verbosity quiet` (código de salida); fila en `STATUS.md`. |
| AC1.2 | Grep de usos (OPT-039) de `PlayerSearchCriteria`, `SearchCriteria`, `CriteriaType` e `IPlayerRepository` antes de cambiar la firma; todos los usos de Player adaptados en este paso; Facility sin cambios. | - | Comandos y número de usos en `STATUS.md`; `git diff --stat` sin ficheros de Facility. |
| AC1.3 | Aceptación (BD): `AND FirstName [a, b]` devuelve exactamente los jugadores con cualquiera de los dos nombres; dos criterios `AND` devuelven la intersección; `AND` + `OR` devuelven la unión según el SQL de la tabla; sin criterios devuelve todos. Los tests existentes de filtros (`Id`, `FirstName`, `LastName`, combinaciones estricta y condicional) siguen en verde adaptados al tipo nuevo. | Aceptación | Tests nuevos en `SqlPlayerRepositoryShould.WhenGettingPlayers`; rojo registrado (`n × CSxxxx` o `n × Failed`) y verde. |
| AC1.4 | Unitario (sin BD): el SQL y los parámetros coinciden **exactamente** con cada fila `IN` de "Forma SQL esperada", en particular nunca aparece `WHERE AND` ni `WHERE OR`, y cada valor está en un parámetro con su valor. | Unitario | `SqlPlayerRepositoryQueryShould` con `IDatabaseConnectionProvider` sustituido (NSubstitute) capturando `query` y `DynamicParameters`; rojo y verde. |
| AC1.5 | Aceptación (BD) `LIKE` (Q1 b)): un valor devuelve los jugadores cuyo campo **contiene** el valor; varios valores devuelven la unión (cualquiera de ellos); detrás de un criterio `AND` restringe con `AND`; un valor con `_` o `%` se busca literalmente (p. ej. `_` no encuentra a un jugador que solo coincidiría usándolo como comodín). | Aceptación | Tests nuevos en `SqlPlayerRepositoryShould` con jugadores que contienen y no contienen el valor; rojo y verde. |
| AC1.6 | Unitario `LIKE` (Q1 b)): SQL exacto y parámetros de las filas `LIKE` de "Forma SQL esperada" (primer criterio con uno y varios valores, detrás de otro criterio con uno y varios valores); cada parámetro vale `%valor%` con `\`, `%` y `_` escapados; ningún valor aparece en el texto SQL. | Unitario | `SqlPlayerRepositoryQueryShould`; rojo y verde. |
| AC1.7 | Seguridad: campo fuera de la lista blanca (incluidos `SecondLastName`, `Gender`, `BirthDate` y `"FirstName IN ('x') OR 1=1 --"`) → `PlayerSearchFilterNotPermittedException` sin consultar; valor con intento de inyección → 0 jugadores; valor con comilla simple encuentra al jugador; nombre de campo sin distinguir mayúsculas. | Aceptación + unitario | Tests existentes `WhenFilteringPlayersWithUntrustedInput` adaptados (mismos casos) en verde; unitario: el texto SQL no contiene ningún valor del llamador. |
| AC1.8 | `ValueCriteria` con lista vacía o nula según Q6 (por defecto a): error de argumento sin consultar). | Unitario | Test en `SqlPlayerRepositoryQueryShould` (el sustituto no recibe `QueryAsync`). |
| AC1.9 | Handlers: `SearchPlayersQueryHandler` mapea cada `QueryFilter` a un `ValueCriteria` `AND` con un único valor (mismo campo y valor); `Update`/`DeletePlayerCommandHandler` buscan por `Id` con un `ValueCriteria` `AND` de un valor. | Unitario | `SearchPlayersQueryHandlerShould.MapEveryQueryFilterToRepositorySearchCriteria` y tests de comandos adaptados (`Arg.Is<...>`), en verde. |
| AC1.10 | Regresión HTTP y contrato: `Acceptance/SearchPlayersEndpointShould.cs`, `Acceptance/PlayerFeaturesShould.cs` y `OpenApiContractShould` en verde sin cambiar sus aserciones; sin diff en `Gaffer.Api/Controllers/` (rama OPT-032). | Aceptación | Filtro `dotnet test --filter` por esas clases; `git diff --stat`. |
| AC1.11 | Refactor y revalidación: build 0 warnings, `dotnet format ... --verify-no-changes` en verde, suite completa en verde (recuento ≥ línea base de AC1.1 + tests nuevos). | Refactor | Comandos y resultados en `STATUS.md`. |
| AC1.12 | Refactor LINQ (D-LINQ): la construcción de criterios de `SqlPlayerRepository` usa `System.Linq` sin bucles imperativos; sin paquetes nuevos (`*.csproj` sin diff de `PackageReference`); comportamiento y SQL sin cambios: los tests de aceptación y unitarios de este paso pasan **sin modificarlos** durante la fase de refactor, con el mismo recuento antes y después. | Refactor | `grep -nE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` → 0; `grep -n "using System.Linq\|\.Select(\|string.Join" ...` muestra el uso; recuento de la suite antes/después del refactor en `STATUS.md`; `git diff --stat -- '*.csproj'` vacío. |
