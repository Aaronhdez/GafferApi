# Agent Harness Rules

This file is the normative source for the repository harness. The harness README and agent/prompt files describe operation and link back here; they must not redefine these rules.

## Common Rules

- Preserve changes made by the user or other contributors. Never discard unrelated work.
- Work only within the approved plan scope and make the smallest coherent change.
- Do not claim success without executable evidence. Record unavailable checks and concrete blockers.
- Agents never create branches, commit, merge, or push. The operator creates the branch and runs commits manually.
- Keep chat responses to at most six concise bullets covering result, validation, blockers, and next step. Put details in the plan files; prefer tables and bullets over prose.
- Every agent or subagent invocation starts with `[Agent:<name>|Mode:<agent|subagent>|Invocation:<short-purpose>]`.

## Project Restrictions

- NSwag-generated `Gaffer.Api.Client` artifacts are outside BUILDER and REVIEWER scope. Do not edit, version, audit, or verify generated output. The operator regenerates and versions it when closing the work; source/configuration changes required by the plan remain in scope.
- MySQL-backed tests must not be skipped by default. Run them against the operator's local MySQL database in Docker. If Docker or the database is unavailable, report the exact blocker in the plan rather than claiming the tests passed.
- The operator creates the feature branch and runs commits manually. Agents do not perform these Git operations.
- Permanent project lessons: update `Gaffer.Api.Tests/TestFixture.cs` DI registrations in the same increment as new handler registrations; after a .NET SDK or target-framework upgrade, check `nswag.json`'s target framework before client generation.
- Documentation uses `doc/features/` for functional material and `doc/features/technical/` for technical material. Keep the established document format where applicable: overview, Mermaid diagram, methods table, dependencies, and usage examples. Update affected existing documents rather than creating duplicates.
- The maximum is six development cycles for a plan. No seventh cycle may start.

## Roles and Permissions

The permission matrix is normative. Frontmatter tool availability does not expand the file scope stated here.

| Agent | Tools | Subagents | May edit | Must not |
| --- | --- | --- | --- | --- |
| PLANNER | `read`, `search`, `edit` | None | `.github/plans/<ID>/`: numbered plan files, initial `STATUS.md`, initial `REVIEW.md`, and empty `usage/` | Edit product code/tests; invent requirements; commit/push |
| BUILDER | `read`, `search`, `edit`, `execute` | None | Planned product code/tests, plan `STATUS.md` and `STATUS-archive.md`, and `usage/BUILDER.md` | Touch generated client output; edit `usage/REVIEWER.md` or `OPTIMIZATION.md`; self-approve; commit/push; exceed plan scope |
| REVIEWER | `read`, `search`, `edit`, `execute` | None | Plan `REVIEW.md`, `STATUS.md`, and `usage/REVIEWER.md` | Correct product code/tests; touch or verify generated client output; edit `usage/BUILDER.md`; commit/push |
| OPTIMIZER | `read`, `search`, `edit`, `web`, `execute` (execute only for `usage-slice`) | None | Plan `OPTIMIZATION.md`, `.github/agent-harness/MEMORIES.md` and `.github/agent-harness/MEMORIES-archive.md`, subject to approval rules below | Edit code, agent/prompt files, `AGENTS.md`, usage logs, or plan state; apply its own proposals; use unapproved information sources |
| DOCUMENTER | `read`, `search`, `edit`, `execute` | None | `doc/features/`, `doc/features/technical/`, and plan `STATUS.md` to mark `DONE` | Edit product code/tests, agents/prompts, or document behavior not demonstrated in `REVIEW.md`; commit/push |
| ORCHESTRATOR | `read`, `search`, `execute`, `agent` | PLANNER, BUILDER, REVIEWER, OPTIMIZER, DOCUMENTER | No files | Decide acceptance criteria; edit files; start another cycle or documentation without explicit human approval; continue after `BLOCKED` or the cycle limit |

## Plans and Evidence

