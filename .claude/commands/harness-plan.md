---
description: "Manual run of PLANNER: creates a harness plan from specifications."
argument-hint: "<plan ID> <specifications text or path>"
---

Invoke the `planner` subagent with the `Agent` tool. Plan ID: `$1`. Plan folder: `.github/plans/$1/`. Specifications: `$ARGUMENTS` (everything after the ID).

Show only its final summary. Do not edit files yourself. Rules: `AGENTS.md`, `CLAUDE.md`, `.github/agent-harness/README.md`.
