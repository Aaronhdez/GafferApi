# Retrospective Memories

| Date | Cycle/Feature | Error/Friction | Root Cause | Prevention Rule |
| :--- | :--- | :--- | :--- | :--- |
| 2026-08-13 | Facility create | Pre-feature workflow adjustments (scaffolding alignment, agent tuning, doc relocation) consumed delivery turns before implementation. | Governance/process edits were executed inside the same feature cycle. | Run governance/scaffolding/doc-structure changes in a separate prep cycle; start feature cycle only after a stable conventions checkpoint. |
| 2026-08-13 | Facility create | Facility requirements were sent repeatedly before planning kickoff. | No enforced single intake template and no planner acknowledgment gate. | Use one normalized feature brief template and require planner ACK before delegation/execution. |
| 2026-08-13 | Facility create | Optimizer first-pass lookup targeted Doc/* while active convention is .github/doc/*. | Path convention drift between legacy agent text and active repository structure. | Define a single DOC_ROOT alias (.github/doc) and reference that alias across planner/orchestrator/optimizer instructions. |
