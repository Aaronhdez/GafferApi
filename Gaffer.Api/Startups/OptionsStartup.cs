using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class OptionsStartup {
    public static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration,
        string startupSettingsLogFile) => SaveStartupConfigurationToLocal(configuration, startupSettingsLogFile);

    private static void SaveStartupConfigurationToLocal(IConfiguration configuration, string fileName) {
        try {
            var enableSaveStartupConfigurationToLocal = configuration["EnableSaveStartupConfigurationToLocal"];
            if (string.IsNullOrEmpty(enableSaveStartupConfigurationToLocal) ||
                !bool.Parse(enableSaveStartupConfigurationToLocal)) {
                return;
            }

            var root = (IConfigurationRoot)configuration;
            File.WriteAllText(fileName, root.GetDebugView());
        } catch {
        }
    }
}
