# Development Stack

## Platform

- Runtime: .NET (`net10.0` target projects)
- SDK pinning: `global.json` (`10.0.302`, `rollForward: latestFeature`, prerelease allowed)
- Language: C# (`ImplicitUsings` enabled, nullable reference types enabled)

## Solution and Projects

- Solution: `gaffer-api.sln`
- Production projects:
  - `Gaffer.Api` (ASP.NET Core Web API)
  - `Gaffer.Api.Client` (client package)
  - `Gaffer.Api.Migrations` (migration runner)

## Frameworks and Core Components

- ASP.NET Core hosting and middleware pipeline
- API versioning via Asp.Versioning
- OpenAPI/Swagger via NSwag
  - Nullability note (OPT-010): the served spec is Swagger 2.0 without `x-nullable`, so NJsonSchema/NSwag treats any property not listed in `required` as nullable (`OpenApiDocument.FromJsonAsync` + `JsonSchemaProperty.IsNullable(document.SchemaType)`). Cite this instead of dumping the schema with `--verbosity detailed`.
- JSON handling with Newtonsoft.Json only (`AddNewtonsoftJson` MVC formatter, `StringEnumConverter`); `System.Text.Json` is no longer configured for MVC input/output (tests still use it only to read HTTP responses)
- Data access with Dapper
- MySQL/MariaDB connectivity: `Gaffer.Api` and `Gaffer.Api.Migrations` use `MySqlConnector` 2.6.1 (`Gaffer.Api` migrated from `MySql.Data` in TECHDEBT-004 step 03). The local and CI database is MariaDB (`mariadb:latest`).
  - Driver note: `MySqlConnection.DisposeAsync()` closes the connection and raises `Component.Disposed`, so `SqlConnectionProvider` disposes asynchronously. Cancelling a token aborts a query already running on the server (`KILL QUERY`). The earlier `MySql.Data` 26.7.0 limitations (OPT-016, TECHDEBT-003 AC3.4) no longer apply.
- Build: `TreatWarningsAsErrors` is `true` for `Gaffer.Api` (TECHDEBT-004 step 01); `.gitattributes` fixes `*.cs` to LF.
- Hosting environment: read from `ASPNETCORE_ENVIRONMENT`, `Development` when absent or blank (step 02).
- CI: `.github/workflows/ci.yml` (step 05); see [`doc/features/technical/ci-github-actions.md`](../../../doc/features/technical/ci-github-actions.md).
- Database migrations with FluentMigrator

## Architecture Patterns Observed

- Startup-based composition root (`Startup` class)
- Layered modules by responsibility:
  - Commands
  - Queries
  - Domain
  - Infrastructure
  - Shared
- Repository abstractions in shared/infrastructure boundaries

## Build and Packaging Notes

- `Gaffer.Api.Client` configured for package generation on Release builds.
- NSwag build target executes OpenAPI client generation on Windows non-Debug builds (`Net100` runtime, matching the `net10.0` target framework).
- `Gaffer.Api.Client` is versioned with Semantic Versioning 2.0.0: the root `Directory.Build.props` sets `GlobalPackageVersion` (default `1.0.0`, override with `/p:GlobalPackageVersion=<version>`) and fails the build/pack if the version is not valid SemVer 2.0.0. This applies only to `Gaffer.Api.Client`; keep `Gaffer.Api/nswag.json`'s `infoVersion` aligned with it. Rule: incompatible contract change → MAJOR, compatible addition → MINOR, fix without contract change → PATCH. Details: [`doc/features/technical/api-client-versioning.md`](../../../doc/features/technical/api-client-versioning.md).
