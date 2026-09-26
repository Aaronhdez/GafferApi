---
name: harness-optimize
description: "Analyze cycle evidence and propose token-usage or workflow optimizations."
argument-hint: "Ruta de la carpeta del plan"
agent: optimizer
---

Follow [AGENTS.md](../../AGENTS.md) and the [harness contract](../agent-harness/README.md).

Plan folder: `${input:planPath}`

Analyze the completed cycle, distinguish measurements from estimates, and record proposals only. Edit shared memories only for proposals explicitly approved by the operator.