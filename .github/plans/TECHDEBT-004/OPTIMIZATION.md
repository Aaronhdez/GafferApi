# OPTIMIZATION - TECHDEBT-004

## Ciclo 1 - Paso 01 (TreatWarningsAsErrors + .gitattributes + cierre de conexión)

### Uso medido (`usage-slice`)

`usage-slice` sí devolvió datos con las ventanas literales de `STATUS.md` y `--repo gaffer-api` (a diferencia de TECHDEBT-003, que tuvo vacío total de telemetría todo el plan):

```
node .github/agent-harness/tools/usage-slice.mjs --file "%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl" --repo gaffer-api --window BUILDER=2026-09-27T17:30:59Z/2026-09-27T17:34:46Z --window REVIEWER=2026-09-27T17:36:05Z/2026-09-27T17:38:43Z
```

| Ventana | Eventos | Entrada nueva | Caché lectura | Caché escritura | Salida | Coste USD | Origen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| BUILDER | 20 | 40 | 782 319 | 39 471 | 4 300 | 2,24 | medido:token-consumer |
| REVIEWER | 8 | 16 | 211 424 | 19 767 | 133 | 0,70 | medido:token-consumer |

Totales: BUILDER 826 130 tokens; REVIEWER 231 340 tokens. En ambos, la caché (lectura+escritura) es >99,99 % del tráfico (BUILDER 99,995 %; REVIEWER 99,993 %); la entrada fresca es casi nula (40 y 16 tokens respectivamente) — todo el coste es re-transmisión de contexto ya visto, no contenido nuevo.

Comparación con las otras dos fuentes disponibles para este ciclo:

| Fuente | BUILDER (tokens) | REVIEWER (tokens) | Ratio vs. `medido:token-consumer` |
| --- | ---: | ---: | --- |
| `medido:token-consumer` (usage-slice) | 826 130 | 231 340 | referencia |
| `medido:agent-result` (ORCHESTRATOR) | 61 231 | 41 898 | BUILDER 13,49x por debajo; REVIEWER 5,52x por debajo |
| `estimado` (`usage/*.md`, autoinforme del subagente) | ~64 000 (~55k+~9k) | ~51 000 (~45k+~6k) | BUILDER 12,9x por debajo; REVIEWER 4,5x por debajo |

### Spikes detectados

| Spike | Valor / origen | Causa probable |
| --- | --- | --- |
| S1 | Brecha estimado/medido y medido:agent-result/medido:token-consumer, ambas de la misma magnitud: el autoinforme de BUILDER (~64k) y el total pasado por el ORCHESTRATOR (61 231) coinciden entre sí dentro de un 5%, pero ambos infravaloran el total realmente medido por `usage-slice` (826 130) en ~13x. Mismo patrón en REVIEWER (~51k / 41 898 vs. 231 340, ~5x) | `medido:agent-result` no es una medición independiente de la caché: su valor casi coincide con el autoinforme "a ciegas" del propio subagente (que no ve caché, per `CLAUDE.md` "Measured Usage"), lo que indica que el total que el harness pasa al ORCHESTRATOR excluye — o subestima gravemente — la lectura/escritura de caché acumulada turno a turno. Es la primera vez en este repositorio que se dispone de `medido:token-consumer` y `medido:agent-result` simultáneamente para el mismo ciclo, así que es la primera confirmación directa de esta brecha (antes solo se sospechaba por la nota del ORCHESTRATOR sobre discrepancia de `tool_uses`) |
| S2 | Discrepancia de recuento de "llamadas": 32 (`tool_uses`, `medido:agent-result`) vs. ~22 (autoinforme `usage/BUILDER.md`, `estimado`) vs. 20 (`eventos`, `usage-slice`) para la misma sesión de BUILDER | Las tres cifras cuentan unidades distintas (invocaciones de herramienta individuales vs. turnos de API que pueden agrupar varias) y no son intercambiables; no hay evidencia de un problema real de reintentos o llamadas duplicadas — el propio `STATUS.md` documenta ~11 comandos distintos más las llamadas de lectura/edición de ficheros, coherente con cualquiera de los tres rangos |
| S3 (no es una regresión, es una confirmación positiva) | Caché por evento de BUILDER este ciclo: (782 319+39 471)/20 ≈ 41 090 tokens/evento. El único punto de comparación histórico equivalente (sesión de un solo paso, ya con `OPT-006`/`OPT-007` aplicados de forma informal) es TECHDEBT-001 ciclo 2: 696 170 caché / 17 eventos ≈ 40 951 tokens/evento (`MEMORIES.md`, efecto de `OPT-007`) | La cifra de este ciclo (41 090) es prácticamente idéntica a la de TECHDEBT-001 ciclo 2 (40 951), pese a ser un plan distinto y una sesión con más aserciones (5 tests nuevos + mutación AC1.3 con 3 builds de mutación). Esto es evidencia cruzada de que la práctica informal de `OPT-006` (resumen de builds) y `OPT-007` (no reabrir salidas ya resumidas en `STATUS.md`) se sostiene por tercer plan consecutivo sin necesidad de haberse formalizado aún en `CLAUDE.md`/`builder.md` |

