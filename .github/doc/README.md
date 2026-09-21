# Scaffolding Documentation Map

## Overview

This directory contains repository scaffolding artifacts generated from project structure and dependency analysis.

## Workflow and Team Guidelines

- `.github/doc/work-guidelines.md`: Branching workflow, merge policy to `main`, and feature-level double test loop.

## Feature Documentation

- `.github/doc/features/facility-create.md`: Facility creation flow from API request mapping to command handling and SQL persistence.
- `.github/doc/features/facility-crud.md`: Facility select (get by id / list), full-replace update, and delete flows.

## Common Stack Documentation

- `.github/doc/common/dev-stack.md`: Development platform, frameworks, architecture patterns, and build notes.
- `.github/doc/common/dev-stack-documentation.md`: Development dependency catalog with official documentation URIs.
- `.github/doc/common/test-stack.md`: Test frameworks, runners, and testing strategy notes.
- `.github/doc/common/test-stack-documentation.md`: Test dependency catalog with official documentation URIs.

## Development Templates

Located in `.github/doc/resources/templates-dev/`:

- `controller.template.md`
- `command-handler.template.md`
- `query-handler.template.md`
- `repository.template.md`
- `dto.template.md`

## Testing Templates

Located in `.github/doc/resources/templates-test/`:

- `unit-test.template.md`
- `integration-test.template.md`
- `acceptance-test.template.md`
- `test-fixture.template.md`
- `mock-fixture.template.md`

## Intended Usage

1. Start from templates when creating new feature slices.
2. Keep naming and boundaries aligned with project conventions.
3. Update stack documentation whenever dependencies or test tooling changes.
