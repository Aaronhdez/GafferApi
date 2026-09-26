---
description: "Manual run of DOCUMENTER on an APPROVED harness plan."
argument-hint: "<plan folder>"
---

Check that `$1/STATUS.md` is `APPROVED`; if not, stop and say so. Otherwise invoke the `documenter` subagent with the `Agent` tool for plan folder `$1`. Show only its final summary. Do not edit files yourself. Rules: `AGENTS.md`, `CLAUDE.md`, `.github/agent-harness/README.md`.