No se repiten propuestas ya registradas en `MEMORIES.md`. S1 es un hallazgo nuevo (brecha de fiabilidad de la fuente `medido:agent-result`, no cubierto por `OPT-008`/`OPT-009`/`OPT-011`, que tratan ausencia de datos de `usage-slice`, no su discrepancia con `agent-result` cuando ambos existen). S3 no es una propuesta nueva: es un dato de refuerzo para `OPT-006`/`OPT-007` (ambos `pendiente` desde TECHDEBT-001), registrado aquí porque no se puede editar `MEMORIES.md` en este modo de análisis.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P1-1 | Añadir una nota en `CLAUDE.md`, sección "Measured Usage (Claude)", advirtiendo que `medido:agent-result` (el total que el ORCHESTRATOR pasa desde el resultado de `Agent`) puede excluir o subestimar gravemente la caché de lectura/escritura acumulada turno a turno — en esta primera comparación directa con `medido:token-consumer` para el mismo ciclo, la brecha fue de 13,5x (BUILDER) y 5,5x (REVIEWER), y el valor de `agent-result` coincidió, en cambio, con el autoinforme "a ciegas" del propio subagente. Instruir a OPTIMIZER a etiquetar explícitamente cualquier cifra de `agent-result` como cota inferior, no como equivalente de `token-consumer`, cuando ambas fuentes estén disponibles para el mismo ciclo | `CLAUDE.md` (sección "Measured Usage (Claude)") | Evita que un futuro ciclo sin `usage-slice` disponible (fallback a `agent-result`) se interprete como "más barato" de lo que realmente es, sesgando la priorización de optimizaciones; no cuantificable en tokens (es una corrección de interpretación, no de coste) | `medido:token-consumer` (tabla de arriba) vs. `medido:agent-result` (totales pasados por el ORCHESTRATOR para este ciclo) vs. `usage/BUILDER.md`/`usage/REVIEWER.md` (autoinforme `estimado`) |

### Brechas de datos

- No hay ciclo previo de este plan con el que comparar crecimiento entre ciclos (este es el ciclo 1 de TECHDEBT-004).
- No es posible aislar, dentro de los 826 130 tokens de BUILDER, qué fracción corresponde específicamente a la mutación de prueba de AC1.3 (3 builds adicionales) frente al resto del paso 01; `usage-slice` no desglosa por comando/turno, solo por ventana temporal completa.
- La cifra de S3 (paridad de caché/evento con TECHDEBT-001 ciclo 2) es indicativa, no concluyente: son dos planes distintos con alcance de paso distinto (5 tests + mutación vs. 1 paso simple); se necesitaría una tercera medición con alcance más comparable para confirmar que la práctica informal de `OPT-006`/`OPT-007` explica la paridad y no es coincidencia.
- Sin la ventana de PLANNER (ciclo 0) medida con `usage-slice`, no se puede comparar el coste de planificación de este plan con el patrón de replanificación de `OPT-017`/`OPT-020` (TECHDEBT-003).

### Estado de aprobación

Pendiente de decisión del operador (checkpoint de fin de ciclo 1).
