# OPTIMIZATION - TECHDEBT-005

## Resumen de ciclos

| Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación |
| --- | --- | --- | --- | --- | --- |
| 1 | 01 `Update(Facility)` fuera del contrato | APPROVED | P1-1 (OPT-032), P1-2 (OPT-033) | 3-5 llamadas BUILDER + 2-5 REVIEWER por paso | Aprobadas 2026-10-11 |
| 2 | 02 Validación de dominio de Player | APPROVED | P2-1, P2-2, P2-3 | ~12-16 llamadas BUILDER (P2-1) en pasos 03-04 | P2-1 (OPT-034) y P2-3 (OPT-036) aprobadas 2026-10-12; P2-2 (OPT-035) no abordada por el operador |

## Ciclo 1 - Paso 01 (`Update(Facility)` fuera del contrato) - APPROVED

### Uso medido

`usage-slice`: no ejecutado. Los ciclos previos (TECHDEBT-004 ciclos 5 y 6, y HARNESS-001 ciclo 1) ya devolvieron "sin datos", así que se omite la escalera de reintento (OPT-031). No hay medición de `token-consumer`, por lo que no se puede separar entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Origen | Notas |
| --- | --- | --- | --- | --- | --- |
| BUILDER | 59 467 | 29 | 257 s | `medido:agent-result` (cota inferior, OPT-023) | Autoinforme `usage/BUILDER.md`: ~60 000 entrada + ~8 000 salida, 25 llamadas (`estimado`) |
| REVIEWER | 39 989 | 19 | 161 s | `medido:agent-result` (cota inferior, OPT-023) | Autoinforme `usage/REVIEWER.md`: ~45 000 + ~5 000, 17 llamadas (`estimado`) |

Los totales de `agent-result` coinciden con los autoinformes `estimado`, el mismo patrón de TECHDEBT-004 ciclo 1 (OPT-023: `agent-result` excluye la caché acumulada). El coste real es probablemente un orden de magnitud mayor y no se puede cuantificar aquí.

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | Evidencia AC1.5 mediante `dotnet run` + `curl` | 3 comandos de BUILDER (línea base, después, `cmp`); `STATUS.md` filas 43 y 52; desviación documentada en el detalle del ciclo | El AC pide "swagger.json servido idéntico" pero no fija procedimiento. `StartupTestServer` (OPT-022) no tiene un test que vuelque la ruta, y BUILDER evitó añadir un test fuera de plan. La alternativa fue arrancar la API real con Dummy y puertos. |
| S2 | REVIEWER improvisó un worktree temporal para el rojo de AC1.1 | `STATUS.md` fila 56 (worktree en `HEAD` + diff de tests, scratchpad, eliminado); `usage/REVIEWER.md` | AGENTS.md exige verificar rojo a verde y REVIEWER no puede tocar el árbol de trabajo ni corregir producto. No existe una receta estándar; REVIEWER la compuso en caliente (`git worktree add`, aplicar diff solo de tests, build, test, limpieza). |
| S3 | Veredicto sobre AC1.5 sin re-ejecutar la captura | `REVIEW.md` desviación AC1.5 | REVIEWER aceptó por análisis (mismo `Startup`, diff sin tipos alcanzables desde controladores). Es un ahorro correcto, pero basado en razonamiento que se rehará en cada paso refactor similar (pasos 02-04 también tocan contrato). |

