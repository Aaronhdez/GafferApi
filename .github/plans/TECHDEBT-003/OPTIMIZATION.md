# OPTIMIZATION - TECHDEBT-003

## Ciclo 1 - Paso 01 (SqlConnectionProvider)

### Uso medido (`usage-slice`)

`usage-slice` no devolvió datos: ni con las ventanas literales de `STATUS.md` (`--repo gaffer-api`), ni con `--repo TECHDEBT-003` (patrón OPT-008), ni ampliando a el día completo `2026-09-27T00:00:00Z/23:59:59Z` para ambos valores de `--repo`. Vacío total de telemetría en las cuatro consultas (no un clúster desplazado de huso horario) → aplica el criterio de corte de OPT-011: se detiene la bisección y se usa `medido:agent-result` sin más intentos.

| Agente | Tokens (medido:agent-result) | Llamadas a herramientas | Duración | Ficheros leídos (usage/*.md) | Comandos de salida grande (usage/*.md) |
| --- | ---: | ---: | ---: | --- | --- |
| BUILDER | 64 411 | 35 | 416 735 ms (~6 min 57 s) | ~8 (00-overview, `SqlConnectionProvider.cs`, `SqlConnectionProviderShould.cs`, `DatabaseStartupShould.cs`) | `dotnet build --no-incremental` x4 (filtrado); `dotnet test` suite completa x3 |
| REVIEWER | 45 375 | 15 | 201 383 ms (~3 min 21 s) | ~5 (`git diff` completo, `01-sql-connection-provider-lifecycle.md`, `STATUS.md`, `00-overview` con grep) | `git diff` completo (reescritura de fichero por EOL, ~340 líneas); `dotnet test`/`dotnet build` filtrados |

Comparación con las estimaciones propias de `usage/*.md`: BUILDER estimó ~60k entrada + ~12k salida (~72k) frente a 64 411 medido; REVIEWER estimó ~45k + ~6k (~51k) frente a 45 375 medido. Ambas estimaciones son razonablemente cercanas (BUILDER ~11% por encima, REVIEWER ~11% por encima); no hay brecha estimado/medido relevante que investigar este ciclo.

Ratio BUILDER/REVIEWER: 1,42x en tokens pero 2,33x en llamadas a herramientas y 2,07x en duración — REVIEWER gastó proporcionalmente más tokens por llamada, consistente con el hallazgo F1 (una sola llamada de `git diff` infló su entrada).

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S1 | `git diff` completo mostrado a REVIEWER: ~340 líneas (159+/182-) reales frente a ~85 líneas (31+/54-) del cambio real con `--ignore-all-space`, según F1 en `REVIEW.md` y confirmado aquí con `git ls-files --eol -- Gaffer.Api/Shared/ConnectionProviders/SqlConnectionProvider.cs` → `i/lf w/-text attr/` (índice en LF, working tree con CRLF, sin atributo `text` declarado). Único "comando de salida grande" no filtrable en la sesión de REVIEWER (15 llamadas totales) | No existe `.gitattributes` en el repo (confirmado: `test -f .gitattributes` → ausente); `core.autocrlf=true` no normaliza un fichero que Git clasifica `w/-text` porque no hay atributo `text` explícito, así que una edición con CRLF (BUILDER usa `sed`/`Edit` en Git Bash sin Python/pwsh, según `usage/BUILDER.md`) se conserva sin corregir y `git diff` reescribe el fichero completo en vez de mostrar solo el cambio real. Esto se repetirá en cada ciclo futuro que edite este fichero (u otros `.cs` tocados de forma similar) mientras no se corrija a nivel de repo |
| S2 | BUILDER construyó un proyecto scratchpad desechable para sondear en caliente el comportamiento de `MySqlConnection.DisposeAsync` (MySql.Data 26.7.0): no lanza `Component.Disposed` ni libera el `Component` de una conexión cerrada (solo `Dispose()` síncrono lo hace). Documentado explícitamente en `STATUS.md` (ciclo 1, paso 01, fila "AC1.2: hallazgo del driver") y en `usage/BUILDER.md` ("Coste extra: sonda en scratchpad...") | Comportamiento del driver no documentado en `.github/doc/common/dev-stack.md` (que ya menciona `MySql.Data` en la línea 24, pero sin detalle de ciclo de vida de conexión/`DisposeAsync`); mismo patrón que OPT-010 (Swagger/NSwag) y OPT-012 (tipo de excepción MySQL) — hechos de comportamiento de dependencias que se descubren por sondeo repetible en vez de estar documentados una vez. No aislable en tokens exactos sin `usage-slice` (dato no disponible este ciclo), pero contribuye al recuento de 35 llamadas a herramientas de BUILDER (más del doble que REVIEWER) y a los 416 s de duración |

No se repiten propuestas ya registradas en `MEMORIES.md`. OPT-012 cubre el tipo de excepción MySQL (`MySqlException`), no el comportamiento de `DisposeAsync`/`Component.Disposed` (S2 es un hecho nuevo y distinto). Ninguna entrada existente cubre CRLF/`.gitattributes` (S1 es nuevo).

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P1-1 | Añadir un `.gitattributes` en la raíz del repo que normalice explícitamente a LF los ficheros de código fuente (p. ej. `* text=auto eol=lf` o, más acotado, `*.cs text eol=lf` junto con las extensiones de proyecto/config habituales), cerrando el hueco por el que `core.autocrlf=true` deja sin normalizar un fichero que Git clasifica `w/-text` sin atributo `text` explícito. Es un fichero de configuración de repo, no código/agente/prompt: queda fuera del alcance de edición de BUILDER/REVIEWER/OPTIMIZER tal como está definido hoy; requiere aplicación por el operador o inclusión explícita en el alcance de un paso futuro del plan | Config. de repo (`.gitattributes`, raíz) | Restaura el diff a su tamaño real (~4x menos líneas: ~85 en vez de ~340) cada vez que se edite este fichero u otros `.cs` con el mismo patrón, en cualquier ciclo futuro de cualquier plan que los toque — no es un ahorro de una sola vez | `REVIEW.md` F1; `git ls-files --eol` (este ciclo); `usage/REVIEWER.md` (única línea de "salida grande" no filtrable) |
| P1-2 | Documentar en `.github/doc/common/dev-stack.md` (junto a la mención existente de `MySql.Data` en la línea 24) el hecho de comportamiento del driver: "`MySql.Data` 26.7.0: `MySqlConnection.DisposeAsync()` no lanza el evento `Component.Disposed` ni libera el `Component` subyacente de una conexión ya cerrada (la conexión puede reabrirse); solo `Dispose()` síncrono ejecuta esa liberación. Para aserciones sobre liberación de conexión al cierre de un scope, basar el test en `Dispose()`, no en `DisposeAsync()`." | `.github/doc/common/dev-stack.md` | Evita repetir el sondeo en scratchpad (proyecto desechable + ejecución) en los pasos 02-05 de este plan (o en planes futuros) que toquen el ciclo de vida de `SqlConnectionProvider` o conexiones MySQL; mismo patrón de ahorro que OPT-010/OPT-012 | `STATUS.md` ciclo 1 paso 01 (fila "AC1.2: hallazgo del driver"); `usage/BUILDER.md` (observación de coste extra) |

### Brechas de datos

- `usage-slice` no proporcionó datos medidos en tokens de entrada nueva / caché lectura / caché escritura / salida para esta ventana (vacío total de telemetría, ver arriba); todo el análisis de tokens de este ciclo se apoya en `medido:agent-result` (totales por subagente), que no distingue caché de entrada fresca. No es posible confirmar cuánta parte de los 64 411 tokens de BUILDER corresponde específicamente al sondeo S2 sin esa granularidad.
- No hay ciclo previo de este plan con el que comparar crecimiento entre ciclos (este es el ciclo 1).

### Estado de aprobación

Decisión del operador (verbatim): "aprueba las propuestas del optimizer, luego espera aq ue te confirme el comit para proceder con el paso 02".

- **P1-1** → aprobada, registrada como `OPT-015` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en un `.gitattributes` en la raíz del repo.
- **P1-2** → aprobada, registrada como `OPT-016` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/doc/common/dev-stack.md`.
