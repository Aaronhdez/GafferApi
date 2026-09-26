# OPTIMIZATION - TECHDEBT-001

## Ciclo 1

### usage-slice (medido:token-consumer)

Ventanas tomadas de `STATUS.md` (`Inicio`/`Fin` por agente):

| Ventana | Agente | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER 2026-09-26T11:25:47Z/2026-09-26T11:47:10Z | claude-code | 119 | 238 | 16 635 645 | 219 538 | 29 193 | 31.26 | medido:token-consumer |
| REVIEWER 2026-09-26T11:49:00Z/2026-09-26T11:52:25Z | claude-code | 23 | 46 | 1 748 153 | 85 165 | 173 | 4.23 | medido:token-consumer |

Total medido: BUILDER ≈ 16.88 M tokens (119 eventos, ~23 min, coste $31.26); REVIEWER ≈ 1.83 M tokens (23 eventos, ~3.5 min, coste $4.23). BUILDER consume ~9.2x los tokens y ~7.4x el coste de REVIEWER.

**Gap estimado vs medido / vs agent-result** (sin datos suficientes para reconciliar, se deja registrado):
- `usage/BUILDER.md` (estimado, propio del subagente) declara ~230 000 tokens de entrada + ~40 000 de salida ≈ 270 000.
- ORCHESTRATOR (`medido:agent-result`) reportó `subagent_tokens` 245 584 para BUILDER.
- `usage-slice` (`medido:token-consumer`, incluye toda la caché) mide ≈ 16.88 M para la misma ventana.
- Los tres órdenes de magnitud no son conciliables con los datos disponibles: es probable que `agent-result` y la autoestimación del subagente reflejen solo el tamaño de contexto de un turno (o los tokens "nuevos" reportados por la API), mientras `usage-slice` sí acumula la lectura de caché en cada uno de los 119 turnos de una sesión larga. Mismo patrón en REVIEWER (108 032 vs 1.83 M). No se propone corrección de proceso todavía: haría falta una segunda muestra (ciclo 2) para confirmar si el ratio es estable antes de tratarlo como una causa de desperdicio real y no solo un artefacto de medición.

### Spikes detectados

| # | Valor | Origen | Agente |
| --- | --- | --- | --- |
| S1 | Caché lectura 16 635 645 tokens = ~98,5 % del total medido de BUILDER (16.88 M), frente a solo 238 tokens de entrada nueva y 29 193 de salida | `usage-slice`, ventana BUILDER | BUILDER |
| S2 | 119 eventos / ~204 llamadas a herramienta (dato ORCHESTRATOR) en una sola sesión continua de ~23 min para 10 pasos del plan (01-09, 11) | `usage-slice` + `subagent_tokens`/`tool_uses` de ORCHESTRATOR | BUILDER |
| S3 | `dotnet build --no-incremental -v q` completo repetido (BUILDER estima 6 veces; `STATUS.md` documenta 4 explícitas de BUILDER + 1 de REVIEWER) solo para contar advertencias | `usage/BUILDER.md` (estimado) + `STATUS.md` líneas 25, 37, 40, 70, 74 | BUILDER, REVIEWER |

### Causa probable

- **S1/S2**: el diseño del plan agrupa 10 pasos independientes en un único ciclo/sesión de BUILDER (decisión correcta de PLANNER para minimizar riesgo de integración, ver `00-overview-and-open-questions.md` "Orden elegido"). En Claude Code cada turno reenvía el historial acumulado; con 119 turnos en una sola sesión, cualquier contenido grande incorporado pronto (ficheros leídos, salidas de comandos, diffs) se retransmite como caché en todos los turnos posteriores. Esto es exactamente el antipatrón "Context flooding" / "Stale conversation" de `context-engineering` (Nivel 4 y 5): salidas completas de baja densidad informativa (listados de advertencias, `grep -A12`) permanecen en el contexto mucho después de que su conclusión (el número, el veredicto) ya quedó registrada en `STATUS.md`.
- **S3**: el recuento de advertencias es un criterio de aceptación real del paso 03 (regresión de advertencias antes/después), no un error de BUILDER; pero la mayoría de esas ejecuciones solo necesitan el **número** total, no el listado completo. `dotnet build -v q` sigue imprimiendo cada línea de advertencia; solo el resumen final (`X Warning(s)`) aporta la información que se usa en `STATUS.md` salvo en la comprobación final AC11.1, donde sí hace falta comparar tipos de advertencia.

### Propuestas

