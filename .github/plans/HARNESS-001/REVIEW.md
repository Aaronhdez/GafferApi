# REVIEW - HARNESS-001

## Ciclo 1 - Veredicto: CHANGES_REQUESTED (estado `NEEDS_CHANGES`)

Alcance: verificación independiente de los AC de los pasos 01-05 sobre cambios aplicados a mano por el operador. No hay código de producto ni tests, así que no se ha ejecutado build ni MySQL (no aplica). REVIEWER no ha editado ficheros del harness.

### Git status / diff

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | Solo `M .github/plans/HARNESS-001/STATUS.md` y `?? .github/plans/HARNESS-001/usage/BUILDER.md` (lo esperado) |
| `git diff --stat HEAD -- CLAUDE.md AGENTS.md .claude .github/doc .github/agent-harness` | Vacío: BUILDER no tocó ficheros del harness ni de stack |
| Diff de `STATUS.md` | Solo registro de verificación de BUILDER (+60/-7); coherente con el rol |

### Rojo→verde

BUILDER no tenía "red" porque los cambios ya estaban versionados. REVIEWER lo reproduce desde el historial: el texto se introdujo en `5352828` (`git log -S`), y en `5352828^` todos los `grep` dan 0 (CLAUDE 005-007, 018, 008-011; `reviewer.md` `date -u`; `AGENTS.md` "Consolidate pending"; `dev-stack.md` OPT-010; `test-stack.md` OPT-022). En `HEAD` dan los resultados de abajo. `git show 5352828 -- CLAUDE.md` no tiene líneas eliminadas.

### Evidencia por criterio

| AC | Resultado | Evidencia (REVIEWER) | Acción del operador |
| --- | --- | --- | --- |
| ACH1.1 | Demostrado | `CLAUDE.md` L45/L46/L47 dentro de `## Minimal Reading` (L41) | - |
| ACH1.2 | Demostrado | 1 línea, `CLAUDE.md:48` (QH5 = a)) | - |
| ACH1.3 | Demostrado | `5352828` solo añade líneas en `CLAUDE.md`; secciones L7/L23/L41 sin cambios | - |
| ACH1.4 | Demostrado | `grep -i "exit code\|warning" AGENTS.md` → 0 | - |
| ACH1.5 | No aplica | QH1 = a) | - |
| ACH2.1 | Demostrado (ubicación distinta) | L31-33 en orden 008→009→011, como bloque "fallback ladder" tras la lista 1-3 y no como sub-viñetas del punto 1. Contenido y orden correctos | Aceptar la desviación de ubicación (sin cambio) |
| ACH2.2 | **No demostrado** | `reviewer.md` L18 `Inicio` con `date -u`; **L25 `Fin` sin comando**; `builder.md` L18 sin cambios | `.claude/agents/reviewer.md` L25: `Record \`Fin\` (ISO 8601 UTC, \`date -u +%Y-%m-%dT%H:%M:%SZ\`) in \`STATUS.md\` ...` |
| ACH2.3 | Demostrado | Lista 1-2-3 intacta (L26-28) | - |
| ACH2.4 | No aplica | QH1 = a) | - |
| ACH3.1 | **No demostrado** | `grep "OPT-017, OPT-020" AGENTS.md` → 0. Viñeta equivalente en L62 dentro de `## Human-Controlled Flow` (L49-66), sin IDs | Añadir ` (OPT-017, OPT-020)` al final de `AGENTS.md` L62, o declarar aceptada la desviación literal |
| ACH3.2 | Demostrado | L62 cubre inicio de ciclo y re-invocación de PLANNER con una sola pregunta | - |
| ACH3.3 | Demostrado | Verbos "raises"/"gathers"; sin edición ni decisión de criterios | - |
| ACH3.4 | Demostrado (texto distinto) | `harness-run.md` L21 con OPT-020/OPT-017; frase literal "open questions blocking" → 0. Parte opcional | Aceptar la redacción (sin cambio) |
| ACH4.1 | **No demostrado** | `grep "OPT-013, OPT-021" AGENTS.md` → 0. L43 "Hierarchical summary" solo cubre `STATUS.md`; `OPTIMIZATION.md` (OPT-013) solo está en `optimizer.md` L26 | `AGENTS.md` L43: extender a `OPTIMIZATION.md` (compacta OPTIMIZER al empezar el análisis) y añadir `(OPT-013, OPT-021)` |
| ACH4.2 | **No demostrado** | `grep OPT-014 AGENTS.md` → 0; L74 "is append-only and outside plans." sin excepción, y contradice `optimizer.md` L33 (compactación) | `AGENTS.md` L74: añadir "except for the approved compaction below" y la viñeta de compactación de `MEMORIES.md` con `(OPT-014)` |
| ACH4.3 | **No demostrado** | `optimizer.md` L33/L43 escriben `MEMORIES-archive.md` (QH2 = b) de facto) pero la matriz `AGENTS.md` L32 solo permite `OPTIMIZATION.md` y `MEMORIES.md`. Igual para BUILDER: `builder.md` L18 escribe `STATUS-archive.md`, no listado en `AGENTS.md` L30 | Elegir: añadir `MEMORIES-archive.md` a la fila OPTIMIZER (y `STATUS-archive.md` a BUILDER) en `AGENTS.md`, o quitar los archivos de `optimizer.md`/`builder.md` (QH2 = a)) |
| ACH4.4 | **No demostrado** (condicional) | `MEMORIES.md` sin tabla resumen; `MEMORIES-archive.md` no existe; 31 IDs únicos (línea base para el recuento posterior) | Aprobar explícitamente la compactación inicial para que OPTIMIZER la haga en modo registro, o declararla fuera de alcance |
| ACH4.5 | No aplica | QH1 = a) | - |
| ACH5.1 | Demostrado | `dev-stack.md` L22, sub-viñeta de L21 "OpenAPI/Swagger via NSwag" | - |
| ACH5.2 | **No demostrado** | `grep OPT-012` en `test-stack.md` → 0. Evidencia disponible: `TECHDEBT-004/STATUS-archive.md` L74 (`MySqlConnector.MySqlException`). `MEMORIES.md` marca OPT-012 como `revertida` | Añadir en `test-stack.md` `## Testing Strategy Notes` (L27): "Driver failures (connection or query) surface as `MySqlConnector.MySqlException`; ... (OPT-012)", o cerrar ACH5.2 como no aplicable por OPT-012 `revertida` |
| ACH5.3 | Demostrado (sección distinta) | `test-stack.md` L25, en `## Test Types Observed` (L19), no en `## Testing Strategy Notes` (L27). El AC solo pide 1 línea | - |
| ACH5.4 | Demostrado | `dev-stack.md` L25-26 (driver `MySqlConnector`) no duplica ni contradice L22; diff de trabajo vacío | - |
| ACH5.5 | Demostrado | Lo aplicó el operador en `5352828` (QH3 = a)) | - |

