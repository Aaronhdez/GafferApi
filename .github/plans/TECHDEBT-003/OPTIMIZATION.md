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

Decisión del operador (verbatim): "aprueba P2-1 Y P2-2 y procede con el paso 3".

- **P2-1** → aprobada, registrada como `OPT-017` en `MEMORIES.md`. Pendiente de aplicación en `AGENTS.md` (sección "Human-Controlled Flow") o `.claude/commands/harness-run` (lógica de re-invocación de PLANNER en ORCHESTRATOR).
- **P2-2** → aprobada, registrada como `OPT-018` en `MEMORIES.md`. Pendiente de aplicación en la skill `incremental-implementation` o `test-driven-development` de BUILDER (`.claude/agents/builder.md`).

## Ciclo 3 - Paso 03 (Propagar la cancelación hasta Dapper)

### Uso medido

`usage-slice` repite el vacío total de telemetría de los ciclos 1-2: sin datos con `--repo gaffer-api` para ambas ventanas (`claude-code` y `copilot`), ni con `--repo TECHDEBT-003` (patrón OPT-008; solo devuelve fila `copilot` "sin datos", ninguna `claude-code`). Vacío total, no un clúster desplazado → aplica el criterio de corte de OPT-011: se usa `medido:agent-result` (totales pasados por el ORCHESTRATOR) sin más bisección.

| Agente | Tokens (medido:agent-result) | Llamadas a herramientas | Duración | Δ vs. ciclo 2 (tokens / llamadas / duración) |
| --- | ---: | ---: | ---: | --- |
| BUILDER | 136 576 | 64 | 728 601 ms (~12 min 9 s) | +8,1 % / −9,9 % / +9,8 % |
| REVIEWER | 75 593 | 25 | 297 739 ms (~4 min 58 s) | −0,1 % / −3,8 % / +26,0 % |
| OPTIMIZER (registro entre ciclos 2→3) | 37 922 | 5 | n/a | Muy por debajo del coste de replanificación S3 del ciclo 2 (211 043 tokens / 86 llamadas); registro de OPT-017/OPT-018 sin lectura adicional relevante |

Comparación con `usage/*.md`: BUILDER estimó ~120k+20k (~140k) frente a 136 576 medido (~2,4 % por debajo); REVIEWER estimó ~75k+8k (~83k) frente a 75 593 medido (~8,9 % por debajo, la brecha estimado/medido más amplia de las tres ciclos, sin causa evidente en `usage/REVIEWER.md`; no investigable más sin `usage-slice`). BUILDER: tokens y duración suben ~9-10 % pese a que las llamadas a herramienta bajan ~10 % frente al ciclo 2 (71→64) — coherente con menos empalmes de shell (ver Efecto observado de OPT-018 abajo) pero más coste por llamada, consistente con las 3 sondas de driver registradas en `usage/BUILDER.md`. REVIEWER: tokens y llamadas casi planos, pero la duración sube +26 % — no aislable en tokens sin `usage-slice`; posible causa la verificación red del AC3.4 (5 s de ejecución real por diseño del test) más la copia completa del árbol (`git archive HEAD` + mutaciones) para el red independiente de 22 tests nuevos.

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S5 | BUILDER ejecutó 3 sondas `dotnet run probe.cs` en scratchpad para caracterizar el comportamiento de `MySql.Data` 26.7.0 ante `CancellationToken` (AC3.4): `SELECT SLEEP(5)` cancelado a los ~100 ms no lanza excepción con `ExecuteScalarAsync`/`ExecuteReaderAsync`/Dapper `QueryAsync` (termina en ~5 s); `MySqlCommand.Cancel()` explícito interrumpe pero lanza `NullReferenceException`. Una de las 3 sondas falló primero por credenciales hasta usar user-secrets (`usage/BUILDER.md` ciclo 3: "Coste extra: 3 sondas... una falló por credenciales..."). REVIEWER reprodujo el mismo hecho reejecutando el test ya existente (`StopALongRunningQueryWhenItsTokenIsCancelled`, sin sonda propia) | Comportamiento del driver no documentado en `.github/doc/common/dev-stack.md`, que ya recoge una nota equivalente sobre `MySql.Data`/`DisposeAsync` (OPT-016) pero no sobre cancelación de comandos en ejecución. Mismo patrón que OPT-010/OPT-012/OPT-016: hecho de comportamiento de dependencia descubierto por sondeo repetible (con coste extra de credenciales) en vez de estar documentado una vez. Contribuye a los 64 llamadas y 728 601 ms de BUILDER (el ciclo más largo de los tres) |
| S6 | Un ciclo completo de BUILDER+REVIEWER (212 169 tokens combinados) se gastó en el paso 03 sabiendo de antemano, por `STATUS.md` del ciclo 2 ("Pasos bloqueados: 03 (solo AC3.5, Q4)..."), que AC3.5 no podría implementarse sin respuesta a Q4; el ciclo terminó, como era previsible, en `NEEDS_CHANGES` únicamente por ese motivo (`REVIEW.md` ciclo 3, H1). El operador había instruido "procede con el paso 3" sin que Q4 estuviera resuelta ni se le hubiera planteado explícitamente la disyuntiva ("Q4 seguirá sin respuesta y el paso 03 no llegará a APPROVED por AC3.5: ¿procedes igualmente, se difiere AC3.5, o respondes Q4 antes?") | El bloqueo de AC3.5 por Q4 ya estaba registrado en `STATUS.md` antes de iniciar el ciclo 3, pero ORCHESTRATOR no lo resurgió como pregunta consolidada al operador antes de invocar a BUILDER (mismo tipo de brecha que motivó `OPT-017`, aquí en el punto de inicio de ciclo en vez de en la re-invocación de PLANNER tras aprobación). El coste no es del todo evitable (AC3.1-AC3.3 y AC3.6 sí eran demostrables y se demostraron), pero el veredicto parcial previsible no se hizo explícito de antemano, y ahora el ciclo 4 debe combinar la corrección de AC3.5 con el paso 04 (Q0, aún abierta) |