Sin otros outliers: lecturas por rangos (`sed -n`), builds y tests filtrados por resumen, sin salidas grandes (`usage/*.md`). OPT-005/006/007/018/024/025/026 sin regresión visible.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P1-1 | Para pasos que prometen "contrato HTTP/swagger sin cambios", PLANNER fija el procedimiento en el AC: (a) si el diff no toca controladores, DTOs ni `Startup`, el criterio se satisface con los tests de contrato existentes (`OpenApiContractShould`, `FacilitiesControllerContractShould`) en verde, sin captura manual; (b) si los toca, usar un volcado único con `StartupTestServer` (nota OPT-022 de `test-stack.md`), no `dotnet run`. Evita que BUILDER y REVIEWER decidan y justifiquen una desviación en cada paso. | Plantilla de plan / agente PLANNER (`.claude/agents/planner.md`) y `.github/doc/common/test-stack.md` | Pasos 02-04: 3 comandos + 1 aceptación de desviación por ciclo, estimado 3-5 llamadas de BUILDER y 2-3 de REVIEWER por paso | `STATUS.md` filas 43, 52; `REVIEW.md` desviación AC1.5 |
| P1-2 | Documentar en `.github/doc/work-guidelines.md` una receta estándar de reproducción del rojo para REVIEWER: `git worktree add <scratchpad>/red HEAD`, aplicar solo el diff de tests (`git diff -- <tests> \| git apply`), `dotnet test --filter`, `git worktree remove --force`. Indicar que no se debe usar `git stash` ni tocar el árbol de trabajo. | `.github/doc/work-guidelines.md` (sección de evidencia de revisión) o `.claude/agents/reviewer.md` | Ahorra el diseño ad hoc en cada ciclo: ~3-5 llamadas de REVIEWER, menos riesgo de dejar un worktree huérfano | `STATUS.md` fila 56; `usage/REVIEWER.md` |
| P1-3 | Registrar en `usage/*.md` también la cifra `medido:agent-result` que pasa ORCHESTRATOR, o dejar que OPTIMIZER la anote en `OPTIMIZATION.md` solo cuando exista. No es una propuesta nueva de contenido: es un recordatorio de la brecha. Sin cambio propuesto; no se registra. | - | - | OPT-023, OPT-031 |

P1-3 no requiere aprobación y no se propone para registro.

**Aprobación (operador, 2026-10-11, "Aprueba y aplica las dos propuestas, y luego ejecuta el ciclo 2"):** P1-1 aprobada y registrada como OPT-032 (la aplica PLANNER en los pasos 02-04); P1-2 aprobada y registrada como OPT-033 (la aplica el operador a mano). Ambas `pendiente` en `MEMORIES.md`.

### Brechas de datos

- `token-consumer` sin datos por tercer ciclo consecutivo con planes distintos; el efecto real de OPT-005 a OPT-031 sigue sin medirse. Considerar con el operador si el dashboard está capturando eventos (fuera de alcance de OPTIMIZER).
- `agent-result` es cota inferior (OPT-023); no hay distinción entre entrada nueva y caché.

### Efecto observado de memorias anteriores (para actualizar tras la aprobación)

| ID | Observación en este ciclo |
| --- | --- |
| OPT-018 | Sin regresión: BUILDER añadió tests a un fichero existente sin builds de diagnóstico por llaves. |
| OPT-024 | Sin regresión: sin intentos de script con intérprete ausente. |
| OPT-025 | La sonda `dotnet run` se redirigió a scratchpad desde la primera ejecución; efecto positivo. |
| OPT-026/029 | Índice resumido de `STATUS.md` presente (vacío, ciclo 1); aún sin datos que colapsar. |
| OPT-031 | Aplicada en este ciclo: se omitió la escalera de `usage-slice`. |

## Ciclo 2 - Paso 02 (validación de dominio de Player) - APPROVED

### Uso medido

`usage-slice` ejecutado una vez (ventanas de `STATUS.md`, `--repo gaffer-api`): "sin datos" en ambas ventanas, claude-code y copilot. Cuarto ciclo sin datos; sin escalera de reintento (OPT-031). Sin `token-consumer` no se separa entrada nueva de caché.