Resumen: 13 demostrados (3 con desviación de ubicación/redacción aceptable), 7 no demostrados (ACH2.2, 3.1, 4.1, 4.2, 4.3, 4.4, 5.2) y 4 que no aplican.

### Hallazgos

| # | Gravedad | Hallazgo |
| --- | --- | --- |
| R1 | Bloqueante | `AGENTS.md` (normativo) contradice a `optimizer.md`: L74 dice "append-only" y la matriz no permite `MEMORIES-archive.md`, mientras que `optimizer.md` L33/L43 compacta y archiva. Mismo patrón con `STATUS-archive.md` para BUILDER (fuera de los AC del plan, pero en la misma fila de la matriz) |
| R2 | Bloqueante | ACH2.2, 3.1, 4.1, 5.2: falta texto o etiquetas, ver la tabla |
| R3 | No bloqueante | `CLAUDE.md` L46 (OPT-006) filtra solo `Warning\(s\)|Error\(s\)`, pero L45 avisa de que la consola está localizada; con salida en español el filtro puede salir vacío. El texto propuesto incluía `Advertencia\(s\)|Errores` |
| R4 | Informativo | Los textos de 01-05 entraron en `5352828`, no en `4e87c87` como dice la nota de BUILDER en `STATUS.md` (`4e87c87` es posterior). No cambia el veredicto |

Bloqueos: ninguno técnico. Ningún rol puede editar los ficheros afectados; todas las acciones son del operador (salvo ACH4.4, que la ejecuta OPTIMIZER con aprobación).

## Ciclo 2 - Veredicto: APPROVED (estado `APPROVED`)

Alcance: nueva verificación independiente de 01-05 tras las ediciones que el operador autorizó ("te autorizo a hacer las ediciones de forma excepcional"), la compactación de `MEMORIES.md` ("Sí, compactar") y el cierre de ACH5.2 ("Cerrar como revertida"). No hay código de producto ni tests, así que no se ejecutan build ni MySQL (no aplica). REVIEWER no ha editado ficheros del harness.

