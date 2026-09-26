# Testing Dependencies Documentation

| Package Name | Version | Documentation URI | Status |
| --- | --- | --- | --- |
| AwesomeAssertions | 9.5.0 | https://www.nuget.org/packages/AwesomeAssertions | Referenced |
| coverlet.collector | 6.0.4 | https://github.com/coverlet-coverage/coverlet | Referenced |
| Dapper | 2.1.79 | https://github.com/DapperLib/Dapper | Referenced |
| Microsoft.AspNetCore.TestHost | 10.0.10 | https://learn.microsoft.com/aspnet/core/test/integration-tests | Referenced |
| Microsoft.NET.Test.Sdk | 17.14.0 | https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test | Referenced |
| NSubstitute | 6.0.0 | https://nsubstitute.github.io/help/getting-started/ | Referenced |
| NUnit | 4.3.2 | https://docs.nunit.org/ | Referenced |
| NUnit.Analyzers | 4.7.0 | https://github.com/nunit/nunit.analyzers | Referenced |
| NUnit3TestAdapter | 5.0.0 | https://docs.nunit.org/articles/vs-test-adapter/Index.html | Referenced |

Removed in TECHDEBT-001: `MySql.Data` (26.7.0) was an unused direct reference in `Gaffer.Api.Tests.csproj` — database access in tests goes through `Gaffer.Api`'s `SqlConnectionProvider` via the project reference, not a direct `MySql.Data` usage in test code.