| Rol | Tokens | Tool uses | Duración | Origen | Autoinforme `usage/*.md` (`estimado`) |
| --- | --- | --- | --- | --- | --- |
| BUILDER | 165 359 (ciclo 1: 59 467; x2,8) | 80 (ciclo 1: 29; x2,8) | 771 s (ciclo 1: 257 s; x3,0) | `medido:agent-result` (cota inferior, OPT-023) | ~150 000 + ~25 000, 55 llamadas (subestima las llamadas en 25) |
| REVIEWER | 98 414 (ciclo 1: 39 989; x2,5) | 39 (ciclo 1: 19; x2,1) | 497 s (ciclo 1: 161 s; x3,1) | `medido:agent-result` (cota inferior, OPT-023) | ~70 000 + ~9 000, 27 llamadas (subestima en 12) |

Los tokens por llamada son estables (BUILDER ~2 070 en ciclo 2 frente a ~2 050 en ciclo 1; REVIEWER ~2 520 frente a ~2 100): el coste crece con el número de llamadas, no con lecturas grandes.

### Spikes

| # | Spike | Valor / origen | Causa probable |
| --- | --- | --- | --- |
| S1 | BUILDER x2,8 en tokens y llamadas | 165 k / 80 usos frente a 59 k / 29 | Alcance del paso, no lecturas: 12+ ficheros de producto/test, 89 tests nuevos (324 a 413), caracterización AC2.0 de 13 casos, dos rojos (aceptación y compilación), cambio de comportamiento Q17 con 4 tests adaptados, `ValidationStartup` nuevo y ~13 invocaciones de build/test en `STATUS.md` (línea base, rojos, 2 verdes, sonda x2, refactor x3...). Lecturas contenidas: 16 ficheros, el mayor `00-overview` ~17 KB. |
| S2 | Primer verde fallido | `STATUS.md`: 3 errores (aserción `Contain([])` del helper; test de 29-feb mal planteado) | Fallos de test propios; un ciclo extra build+test (~3-4 llamadas). El caso bisiesto (2008+16=2024) se podía calcular antes de escribirlo. |
| S3 | Sonda `ZzCycle2ProbeShould.cs` creada y borrada | `STATUS.md` fila AC2.0+AC2.6 | Efecto de OPT-032 rama (b): el AC pide volcado con `StartupTestServer`, pero no hay ayudante reutilizable. BUILDER escribió una clase `[Explicit]` (host de `StartupTestServer` + `TestFixture`), la ejecutó dos veces, hizo `cmp` y la borró: ~6-8 llamadas (estimación). Los pasos 03 y 04 (AC3.8, AC4.6) la repetirán. Positivo: evitó `dotnet run` y la caracterización resolvió el "No verificado" de `00` (DataAnnotations no se evalúan en producción). |
| S4 | Rojo en worktree por REVIEWER (OPT-033 sin aplicar) | `STATUS.md`: dos filas de REVIEWER (rojo aceptación y unitario en el mismo worktree, diffs, borrado) | Segunda improvisación de la receta: ~5-8 de las 39 llamadas (ciclo 1: ~4), estimación. El rojo por compilación en un worktree obliga a restaurar y compilar el proyecto de tests desde cero. Dependencia del operador (OPT-033). |
| S5 | Pruebas manuales REVIEWER con `dotnet run` + curl | `STATUS.md` fila "pipeline de producción"; `usage/REVIEWER.md`: 2 arranques descartados (Dummy y `env` mal citado) | Aporte real: confirmó 400 con clave por operación y sin eco bajo la pila de producción (`AddMvcCore`) y vio el 500 de facility sin dirección. Pero ese 500 ya estaba en la tabla AC2.0 de BUILDER, y los 400 de player los cubren los tests de aceptación. Lo no redundante es solo confirmar `ConfigureValidationResponses` bajo `AddMvcCore` real, porque `TestFixture` usa `AddControllers` (hallazgo del propio REVIEWER). Los 2 arranques descartados cuestan ~4-6 llamadas (estimación) por falta de receta (`GafferDB_Test`, variable de conexión, sin Dummy). |
| S6 | `cat` combinado de 4 ficheros del plan (29,7 KB, persistido) | `usage/REVIEWER.md` | REVIEWER relee `STATUS.md`, `REVIEW.md` y `usage` completos cada ciclo; OPT-007 aplicable con lectura por rangos. |

