# Token Usage Log

Measured token consumption per agent invocation, sourced from the AI Insights extension. Owned by `@optimizer`; other agents only report the line, never edit this file.

Line format reported by agents:
`[Tokens|Agent:<name>|Invocation:<short-purpose>|In:<n>|Out:<n>|Total:<n>|Source:ai-insights]` (`n/a` when AI Insights has no data; never estimated).

| Date | Cycle/Feature | Agent | Invocation | Input | Output | Total | Source |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 2026-09-21 | Facility CRUD completion | planner | plan-facility-crud | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | orchestrator | launch-facility-crud-plan | n/a | n/a | n/a | n/a |
| 2026-09-21 | Facility CRUD completion | developer | facility-repository-crud | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | tester | facility-repository-tests | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | developer | facility-repository-transaction | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | developer | facility-search-query | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | developer | facility-delete-update-commands | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | developer | facility-controller-endpoints | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | tester | facility-crud-tests | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | developer | facility-nswag-regen | n/a | n/a | n/a | token-watch |
| 2026-09-21 | Facility CRUD completion | documenter | facility-crud-documentation | n/a | n/a | n/a | ai-insights |
| 2026-09-21 | Facility CRUD completion | documenter | facility-crud-documentation-write | n/a | n/a | n/a | ai-insights |