No se repiten propuestas ya registradas en `MEMORIES.md`. S5 es un hecho de comportamiento del driver distinto del cubierto por OPT-016 (`DisposeAsync`/`Component.Disposed`): aquí es la cancelación de un comando en ejecución (`SLEEP`, `Cancel()`), no el ciclo de vida de la conexión al cerrar el scope. S6 es una variante del problema de OPT-017 (falta de consolidación de decisiones pendientes antes de actuar) pero en un punto distinto del flujo (inicio de ciclo BUILDER, no re-invocación de PLANNER), por lo que se registra como propuesta nueva que amplía el ámbito de OPT-017 en vez de repetirla.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P3-1 | Documentar en `.github/doc/common/dev-stack.md` (junto a la nota de OPT-016 sobre `MySql.Data`/`DisposeAsync`) el hecho de comportamiento: "`MySql.Data` 26.7.0 no reacciona a un `CancellationToken` una vez que el comando está en ejecución: `ExecuteScalarAsync`/`ExecuteReaderAsync` (directo o vía Dapper `CommandDefinition`) completan la consulta igualmente (p. ej. `SELECT SLEEP(5)` tarda ~5 s pese a cancelar el token a los ~100 ms), sin lanzar excepción. `MySqlCommand.Cancel()` explícito interrumpe la consulta pero el driver lanza `NullReferenceException`, por lo que no es una alternativa limpia. Solo un token ya cancelado antes de abrir/ejecutar detiene la operación." Añadir también que las sondas de conexión en scratchpad deben usar la cadena de conexión de user-secrets del proyecto para evitar el fallo de credenciales del primer intento | `.github/doc/common/dev-stack.md` | Evita repetir las 3 sondas `dotnet run probe.cs` (incluida la de credenciales) en los pasos 04-05 de este plan o en planes futuros que toquen cancelación/timeouts de `MySql.Data`; mismo patrón de ahorro confirmado por OPT-016 (spike no repetido en el ciclo 2) | `STATUS.md` ciclo 3, fila "Sonda del driver"; `usage/BUILDER.md` ciclo 3 ("Coste extra: 3 sondas...") |
| P3-2 | Ampliar el ámbito de `OPT-017` (aún `pendiente` de aplicación): además de consolidar preguntas pendientes antes de re-invocar a PLANNER tras una aprobación, ORCHESTRATOR debe comprobar, antes de invocar a BUILDER para iniciar un nuevo ciclo, si el paso previsto en `STATUS.md`/`00-overview-and-open-questions.md` ya tiene algún criterio marcado como bloqueado por una pregunta sin respuesta (sección "Pasos bloqueados"); si es así, plantear esa disyuntiva explícitamente en el mensaje de checkpoint antes de empezar el ciclo (p. ej. "el paso 03 no llegará a `APPROVED` por AC3.5/Q4 sin respuesta: ¿procedes igualmente, se difiere el criterio, o respondes la pregunta antes?"), en vez de iniciar el ciclo y descubrir el veredicto parcial al final | `AGENTS.md` (sección "Human-Controlled Flow"), extiende el ámbito de `OPT-017` | Convierte un veredicto parcial previsible en una decisión explícita del operador antes de gastar el ciclo; no evita el coste de las ACs sí demostrables (AC3.1-AC3.3, AC3.6 de este ciclo eran necesarias de todos modos), pero evita la sorpresa del checkpoint y la coordinación adicional de fusionar la corrección con el paso siguiente (Q0). Magnitud no cuantificable sin un ciclo de contraste donde se plantee la disyuntiva y el operador decida diferir el criterio | `STATUS.md` ciclo 2 (bloqueo de AC3.5/Q4 ya registrado antes del ciclo 3); `REVIEW.md` ciclo 3, hallazgo H1 |

