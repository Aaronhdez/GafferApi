---
name: harness-document
description: "Document an approved plan after the operator authorizes the documentation phase."
argument-hint: "Ruta de la carpeta del plan"
agent: documenter
---

Follow [AGENTS.md](../../AGENTS.md) and the [harness contract](../agent-harness/README.md).

Plan folder: `${input:planPath}`

Run only after an `APPROVED` review and explicit operator approval. Document demonstrated behavior and mark the plan `DONE` when complete.