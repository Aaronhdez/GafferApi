---
description: "Manual run of OPTIMIZER on a harness plan (analysis, or registration of approved proposals)."
argument-hint: "<plan folder> [approved/rejected proposal IDs]"
---

Invoke the `optimizer` subagent with the `Agent` tool for plan folder `$1`. If more arguments follow, run it in registration mode and pass them verbatim: `$ARGUMENTS`. Show only its final summary. Do not edit files yourself. Rules: `AGENTS.md`, `CLAUDE.md`, `.github/agent-harness/README.md`.
