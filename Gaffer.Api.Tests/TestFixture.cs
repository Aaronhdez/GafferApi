using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api.Tests;

[SetUpFixture]
public class TestFixture
{
    public static IConfigurationRoot Configuration;
    public static HttpClient Client;
    public static TestServer TestServer;
    public static UseCasesMock UseCasesMock;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        UseCasesMock = new UseCasesMock();
        Configuration = PrepareConfiguration(new ConfigurationBuilder()).Build();
        TestServer = await InitializeApiServer(Configuration["ApiUrl"], UseCasesMock);
        Client = InitializeHttpClient(TestServer, Configuration["ApiUrl"]);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Client?.Dispose();
        TestServer?.Dispose();
    }

    private static HttpClient InitializeHttpClient(TestServer testServer, string? baseAddress)
    {
        if (baseAddress == null) throw new NullReferenceException(nameof(baseAddress));
        Client = testServer.CreateClient();
        Client.BaseAddress = new Uri(baseAddress);
        return Client;
    }

    private static async Task<TestServer> InitializeApiServer(string? baseAddress, UseCasesMock useCasesMock)
    {
        if (baseAddress == null) throw new NullReferenceException(nameof(baseAddress));

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer()
                    .UseEnvironment("Test")
                    .UseUrls(baseAddress)
                    .ConfigureAppConfiguration((builderContext, config) => { PrepareConfiguration(config); })
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(useCasesMock.CreatePlayerCommandHandler);
                        services.AddSingleton(useCasesMock.SearchPlayersQueryHandler);
                        services.AddSingleton(useCasesMock.DeletePlayerCommandHandler);
                        services.AddSingleton(useCasesMock.UpdatePlayerCommandHandler);
                        services.AddHttpContextAccessor();
                    })
                    .UseStartup<Startup>();
            })
            .Build();
        await host.StartAsync();
        return host.GetTestServer();
    }

    private static IConfigurationBuilder PrepareConfiguration(IConfigurationBuilder configurationBuilder)
    {
        return configurationBuilder.SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("testsettings.json", optional: false);
    }
}