**P1-1** — Para las comprobaciones de recuento de advertencias que no necesitan comparar tipos (todas menos la línea base inicial y el AC11.1 final), capturar solo el resumen final de MSBuild en vez del listado completo:
- Concreto: `dotnet build gaffer-api.sln --no-incremental -v q 2>&1 | Select-String -Pattern "Warning\(s\)|Error\(s\)"` (o `| grep -E "Warning\(s\)|Error\(s\)"` en bash), reservando la salida completa solo cuando el criterio exige comparar qué advertencias son nuevas (línea base y AC11.1).
- Ámbito: skill `debugging-and-error-recovery` de BUILDER, o sección "Minimal Reading" de `CLAUDE.md` (regla adicional: "para regresiones de recuento, capturar el resumen, no el listado").
- Ahorro esperado: evita que ~3-4 listados completos de 70-85 advertencias (varias decenas de líneas cada uno) queden fijados en el contexto y se retransmitan como caché en el resto de la sesión (~100 turnos restantes tras la primera aparición). Es una fracción del S1, no lo explica por completo.
- Fuente: `STATUS.md` líneas 25, 37, 40, 70; `usage/BUILDER.md` fila del ciclo 1.

**P1-2** — Reducir la retención de salidas de herramientas de baja densidad ya concluidas durante sesiones largas de un solo agente (BUILDER en planes multi-paso como este, con 10 pasos en un ciclo):
- Concreto: aplicar la guía de `context-engineering` ("Nivel 4: Error Output" — capturar el error específico, no 500 líneas de salida; "Nivel 5: gestionar la conversación" — resumir progreso en vez de arrastrar la salida completa). Traducido a instrucción operativa para BUILDER: una vez que el resultado de un comando (build, test, grep) se ha resumido y registrado en `STATUS.md`, no reabrir ni volver a citar la salida completa en turnos posteriores; referenciar la fila de `STATUS.md` en su lugar.
- Ámbito: `CLAUDE.md` (sección "Minimal Reading") o el prompt de `.claude/agents/builder.md`, añadiendo esta regla explícita para pasos largos multi-incremento.
- Ahorro esperado: no cuantificable con los datos de este ciclo (no hay forma de aislar qué fracción de los 16.6 M tokens de caché corresponde a salidas ya concluidas vs. contexto necesario). Se necesita una segunda medición (ciclo 2, si lo hay, o un plan comparable de un solo paso) para confirmar el efecto antes de considerarla más que una hipótesis razonable.
- Fuente: `usage-slice` (S1/S2), `.github/skills/context-engineering/SKILL.md` (Niveles 4-5, tabla de antipatrones "Context flooding").

### Sin datos suficientes

- No se puede atribuir con precisión qué proporción de los 16.6 M tokens de caché de BUILDER corresponde a build/test output vs. lecturas de ficheros de producto vs. diffs; `usage-slice` no desglosa por tipo de contenido y las notas de `usage/BUILDER.md` son estimadas, no medidas.
- No hay ciclo anterior de este plan con el que comparar crecimiento entre ciclos (este es el ciclo 1).
- El bloqueo B1 (paso 10, Q14/Q15) es un bloqueo de alcance/decisión del operador, no un patrón de desperdicio de tokens; no genera propuesta de optimización.

### Revisión de memorias existentes

- No se repite ninguna propuesta ya registrada en `MEMORIES.md`. OPT-005 (BUGFIX-001, grep de literales de estado de test) no aplica aquí: `STATUS.md` de este ciclo usa siempre recuentos explícitos (`159/159`, `127/127`), no grep de "Passed"/"Failed"; no hay evidencia en este plan para actualizar su `Efecto observado`.

### Aprobación

- **P1-1**: aprobada por el operador ("Aplica ambas optimizaciones P1-1 y P1-2"). Registrada como `OPT-006` en `MEMORIES.md` (estado `pendiente`, efecto `sin datos`).
- **P1-2**: aprobada por el operador ("Aplica ambas optimizaciones P1-1 y P1-2"). Registrada como `OPT-007` en `MEMORIES.md` (estado `pendiente`, efecto `sin datos`).

## Ciclo 2

### usage-slice (medido:token-consumer)

