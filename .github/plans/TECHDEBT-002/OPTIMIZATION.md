# OPTIMIZATION - TECHDEBT-002

## Ciclo 1 (paso 01 - Update parcial de Player, veredicto APPROVED)

### usage-slice (medido:token-consumer)

`node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --agent claude-code --window <rol>=<inicio>/<fin>`

Las ventanas literales de `STATUS.md` (`BUILDER 2026-09-26T20:29:17Z/2026-09-26T20:35:13Z`, `REVIEWER 2026-09-26T20:36:13Z/2026-09-26T20:37:28Z`) devolvieron "sin datos". Bisección manual sobre el archivo de eventos (mismo `--repo`, ventanas más anchas) localizó los eventos reales exactamente 2 horas antes de lo registrado en `STATUS.md`:

| Ventana (real, -2h) | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER 2026-09-26T18:29:17Z/18:35:13Z | 2 | 4 | 210 286 | 166 368 | 719 | 5.36 | medido:token-consumer |
| REVIEWER 2026-09-26T18:36:13Z/18:37:28Z | 1 | 2 | 189 158 | 2 012 | 222 | 0.36 | medido:token-consumer |

Contraste con `medido:agent-result` (totales del ORCHESTRATOR, no desglosados en entrada/caché/salida): BUILDER 76 757 tokens / 52 llamadas / 409 s; REVIEWER 42 817 tokens / 12 llamadas / 104 s.

Los dos conjuntos son consistentes en la relación BUILDER≫REVIEWER en coste, pero el recuento de eventos del dashboard (2 y 1) es implausiblemente bajo frente a las 52 y 12 llamadas a herramienta reportadas por el ORCHESTRATOR; ventanas vecinas (18:35:13-18:36:13 y 18:37:28-18:42:00) muestran actividad adicional del mismo orden de magnitud de caché, lo que indica que `Inicio`/`Fin` no acotan con precisión el turno real (además del desfase de huso horario). Se usan ambos orígenes como referencia cruzada; ninguno se trata como cifra exacta por evento.

### Spike 1 - Desfase de huso horario en `Inicio`/`Fin` de `STATUS.md`

- **Valor y origen**: consulta literal con las ventanas de `STATUS.md` → "sin datos" en las cuatro combinaciones probadas (`--repo gaffer-api` y `--repo TECHDEBT-002`, con y sin `--agent claude-code`). Confirmado por bisección: los eventos reales están 2h antes (18:xx en vez de 20:xx), coincidiendo exactamente con el patrón de una zona horaria local (p.ej. CEST, UTC+2) escrita con sufijo `Z` (UTC) sin conversión.
- **Causa probable**: BUILDER/REVIEWER registran `Inicio`/`Fin` con la hora de reloj local del sistema operativo, etiquetada como si ya fuera UTC, en vez de convertir explícitamente a UTC.
- **Propuesta concreta (P1-1)**: en `CLAUDE.md` sección "Measured Usage (Claude)" (o en el prompt de `.claude/agents/builder.md` / `reviewer.md`, donde se instruye registrar `Inicio`/`Fin`), añadir instrucción explícita de obtener la hora ya convertida a UTC antes de escribirla en `STATUS.md` (p.ej. `Get-Date -AsUTC -Format o` en PowerShell, o equivalente), y no el reloj local sin convertir. Añadir además una nota tipo OPT-008: si las ventanas literales de `STATUS.md` devuelven "sin datos" con `--repo` correcto, reintentar desplazando ±el huso horario local conocido antes de recurrir a `medido:agent-result`.
- **Ahorro esperado**: elimina la bisección manual que este ciclo costó ~9 llamadas a `Bash`/`usage-slice` adicionales a OPTIMIZER (~15-20% de sus propias llamadas a herramienta) para localizar los datos; sin la corrección, cada ciclo futuro repetiría el mismo coste o degradaría a `medido:agent-result` (menos granular, sin desglose caché/entrada/salida).
- **Fuente**: `usage-slice.mjs` (10 invocaciones de bisección, esta sesión), `STATUS.md` líneas 27-31.

### Spike 2 - Caché de escritura elevada en BUILDER (166 368 tokens, ~79% de su coste medido)