### Efecto observado de propuestas previas (OPT-017, OPT-018)

- **OPT-018** (`Edit` por ancla en vez de `head`/`tail`): **aplicada con efecto positivo confirmado**. `usage/BUILDER.md` ciclo 3 registra explícitamente "OPT-018 aplicado (Edit por ancla)" y, a diferencia del ciclo 2 ("una llave sobrante al empalmar tests del repositorio con head/tail (1 build extra)"), el ciclo 3 no registra ningún build de diagnóstico por llaves desajustadas pese a añadir bloques de test nuevos a 3 ficheros existentes (`SqlConnectionProviderShould.cs`, `PlayerControllerShould.cs`, `SqlFacilityRepositoryShould.cs`) más uno nuevo. El único build extra del ciclo 3 tiene otra causa (CS7036 en `Features_Refactor`, fuera de alcance). Consistente con la bajada de llamadas a herramienta de BUILDER (71→64) pese a más ficheros de test tocados.
- **OPT-017** (consolidar preguntas pendientes antes de re-invocar a PLANNER): sigue `pendiente` de aplicación (no se ha tocado `AGENTS.md` ni `harness-run`). Este ciclo no reprodujo su escenario original (re-invocación de PLANNER), pero sí una variante del mismo problema de fondo en un punto distinto del flujo — ver spike S6 y propuesta P3-2, que amplía explícitamente su ámbito en vez de darla por probada o refutada.

### Brechas de datos

- `usage-slice` sigue sin devolver datos medidos de entrada fresca vs. caché para BUILDER/REVIEWER del ciclo 3 (vacío total de telemetría en las tres ventanas de `claude-code`, igual que ciclos 1-2); todo el análisis de tokens se apoya en `medido:agent-result`, que no distingue caché de entrada fresca ni permite aislar el coste exacto en tokens de S5 (sondas de driver) o del +26 % de duración de REVIEWER.
- La brecha estimado/medido de REVIEWER (~8,9 % por debajo) es la mayor de las tres ciclos; no hay suficiente detalle en `usage/REVIEWER.md` para atribuirla a una causa concreta.
- No es posible cuantificar el ahorro de P3-2 sin un ciclo futuro de contraste en el que ORCHESTRATOR plantee la disyuntiva de un AC conocido como bloqueado antes de iniciar el ciclo y el operador decida diferirlo.

### Estado de aprobación

Decisión del operador (verbatim): "Q0. opción b / Q4. Opcion b / Q6. \"Gaffer Api\" / H2. Quitalo / Apruebo P3-1 y P3-2".

