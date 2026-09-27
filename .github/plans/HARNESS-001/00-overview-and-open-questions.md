# HARNESS-001 - Aplicación de las optimizaciones aprobadas OPT-005 a OPT-022

## Decisiones del operador (literales, transmitidas por ORCHESTRATOR)

- "Aprobar todas" (OPT-005 a OPT-022, ya registradas en `.github/agent-harness/MEMORIES.md`).
- Reparto: "las de docs de stack (.github/doc/common) van al plan; las del harness las aplico yo" (yo = el operador).
- Ningún rol puede editar `CLAUDE.md`, `AGENTS.md` ni `.claude/agents/*`.

## Reparto de las OPT

| OPT | Ámbito | Quién | Dónde se planifica |
| --- | --- | --- | --- |
| 005, 006, 007, 018 | `CLAUDE.md` "Minimal Reading" (018: edición de ficheros) | Operador (manual) | Paso 01 |
| 008, 009, 011 | `CLAUDE.md` "Measured Usage (Claude)" + `.claude/agents/reviewer.md` (009) | Operador (manual) | Paso 02 |
| 017 + 020 (consolidadas) | `AGENTS.md` "Human-Controlled Flow" (+ opcional `.claude/commands/harness-run.md`) | Operador (manual) | Paso 03 |
| 013 + 014 + 021 (consolidadas) | `AGENTS.md` "Plans and Evidence" y "Usage and Optimization Memory" (+ compactación única de `MEMORIES.md` por OPTIMIZER) | Operador (manual) + OPTIMIZER (compactación, con aprobación) | Paso 04 |
| 010, 012, 022 | `.github/doc/common/dev-stack.md` / `test-stack.md` | **Pregunta abierta QH3** (ningún rol tiene permiso claro) | Paso 05 |
| 015 | `.gitattributes` | BUILDER de TECHDEBT-004 | `TECHDEBT-004/01` (absorbida) |
| 016, 019 | Notas de `MySql.Data` en `dev-stack.md` | Se revisan en TECHDEBT-004 tras la migración de driver | Dependencia: `TECHDEBT-004/03` (evidencia AC3.6) y cierre (Q7 de TECHDEBT-004) |

## Solapes detectados y consolidación propuesta

| Solape | Motivo | Propuesta |
| --- | --- | --- |
| OPT-017 / OPT-020 | Misma causa (decisiones pendientes no consolidadas) en dos puntos del flujo: re-invocar PLANNER e iniciar ciclo. OPT-020 declara que "extiende el ámbito de OPT-017". | Una sola viñeta en `AGENTS.md` que cubra ambos puntos (paso 03). |
| OPT-013 / OPT-014 / OPT-021 | El mismo patrón "Hierarchical Summary" aplicado a `OPTIMIZATION.md` (013), `MEMORIES.md` (014) y `STATUS.md` (021). | Una regla para `STATUS.md` + `OPTIMIZATION.md` y otra para `MEMORIES.md`, ambas en `AGENTS.md` (paso 04). |
| OPT-008 / OPT-009 / OPT-011 | Tres reintentos encadenados de `usage-slice` (otro `--repo`, desplazamiento horario, regla de corte). | Una secuencia ordenada de reintentos bajo el punto 1 de "Measured Usage (Claude)" (paso 02). |
| OPT-005 / OPT-006 | Ambas nacen de la salida localizada de .NET/MSBuild. | Viñetas contiguas con los literales en español e inglés (paso 01). |
| OPT-009 (parte BUILDER) | `.claude/agents/builder.md` L18 ya usa `date -u +%Y-%m-%dT%H:%M:%SZ`. | Solo falta `.claude/agents/reviewer.md` (L18 y L25). |
| OPT-014 vs regla "append-only" | `AGENTS.md` ("Usage and Optimization Memory") dice que `MEMORIES.md` es append-only; compactar filas lo contradice. | El paso 04 modifica también esa frase. |

## Secuenciación: un paso = un ciclo

| Ciclo | Paso | Fichero | OPT |
| --- | --- | --- | --- |
| 1 | 01 | `01-claude-minimal-reading.md` | 005, 006, 007, 018 |
| 2 | 02 | `02-claude-measured-usage.md` | 008, 009, 011 |
| 3 | 03 | `03-agents-consolidated-questions.md` | 017 + 020 |
| 4 | 04 | `04-hierarchical-summary.md` | 013 + 014 + 021 |
| 5 | 05 | `05-stack-doc-notes.md` | 010, 012, 022 |
| 6 | - | - | Reserva |

- En el ciclo N solo se aplica y verifica el paso N (más correcciones del ciclo anterior).
- Qué rol hace cada ciclo depende de QH0 (los pasos 01-04 los aplica el operador; ningún agente puede editar esos ficheros).
- No hay código de producto: el doble bucle se reduce a "red" = `grep` que demuestra que el texto no está, "green" = el mismo `grep` lo encuentra, y "refactor" = revisar que no se duplica ni contradice otra regla (lectura dirigida de la sección).

## Dependencia con TECHDEBT-004

