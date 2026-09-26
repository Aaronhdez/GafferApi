namespace Gaffer.Api.Tests.Unit.Startups;

/// <summary>
/// Runs the production <see cref="Startup.ConfigureServices"/> with the database disabled,
/// so configuration tests inspect the real registrations without touching MySQL.
/// </summary>
internal static class StartupServices
{
    public static ServiceProvider Build()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DatabaseConfig:Enabled"] = "false" })
            .Build();
        var services = new ServiceCollection();
        new Startup(configuration).ConfigureServices(services);
        return services.BuildServiceProvider();
    }
}
