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

## Ciclo 2 - Paso 02 (Update de Facility con gradas por `Id`)

### Uso medido

`usage-slice` repite el vacío total de telemetría del ciclo 1: sin datos con `--repo gaffer-api` (ventanas literales de `STATUS.md`), sin datos con `--repo TECHDEBT-003` (patrón OPT-008), sin datos ampliando a `--repo gaffer-api` el día completo `2026-09-27T00:00:00Z/23:59:59Z` (ninguna fila `claude-code`, solo `copilot` "sin datos"). Vacío total, no un clúster desplazado → aplica el criterio de corte de OPT-011: se usa `medido:agent-result`.

| Agente | Tokens (medido:agent-result) | Llamadas a herramientas | Duración | Δ vs. ciclo 1 (tokens / llamadas / duración) |
| --- | ---: | ---: | ---: | --- |
| BUILDER | 126 322 | 71 | 663 298 ms (~11 min 3 s) | +96 % / +103 % / +59 % |
| REVIEWER | 75 680 | 26 | 236 395 ms (~3 min 56 s) | +67 % / +73 % / +17 % |

Comparación con `usage/*.md`: BUILDER estimó ~110k+22k (~132k) frente a 126 322 medido (~4 % por debajo); REVIEWER estimó ~70k+7k (~77k) frente a 75 680 medido (~2 % por debajo). Ambas estimaciones siguen siendo razonablemente precisas; sin brecha estimado/medido que investigar.

Contexto adicional no capturado por el ciclo BUILDER→REVIEWER: entre los ciclos 1 y 2, PLANNER se invocó 4 veces para revisar el paso 02 (ULID→Guid, R-MAX, Q14-Q18), con 69 389 + 45 876 + 41 815 + 53 963 = **211 043 tokens** y 32+17+18+19 = 86 llamadas a herramienta. Ese total de replanificación **supera** el coste combinado de BUILDER+REVIEWER de este mismo ciclo (126 322 + 75 680 = 202 002).

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S3 | Replanificación entre ciclos: 4 invocaciones de PLANNER, 211 043 tokens totales, más que el ciclo 2 completo de BUILDER+REVIEWER. Cada invocación relee `00-overview-and-open-questions.md`, `02-facility-update-stable-stand-ids.md` y `STATUS.md` completos | El plan del paso 02 se corrigió de forma incremental (ULID descartado → `Guid`; luego R-MAX; luego Q14-Q18) en 4 rondas separadas de decisión del operador, cada una disparando una invocación completa de PLANNER en vez de una única consolidación de todas las decisiones pendientes antes de invocar. Escala con el número de rondas de decisión, no con el tamaño del cambio final (los ACs reescritos por ronda son pocos: AC2.3, AC2.4, AC2.8, AC2.11) |
| S4 | BUILDER: llamadas a herramienta +103 % (35→71) y duración +59 % frente a un crecimiento de alcance de ~4,6x en tests nuevos (5→23) y ~5x en ficheros tocados (1→5) — el coste creció proporcionalmente *menos* que el alcance, salvo por un extra identificado: "una llave sobrante al empalmar tests del repositorio con head/tail" obligó a un build adicional (`usage/BUILDER.md`, ciclo 2) | Empalmar bloques de test nuevos en un fichero existente con herramientas de shell (`head`/`tail`, coherente con "sin Python ni pwsh en Git Bash" del ciclo 1) es propenso a desajustar el recuento de llaves; un `Edit` dirigido a un ancla estable (p. ej. antes del cierre de la clase) no tiene ese riesgo. Coste puntual, no aislable en tokens exactos sin `usage-slice` |

