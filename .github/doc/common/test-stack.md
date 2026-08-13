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
- Data access used in tests: Dapper, MySql.Data

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