- **Valor y origen**: BUILDER cache-write 166 368 vs. REVIEWER cache-write 2 012 en el mismo paso (medido:token-consumer, ventana corregida arriba). Coste USD de BUILDER (5.36) es ~15x el de REVIEWER (0.36) pese a que el read-cache de ambos es similar (210k vs 189k).
- **Causa probable**: `usage/BUILDER.md` (fila ciclo 1) autorreporta la causa: "volcado de esquema OpenAPI con verbosity detailed (filtrado con grep)" y "coste extra por descubrir que el swagger es 2.0 (nulabilidad implícita)" — BUILDER tuvo que investigar en caliente, con un volcado grande del documento OpenAPI, que NJsonSchema/NSwag trata Swagger 2.0 sin `x-nullable`, infiriendo nulabilidad de "no requerido". Ese hallazgo (documentado ahora en `REVIEW.md` hallazgo #1 y en `STATUS.md` "Notas del ciclo 1") no existía antes como conocimiento reutilizable, forzando el volcado verboso para descubrirlo.
- **Propuesta concreta (P1-2)**: registrar la regla de nulabilidad Swagger 2.0/NSwag ("propiedad no listada en `required` ⇒ anulable en el esquema servido por este proyecto, sin `x-nullable`; ver `OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)`") como nota reutilizable en `.github/doc/common/dev-stack.md` (leído por BUILDER/REVIEWER al inicio de ejecución). Así, cualquier paso futuro de este plan o de otros que toque contrato OpenAPI (p.ej. paso 03, filtro de Facilities) puede citar la regla sin repetir el volcado verboso del esquema.
- **Ahorro esperado**: evita repetir un volcado de esquema con `--verbosity detailed` (fuente confirmada del cache-write de 166k) en cada paso futuro que toque nulabilidad de contrato; impacto estimado proporcional al cache-write ya medido en este ciclo (~166k tokens) si se repitiera sin la nota.
- **Fuente**: `usage/BUILDER.md` fila ciclo 1; `REVIEW.md` hallazgo #1; `STATUS.md` "Notas del ciclo 1 (paso 01)".

### Cobertura de MEMORIES.md

Ninguna entrada existente (`OPT-001`..`OPT-008`) cubre el desfase horario de `Inicio`/`Fin` ni la nota de nulabilidad Swagger 2.0; ambas propuestas (P1-1, P1-2) son nuevas. `OPT-008` (atribución de `--repo` por `cwd`) es un problema relacionado pero distinto (aquí el `--repo` fue correcto; el fallo fue de huso horario en la ventana), por lo que no se actualiza su "Efecto observado".

### Brechas de datos

- No se pudo confirmar si el desfase de 2h es exactamente el huso horario del operador (no verificable desde este entorno); se infiere por coincidencia de patrón, no se mide directamente.
- El recuento de eventos del dashboard (2 BUILDER, 1 REVIEWER) es demasiado bajo para corresponder de forma fiable a 52/12 llamadas a herramienta; no se puede descartar solapamiento con turnos vecinos sin leer el JSONL crudo (fuera de alcance de OPTIMIZER). Cifras de caché/coste se reportan como orientativas, no como desglose exacto por rol.

### Estado de aprobación

Aprobadas por el operador ambas propuestas de este ciclo ("ejecuta b aprobando ambas y luego a"):

- **P1-1** → aprobada, registrada como `OPT-009` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `CLAUDE.md` (sección "Measured Usage (Claude)") o en los prompts `.claude/agents/builder.md` / `reviewer.md`.
- **P1-2** → aprobada, registrada como `OPT-010` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/doc/common/dev-stack.md`.

Ninguna propuesta ha sido aplicada por OPTIMIZER; ambas quedan en estado `pendiente` hasta que el operador o el agente correspondiente edite los ficheros de ámbito.

## Ciclo 2 (paso 02 - Fallos de infraestructura → 500, veredicto APPROVED)

### usage-slice (medido:token-consumer) — sin datos; bisección de todo el día

`node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-26T20:55:12Z/2026-09-26T21:02:17Z --window REVIEWER=2026-09-26T21:02:46Z/2026-09-26T21:04:51Z`

Las ventanas literales de `STATUS.md` (ya en UTC real según instrucción del ORCHESTRATOR, OPT-009 aplicado) devolvieron "sin datos" en `claude-code` y `copilot`. A diferencia del ciclo 1, el desfase de −2h tampoco tiene datos (comprobado con las mismas ventanas menos 2h, y también +2h). Se bisectó el día completo para descartar cualquier huso horario:

| Ventana | Eventos (claude-code) | Caché lectura | Caché escritura | Coste USD |
| --- | ---: | ---: | ---: | ---: |
| 2026-09-26T00:00:00Z/23:59:59Z (día completo) | 530 | 53 910 198 | 2 232 758 | 142,37 |
| 2026-09-26T00:00:00Z/18:29:17Z (mañana, previa a ciclo 1) | 508 | 49 735 513 | 2 043 968 | 129,31 |
| 2026-09-26T18:29:17Z/18:37:28Z (ciclo 1 B+R, confirma fila previa) | 3 | 399 444 | 168 380 | 5,72 |
| 2026-09-26T18:37:28Z/19:05:00Z (sesión propia de OPTIMIZER ciclo 1) | 18 | 3 586 574 | 19 919 | 6,90 |
| 2026-09-26T19:05:00Z/23:59:59Z (resto del día, incluye ventanas literales, ±2h y márgenes amplios del ciclo 2) | 0 (sin datos en todas las subventanas probadas) | — | — | — |

Los 530 eventos del día quedan contabilizados (508 + 3 + 18 = 529; 1 evento residual no localizado en un borde de ventana, sin impacto en la conclusión). No existe ningún evento `claude-code` con `repo=gaffer-api` entre las 19:05Z y el final del día — ni en la ventana literal del ciclo 2, ni desplazada ±2h, ni en márgenes de ±25 minutos alrededor de cada ventana. Se usan los totales de `medido:agent-result` (ORCHESTRATOR) como única fuente medida disponible para este ciclo:

| Rol | Tokens (total) | Llamadas | Duración | Origen |
| --- | ---: | ---: | ---: | --- |
| BUILDER ciclo 2 | 100 198 | 58 | 446 s | medido:agent-result |
| REVIEWER ciclo 2 | 53 990 | 13 | 158 s | medido:agent-result |
| OPTIMIZER (registro entre ciclos) | 30 202 | 5 | 46 s | medido:agent-result |

Comparación con ciclo 1 (mismo origen, `medido:agent-result`, ver sección de ciclo 1 arriba): BUILDER 76 757→100 198 tokens (+30,5%), 52→58 llamadas (+11,5%), 409→446 s (+9%); REVIEWER 42 817→53 990 tokens (+26,1%), 12→13 llamadas (+8,3%), 104→158 s (+51,9%).

### Observación sobre OPT-009 (efecto, sin editar `MEMORIES.md`)

Este ciclo BUILDER/REVIEWER registraron `Inicio`/`Fin` ya en UTC real por instrucción directa del ORCHESTRATOR (no hubo aplicación de OPT-009 en `CLAUDE.md`/prompts, sigue `pendiente`). Aun así, `usage-slice` con la ventana literal devolvió "sin datos", y la bisección de todo el día confirma que **no es un problema de huso horario** (ni +2h ni −2h tienen datos): simplemente no hay eventos capturados por el dashboard entre las 19:05Z y el final del día 2026-09-26, un vacío de ~5 horas que cubre por completo el ciclo 2. No se puede confirmar si OPT-009, una vez aplicado en los ficheros de ámbito, habría evitado bisección en un caso *con* datos pero desplazados; este ciclo no es una prueba de ese escenario porque no hay datos en absoluto. Conclusión: `sin datos` para confirmar o refutar el efecto de OPT-009; el fallo de este ciclo es de una naturaleza distinta (ausencia total de telemetría, no desfase).

### Observación sobre OPT-010 (efecto, sin editar `MEMORIES.md`)

El paso 02 no toca contrato OpenAPI ni nulabilidad; no aplica este ciclo. `sin datos` (no evaluable).

### Spike 1 - Vacío total de telemetría del dashboard durante el ciclo 2 (~19:05Z–fin de día)

- **Valor y origen**: bisección de las 24h del día (`usage-slice`, 8 invocaciones de `Bash` en esta sesión) localiza 529/530 eventos antes de las 19:05Z; cero eventos en cualquier ventana probada entre 19:05Z y 23:59:59Z, incluida la ventana literal del ciclo 2 (20:55:12Z–21:04:51Z) y sus desplazamientos ±2h.
- **Causa probable**: a diferencia del ciclo 1 (desfase de huso horario, evento presente pero mal ubicado), aquí el propio evento no existe en el fichero JSONL del dashboard para ese intervalo — posible cierre/reinicio de la extensión Token Consumer Dashboard, cambio de ventana de VS Code, o corte de captura entre la sesión de OPTIMIZER del ciclo 1 (termina ~19:05Z) y el inicio de BUILDER ciclo 2 (~20:55Z registrado). No verificable desde este entorno (fuera de alcance leer el JSONL crudo más allá de `usage-slice`).
- **Propuesta concreta (P2-1)**: añadir a `CLAUDE.md` (sección "Measured Usage (Claude)"), a continuación de la regla de OPT-008/OPT-009, una regla de corte: si la bisección de una ventana amplia (p.ej. el día completo) no muestra **ningún** evento en un margen de ±30 min alrededor de la ventana literal (a diferencia de un clúster desplazado, que sí indicaría huso horario), detener la bisección de inmediato y usar `medido:agent-result` sin más intentos de desplazamiento. Esto distingue explícitamente "desfase de huso horario" (OPT-009, bisección con offset fijo) de "ausencia de datos" (parar y usar el fallback), evitando repetir búsquedas con distintos offsets cuando ya no hay señal.
- **Ahorro esperado**: esta sesión de OPTIMIZER usó 8 llamadas a `Bash`/`usage-slice` para confirmar la ausencia total de datos (equivalente en orden de magnitud a las ~9-10 de la bisección del ciclo 1, pero sin resultado útil al final); con la regla de corte, tras la primera comprobación de "ventana literal + ±30 min sin eventos en absoluto" se pasaría a `medido:agent-result` en 2-3 llamadas en vez de 8.
- **Fuente**: esta sesión (8 invocaciones de `usage-slice.mjs`, ventanas documentadas en la tabla anterior); `STATUS.md` líneas 32-33 (ventanas del ciclo 2).

### Spike 2 - Sondeo ad hoc de `dotnet fsi` para descubrir el tipo de excepción MySQL

- **Valor y origen**: `usage/BUILDER.md` fila ciclo 2 atribuye explícitamente el coste extra a "probe de `MySqlException` con `dotnet fsi`" (además de normalizar CRLF con `perl` y resolver `NUnit1032`). BUILDER creció +30,5% en tokens y +11,5% en llamadas frente al ciclo 1 (medido:agent-result); no hay desglose de caché disponible este ciclo (spike 1) para aislar el coste exacto del sondeo, pero es la única causa nueva y no reutilizable que BUILDER documenta para este paso.
- **Causa probable**: para escribir `PropagateTheRepositoryFailureWhile*` (AC2.2) con el tipo exacto de excepción que lanza el driver MySQL al fallar la conexión, BUILDER tuvo que ejecutar un script `dotnet fsi` ad hoc (fuera del repo, en scratchpad) abriendo una `MySqlConnection` a un puerto inalcanzable con el `MySql.Data.dll` del build, en vez de disponer de esa información como conocimiento reutilizable del proyecto.
- **Propuesta concreta (P2-2)**: registrar en `.github/doc/common/test-stack.md` (o `dev-stack.md`, según dónde viva la guía de dependencias de infraestructura) la nota: "los fallos de conexión/consulta del driver MySQL (`MySql.Data`) se propagan como `MySql.Data.MySqlClient.MySqlException` (con `MySqlException` interna); use este tipo en aserciones `ThrowExactlyAsync<T>` para fallos de repositorio, sin necesidad de sondear con `dotnet fsi`". Así, cualquier paso futuro que necesite el tipo exacto de una excepción de infraestructura (MySQL u otra dependencia ya sondeada una vez) puede citarlo directamente.
- **Ahorro esperado**: evita repetir la compilación/ejecución de un script `dotnet fsi` (coste de arranque de F# Interactive más carga de ensamblados) en cualquier paso futuro de este plan o de otros que toquen fallos de infraestructura de MySQL; mismo patrón de ahorro que OPT-010 (conocimiento de una sola vez documentado en vez de redescubierto). Magnitud exacta no medible este ciclo por el vacío de telemetría (spike 1); pendiente de confirmar con una medición futura que sí tenga desglose de caché.
- **Fuente**: `usage/BUILDER.md` fila ciclo 2; `STATUS.md` fila "Tipo de excepción MySQL" (ciclo 2, paso 02).

### Cobertura de MEMORIES.md

Ninguna entrada existente cubre el vacío total de telemetría (distinto de OPT-008 y OPT-009, que tratan de eventos presentes pero mal atribuidos/ubicados) ni el sondeo `dotnet fsi` de tipos de excepción de infraestructura; P2-1 y P2-2 son propuestas nuevas. No se ha podido actualizar con datos nuevos el "Efecto observado" de OPT-009 ni de OPT-010 más allá de las observaciones cualitativas de esta sección (ambas siguen `pendiente` en `MEMORIES.md`; su edición requiere aprobación del operador).

### Brechas de datos

- Sin desglose de entrada/caché-lectura/caché-escritura/salida para BUILDER y REVIEWER del ciclo 2 (el dashboard no capturó eventos en ese intervalo); todas las cifras de este ciclo son `medido:agent-result` (totales sin desglosar) o `estimado` (`usage/BUILDER.md`, `usage/REVIEWER.md`).
- No se puede confirmar la causa exacta del vacío de telemetría (¿extensión cerrada?, ¿VS Code reiniciado?, ¿corte de captura?) desde este entorno; es una hipótesis, no una medición.
- No se puede aislar cuánto del +30,5% de tokens de BUILDER corresponde específicamente al sondeo `dotnet fsi` frente a `perl`/`NUnit1032`/el propio crecimiento del alcance (4 handlers + `TestFixture.cs` + `DatabaseFailureShould.cs` nuevo); solo se dispone de la atribución cualitativa que el propio BUILDER registró.

### Estado de aprobación

Aprobadas por el operador ambas propuestas de este ciclo ("procede con b aprobando ambas y luego a"):

- **P2-1** → aprobada, registrada como `OPT-011` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `CLAUDE.md` (sección "Measured Usage (Claude)").
- **P2-2** → aprobada, registrada como `OPT-012` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/doc/common/test-stack.md` (o `dev-stack.md`).

Ninguna propuesta ha sido aplicada por OPTIMIZER; ambas quedan en estado `pendiente` hasta que el operador o el agente correspondiente edite los ficheros de ámbito.

## Ciclo 3 (paso 03 - Filtro no permitido en Facilities → 400, veredicto APPROVED)

### usage-slice (medido:token-consumer) — sin datos, regla de corte aplicada (OPT-011)

`node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-26T21:11:41Z/2026-09-26T21:15:10Z --window REVIEWER=2026-09-26T21:16:17Z/2026-09-26T21:17:53Z`

Una única invocación (ventanas literales de `STATUS.md`, ya en UTC según el ciclo 3 contrastó `date -u` con PowerShell): "sin datos" para `claude-code` y `copilot` en las cuatro combinaciones rol/agente. Siguiendo la regla propuesta en `OPT-011` (parar tras la primera comprobación sin eventos en absoluto, sin desplazar ±huso horario), no se hizo bisección adicional; se usan directamente los totales de `medido:agent-result` que pasó el ORCHESTRATOR:

| Rol | Tokens (total) | Llamadas | Duración | Origen |
| --- | ---: | ---: | ---: | --- |
| BUILDER ciclo 3 | 63 486 | 33 | 270 s | medido:agent-result |
| REVIEWER ciclo 3 | 43 360 | 11 | 112 s | medido:agent-result |
| OPTIMIZER (registro entre ciclos 2→3) | 34 858 | 5 | 37 s | medido:agent-result |

Comparación con ciclos anteriores (mismo origen, `medido:agent-result`):

| Rol | Ciclo 1 | Ciclo 2 | Ciclo 3 | Variación 2→3 |
| --- | ---: | ---: | ---: | --- |
| BUILDER tokens | 76 757 | 100 198 | 63 486 | −36,6 % |
| BUILDER llamadas | 52 | 58 | 33 | −43,1 % |
| BUILDER duración | 409 s | 446 s | 270 s | −39,5 % |
| REVIEWER tokens | 42 817 | 53 990 | 43 360 | −19,7 % |
| REVIEWER llamadas | 12 | 13 | 11 | −15,4 % |
| REVIEWER duración | 104 s | 158 s | 112 s | −29,1 % |

### Observación sobre OPT-011 (efecto, sin editar `MEMORIES.md`)

Este mismo análisis es la primera medición directa del efecto esperado: siguiendo la regla de corte de `OPT-011` (parar tras la primera comprobación de ventana literal sin ningún evento, en vez de desplazar ±huso horario u otras bisecciones), OPTIMIZER usó **1 sola llamada** a `usage-slice`/`Bash` este ciclo, frente a las **8** del ciclo 2 (vacío total similar) y las **~10** del ciclo 1 (caso de desfase, distinto). El patrón de "sin datos en absoluto" para las ventanas de BUILDER/REVIEWER se repite por tercer ciclo consecutivo (ciclos 2 y 3 sin ningún evento; ciclo 1 con desfase de huso horario), lo que sugiere que el vacío no es un incidente puntual sino un patrón recurrente para las ventanas de subagentes BUILDER/REVIEWER en este proyecto — hipótesis no confirmable desde este entorno (fuera de alcance leer el JSONL crudo). Efecto observado: **ahorro confirmado** de ~7 llamadas a herramienta en esta sesión de OPTIMIZER al aplicar la regla, aunque `OPT-011` sigue `pendiente` de aplicación formal en `CLAUDE.md`.

### Observación sobre OPT-009 (efecto, sin editar `MEMORIES.md`)

No evaluable de nuevo: el ciclo 3 registró `Inicio`/`Fin` ya contrastados con UTC real (`STATUS.md`, "Ciclo 3 - hora UTC"), pero el problema de este ciclo no fue desfase sino ausencia total de eventos (igual que ciclo 2), así que no hay caso positivo con datos desplazados que confirme si la instrucción de OPT-009, aplicada, habría evitado bisección. `sin datos`.

### Observación sobre OPT-010 y OPT-012 (efecto, sin editar `MEMORIES.md`)

El paso 03 no repite el patrón de OPT-010 (sin volcado de esquema OpenAPI; el cambio de contrato ya existía como patrón conocido de `PlayerSearchFilterNotPermittedException`) ni el de OPT-012 (`usage/BUILDER.md` fila ciclo 3: "OPT-012 no aplicó (sin fallos MySQL)"). Ambos `sin datos` este ciclo, no por regresión sino porque el paso no tocó esas áreas.

### Spike 1 - Descenso de BUILDER/REVIEWER explicado por alcance del paso, no por optimización

- **Valor y origen**: BUILDER −36,6 % tokens / −43,1 % llamadas y REVIEWER −19,7 % tokens / −15,4 % llamadas frente al ciclo 2 (`medido:agent-result`, tabla arriba).
- **Causa probable**: el paso 03 añade un solo tipo de excepción y una línea de repositorio (`STATUS.md`, "Notas del ciclo 3": "`FacilitySearchFilterNotPermittedException`... `SqlFacilityRepository.BuildFilters` la lanza en lugar de `ArgumentException`"), con 7 tests nuevos frente a los 32 del ciclo 2 (4 handlers + `TestFixture.cs` + `DatabaseFailureShould.cs`). El descenso es proporcional a la reducción real de alcance, no evidencia de que una optimización ya aplicada haya reducido coste a igualdad de trabajo. Se registra para evitar atribuir erróneamente esta caída a `OPT-009`/`OPT-011`/`OPT-012` (ninguno de los tres se aplicó formalmente todavía) en una lectura futura de este documento.
- **Propuesta**: ninguna; es una nota de atribución, no un spike accionable. No se abre ID nuevo.
- **Fuente**: `usage/BUILDER.md` fila ciclo 3; `STATUS.md` "Notas del ciclo 3 (paso 03)".

### Spike 2 - Coste de lectura de OPTIMIZER crece cada ciclo por el tamaño acumulado de `OPTIMIZATION.md`/`MEMORIES.md`

- **Valor y origen**: el registro de OPTIMIZER entre ciclos 2→3 (34 858 tokens / 5 llamadas / 37 s) consumió más tokens que el registro entre ciclos 1→2 (30 202 tokens / 5 llamadas / 46 s) pese a menos duración y el mismo número de llamadas a herramienta — el crecimiento de tokens con menos tiempo activo es consistente con más lectura de contexto ya acumulado en vez de más trabajo nuevo. `OPTIMIZATION.md` medía 121 líneas antes de esta edición (crecido de 0 en el ciclo 1 a 121 tras dos ciclos completos) y `MEMORIES.md` 15 líneas (12 filas `OPT-NNN` + cabecera + legado), ambos ficheros de lectura obligatoria en cada análisis (paso 1 del modo Análisis).
- **Causa probable**: cada ciclo añade una sección completa de detalle (tablas `usage-slice`, spikes, brechas de datos, estado de aprobación) a `OPTIMIZATION.md` sin comprimir las secciones de ciclos ya cerrados; `MEMORIES.md` crece de forma similar con cada `OPT-NNN` aprobado. El coste de leer ambos ficheros es fijo y creciente por ciclo, independiente del tamaño real del paso revisado ese ciclo (ver Spike 1: el ciclo 3 tuvo menos trabajo de BUILDER/REVIEWER, pero el registro de OPTIMIZER no bajó en la misma proporción).
- **Propuesta concreta (P3-1)**: aplicar el patrón "Hierarchical Summary" de la skill `context-engineering` a `OPTIMIZATION.md`: mantener el detalle completo (tablas `usage-slice`, spikes, brechas) solo para los 2 ciclos más recientes de cada plan; para ciclos más antiguos, colapsar la sección a una fila compacta en una tabla resumen al inicio del fichero (`Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación`), conservando el detalle íntegro en el historial de Git. Aplicable también a `MEMORIES.md` si el número de filas `pendiente`/`aplicada` de planes ya cerrados crece mucho, aunque este fichero es más pequeño (15 líneas) y su beneficio es menor por ahora.
- **Ahorro esperado**: evita que la lectura obligatoria de `OPTIMIZATION.md` en el paso 1 del modo Análisis crezca linealmente con el número de ciclos del plan (actualmente +60 líneas por ciclo aprox.); en un plan de hasta 6 ciclos, sin compresión, el fichero podría alcanzar ~350-400 líneas para el ciclo 6, frente a un tamaño acotado si se resumen los ciclos 1-2 al llegar al ciclo 4. Magnitud exacta de ahorro en tokens no medible este ciclo (spike de telemetría global, ver arriba); se puede confirmar comparando el tamaño de "registro OPTIMIZER" en ciclos futuros antes/después de aplicar la compresión.
- **Fuente**: `wc -l` sobre `.github/plans/TECHDEBT-002/OPTIMIZATION.md` (121 líneas) y `.github/agent-harness/MEMORIES.md` (15 líneas), esta sesión; comparación de totales `medido:agent-result` del registro de OPTIMIZER entre ciclos 1→2 y 2→3 (tabla arriba); `.github/skills/context-engineering/SKILL.md`, sección "The Hierarchical Summary".

### Cobertura de MEMORIES.md

Ninguna entrada existente cubre el crecimiento acumulado de `OPTIMIZATION.md`/`MEMORIES.md` como coste fijo de lectura de OPTIMIZER; `P3-1` es una propuesta nueva. `OPT-011` no se reformula (solo se registra su efecto observado arriba, sin editar `MEMORIES.md`).

### Brechas de datos

- Sin desglose de entrada/caché-lectura/caché-escritura/salida para BUILDER, REVIEWER ni OPTIMIZER en este ciclo (tercer ciclo consecutivo con vacío total o desfase en el dashboard); todas las cifras son `medido:agent-result` (totales sin desglosar).
- No se puede confirmar si el vacío total de telemetría es un patrón permanente para ventanas de subagentes BUILDER/REVIEWER en este proyecto o una coincidencia de tres ciclos; se necesitaría un ciclo con datos íntegros del dashboard para descartarlo.
- El ahorro exacto en tokens de la propuesta `P3-1` no es medible sin una tercera comparación de "registro OPTIMIZER" con `OPTIMIZATION.md` ya comprimido frente a sin comprimir, a igualdad de otros factores.

### Estado de aprobación

Aprobada por el operador ("procede con b y luego con a"; `b` = opción de checkpoint "aprobar o rechazar `P3-1`", interpretada como aprobación por consistencia con el mismo patrón usado en los checkpoints de los ciclos 1 y 2 de este plan, donde `b` significó aprobar las propuestas pendientes):

- **P3-1** → aprobada, registrada como `OPT-013` en `MEMORIES.md`. Pendiente de aplicación (fuera de alcance de OPTIMIZER) en `.github/plans/TECHDEBT-002/OPTIMIZATION.md` (patrón "Hierarchical Summary" de la skill `context-engineering`).

Ninguna propuesta ha sido aplicada por OPTIMIZER; queda en estado `pendiente` hasta que el operador o el agente correspondiente aplique la compresión.

## Ciclo 4 (paso 04 - Log en el middleware, veredicto APPROVED; último paso planificado, plan completo aprobado)

### usage-slice (medido:token-consumer) — sin datos, regla de corte aplicada (OPT-011)

`node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-26T21:28:57Z/2026-09-26T21:33:40Z --window REVIEWER=2026-09-26T21:34:49Z/2026-09-26T21:36:52Z`

Una única invocación: "sin datos" para `claude-code` y `copilot` en las cuatro combinaciones rol/agente. Siguiendo `OPT-011`, no se hizo bisección adicional (cuarto ciclo consecutivo con vacío total de telemetría en las ventanas de subagentes, tras los ciclos 2 y 3; el ciclo 1 fue un caso distinto de desfase horario). Se usan los totales `medido:agent-result` que pasó el ORCHESTRATOR:

| Rol | Tokens (total) | Llamadas | Duración | Origen |
| --- | ---: | ---: | ---: | --- |
| BUILDER ciclo 4 | 64 836 | 41 | 345 s | medido:agent-result |
| REVIEWER ciclo 4 | 53 606 | 13 (el ORCHESTRATOR reportó 14 en un recuento paralelo; discrepancia no resuelta, ver "Brechas de datos") | 151 s | medido:agent-result |
| OPTIMIZER (registro entre ciclos 3→4) | 50 838 | 6 | 75 s | medido:agent-result |

Comparación con ciclos anteriores (mismo origen, `medido:agent-result`):

| Rol | Ciclo 1 | Ciclo 2 | Ciclo 3 | Ciclo 4 | Variación 3→4 |
| --- | ---: | ---: | ---: | ---: | --- |
| BUILDER tokens | 76 757 | 100 198 | 63 486 | 64 836 | +2,1 % |
| BUILDER llamadas | 52 | 58 | 33 | 41 | +24,2 % |
| BUILDER duración | 409 s | 446 s | 270 s | 345 s | +27,8 % |
| REVIEWER tokens | 42 817 | 53 990 | 43 360 | 53 606 | +23,6 % |
| REVIEWER llamadas | 12 | 13 | 11 | 13(-14) | +18,2 %/+27,3 % |
| REVIEWER duración | 104 s | 158 s | 112 s | 151 s | +34,8 % |
| OPTIMIZER registro (tokens) | 30 202 (1→2) | 34 858 (2→3) | 50 838 (3→4) | — | +45,8 % (2→3 fue +15,4 %) |

### Observación sobre OPT-011 (efecto, sin editar `MEMORIES.md`)

Cuarta confirmación consecutiva: **1 sola llamada** a `usage-slice`/`Bash` este ciclo (frente a las ~10 del ciclo 1 sin la regla). El vacío total de telemetría para ventanas de subagentes ya se repite en 3 de 4 ciclos (2, 3 y 4); solo el ciclo 1 tuvo datos (desplazados por huso horario). Efecto observado: **ahorro confirmado y sostenido** de la regla de corte; sigue `pendiente` de aplicación formal en `CLAUDE.md`.

### Observación sobre OPT-009, OPT-010 y OPT-012 (efecto, sin editar `MEMORIES.md`)

- OPT-009: `STATUS.md` registra de nuevo el contraste `date -u`/PowerShell en UTC (fila "Ciclo 4 - hora UTC"), pero el vacío total de telemetría (no un desfase) impide de nuevo confirmar si la instrucción formal evitaría bisección. `sin datos`.
- OPT-010: el paso 04 no toca contrato OpenAPI ni nulabilidad. `sin datos` (no aplica).
- OPT-012: el paso 04 no tiene fallos de MySQL que sondear. `sin datos` (no aplica).

### Spike 1 - Crecimiento acelerado del registro de OPTIMIZER entre ciclos (evidencia adicional para OPT-013)

- **Valor y origen**: el registro de OPTIMIZER entre ciclos 3→4 (50 838 tokens / 6 llamadas / 75 s, `medido:agent-result`) crece +45,8 % sobre el registro 2→3 (34 858), que a su vez creció +15,4 % sobre el registro 1→2 (30 202). El crecimiento se acelera (no es lineal): +15,4 % → +45,8 %. Al mismo tiempo, el número de llamadas a herramienta apenas crece (5→5→6), lo que descarta que el coste extra venga de más trabajo de registro y apunta de nuevo a más contexto leído por llamada.
- **Causa probable**: la misma de `OPT-013` (`Spike 2`, ciclo 3), agravada: `OPTIMIZATION.md` pasó de 121 líneas (tras el ciclo 3) a 191 líneas (tras añadir la sección del ciclo 3 completa), y `MEMORIES.md` de 15 a 17 filas; ambos de lectura obligatoria íntegra en el paso 1 del modo Análisis, sin compresión de las secciones de ciclos ya cerrados.
- **Propuesta concreta (P4-1)**: dado que este es el último ciclo de desarrollo planificado del plan (STATUS: paso 04 aprobado, plan completo), `OPTIMIZATION.md` de TECHDEBT-002 no tendrá más ciclos futuros que se beneficien de comprimirlo a sí mismo; el coste que sí sigue creciendo entre plan y plan es `MEMORIES.md` (archivo compartido, leído íntegro por OPTIMIZER al inicio de cada análisis en cualquier plan futuro). Se propone extender explícitamente `OPT-013` a `MEMORIES.md` ahora: una vez que una fila tenga `Estado = aplicada` (o `revertida`) y su `Efecto observado` ya no vaya a actualizarse (p. ej. `OPT-001`-`OPT-004`, cerradas desde 2026-08-13/09-21), colapsarla a una línea de una sola tabla-resumen al principio del fichero (`ID | Plan | Problema (1 línea) | Estado`), moviendo el detalle completo a un `MEMORIES-archive.md` o al historial de Git. Las filas `pendiente` (actualmente 9 de 13: `OPT-005` a `OPT-013`) permanecen con el detalle completo hasta que se resuelvan.
- **Ahorro esperado**: no medible para este plan (no habrá ciclo 5); aplicable de inmediato al primer análisis de cualquier plan futuro, cuando `MEMORIES.md` ya acumula 17 filas y seguirá creciendo con cada plan cerrado. Sirve como refuerzo cuantitativo de `OPT-013`, no como ID nuevo (mismo ámbito, ampliado a `MEMORIES.md` con un criterio de corte explícito: filas cerradas y sin more `Efecto observado` pendiente).
- **Fuente**: tabla de comparación arriba (`medido:agent-result`, registros 1→2, 2→3, 3→4); `wc -l` de `MEMORIES.md` (17 filas + cabecera) y de `OPTIMIZATION.md` (191 líneas antes de esta sección), esta sesión.

### Resumen de todo el plan (medido:agent-result, cierre de desarrollo)

| Agente | Detalle | Tokens | Llamadas | Duración |
| --- | --- | ---: | ---: | ---: |
| PLANNER | Ronda 1 (pre-ciclo) | 58 081 | 33 | n/a |
| PLANNER | Ronda 2 (pre-ciclo, revisión) | 46 888 | 20 | n/a |
| BUILDER | Ciclo 1 | 76 757 | 52 | 409 s |
| BUILDER | Ciclo 2 | 100 198 | 58 | 446 s |
| BUILDER | Ciclo 3 | 63 486 | 33 | 270 s |
| BUILDER | Ciclo 4 | 64 836 | 41 | 345 s |
| REVIEWER | Ciclo 1 | 42 817 | 12 | 104 s |
| REVIEWER | Ciclo 2 | 53 990 | 13 | 158 s |
| REVIEWER | Ciclo 3 | 43 360 | 11 | 112 s |
| REVIEWER | Ciclo 4 | 53 606 | 13(-14) | 151 s |
| OPTIMIZER | Análisis ciclo 1 | 51 203 | n/a | n/a |
| OPTIMIZER | Análisis ciclo 2 | 60 931 | n/a | n/a |
| OPTIMIZER | Análisis ciclo 3 | 73 129 | n/a | n/a |
| OPTIMIZER | Análisis ciclo 4 | no medible (el subagente no ve su propio consumo; se conocerá solo si el ORCHESTRATOR lo reporta después) | — | — |
| OPTIMIZER | Registro 1→2 | 30 202 | 5 | 46 s |
| OPTIMIZER | Registro 2→3 | 34 858 | 5 | 37 s |
| OPTIMIZER | Registro 3→4 | 50 838 | 6 | 75 s |

Totales por rol (excluyendo el análisis del ciclo 4, no medible desde dentro de esta sesión):

| Rol | Total tokens | % del total medido |
| --- | ---: | ---: |
| PLANNER | 104 969 | 11,6 % |
| BUILDER | 305 277 | 33,7 % |
| REVIEWER | 193 773 | 21,4 % |
| OPTIMIZER (análisis c1-c3 + registros) | 301 161 | 33,3 % |
| **Total** | **905 180** | 100 % |

**Hallazgo de cierre**: el coste medido de OPTIMIZER a lo largo del plan (301 161 tokens, análisis + registros, sin contar este mismo análisis de cierre) es prácticamente equivalente al de BUILDER (305 277 tokens), pese a que OPTIMIZER no escribe código de producto ni tests — solo lee `usage/*.md`, `STATUS.md`, `REVIEW.md`, `MEMORIES.md` y el propio `OPTIMIZATION.md` creciente, y redacta propuestas. Esto confirma a escala de todo el plan la hipótesis de `OPT-013`/`P4-1`: el coste de OPTIMIZER está dominado por la lectura de contexto acumulado (que crece con cada ciclo) y no por el trabajo de análisis en sí (los ciclos con menos cambio de producto, como el ciclo 3, no redujeron proporcionalmente el coste de OPTIMIZER). Es la evidencia más fuerte reunida en este plan para priorizar `OPT-013`/`P4-1` en el próximo checkpoint.

### Cobertura de MEMORIES.md

`P4-1` amplía el ámbito de `OPT-013` (ya registrada) con un criterio de corte concreto para `MEMORIES.md`; no se abre ID nuevo por instrucción de la tarea (evidencia de refuerzo, mismo ámbito). Ninguna otra entrada existente cubre el hallazgo de cierre (coste de OPTIMIZER ≈ coste de BUILDER a escala de plan).

### Brechas de datos

- Discrepancia sin resolver en las llamadas a herramienta de REVIEWER ciclo 4 (13 según el dato pasado a OPTIMIZER, 14 según "el harness reportó" en la misma entrada): no se puede determinar cuál es la cifra correcta desde esta sesión; se reportan ambas.
- Cuarto ciclo sin desglose de entrada/caché-lectura/caché-escritura/salida (vacío total de telemetría del dashboard); todas las cifras de BUILDER/REVIEWER/OPTIMIZER de este ciclo son `medido:agent-result` (totales sin desglosar), no `medido:token-consumer`.
- El coste de la propia sesión de análisis de OPTIMIZER en el ciclo 4 (este documento) no es medible desde dentro de la sesión; el resumen de todo el plan lo excluye explícitamente y por tanto subestima el total real en, al menos, el orden de magnitud de los análisis de ciclos anteriores (~51 000-73 000 tokens).
- No se puede aislar cuánto del +24,2 % de llamadas de BUILDER en el ciclo 4 corresponde al doble rebote de compilación (NUnit1032 por `_logs` sin liberar, luego CS1729 por el constructor de 2 argumentos) frente al alcance propio del paso (logger de test nuevo, cambio de `StartApiServer`); solo se dispone de la secuencia de comandos registrada en `STATUS.md`.

### Estado de aprobación

Pendiente de checkpoint del operador (cierre del plan; sin decisión registrada todavía en esta sesión):

- **P4-1** → propuesta, no registrada aún en `MEMORIES.md`. Amplía el ámbito de `OPT-013` (ya `pendiente`) a un criterio de corte explícito para `MEMORIES.md`. Pendiente de aprobación explícita del operador antes de registrar cualquier cambio de ámbito.

Ninguna propuesta ha sido aplicada por OPTIMIZER.