### Git status / diff

| Comprobación | Resultado |
| --- | --- |
| `git status --short` | M: `.claude/agents/reviewer.md`, `MEMORIES.md`, `AGENTS.md`, `REVIEW.md` y `STATUS.md` del plan. ??: `MEMORIES-archive.md`, `OPTIMIZATION.md`, `usage/BUILDER.md` y `usage/REVIEWER.md`. Es lo esperado |
| `git diff --stat` (fuera del plan) | `reviewer.md` +1/-1 (L27), `AGENTS.md` 11 (L30, L32, L43, L62, L74 y +L75), `MEMORIES.md` 51. `CLAUDE.md`, `.github/doc/`, `.claude/commands`, `builder.md` y `optimizer.md` sin diff. Sin código de producto |
| `git diff -U0 AGENTS.md reviewer.md` | Solo las líneas autorizadas; no se elimina ninguna regla, solo se amplía |

### Rojo→verde

| Patrón | `HEAD` (red) | Árbol de trabajo (green) |
| --- | --- | --- |
| `AGENTS.md` "OPT-017, OPT-020" / "OPT-013, OPT-021" / "OPT-014" / "except for hierarchical compaction" | 0 / 0 / 0 / 0 | 1 / 1 / 1 / 1 |
| `AGENTS.md` "MEMORIES-archive.md" / "STATUS-archive.md" | 0 / 0 | 2 / 2 |
| `reviewer.md` "date -u" | 1 (solo `Inicio`) | 2 (L18 `Inicio`, L27 `Fin`) |
| `MEMORIES-archive.md` | no existe en `HEAD` | existe, 22 filas |

El red de los AC que ya se cumplían en el ciclo 1 está en la sección del ciclo 1 (`5352828^`).

### Evidencia por criterio

| AC | Resultado | Evidencia (REVIEWER) |
| --- | --- | --- |
| ACH1.1 | Demostrado | `CLAUDE.md` L45-47 dentro de `## Minimal Reading` (L41) |
| ACH1.2 | Demostrado | 1 línea, `CLAUDE.md:48`; 0 en `builder.md` |
| ACH1.3 | Demostrado | `CLAUDE.md` sin diff; secciones L7/L23/L41 |
| ACH1.4 | Demostrado | `grep -c -i "exit code\|warning" AGENTS.md` → 0 |
| ACH1.5 | No aplica | QH1 = a) |
| ACH2.1 | Demostrado (ubicación distinta, aceptada en el ciclo 1) | L31 (008), L32 (009), L33 (011) en orden |
| ACH2.2 | **Demostrado** | `reviewer.md` L18 `Inicio` y L27 `Fin` con `date -u +%Y-%m-%dT%H:%M:%SZ`; `builder.md` L18 sin diff |
| ACH2.3 | Demostrado | Lista 1-2-3 intacta (L26-28); `CLAUDE.md` sin diff |
| ACH2.4 | No aplica | QH1 = a) |
| ACH3.1 | **Demostrado** | 1 línea, `AGENTS.md` L62, en `## Human-Controlled Flow` (L49-66) |
| ACH3.2 | Demostrado | L62 cubre el inicio de ciclo y la re-invocación de PLANNER con una sola pregunta |
| ACH3.3 | Demostrado | "raises"/"gathers"; ORCHESTRATOR no decide ni edita |
| ACH3.4 | Demostrado (texto distinto, aceptado en el ciclo 1) | `harness-run.md` sin diff; parte opcional |
| ACH4.1 | **Demostrado** | 1 línea, `AGENTS.md` L43, en `## Plans and Evidence` (L36-48); cubre `STATUS.md` y `OPTIMIZATION.md` |
| ACH4.2 | **Demostrado** | `OPT-014` → 1 línea (L75) en `## Usage and Optimization Memory` (L67-78); L74 "append-only ... except for hierarchical compaction" coherente con L75 y con `optimizer.md` L33 |
| ACH4.3 | **Demostrado (vía QH2 = b), aceptada)** | Ver la decisión de abajo. La matriz L30/L32 incluye `STATUS-archive.md` (BUILDER) y `MEMORIES-archive.md` (OPTIMIZER); ya no contradice `builder.md` L18 ni `optimizer.md` L33/L43 |
| ACH4.4 | **Demostrado** | Ver la verificación de la compactación |
| ACH4.5 | No aplica | QH1 = a) |
| ACH5.1 | Demostrado | `dev-stack.md` L22, bajo NSwag (L21) |
| ACH5.2 | No aplica (cerrado por el operador) | "Cerrar como revertida". OPT-012 `revertida` en el resumen de `MEMORIES.md` y en el archivo; `test-stack.md` sin OPT-012 (0), como corresponde |
| ACH5.3 | Demostrado (sección distinta, aceptada) | `test-stack.md` L25 |
| ACH5.4 | Demostrado | `.github/doc/common/` sin diff |
| ACH5.5 | Demostrado | Lo aplicó el operador (`5352828`) |

