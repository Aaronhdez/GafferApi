# Testing Stack

## Test Platform

- Test project: `Gaffer.Api.Tests`
- Target framework: `net10.0`
- Test SDK: `Microsoft.NET.Test.Sdk`
- Runner integration: `NUnit3TestAdapter`

## Test Frameworks and Libraries

- Primary test framework: NUnit
- Assertions: AwesomeAssertions
- Mocking/substitution: NSubstitute
- Coverage collector: coverlet.collector
- Integration host testing: Microsoft.AspNetCore.TestHost
- Data access used in tests: Dapper (indirectly, via `Gaffer.Api`'s `SqlConnectionProvider`); the test project no longer references `MySql.Data` directly (removed as an unused direct dependency)

## Test Types Observed in Repository History

- Unit tests
- Acceptance tests
- Integration tests
- Shared fixture-based setup patterns

## Testing Strategy Notes

- Repository commit history reflects TDD cadence (`red` -> `green` -> `refactor`).
- Features are validated with a double loop:
  - Acceptance loop for behavior-level verification.
  - Unit loop for focused logic verification.
- MySQL-backed tests require a configured connection string (User Secrets or environment variable, no password in `testSettings.json`); they are never skipped, and fail with a guidance message if it is missing. Details: [`doc/features/technical/database-configuration.md`](../../../doc/features/technical/database-configuration.md).