Ventanas tomadas de `STATUS.md` (`Inicio`/`Fin` por agente). **Nota de atribución**: `--repo gaffer-api` (el valor por defecto de `CLAUDE.md`) devolvió "sin datos" para las dos ventanas del ciclo 2 pese a que el fichero JSONL sí contenía eventos en ese rango horario (confirmado leyendo el fichero directamente por fecha, sin filtrar por repo, y localizando el bloque de eventos entre 12:05 y 12:17 UTC). La causa: esos eventos llevan `meta.repository = "TECHDEBT-001"` en vez de `"gaffer-api"`, porque el subagente de ese ciclo se invocó con el directorio de trabajo dentro de `.github/plans/TECHDEBT-001/` (el mismo patrón que el `cwd` de este propio OPTIMIZER, ver "Primary working directory" del entorno de esta invocación) y la extensión de dashboard deriva el nombre del repositorio del `cwd`, no de la URL de Git. Repitiendo la consulta con `--repo TECHDEBT-001` sí se obtuvo `medido:token-consumer`. Los eventos del ciclo 1 (mismas fechas, `--repo gaffer-api`) sí se resolvieron con el valor por defecto, confirmando que el `cwd` cambió entre ciclos, no el propio dashboard.

| Ventana | Agente | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER 2026-09-26T12:09:34Z/2026-09-26T12:12:08Z (`--repo TECHDEBT-001`) | claude-code | 17 | 34 | 696 170 | 40 497 | 2 663 | 2.00 | medido:token-consumer |
| REVIEWER 2026-09-26T12:13:32Z/2026-09-26T12:15:55Z (`--repo TECHDEBT-001`) | claude-code | 14 | 28 | 612 725 | 45 315 | 304 | 1.79 | medido:token-consumer |

Comparación con ciclo 1 (mismo origen, `--repo gaffer-api`, ver sección anterior):

| Métrica | BUILDER ciclo 1 | BUILDER ciclo 2 | REVIEWER ciclo 1 | REVIEWER ciclo 2 |
| --- | ---: | ---: | ---: | ---: |
| Eventos | 119 | 17 | 23 | 14 |
| Caché lectura | 16 635 645 | 696 170 | 1 748 153 | 612 725 |
| Caché lectura / evento | ≈139 800 | ≈40 950 | ≈75 000 | ≈43 770 |
| Coste USD | 31.26 | 2.00 | 4.23 | 1.79 |
| `subagent_tokens` (ORCHESTRATOR, `medido:agent-result`) | 245 584 | 63 920 | 108 032 | 62 893 |
| Ratio `usage-slice` total / `agent-result` | ≈68,8x | ≈11,6x* | ≈17,0x | ≈10,5x* |

\* `usage-slice` total = entrada + caché lectura + caché escritura + salida (739 364 BUILDER; 658 372 REVIEWER).

### Spikes detectados

| # | Valor | Origen | Agente |
| --- | --- | --- | --- |
| S4 | `usage-slice --repo gaffer-api` (comando por defecto de `CLAUDE.md`) devuelve "sin datos" para las dos ventanas del ciclo 2, pese a existir eventos medibles; solo aparecen bajo `--repo TECHDEBT-001` | Lectura directa del JSONL (sin filtrar por repo) + comparación de resultados con ambos valores de `--repo` | Infraestructura de medición (dashboard + invocación del subagente), no un agente del plan |
| S5 | El ratio `usage-slice`/`agent-result` no es estable entre ciclos (≈68,8x→≈11,6x BUILDER; ≈17,0x→≈10,5x REVIEWER), a diferencia de lo asumido como "brecha fija" en el ciclo 1 | Tabla comparativa arriba | BUILDER, REVIEWER |

### Causa probable

- **S4**: la extensión de dashboard etiqueta `meta.repository` con el nombre de la carpeta de trabajo del proceso (`cwd`), no con el remoto de Git. En el ciclo 1 el `cwd` de BUILDER/REVIEWER era la raíz del repositorio (`gaffer-api`); en el ciclo 2 pasó a ser `.github/plans/TECHDEBT-001/` (mismo patrón que el `cwd` de esta propia invocación de OPTIMIZER, visible en su bloque de entorno). Si esto se repite en ciclos futuros, la instrucción por defecto de `CLAUDE.md` ("usage-slice.mjs ... --repo gaffer-api") fallará silenciosamente (mensaje "sin datos", no un error) y OPTIMIZER podría caer a `medido:agent-result` (más grueso, sin desglose de caché) sin darse cuenta de que existía un dato mejor bajo otro nombre de repo.
- **S5**: el ratio `usage-slice`/`agent-result` depende del número de turnos de la sesión (a más turnos, más veces se retransmite el mismo contexto como caché), no es una constante del agente. El ciclo 1 (sesión de ~119 turnos para 10 pasos) y el ciclo 2 (sesión de ~17 turnos para 1 paso) confirman que la brecha se reduce cuando la sesión es corta, lo que resuelve parcialmente la pregunta abierta del ciclo 1 ("¿es estable el ratio?"): no lo es, escala con la longitud de sesión, no con el tamaño del ciclo en pasos.

### Propuestas