- A development plan lives in `.github/plans/<ID>/`.
- PLANNER creates numbered `00-*.md`, `01-*.md`, etc. Each step states its objective, file/folder scope, verifiable acceptance criteria, and how to demonstrate them. Open questions belong in `00-*.md` and are not resolved by invention.
- Steps follow the repository's double test loop in `.github/doc/work-guidelines.md`: acceptance, unit, then refactor with revalidation.
- PLANNER initializes `STATUS.md` at `PLANNED` / cycle 0, an empty `REVIEW.md`, and an empty `usage/` directory.
- `STATUS.md` records state, cycle, per-agent `Inicio` and `Fin` in ISO 8601 UTC, completed steps, commands/results, and blockers. It also records changed documentation at completion.
- Hierarchical summary: at cycle start, BUILDER collapses each `APPROVED` cycle with no pending corrections to a row in a summary index at the top of `STATUS.md` (`Ciclo | Paso | Veredicto | AC(s) relevantes | commit/hash`); full detail stays only for the current and previous cycle; older detail moves verbatim to `STATUS-archive.md` in the plan folder. OPTIMIZER applies the same rule to `OPTIMIZATION.md`: full detail only for the 2 most recent cycles, older ones collapsed to a summary row (`Ciclo | Paso | Veredicto | ID(s) de propuesta | Ahorro esperado | Estado de aprobación`). Git history keeps the rest (OPT-013, OPT-021).
- `REVIEW.md` records evidence and the reviewer verdict. A criterion without evidence is not approved.
- Allowed plan states are `PLANNED`, `READY_FOR_REVIEW`, `NEEDS_CHANGES`, `APPROVED`, `BLOCKED`, and `DONE`.
- BUILDER checks prerequisites before tests and records commands and outcomes. REVIEWER checks the full diff and Git status, verifies acceptance criteria and red-to-green evidence, and never fixes product code.
- MySQL-backed tests run against local Docker as stated above; do not classify that planned local dependency as an external-service exclusion.

## Human-Controlled Flow

```text
PLANNER -> human approves plan
  -> cycle 1..6: BUILDER -> REVIEWER -> OPTIMIZER -> human checkpoint
       NEEDS_CHANGES + explicit approval -> next cycle, if below limit
       APPROVED + explicit approval -> DOCUMENTER -> DONE
       BLOCKED, limit reached, or human stops -> stop
```

- ORCHESTRATOR is mandatory for the normal workflow. Standalone prompts remain available for explicitly requested, manual one-agent tasks.
- If specifications arrive without a plan, ORCHESTRATOR invokes PLANNER and stops for plan approval.
- Every cycle runs BUILDER, REVIEWER, then OPTIMIZER, regardless of the review verdict.
- Consolidate pending decisions: before starting a cycle, ORCHESTRATOR raises at the checkpoint any criterion of the next step blocked by an unanswered question; before re-invoking PLANNER on an approved step, it gathers every pending ambiguity from `00-*.md` into one question (OPT-017, OPT-020).
- At the end of every cycle, ORCHESTRATOR reports the cycle, verdict, blockers, BUILDER/REVIEWER usage, main optimization proposal, and asks what to do next. It waits for an explicit human decision.
- No next cycle and no DOCUMENTER invocation may begin without explicit human approval at the checkpoint. If the human approves optimization proposals, OPTIMIZER records those proposals before the flow continues.
- Stop on `BLOCKED`, a human stop, or six completed development cycles. At the limit, report the outcome and do not silently begin another cycle.

## Usage and Optimization Memory

