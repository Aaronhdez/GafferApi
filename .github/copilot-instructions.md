# Global Copilot Instructions

# Repository Overview and Agent Ecosystem
This repository is governed by a specialized multi-agent ecosystem. To ensure consistency, code quality, and architectural integrity, **you must route tasks through the appropriate specialized agents** rather than attempting to write all code directly.

## Agent Roster
- **`@planner`**: **Entry Point.** Always invoke the planner first for new features. It will analyze requirements and generate a technical plan.
- **`@orchestrator`**: The autonomous pipeline coordinator. It will automatically delegate tasks to the developer, tester, and documenter based on the planner's output.
- **`@developer`**: Solely responsible for writing and refactoring production code (API, Domain, Application, Infrastructure).
- **`@tester`**: Solely responsible for QA automation and automated test implementation.
- **`@documenter`**: Solely responsible for maintaining Markdown files and Mermaid diagrams within the `Doc/` directory.
- **`@scaffolding`**: Responsible for repository scaffolding analysis, stack/dependency documentation, template generation, and agent documentation context alignment.
- **`@net-repo-initializer`**: Used only when initializing or scaffolding projects according to core architectural rules.
- **`@optimizer`**: Mandatory final step after every feature cycle to minimize token consumption, record retrospective lessons in `Doc/Memories.md`, and record optimization results in `Doc/Stats.md`.
- **`@committer`**: Responsible for committing and pushing changes to the repository. It will only commit after all agents have completed their tasks and the user has approved the final output.

## 2. Core Architectural Rules
All agents and interactions must strictly adhere to the principles established in `Common/stack-info.md`.

## 3. Workflow Directives
All agents workflow directives must strictly adhere to the principles established in `Common/workflow-directives.md`.

### Invocation Identity Rule
- Every agent and subagent invocation must start with an identity marker using this format: `[Agent:<name>|Mode:<agent|subagent>|Invocation:<short-purpose>]`.

## 4. Style Directives
All coding and testing style rules must strictly adhere to `Common/style-directives.md`. Avoid duplicating style guidance in this file.