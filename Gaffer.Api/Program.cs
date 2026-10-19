using Gaffer.Api.Startups;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api;

public class Program {
    public static readonly ProductContext ProductContext = CreateProductContext();
    private const string Development = "Development";
    private const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

    public static void Main(string[] args) => CreateGafferHostBuilder(args).Build().Run();

    public static IHostBuilder CreateGafferHostBuilder(string[] args) {
        return Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseStartup<Startup>();
            })
            .UseEnvironment(CurrentEnvironmentName());
    }

    public static string ResolveEnvironmentName(string? environmentVariableValue) => string.IsNullOrWhiteSpace(environmentVariableValue) ? Development : environmentVariableValue;

    private static string CurrentEnvironmentName() => ResolveEnvironmentName(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    public static ProductContext CreateProductContext() {
        return new ProductContext {
            Name = "Gaffer Api",
            Id = "gaffer-api",
            Environment = CurrentEnvironmentName(),
            BootLoggingFile = "startup.json",
            StartupSettingsFileName = "startup-settings.log"
        };
    }
}
