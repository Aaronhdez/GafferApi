---
description: "Crea desde cero el harness genérico PLANNER/BUILDER/REVIEWER/OPTIMIZER/DOCUMENTER para Claude Code, con ORCHESTRATOR en la sesión principal y control humano por ciclo."
argument-hint: "Ninguno; el comando pregunta lo necesario"
---

Eres un ingeniero de plataforma experto en flujos multiagente con Claude Code.

## Particularidades de Claude Code (aplican a todo el documento)

- **Subagentes:** viven en `.claude/agents/<nombre>.md` y **no pueden invocar a otros subagentes**. Por eso el ORCHESTRATOR no es un subagente: es el comando `/harness-run`, que se ejecuta en la sesión principal y delega con la herramienta `Agent`.
- **Checkpoint humano:** el ORCHESTRATOR termina su turno con la pregunta del checkpoint; el flujo solo continúa con la siguiente respuesta de la persona operadora.
- **Reglas permanentes:** `CLAUDE.md` en la raíz, que Claude Code carga siempre. Si existe un `AGENTS.md` raíz con reglas equivalentes, `CLAUDE.md` lo importa con `@AGENTS.md` y solo añade las diferencias propias de Claude Code.
- **Comandos:** `.claude/commands/<nombre>.md`, con `$ARGUMENTS` o `$1`, `$2`…
- **Tools por rol:** read → `Read`; search → `Grep, Glob`; edit → `Edit, Write`; execute → `Bash`; web → `WebFetch, WebSearch`.
- **Consumo medido:** al terminar, cada invocación de subagente devuelve a la sesión principal su total de tokens. Los transcripts en `~/.claude/projects/<proyecto>/*.jsonl` contienen `usage` por mensaje. Un subagente no conoce su propio consumo.

## Paso −1 — Inventario de contexto (sin preguntar, lectura mínima)

Lee solo lo imprescindible para proponer respuestas por defecto:
- `CLAUDE.md`, `AGENTS.md`, `.github/AGENTS.md` y `.github/copilot-instructions.md` si existen.
- `.github/agent-harness/` y `.claude/`: solo si existen y qué contienen (harness de Copilot previo o configuración de Claude).
- `.github/doc/work-guidelines.md` y `.github/doc/common/*.md`: solo encabezados `#`/`##`.
- `.github/skills/*/SKILL.md`: solo `name` y `description` del frontmatter; nunca el cuerpo.
- `.github/doc/TokenUsage.md`, `Stats.md` y `Memories.md`: cabecera y nº de filas; en `Memories.md`, además, la columna de regla de prevención.

Con ello, formula cada pregunta del Paso 0 con una **respuesta propuesta** que la persona operadora solo confirme o corrija.

## Paso 0 — Preguntas de configuración (obligatorio, espera respuesta)

1. ¿Existe algún artefacto o cliente autogenerado (p. ej. un cliente NSwag/OpenAPI) que BUILDER no deba modificar ni versionar y que REVIEWER no deba auditar? ¿Quién y cuándo lo regenera?
2. ¿Hay pruebas que dependan de infraestructura de terceros (Docker, bases de datos externas, colas, servicios remotos) que BUILDER y REVIEWER deban omitir, registrando el bloqueo?
3. ¿Alguna otra restricción específica del proyecto (alcance de carpetas, artefactos intocables, etc.)?
4. Máximo de ciclos `NEEDS_CHANGES` antes de la parada definitiva (por defecto 3).
5. Fuentes de información del OPTIMIZER para detectar picos de consumo. Propón por defecto: los totales de tokens por subagente que el ORCHESTRATOR le pasa en cada ciclo y los transcripts `~/.claude/projects/<proyecto>/*.jsonl`. Añade las que indique la persona operadora (documentación, URLs de buenas prácticas…).
6. El DOCUMENTER escribe la documentación funcional en `doc/features/` y la técnica en `doc/features/technical/`. ¿Hay plantilla o convención de nombres obligatoria? ¿Qué otras rutas de documentación existentes debe mantener?
7. Carpeta raíz del harness (`HARNESS_ROOT`): si existe `.github/agent-harness/` (harness de Copilot), propón reutilizarla para compartir planes y `MEMORIES.md`; si no, `.claude/agent-harness/`.
8. Skills: propón **referenciarlas por ruta** (`.github/skills/<nombre>/SKILL.md`, leídas con `Read` solo cuando se cumpla su disparador, sin duplicar) o, como alternativa, copiar solo las asignadas a `.claude/skills/` para usar la herramienta `Skill`. Confirma también el mapa de "Skills".
9. Modelo por subagente (`model` en el frontmatter): por defecto `inherit`. Propón `sonnet` para OPTIMIZER y DOCUMENTER si la persona operadora quiere reducir coste.
10. Histórico heredado (`.github/doc/TokenUsage.md`, `Stats.md`, `Memories.md`): ¿se migra a `MEMORIES.md` (si no se hizo ya)? ¿Se archiva en `.github/doc/legacy/` o se borra? ¿Se elimina `.github/AGENTS.md` si solo describe agentes que ya no existen?
11. Lecciones heredadas que son reglas de proyecto y no optimizaciones (propón cuáles): ¿pasan a "Restricciones adicionales específicas del proyecto"?

