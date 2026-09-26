using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api.Tests.TestShared;

/// <summary>
/// Hosts the production <see cref="Startup"/> (services and pipeline) in a TestServer against the
/// test database, for contract checks that the <see cref="TestFixture"/> host does not cover
/// (served OpenAPI documents, authentication pipeline).
/// </summary>
public sealed class StartupTestServer : IAsyncDisposable
{
    private readonly IHost _host;

    private StartupTestServer(IHost host)
    {
        _host = host;
        Client = host.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<StartupTestServer> StartAsync(string connectionString)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
                webHostBuilder.UseEnvironment("Test");
                webHostBuilder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DatabaseConfig:Enabled"] = "true",
                        ["DatabaseConfig:ConnectionString"] = connectionString
                    }));
                webHostBuilder.UseStartup<Startup>();
            })
            .Build();
        await host.StartAsync();
        return new StartupTestServer(host);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}
