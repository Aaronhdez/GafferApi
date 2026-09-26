---
name: harness-build
description: "Implement approved steps from a harness plan."
argument-hint: "Ruta de la carpeta del plan"
agent: builder
---

Follow [AGENTS.md](../../AGENTS.md) and the [harness contract](../agent-harness/README.md).

Plan folder: `${input:planPath}`

Implement only approved scope, record verification and blockers in the plan, and finish `READY_FOR_REVIEW`. Do not self-approve.