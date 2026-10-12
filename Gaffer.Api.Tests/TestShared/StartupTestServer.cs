using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api.Tests.TestShared;

/// <summary>
/// Hosts the production <see cref="Startup"/> (services and pipeline) in a TestServer against the
/// test database, for contract checks that the <see cref="TestFixture"/> host does not cover
/// (served OpenAPI documents, authentication pipeline, health endpoints).
/// </summary>
public sealed class StartupTestServer : IAsyncDisposable {
    private readonly IHost host;

    private StartupTestServer(IHost host) {
        this.host = host;
        Client = host.GetTestClient();
    }

    public HttpClient Client { get; }

    /// <summary>Root services of the host, to inspect what the production startup registered.</summary>
    public IServiceProvider Services => host.Services;

    public static Task<StartupTestServer> StartAsync(string connectionString)
        => StartAsync(enabled: true, connectionString);

    /// <summary>Starts the host with the given <c>DatabaseConfig</c> (e.g. disabled, without a connection string or unreachable).</summary>
    public static async Task<StartupTestServer> StartAsync(bool enabled, string? connectionString) {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder => {
                webHostBuilder.UseTestServer();
                webHostBuilder.UseEnvironment("Test");
                webHostBuilder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> {
                        ["DatabaseConfig:Enabled"] = enabled.ToString(),
                        ["DatabaseConfig:ConnectionString"] = connectionString
                    }));
                webHostBuilder.UseStartup<Startup>();
            })
            .Build();
        await host.StartAsync();
        return new StartupTestServer(host);
    }

    public async ValueTask DisposeAsync() {
        Client.Dispose();
        await host.StopAsync();
        host.Dispose();
    }
}
