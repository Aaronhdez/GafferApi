# Optimization Stats

| Cycle/Feature | Token Friction | Root Cause | Optimization Applied | Estimated Token Impact |
| :--- | :--- | :--- | :--- | :--- |
| Facility create | Extra turns before coding due to in-cycle workflow/governance edits. | Process and feature work were mixed in one run. | Split preparation work (workflow/docs/agent alignment) into a dedicated pre-cycle lane. | 12-18% fewer planning/delegation tokens per feature cycle. |
| Facility create | Repeated long requirement prompt submissions increased context duplication. | Missing intake normalization and kickoff gate. | Introduce a mandatory one-shot feature brief template plus planner ACK gate. | 8-14% fewer prompt/context tokens at cycle start. |
| Facility create | Initial optimizer file-path discovery required fallback searches. | Legacy Doc/* expectation mismatched active .github/doc/* path. | Standardize DOC_ROOT=.github/doc in agent docs and optimization workflow. | 4-7% fewer post-cycle search/navigation tokens. |
