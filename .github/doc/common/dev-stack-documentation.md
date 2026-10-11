# Development Dependencies Documentation

| Package Name | Version | Documentation URI | Status |
| --- | --- | --- | --- |
| Asp.Versioning.Mvc | 8.1.0 | https://github.com/dotnet/aspnet-api-versioning | Referenced |
| Asp.Versioning.Mvc.ApiExplorer | 8.1.0 | https://github.com/dotnet/aspnet-api-versioning | Referenced |
| Dapper | 2.1.79 | https://github.com/DapperLib/Dapper | Referenced |
| Microsoft.AspNetCore.Mvc.NewtonsoftJson | 10.0.10 | https://learn.microsoft.com/aspnet/core/web-api/advanced/formatting | Referenced |
| Microsoft.Extensions.ApiDescription.Server | 10.0.10 | https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview | Referenced |
| Microsoft.Extensions.Configuration.CommandLine | 10.0.10 | https://learn.microsoft.com/dotnet/core/extensions/configuration-providers | Referenced (Gaffer.Api.Migrations) |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.10 | https://learn.microsoft.com/dotnet/core/extensions/configuration-providers | Referenced (Gaffer.Api.Migrations) |
| Microsoft.Extensions.Configuration.UserSecrets | 10.0.10 | https://learn.microsoft.com/aspnet/core/security/app-secrets | Referenced (Gaffer.Api.Migrations) |
| MySqlConnector | 2.6.1 | https://mysqlconnector.net/ | Referenced (Gaffer.Api, Gaffer.Api.Migrations) |
| NSwag.AspNetCore | 14.6.1 | https://github.com/RicoSuter/NSwag/wiki/AspNetCore-Middleware | Referenced |
| NSwag.MSBuild | 14.6.1 | https://github.com/RicoSuter/NSwag/wiki/NSwag.MSBuild | Referenced (Net100 runtime) |
| Newtonsoft.Json | 13.0.4 | https://www.newtonsoft.com/json/help/html/Introduction.htm | Referenced |
| FluentMigrator | 8.0.1 | https://fluentmigrator.github.io/ | Referenced |
| FluentMigrator.Abstractions | 8.0.1 | https://fluentmigrator.github.io/ | Referenced |
| FluentMigrator.Console | 8.0.1 | https://fluentmigrator.github.io/ | Referenced |
| FluentMigrator.Runner | 8.0.1 | https://fluentmigrator.github.io/ | Referenced |

`MySql.Data` (26.7.0) was replaced by `MySqlConnector` in `Gaffer.Api` in TECHDEBT-004 (step 03).

Removed in TECHDEBT-001 as unused direct references (no code usage found): `AspNetCore.HealthChecks.SqlServer`/`UI.Client`/`Uris` (9.0.0), `ClosedXML` (0.105.0), `Microsoft.ApplicationInsights.AspNetCore` (2.23.0), `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.9), `Microsoft.Extensions.Configuration.Binder`/`DependencyInjection.Abstractions`/`Options` (9.0.9, from `Gaffer.Api`; superseded by the framework-provided net10.0 equivalents), `Microsoft.VisualStudio.Web.CodeGeneration.Design` (9.0.0). `Microsoft.Extensions.Configuration.UserSecrets` for `Gaffer.Api` itself is no longer an explicit package reference (the SDK provides `UserSecretsIdAttribute` support for the target framework); `Gaffer.Api.Migrations` still references it explicitly at 10.0.10.
