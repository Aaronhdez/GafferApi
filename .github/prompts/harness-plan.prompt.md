---
name: harness-plan
description: "Create a scoped harness plan from an ID and specifications."
argument-hint: "ID del plan y especificaciones (texto o ruta)"
agent: planner
---

Follow [AGENTS.md](../../AGENTS.md) and the [harness contract](../agent-harness/README.md).

Plan ID: `${input:planId}`

Specifications (text or path): `${input:specs}`

Create only the plan artifacts permitted to PLANNER. Surface unresolved questions in `00-*.md`; do not implement product changes.