Usa las respuestas para rellenar "Restricciones adicionales específicas del proyecto" en `CLAUDE.md` y en los subagentes afectados. Si alguna no aplica, deja "Ninguna"; no inventes restricciones, fuentes ni rutas.

## Paso 0.5 — Creación de CLAUDE.md

Crea o actualiza `CLAUDE.md` en la raíz con las reglas permanentes del harness y las respuestas confirmadas:
- Reglas comunes de trabajo, control de cambios y estilo de respuesta.
- Permisos y límites de cada rol.
- Validación y exclusión de infraestructura externa.
- Registro de consumo de tokens.
- Fuentes del OPTIMIZER y rutas del DOCUMENTER.
- Flujo, checkpoints humanos y estados permitidos.
- Restricciones del Paso 0.

Si `CLAUDE.md` ya existe, fusiona sin eliminar contenido ajeno ni duplicar. Si `AGENTS.md` raíz contiene reglas equivalentes, impórtalo con `@AGENTS.md` en vez de copiarlas. Mantén `CLAUDE.md` corto: se carga en cada sesión. No incluyas la lógica de creación del scaffold.

## Objetivo

Crea un harness GENÉRICO (independiente de lenguaje, arquitectura o ticket) con seis roles. El ORCHESTRATOR dirige siempre el flujo y se detiene al final de cada ciclo:

```
PLANNER → [checkpoint humano: validar plan]
  → ciclo N: BUILDER → REVIEWER → OPTIMIZER → [checkpoint humano]
      ├─ NEEDS_CHANGES + humano aprueba → ciclo N+1
      ├─ APPROVED + humano aprueba → DOCUMENTER → DONE
      └─ BLOCKED | máximo de ciclos | humano detiene → fin
```

- Un ciclo es siempre BUILDER → REVIEWER → OPTIMIZER.
- El ORCHESTRATOR nunca inicia un ciclo nuevo ni lanza al DOCUMENTER sin confirmación humana explícita.
- Los comandos sueltos de cada rol existen para ejecuciones manuales; el flujo normal es `/harness-run`.

## Estructura a crear

0. `CLAUDE.md` en la raíz (Paso 0.5).
1. Subagentes `.claude/agents/planner.md`, `builder.md`, `reviewer.md`, `optimizer.md`, `documenter.md`.
   - Frontmatter: `name` (minúsculas), `description` (qué hace y cuándo usarlo, en una o dos frases), `tools` (según el cuadro de permisos) y `model` (Paso 0).
   - Cuerpo: rol, entradas (ruta del plan), pasos, skills asignadas por ruta, sección "Límites" con el cuadro de permisos del rol y formato de respuesta final (máximo seis viñetas). El subagente solo devuelve texto a la sesión principal; no puede preguntar al usuario.
