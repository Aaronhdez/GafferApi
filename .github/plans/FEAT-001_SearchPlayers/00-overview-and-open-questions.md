# FEAT-001_SearchPlayers - Resumen, alcance y preguntas abiertas

Plan creado como `TECHDEBT-007` y renombrado por el operador a `FEAT-001_SearchPlayers` (carpeta movida por el operador).

## Petición del operador (literal, transmitida por ORCHESTRATOR)

| Tema | Literal / dato | Paso |
| --- | --- | --- |
| Petición | "Planificar cambios en la búsqueda de jugadores (repositorio SQL de Player), en 3 ciclos." | Todos |
| Criterios de búsqueda | "ValueCriteria: nombre del campo, lista de valores string, tipo de añadido [AND, OR, LIKE]". "IntervalCriteria: nombre del campo, valor mínimo, valor máximo". "Si la lista de criterios tiene al menos una ocurrencia, añadir prefijo \"WHERE \"." | 01, 02 |
| `AddValueCriteria()` | "AND: añade \"AND\", nombre del campo y estructura IN con los valores separados por coma." "OR: añade \"OR\", nombre del campo y estructura IN con los valores separados por coma." | 01 |
| `AddIntervalCriteria()` | "estructura BETWEEN mínimo AND máximo en SQL." | 02 |
| Prefijo de acción | "Si la sentencia de filtros sólo contiene \"WHERE \", se omite el prefijo de acción (evitar \"WHERE AND\"/\"WHERE OR\")." | 01, 02 |
| Ordenación | "OrderCriteria: nombre del campo y sentido [ASC, DESC]." "Si hay al menos uno, añadir prefijo ORDER BY y luego campo + sentido por cada criterio." | 02 |
| Refactor | "hacer los SearchCriteria agnósticos de cualquier entidad de dominio o repositorio, aislándolos en su propia carpeta." | 03 |
| Alcance | "sólo el repositorio SQL de Player por ahora". Plan en 3 ciclos (máximo duro: 6). | Todos |
| Aprobación | "apruebo el plan, ... procede" (operador, checkpoint del plan, transmitido por ORCHESTRATOR). | Todos |

## Decisiones del operador (literales, transmitidas por ORCHESTRATOR)

