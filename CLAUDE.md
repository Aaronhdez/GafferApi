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

`usage-slice` fallback ladder, before dropping to `medido:agent-result`:
- `--repo gaffer-api` returns "sin datos" for every window → retry once with `--repo <plan ID>` (the dashboard tags events by process `cwd`) (OPT-008).
- Still empty with the right `--repo` → retry once shifting the window by the known local UTC offset (e.g. -2h for CEST) (OPT-009).
- A wider bisection shows no events within ±30 min of the literal window (a gap, not a shifted cluster) → stop bisecting and use `medido:agent-result` (OPT-011).

`medido:agent-result` may exclude most turn-by-turn cache; when both sources exist for a cycle, label `agent-result` figures as a lower bound, not an equivalent of `token-consumer` (OPT-023).

After 2 consecutive cycles with no `usage-slice` data, skip the ladder: cite `usage/*.md` as `estimado` and state the gap (OPT-031).

Analyze fresh input separately from cache read/write; in Claude Code cache is most of the context.

## Minimal Reading

`Grep`/`Glob` before `Read`; use `offset`/`limit` on large files; filter `Bash` output (e.g. `dotnet test --verbosity quiet`, failures only).

- Red/green: the process exit code (`$?` / `$LASTEXITCODE`) is the primary criterion; `grep` only extracts failure detail. Console output is localized (`Correctas`/`Con error`, not `Passed`/`Failed`) (OPT-005).
- Warning counts: capture only the MSBuild summary (`dotnet build gaffer-api.sln --no-incremental -v q 2>&1 | grep -E "Warning\(s\)|Error\(s\)|Advertencia\(s\)|Errores"`; the console is localized, so match both languages); keep the full list only when a criterion compares which warnings are new (OPT-006).
- Once a command result is summarized in `STATUS.md`, reference that row; do not reopen or requote the full output later (OPT-007).
- Checking an OPT in `MEMORIES.md`: extract only its row by ID, truncated (`grep "^| OPT-0NN " .github/agent-harness/MEMORIES.md | cut -c1-300`; also `MEMORIES-archive.md` for compacted rows); never `grep` whole rows of the file.
- Adding several methods to an existing file: `Edit` against a stable anchor (e.g. before the class's closing brace), not `head`/`tail` splicing (OPT-018).
- Environment: Git Bash on Windows with no guaranteed Python or other interpreter in `PATH`. Check (`command -v python`) before scripting a mechanical edit, or use `Edit`/`sed` directly (OPT-024).
- Ad-hoc probes (`dotnet run`, `dotnet fsi`): filter the output from the first run (`2>&1 | grep <expected result>` or `| tail -n 40`) and apply OPT-006 to them (OPT-025).
- Timestamps: record `Inicio`/`Fin` with a real UTC clock (`date -u +%Y-%m-%dT%H:%M:%SZ` or `Get-Date -AsUTC -Format o`), never local time with a `Z` suffix (OPT-009). If a run is interrupted and resumed, `Fin` is the time of the last useful work before the cut; record the resumption in a separate row with its own `Inicio`/`Fin`.
