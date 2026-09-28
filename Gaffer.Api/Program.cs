// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Gaffer.Api.Startups;

namespace Gaffer.Api
{
    public class Program
    {
        public static readonly ProductContext ProductContext = CreateProductContext();
        private const string Development = "Development";
        private const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

        public static void Main(string[] args)
        {
            CreateGafferHostBuilder(args).Build().Run();
        }

        /// <summary>Builds the host exactly as <see cref="Main"/> runs it.</summary>
        /// <remarks>Deliberately not named <c>CreateHostBuilder</c>, so hosting tools such as NSwag keep resolving the host through <see cref="Main"/>.</remarks>
        public static IHostBuilder CreateGafferHostBuilder(string[] args)
        {
            // UseEnvironment goes after ConfigureWebHostDefaults: the web defaults add the raw
            // ASPNETCORE_* variables to the host configuration and would otherwise override it.
            return Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, _) => SetConfigurationProviders(context));
                    webBuilder.UseStartup<Startup>();
                })
                .UseEnvironment(CurrentEnvironmentName());
        }

        /// <summary>Resolves the hosting environment name from the value of <c>ASPNETCORE_ENVIRONMENT</c>.</summary>
        /// <remarks>An absent, empty or whitespace-only value falls back to <c>Development</c>.</remarks>
        public static string ResolveEnvironmentName(string? environmentVariableValue)
        {
            return string.IsNullOrWhiteSpace(environmentVariableValue) ? Development : environmentVariableValue;
        }

        private static string CurrentEnvironmentName()
        {
            return ResolveEnvironmentName(Environment.GetEnvironmentVariable(EnvironmentVariableName));
        }

        /// <summary>Creates the product context for the current process environment.</summary>
        public static ProductContext CreateProductContext()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("environment.json", true, false)
                .Build();
            return new ProductContext
            {
                Name = "Gaffer Api",
                Id = "gaffer-api",
                Environment = CurrentEnvironmentName(),
                BootLoggingFile = "startup.json", // You can view your startup logs in this file
                StartupSettingsFileName = "startup-settings.log" // You can view your startup settings in this file
            };
        }

        private static void SetConfigurationProviders(WebHostBuilderContext context)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(context.HostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("environment.json", true);
            configurationBuilder.AddDevelopmentSettings(context);
        }
    }

    public static class ConfigurationBuilderExtensions
    {
        public static void AddDevelopmentSettings(this IConfigurationBuilder builder, WebHostBuilderContext context)
        {
            if (!context.HostingEnvironment.IsDevelopment()) return;
            builder.AddJsonFile("appsettings.Development.json", true, true);
        }
    }
}