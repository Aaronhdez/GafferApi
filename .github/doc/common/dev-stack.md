# Development Stack

## Platform

- Runtime: .NET (`net10.0` target projects)
- SDK pinning: `global.json` (`9.0.0`, `rollForward: latestMajor`, prerelease allowed)
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
- JSON handling with `System.Text.Json` + `Newtonsoft.Json`
- Data access with Dapper
- MySQL connectivity via `MySqlConnector`/`MySql.Data`
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
- NSwag build target executes OpenAPI client generation on Windows non-Debug builds.
