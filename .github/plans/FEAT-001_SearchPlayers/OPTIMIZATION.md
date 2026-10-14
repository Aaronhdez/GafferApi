# OPTIMIZATION - FEAT-001_SearchPlayers

## Resumen de ciclos

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 | APPROVED | P1-1, P1-2 | ~15-25 k tokens y ~10 llamadas (P1-1); ~40-60 k (P1-2) | pendiente |

## Ciclo 1 (paso 01, APPROVED)

### Uso medido

`usage-slice` (`--repo gaffer-api`, ventanas `Inicio`/`Fin` de `STATUS.md`): `sin datos` en las 3 ventanas (claude-code y copilot). No se reintenta la escalera: OPT-031 aplica (ciclos previos de otros planes ya sin datos); se cita `usage/*.md` como `estimado` y los totales del ORCHESTRATOR.

| Rol | subagent_tokens | Usos | Duración | Origen | `usage/*.md` (estimado) |
| --- | ---: | ---: | ---: | --- | --- |
| BUILDER run 1 (bloqueado por Docker) | 93 835 | 51 | 425,6 s | medido:agent-result (cota inferior, OPT-023) | ~90k ent. / ~14k sal. / ~38 usos |
| BUILDER run 2 (reanudación + LINQ) | 49 136 | 26 | 283,4 s | medido:agent-result (cota inferior) | ~45k / ~6k / ~20 usos |
| REVIEWER | 58 867 | 19 | 218,2 s | medido:agent-result (cota inferior) | ~60k / ~8k / ~16 usos |
| PLANNER (precycle) | 87 911 / 59 770 / 41 792 | 38 / 30 / 15 | n/a | medido:agent-result | n/a |

Totales: BUILDER 142 971 (77 usos), REVIEWER 58 867, PLANNER 189 473. Entrada nueva vs caché: no separable sin `usage-slice`; `usage/*.md` indica mayoría caché. Los usos de herramientas medidos (51/26/19) superan los estimados por los agentes (~38/~20/~16): sesgo de subestimación de ~25 %.

### Picos

| Pico | Valor | Origen |
| --- | --- | --- |
| BUILDER run 1 | 93,8 k / 51 usos, 1,9x el run 2 | `STATUS.md`: `docker ps` ya falló (B1), pero BUILDER siguió: línea base `dotnet test` con 700 fallos en `OneTimeSetUp`, filtro de 16 tests fallidos, sonda ad hoc en scratchpad para suplir evidencia de BD |
| Trabajo de línea base duplicado | AC1.1: 700 (erróneo) en run 1, 699 en run 2 (copia `git archive`), 699 de nuevo en REVIEWER (worktree) | Línea base de HEAD obtenida 3 veces; la del run 1 no valió |
| PLANNER mid-cycle | 41,8 k / 15 usos para una decisión LINQ añadida | Requisito del operador tras la aprobación del plan |
| Estimado frente a medido | usos +25 % | `usage/BUILDER.md` frente a `agent-result` |

### Propuestas

**P1-1: Preflight de BD bloqueante antes de cualquier `dotnet test` (extiende el paso 5 de `builder.md`)**
- Causa: `.claude/agents/builder.md` L27 pide registrar el bloqueo, pero no dice que se detenga: tras `docker ps` fallido, BUILDER ejecutó la suite (700 fallos), la repitió filtrada (16 fallos) y construyó una sonda sustituta.
- Propuesta (`.claude/agents/builder.md` y `.github/agents` equivalente): si `docker ps` o la conexión al puerto 3306 fallan, BUILDER puede completar código/tests y build+format, pero no ejecuta `dotnet test` de tests con BD; marca `BLOCKED`/`READY_FOR_REVIEW` parcial con el bloqueo exacto y termina. Una sola comprobación `docker ps --format '{{.Names}}'` y `exec 3<>/dev/tcp/127.0.0.1/3306`.
- Ahorro esperado: ~15-25 k tokens y ~10 llamadas por incidencia (2 ejecuciones inútiles de suite + sonda); evita la cifra errónea de línea base (700 frente a 699).
- Fuente: `STATUS.md` (filas BUILDER, líneas base), `usage/BUILDER.md` ciclo 1, ORCHESTRATOR run 1.

**P1-2: Preflight de BD en ORCHESTRATOR antes de lanzar BUILDER/REVIEWER**
- Causa: el operador arrancó Docker tras el bloqueo y hubo que reanudar BUILDER (2º arranque, 49 k tokens, relectura de contexto: `STATUS.md`, plan 01, `SqlPlayerRepository.cs`).
- Propuesta (`/harness-run`, `Bash` ya permitido solo para `git status`/`git diff --stat`; requiere ampliar a `docker ps`): comprobar el puerto 3306 al inicio del ciclo y pedir al operador que arranque Docker antes de invocar BUILDER; alternativa sin ampliar permisos: que el checkpoint de inicio de ciclo recuerde "Docker/MySQL arriba".
- Ahorro esperado: evita un relanzamiento completo; ~40-60 k tokens (run 2 menos la parte del refactor LINQ, y el sobrecoste de la reanudación) cuando Docker esté caído. Sin efecto con Docker arriba.
- Fuente: totales del ORCHESTRATOR; `STATUS.md` Bloqueos B1.

**Observación sin propuesta nueva**: la línea base de HEAD se calcula en BUILDER y de nuevo en REVIEWER (OPT-033 ya cubre la receta de worktree); no se repite. Requisito añadido a mitad de ciclo (LINQ): coste de PLANNER 41,8 k, inevitable; OPT-017/020 (consolidar decisiones) ya aplicaba al planificar. Verificar en el ciclo 2 si P1-1 evita repetir el patrón.

### Efecto observado de propuestas previas
Sin datos suficientes en este ciclo: OPT-039 (grep de usos antes de editar) se aplicó (`STATUS.md`, fila "Grep de usos OPT-039") sin fallos de compilación por tipos no localizados; OPT-005/006 aplicados (código de salida y filtro bilingüe). Medición de ahorro no posible por falta de `usage-slice`.
