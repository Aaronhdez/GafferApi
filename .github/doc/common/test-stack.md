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
  - Full test server (OPT-022): to dump or compare routes actually served (e.g. `/swagger/*/swagger.json`), use `Gaffer.Api.Tests/TestShared/StartupTestServer.cs`; `TestFixture`'s internal `TestServer` does not expose Swagger routes and returns 404.
  - Served OpenAPI dump helper (OPT-034): `Gaffer.Api.Tests/Acceptance/ServedOpenApiDocumentDump.cs` is an `[Explicit]` test (never run by the normal suite) that writes the `swagger.json` served by `StartupTestServer` byte for byte to the path in `GAFFER_SWAGGER_DUMP_PATH`; capture before and after a change and compare with `cmp` or sha256 to prove the contract did not change. Example (Git Bash): `GAFFER_SWAGGER_DUMP_PATH=/path/swagger.json dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~ServedOpenApiDocumentDump"`. Without the variable it fails and writes nothing.
  - `StartupTestServer.StartAsync(bool enabled, string? connectionString)` and `Services` start the production pipeline with the database disabled, without connection string or unreachable (used for `/health/*`); `StartAsync(string)` is unchanged.

## Testing Strategy Notes

- Repository commit history reflects TDD cadence (`red` -> `green` -> `refactor`).
- Features are validated with a double loop:
  - Acceptance loop for behavior-level verification.
  - Unit loop for focused logic verification.
- MySQL-backed tests require a configured connection string (User Secrets or environment variable, no password in `testSettings.json`); they are never skipped, and fail with a guidance message if it is missing. Details: [`doc/features/technical/database-configuration.md`](../../../doc/features/technical/database-configuration.md).
- MySQL-backed tests run against MariaDB (local `Quionis_DB` container; the `mariadb:latest` service in CI). The CI also runs `dotnet format gaffer-api.sln --verify-no-changes --severity info --exclude Gaffer.Api.Client/` (SDK tool, no package) after the `-warnaserror` build; run it locally before pushing. Test-class private fields follow the same camelCase-without-underscore rule as production code. CI: [`doc/features/technical/ci-github-actions.md`](../../../doc/features/technical/ci-github-actions.md).
- Database migrations: run `./scripts/run-migrations.ps1` (non-interactive; defaults to `GafferDB_Test`, the only database agents may migrate). `GafferDB` requires `-Target GafferDB -AllowNonTestDatabase` and is an operator action. Exit codes: `0` ok, `1` migration failure, `2` missing connection string, `3` non-test target without the switch. One-time machine setup (secrets, migrations, tests): `./scripts/setup-local-environment.ps1`.