- **P3-1** → aprobada, registrada como `OPT-019` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/doc/common/dev-stack.md`.
- **P3-2** → aprobada, registrada como `OPT-020` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `AGENTS.md` (sección "Human-Controlled Flow"), ampliando el ámbito de `OPT-017`.

## Ciclo 4 - Corrección del paso 03 (AC3.5) + Paso 04 (Cambios pequeños)

### Uso medido

`usage-slice` repite el vacío total de telemetría de los ciclos 1-3: sin datos con `--repo gaffer-api` para ambas ventanas (`claude-code` y `copilot`), ni con `--repo TECHDEBT-003` (patrón OPT-008; solo fila `copilot` "sin datos"). Vacío total, no un clúster desplazado → aplica el criterio de corte de OPT-011: se usa `medido:agent-result` (totales pasados por el ORCHESTRATOR) sin más bisección.

| Agente | Tokens (medido:agent-result) | Llamadas a herramientas | Duración | Δ vs. ciclo 3 (tokens / llamadas / duración) |
| --- | ---: | ---: | ---: | --- |
| BUILDER | 93 784 | 61 | 599 599 ms (~9 min 60 s) | −31,3 % / −4,7 % / −17,7 % |
| REVIEWER | 53 708 | 15 | 188 963 ms (~3 min 9 s) | −28,9 % / −40,0 % / −36,5 % |
| OPTIMIZER (registro entre ciclos 3→4) | 45 006 | 5 | n/a | +18,7 % tokens vs. 2→3 (37 922), mismas 5 llamadas |

Comparación con `usage/*.md`: BUILDER estimó ~95k+14k (~109k) frente a 93 784 medido (~14 % por debajo, la estimación se mantiene por exceso como en ciclos previos). REVIEWER estimó ~60k+7k (~67k) frente a 53 708 medido (~19,8 % por debajo, la brecha estimado/medido más amplia del plan hasta ahora; consistente con una revisión más corta de lo habitual — 15 llamadas frente a 20-26 en ciclos anteriores — por un red reproducible en una sola pasada y una sola mutación, sin hallazgos que motivasen exploración adicional). No se propone corrección de la estimación: el patrón varía con la complejidad del ciclo revisado, no apunta a un sesgo sistemático corregible.

**Hallazgo principal**: pese a combinar dos entregas (corrección de AC3.5 + los 7 AC del paso 04 completo), el coste conjunto de BUILDER+REVIEWER de este ciclo (93 784 + 53 708 = 147 492 tokens) es un 30,5 % **menor** que el del ciclo 3 en solitario (136 576 + 75 593 = 212 169), que solo cubrió el paso 03. Ninguna invocación de PLANNER ocurrió en la transición 3→4 (frente a las 4 invocaciones / 211 043 tokens de la transición 1→2, spike S3 del ciclo 2).

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S7 | Segundo ciclo consecutivo (3 y 4) en el que tanto BUILDER como REVIEWER abren `STATUS.md` de forma completa y la salida queda "persistida por salida grande" antes de cambiar a lecturas por rango (`sed -n`): ciclo 3 BUILDER "lectura inicial de plan de 41.7KB persistida"; ciclo 3 REVIEWER "cat inicial de plan+STATUS+REVIEW (44KB, persistido; evitable leyendo por rangos)"; ciclo 4 BUILDER "lectura inicial de STATUS.md persistida por salida grande (31.5KB) antes de pasar a sed -n"; ciclo 4 REVIEWER "cat inicial de STATUS.md (33KB, persistido)" (`usage/BUILDER.md`, `usage/REVIEWER.md`) | `STATUS.md` crece cada ciclo con el detalle íntegro de todos los ciclos anteriores (196 líneas tras 4 ciclos; el ciclo 1 y 2 ya suman ~150 de esas líneas y están cerrados con veredicto `APPROVED`, sin cambios previstos). La guía de "Minimal Reading" de `CLAUDE.md` (grep/glob antes de `Read`; `offset`/`limit` en ficheros grandes) no evita el primer `cat` completo porque el agente necesita contexto íntegro del historial al empezar; el patrón "Hierarchical Summary" ya aprobado para `OPTIMIZATION.md` (`OPT-013`) y `MEMORIES.md` (`OPT-014`) no se ha extendido a `STATUS.md`, que es justamente el fichero que BUILDER y REVIEWER leen primero en cada ciclo |

No se repiten propuestas ya registradas en `MEMORIES.md`. S7 extiende el patrón de fondo de `OPT-013`/`OPT-014` (ficheros de harness que crecen sin compactación y se leen íntegros en cada ciclo) a un tercer fichero (`STATUS.md`) y una causa distinta (lectura inicial de BUILDER/REVIEWER, no de OPTIMIZER entre ciclos); se registra como propuesta nueva por tratarse de un fichero y unos agentes distintos.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P4-1 | Extender el patrón "Hierarchical Summary" (ya aprobado como `OPT-013`/`OPT-014` para `OPTIMIZATION.md`/`MEMORIES.md`) a `STATUS.md`: cuando un ciclo alcance veredicto `APPROVED` y no se prevean más correcciones sobre sus filas, compactar su sección de detalle (bloque "Ciclo N - Paso X" y las filas correspondientes de "Comandos y resultados") a una fila resumen de una tabla-índice al inicio del fichero (`Ciclo \| Paso \| Veredicto \| AC(s) relevantes \| referencia al commit/hash`), conservando el detalle íntegro en el historial de Git; mantener el detalle completo solo para el ciclo en curso y el inmediatamente anterior (necesario para "correcciones de REVIEWER del ciclo anterior" según la secuenciación del plan). Añadir la instrucción en `AGENTS.md` (sección "Plans and Evidence", regla de contenido de `STATUS.md`) para que aplique a BUILDER al iniciar cada ciclo | `AGENTS.md` (sección "Plans and Evidence") o `.claude/agents/builder.md` (instrucción de mantenimiento de `STATUS.md`) | Reduce el tamaño de la primera lectura íntegra de `STATUS.md` que BUILDER y REVIEWER hacen en cada ciclo; en este plan, compactar los ciclos 1-2 (ya cerrados, ~150 de las 196 líneas actuales) recortaría esa primera lectura en más de la mitad para los ciclos 5-6 restantes, evitando el patrón de "salida grande persistida" repetido en 2 ciclos consecutivos por 2 agentes distintos | `usage/BUILDER.md` ciclos 3-4; `usage/REVIEWER.md` ciclos 3-4 (cuatro observaciones de lectura inicial "persistida" por tamaño) |

### Efecto observado de propuestas previas (OPT-017, OPT-018, OPT-020)

- **OPT-018** (`Edit` por ancla en vez de `head`/`tail`): sin regresión, efecto positivo sostenido. `STATUS.md` ciclo 4 no registra ningún build de diagnóstico por llaves desajustadas pese a añadir bloques de test a `ExceptionHandlingMiddlewareShould.cs` y `SqlConnectionProviderShould.cs` (ficheros existentes) más un fichero nuevo. Llamadas a herramienta de BUILDER siguen bajando (64→61) pese a combinar la corrección del paso 03 con el paso 04 completo.
- **OPT-017 / OPT-020** (consolidar decisiones pendientes antes de actuar): **efecto positivo confirmado** en su forma extendida. Según el encargo de este ciclo, ORCHESTRATOR consolidó Q4 (nivel de log) y el bloqueo conocido del paso siguiente (Q7) en una sola pregunta antes de iniciar el ciclo 4, y pasó las respuestas directamente a BUILDER sin reinvocar a PLANNER (los AC ya estaban parametrizados por las respuestas de Q). Resultado medible: cero invocaciones de PLANNER en la transición 3→4 (frente a 4 invocaciones / 211 043 tokens en la transición 1→2), y un ciclo con más alcance (corrección + paso 04 completo) que costó un 30,5 % menos que el ciclo 3 en solitario (147 492 vs. 212 169 tokens BUILDER+REVIEWER), terminando en `APPROVED` sin la sorpresa de veredicto parcial que motivó `OPT-020` en el ciclo 3 (S6). `Estado` se mantiene `pendiente` en `MEMORIES.md` porque `AGENTS.md` no se ha editado formalmente; la aplicación ha sido conductual por parte de ORCHESTRATOR.

### Brechas de datos

- `usage-slice` sigue sin devolver datos medidos de entrada fresca vs. caché para BUILDER/REVIEWER del ciclo 4 (vacío total de telemetría en las cuatro consultas de esta invocación, igual que los ciclos 1-3); todo el análisis de tokens se apoya en `medido:agent-result`.
- La brecha estimado/medido de REVIEWER (~19,8 % por debajo) es la mayor observada en el plan; no hay suficiente detalle en `usage/REVIEWER.md` para separar cuánto se debe a la brevedad real de la revisión (15 llamadas, veredicto limpio) frente a una sobreestimación sistemática del propio REVIEWER.
- No es posible aislar en tokens exactos qué parte del ahorro conjunto del ciclo 4 (−30,5 % vs. ciclo 3) corresponde a OPT-017/OPT-020 (consolidación de preguntas) frente a otros factores no controlados (p. ej. menor superficie de descubrimiento en el paso 04, que es mayoritariamente limpieza mecánica frente al paso 03, que exigió sondeo de driver).
- No se puede cuantificar el ahorro de P4-1 sin un ciclo futuro en el que `STATUS.md` ya esté compactado y se mida la primera lectura de BUILDER/REVIEWER frente a la actual.

### Estado de aprobación

Decisión del operador (verbatim): "Aprueba P4 y sigue con el ciclo 5".

- **P4-1** → aprobada, registrada como `OPT-021` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `AGENTS.md` (sección "Plans and Evidence") o `.claude/agents/builder.md`.

## Ciclo 5 - Paso 05 (0 warnings de compilación) — todos los pasos APPROVED, cierre probable del plan

### Uso medido

`usage-slice` repite el vacío total de telemetría de los ciclos 1-4: sin datos con `--repo gaffer-api` (`claude-code` y `copilot`), ni con `--repo TECHDEBT-003` (patrón OPT-008; solo fila `copilot` "sin datos"). Vacío total, no un clúster desplazado → aplica el criterio de corte de OPT-011: se usa `medido:agent-result` sin más bisección.

| Agente | Tokens (medido:agent-result) | Llamadas a herramientas | Duración | Δ vs. ciclo 4 (tokens / llamadas / duración) |
| --- | ---: | ---: | ---: | --- |
| BUILDER | 77 802 | 41 | 389 925 ms (~6 min 30 s) | −17,0 % / −32,8 % / −35,0 % |
| REVIEWER | 54 888 | 21 | 206 491 ms (~3 min 26 s) | +2,2 % / +40,0 % / +9,3 % |
| OPTIMIZER (registro entre ciclos 4→5) | 50 800 | 5 | n/a | +12,9 % tokens vs. 3→4 (45 006), mismas 5 llamadas, para 1-2 propuestas (mismo patrón que 2→3→4: 37 922 → 45 006 → 50 800) |

Comparación con `usage/*.md`: BUILDER estimó ~70k+10k (~80k) frente a 77 802 medido (~3 % por debajo, la estimación más precisa del plan hasta ahora). REVIEWER estimó ~55k+7k (~62k) frente a 54 888 medido (~11,5 % por debajo, en línea con las brechas de ciclos 3-4).

**Hallazgo principal**: BUILDER siguió la tendencia de mejora ya observada en el ciclo 4 (tokens/llamadas/duración a la baja pese a un paso de alcance no trivial, 56→0 warnings en 21 ficheros); tanto `usage/BUILDER.md` como `usage/REVIEWER.md` registran `STATUS.md` leído "por rangos"/"grep/sed por secciones" en vez de una lectura íntegra persistida como salida grande (a diferencia de los ciclos 3-4, donde ambos agentes registraron un `cat`/lectura inicial completa de 31,5-44 KB). REVIEWER, en cambio, subió llamadas a herramienta un 40 % (15→21) pese a revisar un solo paso de refactor mecánico: la causa registrada es un intento fallido reproduciendo AC5.3.

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S8 | REVIEWER reprodujo AC5.3 (swagger.json idéntico antes/después) en **dos** copias scratchpad: el primer intento arrancó el servidor de pruebas con `TestFixture.TestServer` y obtuvo 404 en las cuatro rutas comprobadas; el segundo intento, con `TestShared/StartupTestServer.cs` (la clase que sí usó BUILDER en el mismo ciclo y en el ciclo 4), dio los 4/4 idénticos esperados (`usage/REVIEWER.md` ciclo 5: "primer intento con `TestFixture.TestServer` dio 404 en todos, repetido con `StartupTestServer`") | Ninguna nota de `.github/doc/common/test-stack.md` o `test-stack-documentation.md` distingue `TestFixture.TestServer` (el `TestServer` compartido entre tests, con el pipeline de test ya configurado por otros medios) de `Gaffer.Api.Tests/TestShared/StartupTestServer.cs` (arranque de servidor completo necesario para volcar rutas Swagger reales, ya usado por BUILDER en los ciclos 4 y 5 para el mismo propósito). REVIEWER repitió el mismo tipo de sondeo por ensayo-error que BUILDER ya había resuelto, sin una nota reutilizable que se lo evitara — mismo patrón de fondo que OPT-010/012/016/019 (hecho de comportamiento de la infraestructura de test descubierto por sondeo repetido, en vez de documentado una vez), aquí por primera vez del lado de REVIEWER en vez de BUILDER |

No se repiten propuestas ya registradas en `MEMORIES.md`. S8 es un hecho nuevo y distinto de OPT-010 (nulabilidad Swagger/NSwag), OPT-012 (tipo de excepción MySQL), OPT-016/OPT-019 (ciclo de vida de conexión/cancelación de `MySql.Data`): aquí el objeto sondeado es la infraestructura de arranque de servidor de pruebas del propio proyecto (`StartupTestServer` vs. `TestFixture.TestServer`), no una dependencia externa.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P5-1 | Documentar en `.github/doc/common/test-stack-documentation.md` (o `test-stack.md`) la nota: "Para arrancar un servidor de pruebas completo y volcar/comparar rutas servidas realmente (p. ej. `/swagger/*/swagger.json`), usar `Gaffer.Api.Tests/TestShared/StartupTestServer.cs`, no el `TestServer` interno de `TestFixture` (este último no expone las rutas de Swagger con las convenciones de arranque completo; da 404 en todas)." | `.github/doc/common/test-stack-documentation.md` (o `test-stack.md`) | Evita que REVIEWER (o BUILDER en un plan futuro) repita el sondeo de ensayo-error con la clase de servidor de pruebas equivocada al verificar contrato OpenAPI servido; en este ciclo costó una copia scratchpad completa y una ejecución adicional del test temporal, visible en el +40 % de llamadas a herramienta de REVIEWER (15→21) pese a revisar un único paso de refactor mecánico | `usage/REVIEWER.md` ciclo 5 ("primer intento con `TestFixture.TestServer` dio 404 en todos, repetido con `StartupTestServer`"); `STATUS.md`/`usage/BUILDER.md` ciclos 4-5 (BUILDER ya usaba `StartupTestServer` correctamente para el mismo propósito) |

### Efecto observado de propuestas previas (OPT-013/OPT-014, OPT-017/OPT-020, OPT-021)

- **OPT-013 / OPT-014** (Hierarchical Summary para `OPTIMIZATION.md`/`MEMORIES.md`): siguen sin aplicarse. El registro de OPTIMIZER entre ciclos continúa la misma tendencia de crecimiento ya documentada (30 202 → 34 858 → 50 838 en TECHDEBT-002; 37 922 → 45 006 → **50 800** en este plan, transiciones 2→3, 3→4, 4→5), con el mismo número de llamadas (5) y, según el dato aportado para este ciclo, para solo 1-2 propuestas nuevas que registrar — confirma que el coste escala con el contexto acumulado de `OPTIMIZATION.md`/`MEMORIES.md` (ambos de lectura íntegra obligatoria), no con el volumen de trabajo de registro en sí. Tercera medición consecutiva de esta plantilla en este plan sin que se haya aplicado la compactación propuesta.
- **OPT-017 / OPT-020** (consolidar decisiones pendientes antes de actuar): efecto positivo sostenido. La transición al ciclo 5 repitió el patrón del ciclo 4: ninguna re-invocación de PLANNER, ninguna pregunta pendiente bloqueando un AC al iniciar el ciclo (Q7 y Q8 ya resueltas o con valor por defecto antes de empezar, según `STATUS.md` ciclo 5), y el ciclo terminó `APPROVED` a la primera sin sorpresa de veredicto parcial — tercer ciclo consecutivo (3→4→5, descontando el propio ciclo 3 donde se originó el hallazgo) sin recurrencia de S6/H1 del ciclo 3. `Estado` se mantiene `pendiente` en `MEMORIES.md` (no se ha editado `AGENTS.md`), pese al efecto conductual positivo sostenido en los tres últimos ciclos del plan.
- **OPT-021** (Hierarchical Summary para `STATUS.md`): no aplicada formalmente (no se compactó ninguna sección de `STATUS.md`, que sigue creciendo íntegro: 196 líneas en el ciclo 4 → 258 líneas en el ciclo 5), pero el síntoma que motivó la propuesta (S7: lectura inicial completa de `STATUS.md` persistida como salida grande, dos ciclos consecutivos 3-4) **no se repitió** en el ciclo 5: `usage/BUILDER.md` registra `STATUS.md (grep/sed por secciones)` y `usage/REVIEWER.md` registra `STATUS.md por rangos`, ninguno de los dos con una lectura íntegra en la lista de "comandos con salida grande". Esto es consistente con la guía informal de lecturas acotadas de `STATUS.md` que este ciclo se pasó a BUILDER/REVIEWER en lugar de la compactación formal propuesta en `OPT-021` (aún sin aplicar en `AGENTS.md`). Efecto parcial: mitiga el síntoma por disciplina de lectura, pero no resuelve la causa de fondo (el fichero sigue sin compactarse y seguirá creciendo en cualquier plan futuro que reutilice el mismo patrón de secuenciación).

### Brechas de datos

- `usage-slice` sigue sin devolver datos medidos de entrada fresca vs. caché para BUILDER/REVIEWER del ciclo 5 (vacío total de telemetría en las cuatro consultas de esta invocación, igual que los ciclos 1-4); todo el análisis de tokens se apoya en `medido:agent-result`.
- No es posible aislar en tokens exactos el coste del segundo intento de REVIEWER en AC5.3 (S8) sin `usage-slice`; se infiere de la diferencia de llamadas a herramienta (+40 %) frente a un alcance de revisión más simple que ciclos previos.
- El total de tokens del plan completo (ver resumen abajo) no incluye: el coste de la planificación inicial (ciclo 0, sin dato reportado), el registro de OPTIMIZER entre los ciclos 1→2 (no se reportó explícitamente en `OPTIMIZATION.md` de ese momento), ni el coste de las propias invocaciones de OPTIMIZER en modo análisis (solo se ha registrado el coste de sus invocaciones en modo registro entre ciclos). El total es, por tanto, un suelo medido, no el gasto real completo del plan.

### Estado de aprobación

Decisión del operador (verbatim): "apruebo P5-1 y pasa al documentador".

- **P5-1** → aprobada, registrada como `OPT-022` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/doc/common/test-stack-documentation.md` (o `test-stack.md`).

