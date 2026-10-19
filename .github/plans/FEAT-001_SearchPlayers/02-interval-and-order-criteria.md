# Paso 02 (ciclo 2) - `IntervalCriteria` (`AddIntervalCriteria()`) y `OrderCriteria` (`ORDER BY`)

Objetivo: añadir a la búsqueda de Player:

- `IntervalCriteria<T>` (nombre del campo, valor mínimo, valor máximo; Q5 b2)): genérico con `T` comparable (`where T : IComparable<T>`; p. ej. `DateTime`, `int`), agnóstico de entidad, **ambos límites obligatorios**. Generado en `AddIntervalCriteria()` como `` `Campo` BETWEEN @min AND @max ``. Cuenta para el prefijo `WHERE ` y respeta la regla de no emitir prefijo de acción cuando la sentencia solo contiene `WHERE `.
- `OrderCriteria` (nombre del campo, sentido `ASC`/`DESC`): si hay al menos uno, sufijo ``ORDER BY `Campo` SENTIDO`` por criterio, en el orden de la lista.

Sin cambio de contrato HTTP (Q7 a)).

**Decisiones aplicadas** (ver `00-overview-and-open-questions.md`): Q3 b), Q4 b), Q5 b2) (operador); Q2 a), Q7 a), Q9 a) por defecto (no respondidas explícitamente); Q12 a), Q13 a), Q14 a) si el operador no responde antes del ciclo 2; D-LINQ a) (operador: "La opcion a") como norma de estilo. Sin bloqueos.

- **Estilo LINQ (D-LINQ)**: `AddIntervalCriteria()`, el paréntesis del bloque de valores y el `ORDER BY` se construyen con `System.Linq` (`Select` con índice para `@i{k}_min`/`@i{k}_max`, `string.Join(", ", ...)` para el orden, `Any` para Q12 a)…), sin bucles `for`/`foreach`/`while`. Sin paquetes nuevos.

- **Listas blancas por tipo (Q3 b))**: valor {`Id`, `FirstName`, `LastName`} (sin cambios), intervalo {`BirthDate`}, orden {`FirstName`, `LastName`, `BirthDate`}, sin distinguir mayúsculas; campo no permitido en cualquier tipo → `PlayerSearchFilterNotPermittedException` sin consultar.
- **Combinación (Q4 b))**: primero los criterios de valor, después los intervalos, cada uno con `AND`; el bloque de valores va entre paréntesis si contiene algún `OR` (Q12 a): un conector `OR` entre criterios de valor, y solo cuando hay al menos un intervalo, de modo que el SQL del paso 01 no cambia).
- **Contenedor (Q5 b2) + Q9 a))**: `CriteriaSet` debe admitir intervalos de distinto `T` en la misma lista; BUILDER introduce para ello una abstracción no genérica (p. ej. interfaz o base `IIntervalCriteria` con el campo y los límites como `object`) y anota el nombre elegido en `STATUS.md`. Es una necesidad de implementación, no un requisito nuevo.
- **Límites (Q5 b2))**: "ambos obligatorios" = un límite `null` (posible si `T` es de referencia, p. ej. `string`) → error de argumento (`ArgumentNullException`) al construir el criterio o, como tarde, antes de consultar. Mínimo > máximo: Q13 (por defecto sin validación). `T` distinto del tipo de la columna: Q14 (por defecto sin validación).

## Forma SQL esperada (Q3 b), Q4 b), Q5 b2), Q9 a), Q12 a))

Parámetros de intervalo: `@i{k}_min`, `@i{k}_max` (recomendados; si BUILDER elige otros, los fija en `STATUS.md`).

| Criterios | SQL exacto |
| --- | --- |
| Solo intervalo `IntervalCriteria<DateTime>` `BirthDate [1990-01-01, 1999-12-31]` | ``SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max;`` con parámetros `DateTime` |
| Valor `AND FirstName [Paul]` + intervalo | ``SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) AND `BirthDate` BETWEEN @i0_min AND @i0_max;`` |
| Valores con `OR` + intervalo (Q4 b)) | ``SELECT * FROM `Players` WHERE (`FirstName` IN (@v0_0) OR `LastName` IN (@v1_0)) AND `BirthDate` BETWEEN @i0_min AND @i0_max;`` |
| Valor `LIKE FirstName [Pa, Xa]` (sin `OR` entre criterios) + intervalo (Q12 a)) | ``SELECT * FROM `Players` WHERE (`FirstName` LIKE @v0_0 OR `FirstName` LIKE @v0_1) AND `BirthDate` BETWEEN @i0_min AND @i0_max;`` (sin paréntesis adicionales) |
| Valores con `OR`, sin intervalo | Igual que en el paso 01, sin paréntesis (Q12 a)) |
| Dos intervalos sobre `BirthDate` | ``SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max AND `BirthDate` BETWEEN @i1_min AND @i1_max;`` |
| Solo orden `LastName ASC`, `FirstName DESC` | ``SELECT * FROM `Players` ORDER BY `LastName` ASC, `FirstName` DESC;`` (sin `WHERE`) |
| Valor + intervalo + orden | ``... WHERE `FirstName` IN (@v0_0) AND `BirthDate` BETWEEN @i0_min AND @i0_max ORDER BY `BirthDate` DESC;`` |

