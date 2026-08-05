using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Tests.Unit.Repositories.Players;

public class DummyPlayerRepositoryShould
{
    private DummyPlayerRepository _repository;
    private IDatabaseConnectionProvider _connectionProvider;

    [SetUp]
    public void Setup()
    {
        _connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
    }


    [Test]
    public async Task ThrowExceptionWhenDatabaseIsNull()
    {
        _repository = new DummyPlayerRepository(_connectionProvider, null);
        var playerId = Guid.NewGuid();

        var result = async () => await _repository.Get(playerId);

        await result.Should().ThrowAsync<PlayerNotFoundException>();
    }

    [Test]
    public async Task ThrowExceptionWhenDatabaseIsEmpty()
    {
        var players = new List<Player>();
        _repository = new DummyPlayerRepository(_connectionProvider, players);
        var playerId = Guid.NewGuid();

        var result = async () => await _repository.Get(playerId);

        await result.Should().ThrowAsync<PlayerNotFoundException>();
    }
}