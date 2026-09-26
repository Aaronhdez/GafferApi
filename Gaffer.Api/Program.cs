// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Gaffer.Api.Startups;

namespace Gaffer.Api
{
    public class Program
    {
        public static readonly ProductContext ProductContext = GetProductContext();
        private const string Development = "Development";

        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseEnvironment(ProductContext.Environment ?? Development)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, _) => SetConfigurationProviders(context));
                    webBuilder.UseStartup<Startup>();
                });
            host.Build().Run();
        }

        private static ProductContext GetProductContext()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("environment.json", true, false)
                .Build();
            return new ProductContext
            {
                Name = "Gaffer Api",
                Id = "gaffer-api",
                Environment = "Development",
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