| Pregunta | Respuesta literal | Efecto en el plan |
| --- | --- | --- |
| Q1 | "Q1: b" | `LIKE`: conector `AND`; un `` `Campo` LIKE @p `` por valor, unidos con `OR` entre paréntesis si hay varios; el repositorio envuelve cada valor en `%valor%` y escapa `%`, `_` y `\`. Desbloquea AC1.5-AC1.6. |
| Q3 | "Q3: b" | Lista blanca por tipo: valor {`Id`, `FirstName`, `LastName`}; intervalo {`BirthDate`}; orden {`FirstName`, `LastName`, `BirthDate`}; campo no permitido → `PlayerSearchFilterNotPermittedException`. |
| Q4 | "Q4: b" | Primero valores, después intervalos con `AND`; el bloque de valores va entre paréntesis si contiene algún `OR`. |
| Q5 | "Q5: b2", respuesta a la aclaración de ORCHESTRATOR: "b2) `IntervalCriteria<T>` genérico con `T` comparable (`DateTime`, `int`…), agnóstico de entidad; ambos límites obligatorios" | `IntervalCriteria<T>` genérico, `T` comparable; mínimo y máximo obligatorios. |
| D-LINQ (ciclo 1, tras el bloqueo por Docker) | Petición: "quisiera que uses LINQ en esta feature, tengo previso añadirlo en el futuro a otras partes del proyecto". Opciones de ORCHESTRATOR: a) operadores `System.Linq` dentro del constructor SQL actual (`Select`, `Where`, `string.Join`… en lugar de bucles en `AddValueCriteria()`/`AddIntervalCriteria()`/`ORDER BY`), sin dependencias nuevas, sigue Dapper + SQL parametrizado; b) proveedor LINQ-to-SQL (EF Core, linq2db…). Respuesta: "La opcion a" | Estilo LINQ (`System.Linq`, sin paquetes nuevos) para construir cláusulas y parámetros: fase de refactor del paso 01 (AC1.12) y norma de estilo en 02 (AC2.9) y 03 (AC3.8). SQL generado, parámetros y comportamiento sin cambios. Ver "Reglas del plan". |
| Q0, Q2, Q6-Q11 | Sin respuesta explícita; el operador aprobó el plan ("apruebo el plan, ... procede") | Se aplica la recomendación de PLANNER: **por defecto (no respondida explícitamente)**. |

## Estado de partida (lectura de PLANNER, 2026-10-14, `main` d2eb7b4)

| Punto | Evidencia en el código |
| --- | --- |
| Repositorio | `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` L9-41: lista blanca `AllowedFilterColumns` (`Id`, `FirstName`, `LastName`, sin distinguir mayúsculas); `BuildFilters` genera `` WHERE `col` = @p0 `` y, del segundo en adelante, `` AND|OR `col` = @pN `` con `CriteriaType`. Valores siempre parametrizados (Dapper `DynamicParameters`); campo no permitido → `PlayerSearchFilterNotPermittedException` (400 por HTTP). Sin `ORDER BY`. |
| Tipos actuales | `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` L13-26: `PlayerSearchCriteria { List<SearchCriteria> CriteriaList }`, `SearchCriteria { Name, Value (string único), CriteriaType = AND }`, `enum CriteriaType { AND, OR }`. **`SearchCriteria` y `CriteriaType` también los usa Facility** (`IFacilityRepository.cs` L50-52 `FacilitySearchCriteria`, `SqlFacilityRepository.cs` L14-51 y L191-192, `DeleteFacilityCommandHandler.cs` L18-23). |
| Usos de Player | `SearchPlayersQueryHandler.cs` L10-13 (mapea cada `QueryFilter{Name,Value}` sin `CriteriaType`, es decir `AND`), `UpdatePlayerCommandHandler.cs` L20-25 y `DeletePlayerCommandHandler.cs` L18-24 (criterio único `Id`). |
| Contrato HTTP | `PlayerController.Search` (`POST api/.../search`) recibe `SearchPlayersRequest { Filters: [{ Name, Value }] }` (L80-87); no expone `AND/OR`, listas, intervalos ni orden. |
| Tabla | `M0001_CreatePlayersTable`: `Id` (Guid), `FirstName`, `LastName`, `SecondLastName` (nullable), `Gender` (string), `BirthDate` (DateTime). |
| Tests afectados (`grep -c "SearchCriteria\|CriteriaType\|QueryFilter"`) | `Unit/Repositories/SqlPlayerRepositoryShould.cs` (46, BD real), `Unit/Queries/SearchPlayersQueryHandlerShould.cs` (16), `Unit/Commands/UpdatePlayerCommandHandlerShould.cs` (11), `Unit/Commands/DeletePlayerCommandHandlerShould.cs` (4), `Unit/Exceptions/ExceptionHandlingMiddlewareShould.cs` (2), `Unit/Controllers/PlayerControllerShould.cs` (1). Facility: `SqlFacilityRepositoryShould.cs` (16), `SearchFacilitiesQueryHandlerShould.cs` (18), `DeleteFacilityCommandHandlerShould.cs` (4). |
| Aceptación HTTP existente | `Acceptance/SearchPlayersEndpointShould.cs`: inyección, campo no permitido (`SecondLastName`, `Gender`, `BirthDate` → 400), comilla, mayúsculas, sin filtros, por `Id`, por `FirstName`/`LastName`. Deben seguir en verde en todos los pasos. |
| CI | `.github/workflows/ci.yml` L42 `client-and-package`, L54 "Fail if the generated client differs from the versioned one". |

BUILDER revalida la línea base al inicio del ciclo 1 (`docker ps`, build y suite completa) y la anota en `STATUS.md`.

## Secuenciación: un paso = un ciclo

| Ciclo | Paso | Fichero | Contenido | ¿Contrato HTTP? |
| --- | --- | --- | --- | --- |
| 1 | 01 | `01-value-criteria.md` | `ValueCriteria` (campo, lista de valores, `AND`/`OR`/`LIKE` según Q1 b)), prefijo `WHERE `, `AddValueCriteria()` con `IN (...)`, sin `WHERE AND`/`WHERE OR`; adaptar los usos de Player | No (con Q7 a)) |
| 2 | 02 | `02-interval-and-order-criteria.md` | `IntervalCriteria<T>` (Q5 b2)) + `AddIntervalCriteria()` (`BETWEEN`, Q4 b)); `OrderCriteria` + `ORDER BY`; listas blancas por tipo (Q3 b)) | No (con Q7 a)) |
| 3 | 03 | `03-agnostic-search-criteria-folder.md` | Refactor: criterios agnósticos de entidad/repositorio en su propia carpeta; revalidación completa | No |
| 4-6 | - | - | Reserva para `NEEDS_CHANGES` (3 ciclos) | - |

Justificación: el orden sigue la especificación (filtros por valor → intervalo y orden → refactor). 01 es el núcleo (cambia el modelo de criterio de Player y todos sus usos); 02 añade dos cláusulas independientes que dependen del constructor de 01; 03 solo mueve/renombra sin cambiar comportamiento, al final para no chocar con los diffs de 01-02.

## Reglas del plan (todas las fases)

- En el ciclo N, BUILDER implementa **solo el paso N** más las correcciones que REVIEWER pidió en el ciclo anterior. REVIEWER solo aprueba el paso N (y verifica esas correcciones).
- Doble bucle de `.github/doc/work-guidelines.md` en cada paso. En este plan:
  - **Aceptación** = comportamiento observable contra MariaDB local (`Quionis_DB`): qué jugadores devuelve `SqlPlayerRepository.Get` (`Unit/Repositories/SqlPlayerRepositoryShould.cs`, clase con BD real), más la regresión HTTP `Acceptance/SearchPlayersEndpointShould.cs` en verde.
  - **Unitario** = SQL y parámetros generados, sin BD: `IDatabaseConnectionProvider` sustituido con NSubstitute, capturando `query` y `DynamicParameters` de `QueryAsync<Player>` (fichero de test nuevo, p. ej. `Unit/Repositories/SqlPlayerRepositoryQueryShould.cs`). Se comparan el texto SQL exacto (espacios incluidos, ver "Forma SQL esperada" en cada paso) y los valores de los parámetros.
  - **Refactor** con revalidación completa (build + suite).
- **Seguridad (BUGFIX-001)**: el texto SQL solo puede contener nombres de columna de la lista blanca (entre comillas invertidas), palabras clave fijas y nombres de parámetro; ningún valor del llamador se concatena. `ReturnNoPlayersWhenTheFilterValueTriesToInjectSql` (repositorio y HTTP) sigue en verde en todos los pasos. Ver Q2.
- **Contrato/swagger (OPT-032 + OPT-034)**: con Q7 a) no se tocan `Gaffer.Api/Controllers/` ni DTO; basta `OpenApiContractShould` en verde. Si un paso toca controladores o DTO, se compara el `swagger.json` servido (`Acceptance/ServedOpenApiDocumentDump.cs`, `GAFFER_SWAGGER_DUMP_PATH`) **antes de editar** y después (sha256 o `cmp`). `STATUS.md` indica la rama aplicada.
- **Rojo cuantitativo (OPT-038/OPT-041)**: el rojo se registra como `n × estado` por clase de test o `n × CSxxxx` si es de compilación; BUILDER define una sola vez en `STATUS.md` el comando de resumen y lo reutiliza.
- **Grep de usos (OPT-039)**: antes de cambiar `PlayerSearchCriteria`, `SearchCriteria`, `CriteriaType` o `IPlayerRepository.Get`, `grep -rn "<Tipo>" Gaffer.Api Gaffer.Api.Tests --include=*.cs`, adaptar todos los usos en el mismo paso y anotar comando y número de usos.
- **Revisión acotada (OPT-040)**: REVIEWER parte de `git diff --stat`, `git status` y `grep -n "\[Test\|\[TestCase"` sobre los tests cambiados.
- **Estilo LINQ (D-LINQ, operador "La opcion a")**: en `SqlPlayerRepository` las cláusulas (`IN (...)`, grupos `LIKE`, `BETWEEN`, `ORDER BY`) y los `DynamicParameters` se construyen con operadores `System.Linq` (`Select` con índice, `Where`, `SelectMany`, `Aggregate`…) y `string.Join`, sin bucles imperativos `for`/`foreach`/`while`. Sin paquetes nuevos ni proveedor LINQ-to-SQL (EF Core, linq2db…): se mantiene Dapper + SQL parametrizado y la seguridad de BUGFIX-001. El SQL exacto y los parámetros de cada paso no cambian. Verificación: `grep -nE "\b(for|foreach|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` → 0 resultados.
- Build: `dotnet build gaffer-api.sln --no-incremental -v q` con el filtro de `CLAUDE.md`; 0 warnings (`TreatWarningsAsErrors` en `Gaffer.Api`). `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` en verde (lo exige el CI). BUILDER y REVIEWER no ejecutan `-c Release`.