Sin regresión visible en OPT-005/006/018/024/025/026/029.

### Propuestas

| ID | Propuesta | Ámbito | Ahorro esperado | Fuente |
| --- | --- | --- | --- | --- |
| P2-1 | Ayudante permanente de test (p. ej. un test `[Explicit]` de nombre estable en `Gaffer.Api.Tests`) que escribe el `swagger.json` servido por `StartupTestServer` a una ruta dada por variable de entorno; documentarlo en `test-stack.md`. PLANNER lo incluye como tarea explícita del paso 03 y lo cita en AC3.8/AC4.6 ("usar el ayudante") en lugar de que BUILDER cree una sonda. | Plan (paso 03) y `.github/doc/common/test-stack.md` | Pasos 03-04: ~12-16 llamadas de BUILDER en total (~25-30 k tokens `agent-result`) y sin host duplicado | `STATUS.md` fila AC2.0+AC2.6; S3 |
| P2-2 | Decidir antes del paso 03 si `TestFixture` pasa a `AddMvcCore` como producción (hallazgo de REVIEWER). Si sí, desaparece la divergencia y la necesidad de `dotnet run` manual. Si no, en "Cómo demostrarlo" de 03-04 limitar la prueba de producción de REVIEWER a un caso por `AddMvcCore`, tomando como línea base la tabla de caracterización, y fijar la receta de arranque en `STATUS.md`. | Decisión del operador / plan (03-04) | ~8-12 llamadas de REVIEWER por paso (estimación) | `STATUS.md` fila "pipeline de producción"; `REVIEW.md` ciclo 2; S5 |
| P2-3 | Limitar el tamaño del paso 03: PLANNER lo divide en 03a (Facility) y 03b (gradas) si prevé más de ~50 tests nuevos o ~10 ficheros, reutilizando `DomainValidationErrors`/`ValidationFailedException`. Calcular fechas de calendario antes de escribir tests. | Agente PLANNER / `03-*.md` | No reduce el total, pero un `NEEDS_CHANGES` costaría ~80-100 k en lugar de ~165 k de repetición | S1, S2 |

**Propuesta principal: P2-1.** Aprobación (operador, 2026-10-12, "Aprueba las propuestas P2-1 a 3 y preguntame lo que necesites"): P2-1 registrada como OPT-034 (la aplica PLANNER/BUILDER en el paso 03a); P2-3 registrada como OPT-036 (la aplica PLANNER ahora; división elegida "Facility / gradas": 03a Facility + dirección, 03b gradas + `/stands`, health check al paso 05); P2-2 registrada como OPT-035 sin aplicación: el operador respondió "Obviemos este punto"; el esquema no admite un estado "descartada", así que consta `pendiente` con la nota en la columna de optimización.

### Brechas de datos

- `token-consumer` sin datos por cuarto ciclo seguido; `agent-result` es cota inferior (OPT-023). La causa de S1 se deduce de llamadas y ficheros tocados, no de caché medida.
- Las llamadas atribuidas a worktree y `dotnet run` (S4, S5) son estimaciones a partir de las filas de `STATUS.md`.

### Efecto observado de memorias anteriores

| ID | Observación |
| --- | --- |
| OPT-032 | Aplicada por PLANNER a AC2.6, AC3.8, AC4.6. En el paso 02 rige la rama (b): `StartupTestServer` antes/después (sha256 idéntico `be49f0ab...a5a6bfe`, `cmp` exit 0), sin `dotnet run` ni desviación que aceptar. Efecto positivo en decisión y riesgo; el ahorro en llamadas es parcial porque el coste pasó a la sonda (S3). Actualizada a `aplicada` en `MEMORIES.md`. |
| OPT-033 | Sin aplicar por el operador; REVIEWER repitió la receta (S4). Sigue `pendiente`. |
| OPT-031 | Aplicada: una llamada a `usage-slice`, sin escalera. |
| OPT-007 | Parcial: relectura de 29,7 KB de ficheros del plan (S6). |
