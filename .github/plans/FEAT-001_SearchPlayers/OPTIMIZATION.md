# OPTIMIZATION - FEAT-001_SearchPlayers

## Resumen de ciclos

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 | APPROVED | P1-1, P1-2 | ~15-25 k tokens y ~10 llamadas (P1-1); ~40-60 k (P1-2) | rechazadas (operador: errores humanos) |
| 2 | 02 | APPROVED | P2-1 | ~1-3 llamadas y ~2-4 k tokens por ciclo de REVIEWER | pendiente de decisión |
| 3 | 03 | APPROVED | P3-1 | ~5-15 k tokens y ~3-6 llamadas por ciclo de REVIEWER cuando haya ciclos previos sin commit | pendiente de decisión |

## Ciclo 2 (paso 02, APPROVED)

### Uso medido

`usage-slice`: no ejecutado. OPT-031 aplica: el ciclo 1 de este plan ya devolvió `sin datos` en las 3 ventanas, y este es el 2º ciclo consecutivo sin datos. Se cita `usage/*.md` como `estimado` y los totales del ORCHESTRATOR como `medido:agent-result` (cota inferior, OPT-023). Brecha de datos declarada: no hay separación de entrada nueva frente a caché.

| Rol | subagent_tokens | Usos | Duración | Origen | `usage/*.md` (estimado) |
| --- | ---: | ---: | ---: | --- | --- |
| BUILDER | 90 572 | 34 | 419,1 s | medido:agent-result (cota inferior) | ~110k ent. / ~16k sal. / ~25 usos |
| REVIEWER | 63 787 | 19 | 195,5 s | medido:agent-result (cota inferior) | ~70k / ~7k / ~17 usos |
| OPTIMIZER (registro, rechazos ciclo 1) | 19 367 | 6 | n/a | medido:agent-result | n/a |

Total de ciclo (BUILDER+REVIEWER): 154 359 tokens, 53 usos. Frente al ciclo 1 (BUILDER 142 971 en dos arranques, REVIEWER 58 867): el BUILDER de un solo arranque cuesta 63 % del total del ciclo 1; REVIEWER +8 %.

### Picos

| Pico | Valor | Origen |
| --- | --- | --- |
| Sesgo de usos de herramientas | BUILDER 34 medidos frente a ~25 estimados (+36 %); REVIEWER 19 frente a ~17 (+12 %) | `usage/*.md` frente a `agent-result`; confirma el sesgo del ciclo 1 (+25 %) y lo agranda |
| Entrada estimada BUILDER | ~110k estimado frente a 90,6 k medido (total, cota inferior) | La estimación de tokens sobrestima mientras la de usos subestima; ninguna es fiable sin `usage-slice` |
| `git worktree remove` fallido | 2º ciclo consecutivo (REVIEWER) | `usage/REVIEWER.md` ciclos 1 y 2: ruta larga en scratchpad, resuelto con `rm -rf` + `git worktree prune` |
| BUILDER sin bloqueo | 90,6 k / 34 usos, sin reanudación | Ausencia del patrón del ciclo 1 (Docker caído); coste razonable para el paso (rojo en dos fases: tipos y luego aserción) |

No hay picos de lectura ni de salida: ningún comando con salida grande sin filtrar, lecturas por rangos (OPT de lectura mínima aplicadas).

### Propuestas

**P2-1: Ruta corta para el worktree de línea base/rojo (ajusta la receta de OPT-033)**
- Causa: `git worktree remove` falla por ruta larga dos ciclos seguidos porque el worktree se crea bajo el scratchpad (ruta muy larga en Windows); cada vez cuesta 2-3 llamadas extra (`rm -rf` + `prune` + comprobación).
- Propuesta (`.github/doc/work-guidelines.md`, la aplica el operador a mano; y recordatorio en `.claude/agents/reviewer.md`): crear el worktree en una ruta corta (p. ej. `$(git rev-parse --show-toplevel)/../wt-red` o `C:/wt/red`, fuera del repo) y, si `remove --force` falla, el fallback documentado es `rm -rf <ruta> && git worktree prune`, en una sola llamada.
- Ahorro esperado: ~1-3 llamadas y ~2-4 k tokens por ciclo de REVIEWER (estimado, sin datos medibles); elimina un fallo recurrente.
- Fuente: `usage/REVIEWER.md` ciclos 1 y 2; `MEMORIES.md` OPT-033.
- Estado de aprobación: pendiente de decisión del operador.

**Observación sin propuesta nueva**: el sesgo de subestimación de usos (+25 % ciclo 1, +36 % ciclo 2 en BUILDER) no se resuelve con otra regla; la causa es que los subagentes no ven su consumo. La corrección es que el ORCHESTRATOR pase los totales y que `usage/*.md` marque `estimado` (ya cumplido). No se propone.