- **Pasos 01-04: pueden ir en paralelo** (no tocan ficheros de TECHDEBT-004). Recomendación: aplicarlos **antes del ciclo 1 de TECHDEBT-004** o entre ciclos, nunca durante un ciclo en curso, porque cambian las reglas que siguen BUILDER/REVIEWER/OPTIMIZER/ORCHESTRATOR. El paso 04 introduce la compactación de `STATUS.md`: si entra a mitad de TECHDEBT-004, se aplica desde el ciclo siguiente.
- **Paso 05: depende de TECHDEBT-004.** OPT-012 necesita la evidencia de `TECHDEBT-004` AC3.6 (tipo de excepción de `MySqlConnector`); `dev-stack.md` lo edita también DOCUMENTER al cerrar TECHDEBT-004 (cambio de driver, OPT-016/OPT-019). Para no editar dos veces las mismas líneas, el paso 05 va después del ciclo 3 de TECHDEBT-004 y coordinado con su cierre (QH4).

## Preguntas abiertas

| ID | Paso | Pregunta | Opciones | Recomendación PLANNER (no vinculante) | Bloquea |
| --- | --- | --- | --- | --- | --- |
| QH0 | Todos | ¿Cómo se ejecuta este plan si ningún agente puede editar los ficheros de los pasos 01-04? `AGENTS.md` exige BUILDER → REVIEWER → OPTIMIZER por ciclo. | a) Fuera del flujo de ciclos: el operador aplica los pasos 01-04 cuando quiera y un único REVIEWER (prompt manual) verifica con los `grep` de cada paso. b) Un ciclo por paso: el operador aplica el paso N; BUILDER solo ejecuta los `grep` de verificación y los registra en `STATUS.md` (sin editar nada más); REVIEWER verifica; OPTIMIZER registra `aplicada`. c) Agrupar 01-04 en un solo ciclo con el esquema de b). | c): son cambios de texto sin código; un ciclo evita tres rondas de BUILDER/REVIEWER/OPTIMIZER. Si el operador prefiere un paso por ciclo (petición de ORCHESTRATOR), b). | **Sí, antes del ciclo 1** |
| QH1 | 01-04 | ¿Se replican los cambios en los ficheros de Copilot (`.github/copilot-instructions.md`, `.github/agents/*.agent.md`, `.github/prompts/harness-*.prompt.md`)? | a) No: `AGENTS.md` es común a ambos; los cambios de `CLAUDE.md` y `.claude/*` son específicos de Claude. b) Sí, los equivalentes (p. ej. 005/006/007/018 en `copilot-instructions.md`; 009 en `reviewer.agent.md`). | a) para los pasos 03-04 (van en `AGENTS.md`); b) solo si el operador usa Copilot en los próximos planes. | No (sin respuesta: a)) |
| QH2 | 04 | Detalle de la compactación. | a) Solo historial de Git (sin fichero nuevo). b) `MEMORIES-archive.md` (ningún rol tiene hoy permiso para crearlo; habría que añadirlo al alcance de OPTIMIZER en `AGENTS.md`). | a). | No (sin respuesta: a)) |
| QH3 | 05 | ¿Qué rol aplica OPT-010, OPT-012 y OPT-022 (notas en `.github/doc/common/*-stack.md`)? Según `AGENTS.md`: BUILDER solo edita "planned product code/tests"; DOCUMENTER edita `*-stack*.md` solo "if the approved diff changes package or test-tool references"; OPTIMIZER no puede. Precedente: en TECHDEBT-003 (AC2.10) BUILDER añadió la nota OPT-016 como parte de un paso aprobado. | a) El operador, a mano, como el resto de este plan. b) DOCUMENTER de TECHDEBT-004 al cerrar: cubre OPT-012 (el cambio de paquete lo justifica); OPT-010 y OPT-022 no tienen esa justificación. c) Un AC de un paso de TECHDEBT-004 que haga BUILDER (precedente AC2.10, fuera de la letra de la matriz). d) Ampliar la matriz de `AGENTS.md` (lo hace el operador) para que DOCUMENTER aplique notas de stack aprobadas por OPTIMIZER. | OPT-012 → b). OPT-010 y OPT-022 → a) (o d) si el operador quiere que no vuelva a pasar). PLANNER no lo resuelve. | **Sí, el paso 05** |
| QH4 | 05 | Orden respecto al cierre de TECHDEBT-004. | a) Después de `TECHDEBT-004` `DONE`. b) Entre el ciclo 3 de TECHDEBT-004 y su cierre, avisando a DOCUMENTER para que no duplique. | a). | No (sin respuesta: a)) |
| QH5 | 01 | Ubicación de OPT-018 (no es una regla de lectura). | a) `CLAUDE.md` "Minimal Reading" (aplica a quien edite código, en la práctica BUILDER). b) `.claude/agents/builder.md`, paso 4 de "Work". c) Skill `incremental-implementation` (fichero compartido `.github/skills/`, no específico del repo). | a) o b); c) no, porque las skills son genéricas. | No (sin respuesta: a)) |

## Cierre de cada paso

- Tras verificar un paso, OPTIMIZER (modo registro, con aprobación explícita del operador) cambia a `aplicada` el `Estado` de las OPT del paso en `MEMORIES.md`. Ni el operador ni otro rol lo hacen por él.
- Las OPT absorbidas por TECHDEBT-004 (015, 016, 019, y 012 si QH3 = b)) se marcan cuando ese plan lo demuestre.

## Restricciones

- Ningún agente edita `CLAUDE.md`, `AGENTS.md`, `.claude/agents/*`, `.claude/commands/*` ni los ficheros de Copilot. Los textos propuestos son orientativos: el operador los puede reescribir; lo verificable es el contenido mínimo de cada AC.
- Sin cambios de producto ni de tests. Sin operaciones de Git. Máximo 6 ciclos.