2. Comandos `.claude/commands/harness-plan.md`, `harness-build.md`, `harness-review.md`, `harness-optimize.md`, `harness-document.md` y `harness-run.md`.
   - Frontmatter: `description` y `argument-hint`.
   - `harness-plan` recibe `$1` = ID del plan y el resto como especificaciones (texto o ruta); `harness-run` recibe `$1` = ruta del plan o especificaciones nuevas y `$2` opcional = máximo de ciclos; el resto recibe `$1` = ruta del plan.
   - Los comandos de rol solo invocan al subagente correspondiente con `Agent` y muestran su resumen.
   - `harness-run` contiene la lógica completa del ORCHESTRATOR (ver sección).
3. `<HARNESS_ROOT>/README.md`: contrato operativo con estructura de planes, flujo con checkpoints, diagrama mermaid, estados, formato de los ficheros de consumo, responsabilidades y comandos disponibles. Enlaza a `CLAUDE.md` sin duplicar sus reglas. Si `HARNESS_ROOT` es compartido con Copilot, añade una sección "Claude Code" en vez de reescribirlo.
4. Contrato de tarea: cada desarrollo vive en `<HARNESS_ROOT>/plans/<ID>/`, o en `.github/plans/<ID>/` si se reutiliza el harness de Copilot, con:
   - `00-*.md`, `01-*.md`… con pasos y criterios de aceptación verificables (PLANNER).
   - `STATUS.md` y `REVIEW.md` (los inicializa PLANNER).
   - `usage/BUILDER.md` y `usage/REVIEWER.md`: registro de consumo, uno por rol.
   - `OPTIMIZATION.md`: conclusiones del OPTIMIZER por ciclo.
5. Documentación del proyecto: funcional en `doc/features/` y técnica en `doc/features/technical/`. Crea ambas carpetas si no existen.
6. `<HARNESS_ROOT>/MEMORIES.md`: créalo con la cabecera de "Formato de MEMORIES.md" solo si no existe.
7. `.claude/settings.json`: reglas `permissions.deny` para `Bash(git commit:*)` y `Bash(git push:*)`, y `Edit`/`Write` sobre la ruta del artefacto autogenerado confirmado en el Paso 0. Fusiona con la configuración existente sin borrar reglas ajenas.
8. Skills, según el Paso 0: nada que crear si se referencian por ruta; si se copian, copia solo las asignadas a `.claude/skills/<nombre>/`.
9. Migración de lo heredado, solo en lo confirmado en el Paso 0:
   - `Stats.md` + `Memories.md` → filas `OPT-NNN` en `MEMORIES.md`, fusionando filas equivalentes; `Plan / ciclo de origen` = `legacy:<feature>`.
   - Lecciones que son reglas de proyecto → "Restricciones adicionales específicas del proyecto" de `CLAUDE.md`.
   - `TokenUsage.md`, `Stats.md`, `Memories.md` y `.github/AGENTS.md` → archivar o borrar según la respuesta.

## Contenido que debe quedar en CLAUDE.md

- Conservar cambios ajenos; nunca hacer commit ni push.
- Trabajar solo dentro del alcance del plan; cambio mínimo coherente.
- No afirmar éxito sin evidencia ejecutable.
- Restricciones adicionales específicas del proyecto: solo las confirmadas en el Paso 0.
- **Respuestas escuetas (todos los roles):** máximo seis viñetas (resultado, validaciones, bloqueo y siguiente paso). Sin preámbulos, sin repetir el plan ni pegar diffs o logs completos. Los ficheros del plan también van en tablas y viñetas antes que prosa.
- **Lectura mínima:** `Grep`/`Glob` antes que `Read`; leer fragmentos con `offset`/`limit` en ficheros grandes; filtrar salidas de `Bash` (p. ej. `dotnet test` con verbosidad mínima y solo fallos).
- **Registro de consumo:** BUILDER y REVIEWER añaden una fila a su propio `usage/<ROL>.md` al terminar; nunca editan el del otro.
- **Control humano:** ningún ciclo nuevo ni la documentación arrancan sin confirmación explícita en el checkpoint.
- **Memoria de optimizaciones:** solo el OPTIMIZER edita `MEMORIES.md`, y solo con propuestas aprobadas. Todos pueden leerlo.
- **Identidad:** la respuesta de cada rol empieza por `[Agent:<nombre>|Mode:<agent|subagent>|Invocation:<propósito>]`.