## Preguntas abiertas

La recomendación de PLANNER no es vinculante. Columna "Decisión": respuesta del operador o, si no la hubo, la recomendación marcada como **por defecto (no respondida explícitamente)**. Ninguna pregunta bloquea ya el plan.

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER | Decisión | Bloquea |
| --- | --- | --- | --- | --- | --- | --- |
| Q0 | Todos | Política de correcciones con `NEEDS_CHANGES`. | a) Solo corregir en el ciclo siguiente. b) Corregir y avanzar al paso siguiente si la corrección es pequeña (≤ 1 fichero de producto, sin contrato). | b), como en TECHDEBT-005/006. | b), por defecto (no respondida explícitamente) | No |
| Q1 | 01 | Semántica de `LIKE`. La especificación lo pone como "tipo de añadido" junto a `AND`/`OR`, pero solo define `AND` y `OR`. | a) Conector `AND` + un `` `Campo` LIKE @p `` por valor, unidos con `OR` entre paréntesis si hay varios; el valor se usa tal cual (el llamador pone `%`/`_`). b) Como a), pero el repositorio envuelve cada valor en `%valor%` (contiene) y escapa `%`, `_` y `\` del valor. c) Como a)/b) pero con conector `OR`. d) `LIKE` es un operador y el conector va en otro campo (cambia el modelo pedido). | b): búsqueda "contiene" segura ante comodines del usuario. | **b)** (operador: "Q1: b") | No (resuelta) |
| Q2 | 01, 02 | Valores: parametrizados o literales. La especificación dice "estructura IN con los valores separados por coma". | a) Un parámetro por valor (`IN (@v0_0, @v0_1)`); lo mismo para mínimo/máximo de `BETWEEN`. b) Valores literales concatenados (rompe los tests de inyección de BUGFIX-001). | a). | a), por defecto (no respondida explícitamente) | No |
| Q3 | 02 | Lista blanca por tipo de criterio. Hoy solo `Id`, `FirstName`, `LastName`; `BirthDate`, `Gender` y `SecondLastName` dan 400 por HTTP (tests de aceptación). | a) Una sola lista para todo (sin `BirthDate` no hay intervalo útil). b) Por tipo: valor {`Id`, `FirstName`, `LastName`} (sin cambios), intervalo {`BirthDate`}, orden {`FirstName`, `LastName`, `BirthDate`}. c) Las 6 columnas para todo (cambia el 400 de los tests HTTP actuales). | b). Campo no permitido en intervalo u orden → misma `PlayerSearchFilterNotPermittedException`. | **b)** (operador: "Q3: b") | No (resuelta) |
| Q4 | 02 | Combinación de `IntervalCriteria` con el resto: conector y precedencia. La especificación no da conector para el intervalo ni dice si `AddValueCriteria()` va antes que `AddIntervalCriteria()`. En SQL `AND` precede a `OR`: `WHERE a IN (..) OR b IN (..) AND c BETWEEN ..` = `a OR (b AND c)`. | a) Primero valores y después intervalos, cada intervalo con `AND`, sin paréntesis (literal). b) Como a), pero el bloque de valores va entre paréntesis si contiene algún `OR`, de modo que los intervalos restringen todo. c) `IntervalCriteria` lleva también tipo de añadido (cambia el modelo pedido). | b): resultado predecible; a) es la lectura literal. | **b)** (operador: "Q4: b") | No (resuelta) |
| Q5 | 02 | Tipo y límites de `IntervalCriteria`. | a) Mínimo y máximo `string`, ambos obligatorios (nulo o vacío → error de argumento); la BD convierte (`'2000-01-01'`). b) Tipados (p. ej. `DateTime`, o genérico). b2) (aclaración de ORCHESTRATOR) `IntervalCriteria<T>` genérico con `T` comparable (`DateTime`, `int`…), agnóstico de entidad; ambos límites obligatorios. c) Límites opcionales: solo mínimo → `>=`, solo máximo → `<=`. | a): coherente con "lista de valores string" y con el modelo agnóstico. | **b2)** (operador: "Q5: b2") | No (resuelta) |
| Q6 | 01 | `ValueCriteria` con lista de valores vacía o nula (`IN ()` es SQL inválido). | a) Error de argumento antes de consultar. b) Ignorar ese criterio. | a): no amplía resultados en silencio. | a), por defecto (no respondida explícitamente) | No |
| Q7 | Todos | Impacto en el contrato HTTP. | a) Sin cambios: `SearchPlayersRequest` sigue con `{Name, Value}`; cada filtro se mapea a un `ValueCriteria` `AND` de un valor; swagger idéntico. b) Exponer listas, `LIKE`, intervalos y orden en `SearchPlayersRequest` (cambio de contrato: regenerar y versionar `Gaffer.Api.Client` antes del push por `client-and-package` (OPT-030), versión del cliente y paso nuevo; no cabe en 3 ciclos sin usar reserva). | a): la especificación limita el alcance al repositorio. | a), por defecto (no respondida explícitamente) | No |
| Q8 | 03 | Facility y los tipos heredados. `SearchCriteria`/`CriteriaType` están en `IPlayerRepository.cs` y los usa Facility. | a) Facility conserva `SearchCriteria`/`CriteriaType` tal cual; en el paso 03 se mueven sin cambios a un fichero propio (fuera de `IPlayerRepository.cs`). b) Migrar Facility a los criterios nuevos (fuera de "sólo Player"; paso nuevo). c) Dejarlos donde están. | a). | a), por defecto (no respondida explícitamente) | No |
| Q9 | 01, 02, 03 | Nombres, carpeta y espacio de nombres. Una carpeta `SearchCriteria` daría el espacio de nombres `Gaffer.Api.Shared.SearchCriteria`, que choca con la clase heredada `SearchCriteria` (Q8 a)). "Agnóstico de cualquier entidad" implica que `PlayerSearchCriteria` deja de existir en la firma de `IPlayerRepository.Get`. | a) Carpeta `Gaffer.Api/Shared/Criteria/` (namespace `Gaffer.Api.Shared.Criteria`); tipos `ValueCriteria`, `ValueCriteriaType { AND, OR, LIKE }`, `IntervalCriteria<T>` (Q5 b2)), `OrderCriteria`, `OrderDirection { ASC, DESC }` y contenedor `CriteriaSet { ValueCriteria, IntervalCriteria, OrderCriteria }`; `IPlayerRepository.Get(CriteriaSet, ...)`. b) Otros nombres o carpeta que indique el operador. | a). En 01-02 los tipos viven junto a los actuales y el paso 03 solo los mueve. | a), por defecto (no respondida explícitamente) | No |
| Q10 | 03 | ¿El refactor también saca `AddValueCriteria()`/`AddIntervalCriteria()`/orden del repositorio? | a) No: siguen como métodos privados de `SqlPlayerRepository`; solo se aíslan los tipos de criterio. b) Sí: constructor SQL reutilizable en `Gaffer.Api/Infrastructure/` que recibe las listas blancas del repositorio (preparado para Facility). | a): cambio mínimo; b) cuando Facility lo necesite. | a), por defecto (no respondida explícitamente) | No |
| Q11 | 01 | `ValueCriteria` del primer criterio con `OR`/`LIKE`: la especificación omite el prefijo de acción cuando solo hay `WHERE `. Con `LIKE` (Q1 b)) el primer criterio emite solo `` `Campo` LIKE @p `` (o el grupo entre paréntesis si hay varios valores). ¿Se confirma que el tipo del primer criterio no altera nada más? | a) Sí (literal). b) Otra regla. | a). | a), por defecto (no respondida explícitamente) | No |

### Preguntas derivadas de las decisiones (nuevas, no bloquean)

Surgen al aplicar Q1 b), Q4 b) y Q5 b2). Si no hay respuesta antes del ciclo indicado, se aplica la columna "Sin respuesta"; ORCHESTRATOR puede elevarlas en el checkpoint previo (OPT-017).

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER | Sin respuesta | Bloquea |
| --- | --- | --- | --- | --- | --- | --- |
| Q12 | 02 | Q4 b) "si contiene algún `OR`": ¿cuenta el `OR` interno de un grupo `LIKE` de varios valores (ya va entre paréntesis por Q1 b))? ¿Y se aplican los paréntesis aunque no haya intervalos? | a) Solo cuenta un conector `OR` emitido entre criterios de valor, y los paréntesis solo se añaden cuando hay al menos un intervalo (el SQL del paso 01 no cambia). b) Cualquier `OR` del texto, y siempre que haya `OR`. | a): mismo resultado semántico que b) y no altera el SQL exacto aprobado en el paso 01. | a) | No |
| Q13 | 02 | Q5 b2) no dice qué pasa si mínimo > máximo. | a) Sin validación: se emite `BETWEEN` tal cual y la BD devuelve 0 filas (comportamiento SQL literal). b) Error de argumento sin consultar. | a): no añade reglas no pedidas. | a) | No |
| Q14 | 02 | Q5 b2) no dice qué pasa si `T` no corresponde al tipo de la columna (p. ej. `IntervalCriteria<int>` sobre `BirthDate`). | a) Sin validación en el repositorio: la lista blanca solo valida el nombre del campo; Dapper envía el parámetro tipado y la BD compara. b) Mapa columna → tipo en el repositorio; tipo distinto → `PlayerSearchFilterNotPermittedException`. | a): Q3 b) define listas de nombres, no de tipos. | a) | No |

## Acciones del operador (los agentes no las hacen)

| Cuándo | Acción |
| --- | --- |
| Antes del ciclo 1 | Plan aprobado; Q1, Q3, Q4 y Q5 respondidas. Pendiente: crear la rama de trabajo (ver `STATUS.md`, "Rama"). |
| Antes del ciclo 2 | Opcional: Q12, Q13, Q14 (sin respuesta se aplica a)). |
| Antes del ciclo 3 | Opcional: Q8, Q10 (aplicada la recomendación por defecto). |
| Antes del push | Con Q7 a) el OpenAPI no cambia: no hace falta regenerar el cliente. **Si Q7 = b)**: prerrequisito del job `client-and-package` (OPT-030): regenerar `Gaffer.Api.Client` (build Release), versionar `GafferApiClient.cs` y `OpenApiSpecs.json`, y subir `GlobalPackageVersion` (`Directory.Build.props`) e `infoVersion` (`Gaffer.Api/nswag.json`) según SemVer; sin ello el CI falla en "Fail if the generated client differs from the versioned one". |

## Fuera de alcance

- `SqlFacilityRepository` y la búsqueda de Facility (salvo mover tipos heredados sin cambios, Q8 a)).
- Paginación, `SELECT` de columnas concretas, búsqueda de texto completo.
- Cambios de contrato HTTP (salvo Q7 b), que requiere replanificar).
- Artefactos generados de `Gaffer.Api.Client`.

## Restricciones

- No se tocan, versionan, auditan ni verifican los artefactos generados de `Gaffer.Api.Client`. BUILDER no ejecuta `dotnet build -c Release`.
- Tests con BD contra el Docker local `Quionis_DB`; nunca se omiten. Si no está disponible: `BLOCKED` con el error exacto.
- DI: no se prevén registros nuevos; si aparece alguno, se refleja en `TestFixture` en el mismo incremento (`TestServerDependenciesShould` en verde).
- Sin paquetes nuevos (D-LINQ usa solo `System.Linq` del BCL). Sin operaciones de Git (salvo `git worktree` de REVIEWER según `work-guidelines.md`). Máximo 6 ciclos.
- Documentación afectada (DOCUMENTER, solo tras `APPROVED`): `doc/features/player-search.md` y `doc/features/technical/player-search-and-persistence.md`.
