using AwesomeAssertions;
using Gaffer.Api.Startups;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Tests.Integration;

public class TestServerDependenciesShould
{
    private const int ProductionRepositories = 2;
    private const int ProductionHandlers = 13;

    // The expected services come from the production startups, so a handler registered only in
    // ActionsStartup is covered here without touching TestFixture.
    [Test]
    public async Task ResolveEveryRepositoryAndHandlerRegisteredInProduction()
    {
        var repositories = RegisteredServiceTypes(services => services.ConfigureRepositories(new ConfigurationBuilder().Build()));
        var handlers = RegisteredServiceTypes(services => services.ConfigureActions());
        await using var scope = TestFixture.TestServer.Services.CreateAsyncScope();

        repositories.Should().HaveCount(ProductionRepositories);
        handlers.Should().HaveCount(ProductionHandlers);
        foreach (var serviceType in repositories.Concat(handlers))
        {
            scope.ServiceProvider.GetService(serviceType).Should().NotBeNull($"{serviceType.Name} is registered in production");
        }
    }

    private static List<Type> RegisteredServiceTypes(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.Select(descriptor => descriptor.ServiceType).ToList();
    }
}