- `usage/BUILDER.md` and `usage/REVIEWER.md` are append-only tables, one row per cycle. Each agent edits only its own file.
- Required columns: `Ciclo`, `Tokens entrada`, `Tokens salida`, `Origen del dato`, `Llamadas a herramientas`, `Ficheros leídos (nº y mayores)`, `Comandos con salida grande`, `Observaciones`.
- Use `medido:<herramienta>` only for actual measurements, `estimado` for an explicitly estimated value, and `n/a` when neither is possible. Never present an estimate as a measurement.
- Token Consumer Dashboard is an optional source if installed. Use `tokenConsumer.dataDirectory` when configured; otherwise use `%APPDATA%/Code/User/globalStorage/joynulabedinshokal.token-eater-dashboard/events-*.jsonl`. If the data directory is moved inside the repository, add it to `.gitignore`.
- When Dashboard data is available, OPTIMIZER uses `.github/agent-harness/tools/usage-slice.mjs`; it streams JSONL and must never print raw events. Dashboard Copilot events are session-aggregated and estimated, so do not attribute them across roles in a shared chat session.
- `.github/agent-harness/MEMORIES.md` is append-only and outside plans, except for hierarchical compaction. Only OPTIMIZER edits it, and only after explicit operator approval. All agents may read it.
- Hierarchical summary: once a row is `aplicada`/`revertida` and its `Efecto observado` is final (a measured effect, or `sin datos` explicitly declared not measurable), OPTIMIZER collapses it to one line in a summary table at the top of `MEMORIES.md` (`ID | Plan | Problema (1 línea) | Estado`) and moves the full row verbatim to `.github/agent-harness/MEMORIES-archive.md`. `pendiente` rows keep full detail; IDs are never reused or renumbered (OPT-014).
- `MEMORIES.md` columns: `ID`, `Fecha`, `Plan / ciclo de origen`, `Problema detectado`, `Optimización aprobada`, `Ámbito (agente, prompt, plan, AGENTS.md)`, `Estado (pendiente / aplicada / revertida)`, `Efecto observado`. IDs are unique, sequential `OPT-NNN`. Use `sin datos` for an unmeasured effect.
- OPTIMIZER reviews plan usage, status/review evidence, memories, and only the user-approved sources. It records cycle findings, evidence/origin, cause, concrete proposal, expected savings, and source in `OPTIMIZATION.md`; it proposes but does not apply changes.

## Context and Skills

Read referenced context on demand and in relevant fragments; do not copy its contents into harness files.

| Source | Readers | When |
| --- | --- | --- |
| `.github/doc/common/dev-stack.md`, `.github/doc/common/test-stack.md` | PLANNER, BUILDER, REVIEWER | At execution start |
| `.github/doc/common/*-documentation.md` | BUILDER, REVIEWER | Only when changing a dependency; use official documentation URLs |
| `.github/doc/work-guidelines.md` | PLANNER, BUILDER, REVIEWER | For acceptance/unit/refactor sequencing and review evidence |
| `.github/doc/resources/templates-*/` | BUILDER | When a matching template exists for a new artifact |

| Agent | Skills |
| --- | --- |
| PLANNER | `spec-driven-development`, `planning-and-task-breakdown`, `api-and-interface-design` when a contract is involved |
| BUILDER | `test-driven-development`, `incremental-implementation`, `source-driven-development`, `debugging-and-error-recovery` on failed build/tests, `security-and-hardening` for input/auth work |
| REVIEWER | `code-review-and-quality`, `doubt-driven-development`, `security-and-hardening`, `performance-optimization` when applicable |
| OPTIMIZER | `context-engineering` |
| DOCUMENTER | `documentation-and-adrs` |
| ORCHESTRATOR | None |

Other skills are not assigned; use them only when the operator requests it.

## Documentation

- DOCUMENTER runs only after `APPROVED` and human approval.
- Functional documentation belongs in `doc/features/`; technical documentation belongs in `doc/features/technical/`.
- Follow the established project format where relevant: overview, Mermaid diagram, methods table, dependencies, and usage examples. Inspect and update impacted existing documentation before adding another account of the same behavior.
- Describe only behavior demonstrated by approved code, `STATUS.md`, and `REVIEW.md`. Assess indirect impacts through changed contracts, dependencies, flows, and shared configuration.
- If the approved diff changes package or test-tool references, update the affected `.github/doc/common/*-stack*.md` guidance.
- DOCUMENTER lists created documents and direct/indirect updates in `STATUS.md`, then marks it `DONE`.