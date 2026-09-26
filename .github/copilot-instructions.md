# Copilot Instructions

Repository policy, project restrictions, permissions, and workflow: [AGENTS.md](../AGENTS.md).

## Agents
- PLANNER: `.github/agents/planner.agent.md` creates scoped plans.
- BUILDER: `.github/agents/builder.agent.md` implements approved steps.
- REVIEWER: `.github/agents/reviewer.agent.md` reviews evidence and changes.
- OPTIMIZER: `.github/agents/optimizer.agent.md` analyzes usage and proposes improvements.
- DOCUMENTER: `.github/agents/documenter.agent.md` documents approved behavior.
- ORCHESTRATOR: `.github/agents/orchestrator.agent.md` runs the human-gated workflow.

Every agent and subagent invocation starts with `[Agent:<name>|Mode:<agent|subagent>|Invocation:<short-purpose>]`.