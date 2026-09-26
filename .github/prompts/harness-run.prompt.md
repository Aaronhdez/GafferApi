---
name: harness-run
description: "Run the full plan/build/review/optimize/document workflow with mandatory human checkpoints."
argument-hint: "Ruta de la carpeta del plan o especificaciones nuevas; opcional: máximo de ciclos NEEDS_CHANGES"
agent: orchestrator
---

Follow [AGENTS.md](../../AGENTS.md) and the [harness contract](../agent-harness/README.md).

Existing plan folder (or blank if planning from specifications): `${input:planPath}`

Specifications if no plan exists: `${input:specs}`

Maximum development cycles (default 6, never exceed the repository limit): `${input:maxCycles}`

Run ORCHESTRATOR only. Stop for explicit human approval after planning and after every cycle. Never infer approval from silence.