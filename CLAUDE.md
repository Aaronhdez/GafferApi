@AGENTS.md

# Claude Code Harness Notes

`AGENTS.md` (imported above) is the normative source. This file only adds what differs in Claude Code.

## Roles in Claude Code

| Role | Lives in | Claude tools |
| --- | --- | --- |
| ORCHESTRATOR | `/harness-run` command, main session (subagents cannot spawn subagents) | `Agent`, `Read`, `Grep`, `Glob`; `Bash` only for `git status` / `git diff --stat` |
| PLANNER | `.claude/agents/planner.md` | `Read, Grep, Glob, Edit, Write` |
| BUILDER | `.claude/agents/builder.md` | `Read, Grep, Glob, Edit, Write, Bash` |
| REVIEWER | `.claude/agents/reviewer.md` | `Read, Grep, Glob, Edit, Write, Bash` |
| OPTIMIZER | `.claude/agents/optimizer.md` (model `sonnet`) | `Read, Grep, Glob, Edit, Write, WebFetch, WebSearch, Bash` (`Bash` only for `usage-slice`) |
| DOCUMENTER | `.claude/agents/documenter.md` (model `sonnet`) | `Read, Grep, Glob, Edit, Write, Bash` |

- Harness root is shared with Copilot: `.github/agent-harness/`; plans in `.github/plans/<ID>/`.
- Human checkpoint = ORCHESTRATOR ends its turn with the checkpoint question; it resumes only on the operator's next message.
- Subagents cannot ask the operator; open questions go into plan files and their final summary.
- Skills are read by path (`.github/skills/<name>/SKILL.md`) only when their trigger applies; never copied.

## Measured Usage (Claude)

Subagents cannot see their own token usage, so `usage/*.md` rows are `estimado` or `n/a`. OPTIMIZER records measured data only in `OPTIMIZATION.md`, in this order of preference:
1. `medido:token-consumer`: `node .github/agent-harness/tools/usage-slice.mjs` with the `Inicio`/`Fin` windows from `STATUS.md`.
2. `medido:agent-result`: per-subagent totals the ORCHESTRATOR passes along.
3. `medido:transcript`: `~/.claude/projects/<project>/*.jsonl`, filtered, never read whole.

Analyze fresh input separately from cache read/write; in Claude Code cache is most of the context.

## Minimal Reading

`Grep`/`Glob` before `Read`; use `offset`/`limit` on large files; filter `Bash` output (e.g. `dotnet test --verbosity quiet`, failures only).
