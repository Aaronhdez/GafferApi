using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
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

    [Test]
    public async Task CloseButNotReleaseTheConnectionAfterEachOperation()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();

        await provider.QueryScalarAsync<int>("SELECT 1;");
        AssertClosedAndNotDisposed(provider);
        await provider.ExecuteAsync(PlayersInsert, new { Id = id });
        AssertClosedAndNotDisposed(provider);
        (await provider.QueryAsync<Guid>(PlayerById, new { Id = id })).Should().ContainSingle();
        AssertClosedAndNotDisposed(provider);
        await provider.ExecuteInTransactionAsync(() => provider.ExecuteAsync(PlayerDelete, new { Id = id }));
        AssertClosedAndNotDisposed(provider);
        (await provider.QueryScalarAsync<long>(PlayerCount, new { Id = id })).Should().Be(0);
        AssertClosedAndNotDisposed(provider);
    }

    [Test]
    public async Task ReleaseTheConnectionOnlyWhenItsScopeIsDisposed()
    {
        await using var serviceProvider = BuildScopedServiceProvider();
        var scope = serviceProvider.CreateAsyncScope();
        var provider = (SqlConnectionProvider)scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var releases = 0;
        UnderlyingConnectionOf(provider).Disposed += (_, _) => releases++;

        await provider.QueryScalarAsync<int>("SELECT 1;");
        await provider.ExecuteAsync("SELECT 1;");
        await provider.QueryAsync<int>("SELECT 1;");
        await provider.ExecuteInTransactionAsync(() => provider.QueryScalarAsync<int>("SELECT 1;"));
        releases.Should().Be(0);

        await scope.DisposeAsync();

        releases.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task MakeTheWorkOfACommittedTransactionVisible()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();

        await provider.ExecuteInTransactionAsync(() => provider.ExecuteAsync(PlayersInsert, new { Id = id }));

        await using var observer = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        (await observer.QueryScalarAsync<long>(PlayerCount, new { Id = id })).Should().Be(1);
    }

    [Test]
    public async Task RollBackAndPropagateTheFailureOfATransaction()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();

        var transaction = async () => await provider.ExecuteInTransactionAsync(async () =>
        {
            await provider.ExecuteAsync(PlayersInsert, new { Id = id });
            throw new InvalidOperationException("force rollback");
        });

        await transaction.Should().ThrowAsync<InvalidOperationException>();
        (await provider.QueryScalarAsync<long>(PlayerCount, new { Id = id })).Should().Be(0);
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task RunANestedTransactionInsideTheOuterOne()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();

        var transaction = async () => await provider.ExecuteInTransactionAsync(async () =>
        {
            await provider.ExecuteInTransactionAsync(() => provider.ExecuteAsync(PlayersInsert, new { Id = id }));
            provider.ConnectionState.Should().Be(ConnectionState.Open);
            throw new InvalidOperationException("force outer rollback");
        });

        await transaction.Should().ThrowAsync<InvalidOperationException>();
        (await provider.QueryScalarAsync<long>(PlayerCount, new { Id = id })).Should().Be(0);
    }

    [Test]
    public async Task NotRunAQueryWhenItsTokenIsAlreadyCancelled()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);

        var query = async () => await provider.QueryAsync<int>("SELECT 1;", cancellationToken: CancelledToken());

        await query.Should().ThrowAsync<OperationCanceledException>();
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task NotWriteAnythingWhenAnExecuteTokenIsAlreadyCancelled()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();

        var execute = async () => await provider.ExecuteAsync(PlayersInsert, new { Id = id }, CancelledToken());

        await execute.Should().ThrowAsync<OperationCanceledException>();
        await AssertPlayerNotWritten(id);
    }

    [Test]
    public async Task NotWriteAnythingWhenATransactionTokenIsAlreadyCancelled()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();
        var token = CancelledToken();

        var transaction = async () => await provider.ExecuteInTransactionAsync(
            () => provider.ExecuteAsync(PlayersInsert, new { Id = id }), token);

        await transaction.Should().ThrowAsync<OperationCanceledException>();
        await AssertPlayerNotWritten(id);
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task RollBackATransactionWhoseOperationIsCancelled()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();

        var transaction = async () => await provider.ExecuteInTransactionAsync(async () =>
        {
            await provider.ExecuteAsync(PlayersInsert, new { Id = firstId }, cancellation.Token);
            await cancellation.CancelAsync();
            await provider.ExecuteAsync(PlayersInsert, new { Id = secondId }, cancellation.Token);
        }, cancellation.Token);

        await transaction.Should().ThrowAsync<OperationCanceledException>();
        await AssertPlayerNotWritten(firstId);
        await AssertPlayerNotWritten(secondId);
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
    }

    [Test]
    public async Task CloseTheConnectionWhenAQueryFails()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);

        var query = async () => await provider.QueryAsync<int>(InvalidSql);

        await AssertPropagatesTheDriverFailure(query, "syntax");
        AssertClosedAndNotDisposed(provider);
    }

    [Test]
    public async Task CloseTheConnectionWhenAScalarQueryFails()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);

        var query = async () => await provider.QueryScalarAsync<int>(InvalidSql);

        await AssertPropagatesTheDriverFailure(query, "syntax");
        AssertClosedAndNotDisposed(provider);
    }

    [Test]
    public async Task CloseTheConnectionWhenAnExecuteViolatesAConstraint()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();
        await provider.ExecuteAsync(PlayersInsert, new { Id = id });

        var duplicatedInsert = async () => await provider.ExecuteAsync(PlayersInsert, new { Id = id });

        await AssertPropagatesTheDriverFailure(duplicatedInsert, "Duplicate entry");
        AssertClosedAndNotDisposed(provider);
    }

    [Test]
    public async Task CloseTheConnectionWhenAnOperationIsCancelledAfterOpeningIt()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        using var cancellation = new CancellationTokenSource();
        UnderlyingDbConnectionOf(provider).StateChange += (_, change) =>
        {
            if (change.CurrentState == ConnectionState.Open) cancellation.Cancel();
        };

        var query = async () => await provider.QueryAsync<int>("SELECT 1;", cancellationToken: cancellation.Token);

        await query.Should().ThrowAsync<OperationCanceledException>();
        AssertClosedAndNotDisposed(provider);
    }

    [Test]
    public async Task LeaveTheConnectionToTheTransactionWhenAnOperationInsideItFails()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        var id = Guid.NewGuid();
        var stateAfterFailure = ConnectionState.Closed;

        var transaction = async () => await provider.ExecuteInTransactionAsync(async () =>
        {
            await provider.ExecuteAsync(PlayersInsert, new { Id = id });
            try
            {
                await provider.ExecuteAsync(InvalidSql);
            }
            finally
            {
                stateAfterFailure = provider.ConnectionState;
            }
        });

        await AssertPropagatesTheDriverFailure(transaction, "syntax");
        stateAfterFailure.Should().Be(ConnectionState.Open);
        await AssertPlayerNotWritten(id);
        AssertClosedAndNotDisposed(provider);
    }

    [SetUp]
    [TearDown]
    public async Task ClearPlayers()
    {
        await using var provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        await provider.ClearAsync([DbTableNames.PlayersTable]);
    }

    private const string PlayersInsert =
        "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
        "VALUES (@Id, 'Lifecycle', 'Probe', NULL, 'MALE', '2000-01-01');";
    private const string PlayerCount = "SELECT COUNT(*) FROM `Players` WHERE Id = @Id;";
    private const string PlayerById = "SELECT Id FROM `Players` WHERE Id = @Id;";
    private const string PlayerDelete = "DELETE FROM `Players` WHERE Id = @Id;";
    private const string InvalidSql = "SELEC 1;";

    /// <summary>The driver's own exception reaches the caller unwrapped (checked by type name to stay driver-agnostic).</summary>
    private static async Task AssertPropagatesTheDriverFailure(Func<Task> operation, string messageFragment)
    {
        var failure = (await operation.Should().ThrowAsync<DbException>()).Which;
        failure.GetType().Name.Should().Be("MySqlException");
        failure.Message.Should().Contain(messageFragment);
    }

    private static CancellationToken CancelledToken() => new(canceled: true);

    private static async Task AssertPlayerNotWritten(Guid id)
    {
        await using var observer = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        (await observer.QueryScalarAsync<long>(PlayerCount, new { Id = id })).Should().Be(0);
    }

    private static void AssertClosedAndNotDisposed(SqlConnectionProvider provider)
    {
        provider.ConnectionState.Should().Be(ConnectionState.Closed);
        provider.IsDisposed.Should().BeFalse();
    }

    /// <summary>Reads the private connection by reflection to observe its <see cref="Component.Disposed"/> event without widening the public API.</summary>
    private static Component UnderlyingConnectionOf(SqlConnectionProvider provider)
        => (Component)typeof(SqlConnectionProvider)
            .GetField("_sqlConnection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(provider)!;

    private static DbConnection UnderlyingDbConnectionOf(SqlConnectionProvider provider)
        => (DbConnection)UnderlyingConnectionOf(provider);

    private static ServiceProvider BuildScopedServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDatabaseConnectionProvider>(_ =>
            new SqlConnectionProvider(TestDefaultValues.TestConnectionString));
        return services.BuildServiceProvider();
    }
}
