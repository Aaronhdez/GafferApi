# Work Guidelines

## Workflow Model

This repository uses a feature-branch workflow with trunk integration to `main`.

1. Create one branch per feature from `main`.
2. Develop and validate changes inside the feature branch.
3. Merge the feature branch into `main` before the final push.
4. Keep integration small and frequent to reduce merge risk.

## Testing Loop Per Feature

Each feature follows a double test loop:

1. Acceptance test loop:
- Define/adjust acceptance scenarios for the feature.
- Execute failing acceptance tests (`red`).
- Implement behavior until acceptance tests pass (`green`).

2. Unit test loop:
- Define/adjust unit tests for domain/application logic.
- Execute failing unit tests (`red`).
- Implement/refactor until unit tests pass (`green`).

3. Refactor loop:
- Refactor after both loops pass.
- Re-run acceptance and unit tests to avoid regressions.

## Branching and Integration Rules

- `main` is the integration branch.
- Feature branches should be short-lived.
- Commits should be small and focused.
- Prefer deterministic, reproducible test runs before merge.

## Commit Conventions

Observed convention in repository history:

- `red - <scope>: <scenario>`
- `green - <scope>: <scenario>`
- `refactor: <change>`
- `chore: <maintenance>`
- `feat: <feature delivery>`

Where `<scope>` is commonly `Unit`, `Acceptance`, or `Integration`.
