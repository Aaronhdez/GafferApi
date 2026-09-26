using System.Data;
using AwesomeAssertions;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Unit.ConnectionProviders;

public class SqlConnectionProviderShould
{
    [Test]
    public async Task BeDisposedWithItsScopeLeavingTheConnectionClosed()
    {
        await using var serviceProvider = BuildScopedServiceProvider();
        var scope = serviceProvider.CreateAsyncScope();
        var provider = (SqlConnectionProvider)scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        await provider.QueryScalarAsync<int>("SELECT 1;");

        await scope.DisposeAsync();

        provider.IsDisposed.Should().BeTrue();
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task CloseAnOpenConnectionWhenItsScopeIsDisposed()
    {
        await using var serviceProvider = BuildScopedServiceProvider();
        var scope = serviceProvider.CreateAsyncScope();
        var provider = (SqlConnectionProvider)scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var connectionOpened = new TaskCompletionSource();
        var releaseTransaction = new TaskCompletionSource();
        var pendingTransaction = provider.ExecuteInTransactionAsync(async () =>
        {
            await provider.QueryScalarAsync<int>("SELECT 1;");
            connectionOpened.SetResult();
            await releaseTransaction.Task;
        });
        await connectionOpened.Task;
        provider.ConnectionState.Should().Be(ConnectionState.Open);

        await scope.DisposeAsync();

        provider.IsDisposed.Should().BeTrue();
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
        releaseTransaction.SetResult();
        await FluentActions.Awaiting(() => pendingTransaction).Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task NotThrowWhenDisposedTwice()
    {
        var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        await provider.QueryScalarAsync<int>("SELECT 1;");

        await provider.DisposeAsync();
        var disposeAgain = async () => await provider.DisposeAsync();
        var disposeSynchronously = () => provider.Dispose();

        await disposeAgain.Should().NotThrowAsync();
        disposeSynchronously.Should().NotThrow();
    }

    [Test]
    public async Task ReportAReachableDatabaseWithoutDependingOnItsSchema()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);

        var isUp = await provider.TestConnection();

        isUp.Should().BeTrue();
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task ReportAnUnreachableDatabaseWithoutThrowing()
    {
        await using var provider = new SqlConnectionProvider(TestConfiguration.UnreachableDatabaseConnectionString);

        var testConnection = async () => await provider.TestConnection();

        (await testConnection.Should().NotThrowAsync()).Subject.Should().BeFalse();
    }

    private static ServiceProvider BuildScopedServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDatabaseConnectionProvider>(_ =>
            new SqlConnectionProvider(TestDefaultValues.TestConnectionString));
        return services.BuildServiceProvider();
    }
}