### Efecto observado de propuestas previas
- P1-1/P1-2 rechazadas (error humano): no se reevalúan. En este ciclo no hubo bloqueo de Docker, sin datos para juzgar.
- OPT-033 (receta de worktree): REVIEWER la usó (un único worktree para rojo 1 y 2), pero reaparece el fallo de eliminación por ruta larga; detalle en P2-1. Ahorro no medible por falta de `usage-slice`.
- OPT-005/006/039: aplicados sin incidencias visibles (suites filtradas, sin errores de tipos no localizados).

## Ciclo 3 (paso 03, APPROVED, último ciclo planificado)

### Uso medido

`usage-slice`: no ejecutado. OPT-031 aplica (3er ciclo consecutivo sin datos). Se cita `usage/*.md` como `estimado` y los totales del ORCHESTRATOR como `medido:agent-result` (cota inferior, OPT-023). Brecha: sin separación de entrada nueva frente a caché.

| Rol | subagent_tokens | Usos | Duración | Origen | `usage/*.md` (estimado) |
| --- | ---: | ---: | ---: | --- | --- |
| BUILDER | 62 309 | 24 | 278,9 s | medido:agent-result (cota inferior) | ~70k ent. / ~9k sal. / ~18 usos |
| REVIEWER | 52 239 | 18 | 190,2 s | medido:agent-result (cota inferior) | ~55k / ~6k / ~12 usos |
| OPTIMIZER (análisis ciclo 2) | 25 498 | 8 | n/a | medido:agent-result | n/a |

Total BUILDER+REVIEWER: 114 548 tokens, 42 usos. Frente al ciclo 2 (154 359 / 53): -26 % tokens, -21 % usos; el menor del plan (refactor puro, 754 tests antes/después). Sesgo de usos: BUILDER 24 frente a ~18 (+33 %), REVIEWER 18 frente a ~12 (+50 %); confirma el patrón de subestimación (ciclos 1-2: +25 %/+36 %).

### Picos

| Pico | Valor | Origen |
| --- | --- | --- |
| Sesgo de usos REVIEWER | +50 % (18 medidos frente a ~12) | `usage/REVIEWER.md` frente a `agent-result`; el mayor del plan |
| Ciclos 1-2 sin commit | Diff del ciclo 3 frente a `HEAD` mezcla los 3 ciclos; el rojo del ciclo 3 no se pudo reproducir en worktree | `usage/REVIEWER.md` ciclo 3 (nota O7); AC3.4 verificado por comparación de bloques |
| Compactación manual de `STATUS.md` | `awk`/`sed` por BUILDER | `usage/BUILDER.md` ciclo 3; cumple OPT-029, sin incidencia |

Sin picos de lectura ni de salida. Tendencia a la baja de coste por ciclo (el ciclo 3 es el más barato).

### Propuestas

**P3-1: Commit del operador al cerrar cada ciclo APPROVED, antes del siguiente (recordatorio en el checkpoint de `/harness-run`)**
- Causa: con los ciclos 1-2 sin commit, `HEAD` no contiene el estado "antes" del ciclo 3. El diff mezcla ciclos, REVIEWER no puede reproducir el rojo (OPT-033 inaplicable) y debe sustituirlo por comparación manual de bloques, con menos evidencia y más lecturas.
- Propuesta: añadir al texto del checkpoint de ORCHESTRATOR (`.claude/commands/harness-run.md`, lo aplica el operador) una línea: "Si el veredicto es APPROVED o el siguiente ciclo es un refactor, haz commit/stash del estado actual antes de aprobar el ciclo siguiente". Respeta AGENTS.md: el agente no hace commit, solo lo recuerda al operador.
- Ahorro esperado: ~5-15 k tokens y ~3-6 llamadas por ciclo de REVIEWER (estimado) y evidencia de rojo reproducible en lugar de comparación manual. Sin efecto si el operador ya hace commit por ciclo.
- Fuente: `usage/REVIEWER.md` ciclo 3; nota O7 del REVIEWER; `AGENTS.md` (commits manuales).
- Estado de aprobación: pendiente de decisión del operador.

**P2-1** (ruta corta del worktree): sin decisión del operador; sigue pendiente, no se repropone. En el ciclo 3 no hubo worktree, sin datos nuevos.

### Efecto observado de propuestas previas
- OPT-029 (colapso de `STATUS.md`): BUILDER compactó el ciclo 1 a `STATUS-archive.md` sin incidencias.
- OPT-005/006/039: sin incidencias; suites filtradas.
- Ahorro de OPT-033/OPT-031 no medible por falta de `usage-slice`.
