// See https://aka.ms/new-console-template for more information

using Gaffer.Api.Startups;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api;

public class Program {
    public static readonly ProductContext ProductContext = CreateProductContext();
    private const string Development = "Development";
    private const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

    public static void Main(string[] args) => CreateGafferHostBuilder(args).Build().Run();

    /// <summary>Builds the host exactly as <see cref="Main"/> runs it.</summary>
    /// <remarks>Deliberately not named <c>CreateHostBuilder</c>, so hosting tools such as NSwag keep resolving the host through <see cref="Main"/>.</remarks>
    public static IHostBuilder CreateGafferHostBuilder(string[] args) {
        // UseEnvironment goes after ConfigureWebHostDefaults: the web defaults add the raw
        // ASPNETCORE_* variables to the host configuration and would otherwise override it.
        return Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseStartup<Startup>();
            })
            .UseEnvironment(CurrentEnvironmentName());
    }

    /// <summary>Resolves the hosting environment name from the value of <c>ASPNETCORE_ENVIRONMENT</c>.</summary>
    /// <remarks>An absent, empty or whitespace-only value falls back to <c>Development</c>.</remarks>
    public static string ResolveEnvironmentName(string? environmentVariableValue) => string.IsNullOrWhiteSpace(environmentVariableValue) ? Development : environmentVariableValue;

    private static string CurrentEnvironmentName() => ResolveEnvironmentName(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    /// <summary>Creates the product context for the current process environment.</summary>
    public static ProductContext CreateProductContext() {
        return new ProductContext {
            Name = "Gaffer Api",
            Id = "gaffer-api",
            Environment = CurrentEnvironmentName(),
            BootLoggingFile = "startup.json", // You can view your startup logs in this file
            StartupSettingsFileName = "startup-settings.log" // You can view your startup settings in this file
        };
    }
}
