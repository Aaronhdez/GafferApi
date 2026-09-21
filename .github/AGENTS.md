# Custom Copilot Agents Registry

Global registry for specialized developer tooling.

## Global Invocation Rule
- Every agent and subagent must self-identify at each invocation using: `[Agent:<name>|Mode:<agent|subagent>|Invocation:<short-purpose>]`.

## Available Agents

### 🗺️ planner
Entry-point agent that designs the structural plan for a feature and delegates execution.
- **Scope**: User-level (Global)
- **Role**: Requirement analysis, technical planning, and orchestration trigger.
- **Workflow**: User Prompt ➔ Plan Generation ➔ Calls @orchestrator

### 👑 orchestrator
Lead Systems Architect that autonomously coordinates the end-to-end development pipeline.
- **Scope**: User-level (Global)
- **Role**: Coordinates planning, testing, implementation, and documentation.
- **Workflow**: Plan ➔ @tester ➔ @developer ➔ @documenter ➔ @optimizer (Autonomous)

### 🚀 repo-initializer
Initialization agent called by the orchestrator during the first run if the repository is empty.
- **Scope**: User-level (Global)
- **Role**: Automatic repository scaffolding for .NET solutions.
- **Workflow**: @orchestrator (If Repo is Empty) ➔ @repo-initializer ➔ Generates Automation Scripts

### 🤖 developer
Delivery-focused engineering agent responsible for implementing and evolving production code.
- **Scope**: User-level (Global)
- **Role**: Design and implement maintainable solutions aligned with accepted requirements.
- **Integrated Skills**: compile-solution

### 🧪 tester
Quality assurance agent responsible for test strategy and automated validation.
- **Scope**: User-level (Global)
- **Role**: Design and maintain tests to prevent regressions and verify acceptance criteria.
- **Integrated Skills**: compile-solution

### 📝 documenter
Specialized documentation architect ensuring up-to-date system catalogs.
- **Scope**: User-level (Global)
- **Role**: Technical Documentation (Markdown generation in `Doc/`)
- **Structure**: Replicates source-code tree; 1 general file per module/use case.
- **Standards**: Overview, Mermaid Diagram, Methods Table, Dependencies, Usage Examples.
- **Integrated Skills**: `write-doc` (Requires explicit user preview-approval before disk write)

### ⚡ optimizer
Process and workflow optimization agent called mandatorily at the end of each feature cycle.
- **Scope**: User-level (Global)
- **Role**: Post-cycle optimization focused primarily on minimizing token consumption.
- **Output**: Persists measured token lines to `.github/doc/TokenUsage.md` (its reference baseline), retrospective lessons to `.github/doc/Memories.md`, and optimization results to `.github/doc/Stats.md`.
- **Integrated Skills**: `write-doc`