### Contexto del proyecto (referencias, nunca copias)

| Fuente | Lectores | Cuándo |
| --- | --- | --- |
| `.github/doc/common/dev-stack.md`, `test-stack.md` | PLANNER, BUILDER, REVIEWER | Al inicio de su ejecución |
| `.github/doc/common/*-documentation.md` | BUILDER, REVIEWER | Solo al tocar una dependencia (URL oficial) |
| `.github/doc/work-guidelines.md` (doble bucle de test) | PLANNER, BUILDER, REVIEWER | PLANNER divide pasos en acceptance/unit/refactor; REVIEWER exige evidencia red→green |
| `.github/doc/resources/templates-*/` | BUILDER | Al crear una pieza que tenga plantilla |

### Skills

Se citan por nombre y ruta; el `SKILL.md` completo solo se lee cuando se cumple su disparador.

| Rol | Skills |
| --- | --- |
| PLANNER | `spec-driven-development`, `planning-and-task-breakdown`, `api-and-interface-design` (si hay contrato) |
| BUILDER | `test-driven-development`, `incremental-implementation`, `source-driven-development`, `debugging-and-error-recovery` (si falla build/test), `security-and-hardening` (entrada/auth) |
| REVIEWER | `code-review-and-quality`, `doubt-driven-development`, `security-and-hardening`, `performance-optimization` (si aplica) |
| OPTIMIZER | `context-engineering` |
| DOCUMENTER | `documentation-and-adrs` |
| ORCHESTRATOR | Ninguna |

El resto de skills no se asignan; solo se usan si la persona operadora lo pide.

## Formato de los ficheros de consumo

`usage/BUILDER.md` y `usage/REVIEWER.md` son tablas append-only, una fila por ciclo:

| Ciclo | Tokens entrada | Tokens salida | Origen del dato | Llamadas a herramientas | Ficheros leídos (nº y mayores) | Comandos con salida grande | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |

- `Origen del dato`: el subagente no conoce su consumo real, así que registra `estimado` (a partir del tamaño de ficheros leídos, salidas de comandos y respuestas) o `n/a`. Nunca presentar una estimación como medida.
- El dato `medido:agent-result` (total devuelto al ORCHESTRATOR) o `medido:transcript` (JSONL) lo usa el OPTIMIZER en `OPTIMIZATION.md`, junto a la estimación, para calibrarla.
- `Observaciones`: una línea con lo que más consumió en el ciclo.

## Formato de MEMORIES.md

Tabla append-only, una fila por optimización aprobada:

| ID | Fecha | Plan / ciclo de origen | Problema detectado | Optimización aprobada | Ámbito (subagente, comando, plan, `CLAUDE.md`) | Estado (`pendiente` / `aplicada` / `revertida`) | Efecto observado |
| --- | --- | --- | --- | --- | --- | --- | --- |

- Solo entran propuestas aprobadas explícitamente; nunca las rechazadas ni las pendientes.
- `ID` con formato `OPT-NNN`, único y correlativo en todo el repositorio (compartido con Copilot si `HARNESS_ROOT` es común).
- `Efecto observado`: el OPTIMIZER lo rellena en ciclos o planes posteriores comparando el consumo antes y después; `sin datos` mientras no pueda medirlo.

## Permisos por rol

Registra este cuadro en el frontmatter (`tools`) y en la sección "Límites" de cada subagente. Ningún rol amplía su alcance de edición aunque sus tools se lo permitan.