## Resumen de uso de todo el plan (ciclos 1-5, cierre probable)

| Rol | Origen | Total medido (tokens) | Detalle por ciclo |
| --- | --- | ---: | --- |
| BUILDER | `medido:agent-result` | **498 895** | 64 411 / 126 322 / 136 576 / 93 784 / 77 802 (ciclos 1-5) |
| REVIEWER | `medido:agent-result` | **305 244** | 45 375 / 75 680 / 75 593 / 53 708 / 54 888 (ciclos 1-5) |
| PLANNER (solo re-planificación post-aprobación) | `medido:agent-result` | **211 043** | 4 invocaciones entre ciclos 1→2 (69 389 + 45 876 + 41 815 + 53 963); ninguna invocación de re-planificación en las transiciones 2→3, 3→4 ni 4→5 |
| OPTIMIZER (solo registro entre ciclos) | `medido:agent-result` | **133 728** | 37 922 (2→3) + 45 006 (3→4) + 50 800 (4→5); sin dato del registro 1→2 |
| **Total parcial medido** | — | **1 148 910** | Suelo medido; excluye planificación inicial (ciclo 0), registro OPTIMIZER 1→2 y coste propio de las invocaciones de OPTIMIZER en modo análisis (no autoinformado) |

Patrón dominante de todo el plan: el coste de BUILDER+REVIEWER por ciclo bajó de forma sostenida desde el pico del ciclo 3 (212 169) hasta el ciclo 5 (132 690), pese a alcance comparable o mayor por ciclo, coincidiendo con la aplicación conductual de OPT-017/OPT-018/OPT-020 (consolidación de decisiones, `Edit` por ancla) — evidencia consistente de que estas optimizaciones, aunque `pendiente` formalmente en `MEMORIES.md`, ya producen ahorro medible. En paralelo, el coste de coordinación entre ciclos (PLANNER de re-planificación + OPTIMIZER de registro) se concentró casi por completo en la transición 1→2 (211 043 de un total de 344 771 en coordinación), un patrón que OPT-017 explica directamente y que no se repitió en ninguna transición posterior.
