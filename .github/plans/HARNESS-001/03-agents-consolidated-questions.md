# Paso 03 (ciclo 3) - `AGENTS.md` "Human-Controlled Flow": OPT-017 + OPT-020 (consolidadas)

Aplicación manual del operador. Ejecución según QH0.

## Objetivo

Antes de re-invocar a PLANNER sobre un plan aprobado (017) y antes de invocar a BUILDER para empezar un ciclo (020), ORCHESTRATOR reúne todas las preguntas pendientes que afectan al paso previsto y las plantea en una sola pregunta de checkpoint.

## Fichero, sección y texto propuesto

### `AGENTS.md`, `## Human-Controlled Flow` (obligatorio)

Nueva viñeta a continuación de "No next cycle and no DOCUMENTER invocation may begin without explicit human approval...":

```markdown
- Consolidated questions: before re-invoking PLANNER on an approved plan and before invoking BUILDER to start a cycle, ORCHESTRATOR reads the open questions in `00-*.md` and the blockers in `STATUS.md`, and asks every question that blocks the planned step or any of its criteria in a single checkpoint question. It does not start the cycle, or re-invoke PLANNER, after each individual operator message. (OPT-017, OPT-020)
```

### `.claude/commands/harness-run.md`, `## Flow`, punto 2 (opcional, operativo)

Prefijo al punto 2:

```markdown
2. **Cycle N** (only after explicit approval): first check `00-*.md`/`STATUS.md` for open questions blocking step N or its criteria; if any, ask them in the checkpoint instead of starting. Then `builder` → `reviewer` → `optimizer`, always all three. ...
```

`AGENTS.md` es la fuente normativa; si se añade en `harness-run.md`, debe remitir a ella sin redefinirla.

## Criterios de aceptación

| ID | Criterio |
| --- | --- |
| ACH3.1 | `grep -n "OPT-017, OPT-020" AGENTS.md` → 1 línea dentro de `## Human-Controlled Flow`. |
| ACH3.2 | La viñeta cubre los dos momentos (re-invocación de PLANNER e inicio de ciclo) y exige una sola pregunta consolidada (lectura dirigida). |
| ACH3.3 | No contradice la tabla de permisos de ORCHESTRATOR (solo lee y pregunta; no decide criterios ni edita). |
| ACH3.4 | Si se aplica la parte opcional: `grep -n "open questions blocking" .claude/commands/harness-run.md` → 1. Si QH1 = b): equivalente en `.github/agents/orchestrator.agent.md` y `.github/prompts/harness-run.prompt.md`. |

## Cómo demostrarlo

- `grep` antes/después en `STATUS.md`.
- Efecto (no es AC): ningún ciclo de TECHDEBT-004 que termine en `NEEDS_CHANGES` solo por una pregunta ya conocida al empezar (patrón H1 de TECHDEBT-003 ciclo 3).