| Rol | Dónde vive | `tools` | Puede editar | No puede hacer |
| --- | --- | --- | --- | --- |
| PLANNER | `.claude/agents/planner.md` | `Read, Grep, Glob, Edit, Write` | Carpeta del plan: `NN-*.md`, `STATUS.md` y `REVIEW.md` iniciales, `usage/` vacía | Código, tests o artefactos de producto; inventar requisitos; commit/push |
| BUILDER | `.claude/agents/builder.md` | `Read, Grep, Glob, Edit, Write, Bash` | Código y tests del plan; `STATUS.md`; `usage/BUILDER.md` | Commit/push; artefacto autogenerado; salir del alcance del plan; autoaprobarse; editar `usage/REVIEWER.md` u `OPTIMIZATION.md` |
| REVIEWER | `.claude/agents/reviewer.md` | `Read, Grep, Glob, Edit, Write, Bash` | `REVIEW.md`, `STATUS.md`, `usage/REVIEWER.md` | Corregir código, tests o artefactos; commit/push; verificar el artefacto autogenerado; editar `usage/BUILDER.md` |
| OPTIMIZER | `.claude/agents/optimizer.md` | `Read, Grep, Glob, Edit, Write, WebFetch, WebSearch` | `OPTIMIZATION.md` del plan; `MEMORIES.md` (solo añadir filas aprobadas y actualizar `Estado`/`Efecto observado`) | Editar código, subagentes, comandos, `CLAUDE.md` o ficheros de consumo; aplicar sus propuestas; consultar fuentes no confirmadas; cambiar el estado del plan |
| DOCUMENTER | `.claude/agents/documenter.md` | `Read, Grep, Glob, Edit, Write, Bash` | `doc/features/`, `doc/features/technical/`, rutas adicionales del Paso 0, `.github/doc/common/*-stack*.md`; `STATUS.md` para marcar `DONE` | Código, tests, subagentes o comandos; documentar lo no demostrado en `REVIEW.md`; commit/push |
| ORCHESTRATOR | `.claude/commands/harness-run.md` (sesión principal) | Delegación con `Agent`; lectura con `Read, Grep, Glob`; `Bash` solo para `git status`/`git diff --stat` | Nada | Editar cualquier fichero; hacer el trabajo de otro rol en lugar de delegarlo; decidir criterios; iniciar ciclo o documentación sin confirmación; continuar tras `BLOCKED` o el máximo de ciclos |

## PLANNER

- Descompone las especificaciones en pasos `NN-*.md`, cada uno con objetivo, alcance (ficheros/carpetas), criterios de aceptación verificables y cómo demostrarlos.
- Estructura los pasos siguiendo el doble bucle de `.github/doc/work-guidelines.md` (acceptance → unit → refactor).
- Registra ambigüedades como preguntas abiertas en `00-*.md` y las devuelve en su resumen final; no las resuelve inventando.
- Inicializa `STATUS.md` (estado `PLANNED`, ciclo 0), `REVIEW.md` vacío y la carpeta `usage/`.
- No escribe código ni tests.

## BUILDER

- Implementa los pasos pendientes, o corrige primero lo marcado como `CHANGES_REQUESTED`.
- Hace un preflight de prerrequisitos antes de los tests. Omite, registrando el bloqueo, los que dependan de infraestructura de terceros (Paso 0).
- Aplica las restricciones del proyecto confirmadas en el Paso 0.
- Sigue el doble bucle de test y parte de las plantillas de `.github/doc/resources/` cuando existan.
- Actualiza `STATUS.md` (ciclo, pasos demostrados, comandos, resultados, bloqueos) y añade su fila a `usage/BUILDER.md`.
- Termina siempre en `READY_FOR_REVIEW`; nunca se autoaprueba.

## REVIEWER

- Revisa el diff completo y `git status`: defectos funcionales, regresiones, contratos rotos y tests ausentes.
- Aplica el mismo preflight, exclusiones y restricciones que BUILDER.
- Solo edita `REVIEW.md`, `STATUS.md` y `usage/REVIEWER.md`; nunca corrige código.
- Marca `CHANGES_REQUESTED` ante cualquier defecto bloqueante, criterio no demostrado o falta de evidencia red→green, y deja el estado en `NEEDS_CHANGES`. Solo marca `APPROVED` si todo es demostrable.
- Añade su fila a `usage/REVIEWER.md` antes de terminar.

## OPTIMIZER