Resumen: 19 demostrados (4 con desviación aceptada: ACH2.1, 3.4, 4.3 y 5.3) y 4 que no aplican (ACH1.5, 2.4, 4.5 y 5.2). Ninguno sin demostrar. Coincide con BUILDER.

### Verificación de la compactación (ACH4.4)

Comparación sin CR de `git show HEAD:.github/agent-harness/MEMORIES.md` frente a `MEMORIES.md` y `MEMORIES-archive.md` del árbol de trabajo.

| Comprobación | Resultado |
| --- | --- |
| Filas `OPT-` en `HEAD` | 41, sin duplicados |
| IDs del resumen (`MEMORIES.md` L7-29) frente a los del archivo | 22 = 22, conjuntos idénticos |
| IDs del resumen frente a los de la tabla de detalle (L34+, 19) | Ningún ID se repite entre ambas |
| Unión archivo + detalle frente a `HEAD` | Idéntica; `seq OPT-001..041` sin huecos; sin renumeración |
| Filas archivadas copiadas literalmente de `HEAD` (`grep -vxFf`) | 22/22 literales, 0 discrepancias |
| Filas de detalle conservadas en `MEMORIES.md`, literales respecto a `HEAD` | 19/19 literales |
| Filas `HEAD` que no están en archivo ni detalle | 0 |
| `pendiente` | 1 (OPT-035), sigue en `MEMORIES.md` con todo su detalle y sin cambios |
| Estado del resumen frente al archivo | 20 `aplicada` y 2 `revertida` (OPT-012, OPT-019) en ambos |

La línea base de 31 IDs del ciclo 1 estaba mal contada (son 41 en `HEAD` `c64838d`). El recuento de BUILDER es correcto.

### Decisión sobre ACH4.3

Se acepta. El texto del AC presuponía QH2 = a) (la opción por defecto, sin respuesta), pero lo que pretendía (que la regla no contradiga los permisos) se cumple con la opción b), y el plan ya la contemplaba (`04-hierarchical-summary.md` L30). El operador eligió b) al autorizar la edición de la matriz y aprobar "Sí, compactar". Además, b) corrige la contradicción R1 del ciclo 1, que a) dejaba sin resolver para `STATUS-archive.md`.

### Cambio externo en `STATUS.md`

`stat` da la última escritura de `STATUS.md` a las 11:41:53Z, el mismo segundo que el `Fin` de BUILDER: no hay ninguna escritura posterior. `git diff --ignore-cr-at-eol --stat` da lo mismo que el diff normal (144+/8-), así que no hay un cambio solo de finales de línea. Las 8 líneas eliminadas respecto a `HEAD` son la cabecera `PLANNED` y las filas 1-2 de la secuenciación, lo esperado. No queda ninguna instantánea anterior para comparar; aun así, todos los valores de "Ciclo 2 - Comandos y resultados" y "Evidencia por criterio" coinciden con las comprobaciones de REVIEWER. La evidencia no cambia.

### Hallazgos

| # | Gravedad | Hallazgo |
| --- | --- | --- |
| R5 | No bloqueante | `00-overview-and-open-questions.md` L58 sigue registrando QH2 como "sin respuesta: a)". Conviene que PLANNER u operador anoten b) para que la decisión quede trazada |
| R6 | No bloqueante | La regla nueva (`AGENTS.md` L75) archiva solo filas con `Efecto observado` final, pero el archivo contiene filas con efecto `sin datos` (OPT-001 a 004) o "sigue sin aplicarse" (OPT-013, 017, 021, que este plan aplica ahora junto con OPT-014). Son copias literales, así que no se pierde información, pero el efecto de OPT-013/014/021 tendrá que anotarse en `MEMORIES-archive.md` |
| R3 | No bloqueante (abierto desde el ciclo 1) | `CLAUDE.md` L46: filtro sin variantes localizadas |
| R1, R2 | Resueltos | Matriz y "append-only" coherentes con `optimizer.md`/`builder.md`; textos e IDs añadidos |

Bloqueos: ninguno.