- El sentido sale de un enum (`ASC`/`DESC`), nunca de texto del llamador; la columna, de la lista blanca.
- Varios `OrderCriteria` se separan con `, ` tras un único `ORDER BY` (lectura de "añadir prefijo ORDER BY y luego campo + sentido por cada criterio"; REVIEWER lo verifica contra esta tabla).

## Alcance de ficheros

| Fichero | Cambio |
| --- | --- |
| `Gaffer.Api/Shared/Repositories/IPlayerRepository.cs` | `IntervalCriteria<T>` y su abstracción no genérica, `OrderCriteria`, `OrderDirection` y listas en el contenedor (Q5 b2), Q9 a)). |
| `Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` | `AddIntervalCriteria()`, paréntesis del bloque de valores (Q4 b)), construcción de `ORDER BY`, listas blancas por tipo (Q3 b)). |
| Tests: `Unit/Repositories/SqlPlayerRepositoryShould.cs` (BD), `Unit/Repositories/SqlPlayerRepositoryQueryShould.cs` (SQL sin BD) | Casos nuevos. |

Fuera: controladores, DTO, handlers (no construyen intervalos ni orden con Q7 a)), Facility, cliente generado.

## Criterios de aceptación

| AC | Criterio verificable | Capa | Cómo demostrarlo |
| --- | --- | --- | --- |
| AC2.1 | Aceptación (BD): un `IntervalCriteria<DateTime>` sobre `BirthDate` (Q3 b)) devuelve exactamente los jugadores dentro del rango, **límites incluidos** (`BETWEEN` es inclusivo). | Aceptación | Tests nuevos en `SqlPlayerRepositoryShould` con jugadores en el límite, dentro y fuera; rojo y verde. |
| AC2.2 | Aceptación (BD): valor + intervalo devuelve la intersección; valores con `OR` + intervalo (Q4 b)): el intervalo restringe **ambos** lados del `OR` (un jugador que cumple solo el primer lado del `OR` y queda fuera del rango no se devuelve). | Aceptación | Tests en `SqlPlayerRepositoryShould`. |
| AC2.3 | Aceptación (BD): `OrderCriteria` devuelve la lista en el orden pedido (un criterio `ASC`, un criterio `DESC`, dos criterios con desempate). Sin `OrderCriteria`, el resultado no se ordena explícitamente (los tests existentes no dependen del orden). | Aceptación | Tests en `SqlPlayerRepositoryShould` que comparan la secuencia (`ContainInOrder`/`Equal`). |
| AC2.4 | Unitario: SQL y parámetros exactos para cada fila de "Forma SQL esperada"; nunca `WHERE AND`/`WHERE OR`; solo orden no emite `WHERE`; los límites del intervalo van en parámetros con su tipo `T` (p. ej. `DateTime`), no convertidos a texto. | Unitario | `SqlPlayerRepositoryQueryShould`; rojo y verde. |
| AC2.5 | Seguridad (Q3 b)): campo de intervalo fuera de {`BirthDate`} (p. ej. `FirstName`, `Id`) o de orden fuera de {`FirstName`, `LastName`, `BirthDate`} (p. ej. `Id`, `Gender`, `"FirstName; DROP TABLE Players"`) → `PlayerSearchFilterNotPermittedException` sin consultar; `BirthDate` como criterio de valor sigue rechazado; ningún límite aparece en el texto SQL. | Unitario | Tests en `SqlPlayerRepositoryQueryShould` (el sustituto no recibe `QueryAsync`). |
| AC2.6 | Límites obligatorios (Q5 b2)): un `IntervalCriteria<T>` con `T` de referencia y mínimo o máximo `null` → error de argumento (`ArgumentException` o derivada, p. ej. `ArgumentNullException`) sin consultar; `IntervalCriteria<T>` compila solo con `T` comparable (restricción genérica visible en la firma). Con Q13 a) y Q14 a) no hay más validaciones. | Unitario | Test en `SqlPlayerRepositoryQueryShould` (o en un test del propio tipo); `grep -n "class IntervalCriteria" Gaffer.Api -r` muestra la restricción `IComparable<T>`. |
| AC2.7 | Regresión: todos los AC del paso 01 siguen en verde; `SearchPlayersEndpointShould`, `PlayerFeaturesShould` y `OpenApiContractShould` en verde; sin diff en `Gaffer.Api/Controllers/`. | Aceptación | Filtro `dotnet test`; `git diff --stat`. |
| AC2.8 | Refactor y revalidación: build 0 warnings, `dotnet format ... --verify-no-changes` en verde, suite completa en verde. | Refactor | Comandos y resultados en `STATUS.md`. |
| AC2.9 | Estilo LINQ (D-LINQ): el código nuevo de intervalos, paréntesis y orden sigue sin bucles imperativos en `SqlPlayerRepository`; sin paquetes nuevos. | Refactor | `grep -nE "\b(for\|foreach\|while)\s*\(" Gaffer.Api/Infrastructure/SqlPlayerRepository.cs` → 0; `git diff --stat -- '*.csproj'` vacío. |