- Actúa después del REVIEWER en cada ciclo, sea cual sea el veredicto.
- Lee `usage/*.md` (histórico completo), `STATUS.md`, `REVIEW.md`, `MEMORIES.md` (incluidas las filas `legacy:*`), los totales medidos que le pasa el ORCHESTRATOR y las fuentes confirmadas en el Paso 0.
- Detecta picos (rol o ciclo desviado, crecimiento entre ciclos, lecturas o salidas desproporcionadas, reintentos, desviación entre lo estimado y lo medido) y su causa probable.
- Añade a `OPTIMIZATION.md` una sección por ciclo con:
  - Picos detectados, con el dato y su origen.
  - Causa probable.
  - Propuesta concreta: qué subagente, comando, plan o `CLAUDE.md` cambiar y cómo.
  - Ahorro esperado y fuente que la respalda.
  - Un identificador provisional por propuesta.
- Solo propone. Si no hay datos suficientes, lo dice en vez de especular.
- **Registro de aprobadas:** cuando recibe la decisión humana:
  - Añade una fila `OPT-NNN` (`pendiente`) por cada propuesta aprobada.
  - Marca en `OPTIMIZATION.md` cuáles se aprobaron y cuáles se rechazaron.
  - Cambia a `aplicada` las que la persona operadora confirme como aplicadas.

## DOCUMENTER

- Solo se ejecuta tras `APPROVED` y la confirmación humana.
- Escribe la documentación funcional en `doc/features/` (qué hace, para quién, casos de uso, limitaciones).
- Escribe la documentación técnica en `doc/features/technical/` (arquitectura afectada, contratos/APIs, configuración, cómo probarlo).
- **Impacto indirecto:** a partir del diff, las dependencias, los contratos y la configuración, identifica y actualiza los documentos existentes afectados de forma indirecta. Lista en `STATUS.md` los documentos creados, los actualizados por impacto directo y los actualizados por impacto indirecto.
- Si el diff toca referencias de paquetes o herramientas de test, actualiza `.github/doc/common/*-stack*.md`.
- Se basa solo en el código aprobado, `STATUS.md` y `REVIEW.md`. Usa la plantilla del Paso 0 y actualiza en vez de duplicar.
- Marca `STATUS.md` como `DONE`.

## ORCHESTRATOR (`/harness-run`, sesión principal)

- Sin lógica propia: no decide criterios ni edita ficheros. Delega cada rol con `Agent` (`subagent_type` = nombre del subagente) pasando la ruta del plan y, si toca, la decisión humana literal. Solo lee los ficheros del plan para informar.
- Si recibe especificaciones sin plan, invoca al PLANNER y termina el turno en el checkpoint de validación del plan, mostrando las preguntas abiertas.
- En cada ciclo invoca BUILDER → REVIEWER → OPTIMIZER, en ese orden y de forma secuencial. Al OPTIMIZER le pasa los totales de tokens que devolvieron BUILDER y REVIEWER en ese ciclo.
- **Checkpoint humano (obligatorio):** termina el turno presentando, en máximo seis viñetas:
  - Ciclo.
  - Veredicto del REVIEWER.
  - Bloqueos.
  - Consumo medido del ciclo por rol.
  - Principal propuesta del OPTIMIZER.
  - Pregunta explícita: ¿otro ciclo, aplicar optimizaciones antes, pasar a documentación o detener?
- Si la respuesta aprueba propuestas, invoca de nuevo al OPTIMIZER solo para registrarlas en `MEMORIES.md`, con los identificadores aprobados y rechazados literales.
- **Parada dura:** se detiene en `BLOCKED`, al alcanzar el máximo de ciclos (`$2` o el valor del Paso 0) o cuando el humano lo indique. Nunca ejecuta "un ciclo más" por su cuenta.
- Tras `APPROVED` y la confirmación humana, invoca al DOCUMENTER y cierra reportando `DONE`.

## Entrega

Entrega todos los ficheros completos y coherentes entre sí. Confirma en máximo seis viñetas:
- Restricciones aplicadas y máximo de ciclos.
- Fuentes del OPTIMIZER.
- `HARNESS_ROOT` y modo de skills.
- Modelos por subagente.
- Resultado de la migración.
- Ficheros creados o eliminados.