**P2-1** — Cuando `usage-slice` con `--repo <nombre-del-repo>` no encuentre eventos en una ventana, reintentar una vez con `--repo <ID-del-plan>` antes de recurrir a `medido:agent-result`, ya que el nombre de repositorio registrado por el dashboard puede derivarse del directorio de trabajo del subagente (que a veces es la carpeta del plan) y no del repositorio Git:
- Concreto: añadir esta comprobación como paso intermedio en la sección "Measured Usage (Claude)" de `CLAUDE.md`, entre `medido:token-consumer` y `medido:agent-result`: "si `--repo <repo>` devuelve 'sin datos' para todas las ventanas, repetir con `--repo <ID-de-plan>` antes de usar `medido:agent-result`".
- Ámbito: `CLAUDE.md` (sección "Measured Usage (Claude)"); alternativamente, la causa raíz (que el `cwd` del subagente cambie entre ciclos) podría corregirse en la invocación del ORCHESTRATOR, pero esa invocación no es editable por OPTIMIZER ni se ha observado su código en este ciclo.
- Ahorro esperado: no es un ahorro de tokens; es una mejora de cobertura de medición. Sin este reintento, OPTIMIZER pierde el desglose caché/entrada-nueva/salida (la única fuente que permite distinguir contexto reciclado de trabajo nuevo) y solo dispondría del total agregado de `agent-result`, degradando la precisión de futuros análisis de picos.
- Fuente: comandos `usage-slice` de esta sesión (`--repo gaffer-api` → sin datos; `--repo TECHDEBT-001` → medido, ver tabla arriba); lectura directa del JSONL confirmando `meta.repository = "TECHDEBT-001"` en los eventos de la ventana del ciclo 2.

### Efecto observado de OPT-006 y OPT-007 (con los datos ya medidos de este ciclo)

- Ambas se aplicaron según las notas de `usage/BUILDER.md` ciclo 2 ("solo resumen por código al contexto, OPT-006"; "OPT-007 aplicado: resultados referenciados vía STATUS").
- Caché/evento de BUILDER bajó de ≈139 800 (ciclo 1) a ≈40 950 (ciclo 2); de REVIEWER de ≈75 000 a ≈43 770. Dirección consistente con lo esperado por ambas optimizaciones.
- **No atribuible a una sola causa**: el ciclo 2 cubrió únicamente el paso 10 (1 de 11), frente a los pasos 01-09 y 11 del ciclo 1 en una sola sesión continua; parte de la caída de caché/evento es simplemente que una sesión de 17 turnos acumula menos contexto retransmitido que una de 119 (ver S5). No se puede separar el efecto de OPT-006, el de OPT-007 y el de la sesión más corta con los datos de dos ciclos con alcances tan distintos.
- Efecto registrado en `MEMORIES.md` (`OPT-006`, `OPT-007`) con esta misma salvedad; se recomienda una tercera medición sobre un ciclo de alcance comparable (varios pasos en una sesión larga) antes de considerar el efecto confirmado.

### Sin datos suficientes

- No hay forma de aislar, dentro de la caída de caché/evento del ciclo 2, qué parte corresponde a OPT-006, cuál a OPT-007 y cuál al menor número de turnos de la sesión.
- No se puede confirmar si el cambio de `cwd` (causa de S4) es un patrón estable en los ciclos del harness o un caso aislado de esta invocación; haría falta observarlo en otro plan o ciclo para saberlo.

### Revisión de memorias existentes

- No se registra ninguna propuesta nueva que ya exista en `MEMORIES.md`. `Efecto observado` de `OPT-006` y `OPT-007` actualizado arriba y en `MEMORIES.md` con los datos de este ciclo (parcial, ver salvedades).
- `OPT-005` (BUGFIX-001) sigue sin aplicar a este plan: `STATUS.md` del ciclo 2 usa recuentos explícitos (`159/159`, exit codes), no grep de literales de estado.

### Aprobación

- **P2-1**: aprobada por el operador ("procede con los pasos 1 y 2", interpretado por ORCHESTRATOR como aprobación de P2-1). Registrada como `OPT-008` en `MEMORIES.md` (estado `pendiente`, efecto `sin datos`). Nota: el cambio de `cwd` de sesión que causó S4/motivó P2-1 (subagentes de ciclo 2 invocados dentro de `.github/plans/TECHDEBT-001/` en vez de la raíz del repositorio) fue del propio ORCHESTRATOR, no de BUILDER/REVIEWER; ya se ha revertido a la raíz del repositorio para invocaciones posteriores a este ciclo (confirmado por el `cwd` de esta invocación de OPTIMIZER). `OPT-008` queda registrada como salvaguarda de medición para si el patrón reaparece.