No se repiten propuestas ya registradas en `MEMORIES.md`. S3 es un patrón distinto de OPT-002 (que cubre el reenvío de requisitos *antes* de iniciar la planificación); aquí la replanificación ocurre *después* de la aprobación inicial del paso, en rondas sucesivas de corrección.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P2-1 | Cuando una decisión del operador obligue a revisar un paso ya aprobado (post-planificación), ORCHESTRATOR debe recoger explícitamente todas las ambigüedades pendientes conocidas en ese momento (revisando `00-overview-and-open-questions.md`) y plantearlas al operador en una sola pregunta consolidada antes de invocar a PLANNER, en vez de invocar a PLANNER de forma inmediata tras cada mensaje individual del operador. Extiende el principio de OPT-002 (brief de una sola pasada) a las rondas de revisión posteriores a la aprobación | `AGENTS.md` (sección "Human-Controlled Flow") o `.claude/commands/harness-run` (lógica de re-invocación de PLANNER en ORCHESTRATOR) | Reduce el número de invocaciones completas de PLANNER por corrección de plan; en este ciclo, consolidar las 4 rondas (211 043 tokens, 86 llamadas) en 1-2 invocaciones podría ahorrar un 40-60 % de ese coste en escenarios equivalentes futuros. No aplicable retroactivamente a este ciclo | `medido:agent-result` (4 invocaciones de PLANNER reportadas por el harness); `STATUS.md` (histórico de decisiones Q2, R-MAX, Q14-Q19) |
| P2-2 | Al añadir varios métodos de test nuevos a un fichero existente, usar `Edit` dirigido a un ancla estable (p. ej. inmediatamente antes del cierre de la clase) en vez de empalmar con `head`/`tail` de shell, para evitar desajustes de llaves que fuercen un build adicional de diagnóstico | Skill `incremental-implementation` o `test-driven-development` de BUILDER (`.claude/agents/builder.md`) | Evita 1 build adicional (~1 ciclo de `dotnet build --no-incremental` + diagnóstico) cada vez que se empalman bloques de test grandes con herramientas de shell; magnitud exacta no aislable sin `usage-slice` | `usage/BUILDER.md` ciclo 2 ("una llave sobrante al empalmar tests del repositorio con head/tail (1 build extra)") |

### Efecto observado de propuestas previas (OPT-015, OPT-016)

Ambas se confirman **aplicadas** por evidencia de fichero y de `REVIEW.md` (aunque el `Estado` en `MEMORIES.md` solo cambia a `aplicada` con confirmación explícita del operador, fuera del alcance de este modo de análisis):

- **OPT-015** (`.gitattributes`): el fichero existe en la raíz (`*.cs text eol=lf`). `REVIEW.md` ciclo 2 confirma el efecto directamente: `git ls-files --eol` muestra los `.cs` editados como `i/lf w/crlf attr/text eol=lf` (se normalizan a LF al commitear) y anota explícitamente "el diff no es reescritura completa (distinto de F1 del ciclo 1)". El spike S1 del ciclo 1 no se repitió en el ciclo 2 pese a tocar 5 ficheros de producto.
- **OPT-016** (nota `DisposeAsync` en `dev-stack.md`): la nota se añadió como AC2.10 (confirmado por `grep -n "DisposeAsync" dev-stack.md` → línea 25) y por `REVIEW.md` ("Demostrado"). El paso 02 no volvió a tocar el ciclo de vida de `SqlConnectionProvider`, así que este ciclo no ofrece una prueba directa de que evitó repetir el sondeo en scratchpad (S2); el efecto queda confirmado como "aplicado" pero "sin datos" de ahorro adicional hasta que un paso futuro (03-05) toque de nuevo conexiones MySQL.

### Brechas de datos

- `usage-slice` sigue sin devolver datos medidos de entrada fresca vs. caché para BUILDER/REVIEWER del ciclo 2 (mismo vacío total de telemetría que el ciclo 1); no se puede separar cuánto del crecimiento de tokens es caché repetido vs. entrada nueva.
- No se puede aislar el coste exacto en tokens de S3 (replanificación) por invocación individual más allá de los totales agregados ya reportados por el harness; tampoco se puede saber si alguna de las 4 rondas habría sido evitable por sí sola sin conocer el contenido exacto de cada mensaje del operador entre invocaciones.
- El ahorro de OPT-016 sobre un sondeo futuro evitado no es verificable hasta que un paso posterior de este plan (03-05) vuelva a tocar conexiones MySQL.

### Estado de aprobación

Pendiente de decisión del operador en el checkpoint de este ciclo (P2-1, P2-2).
