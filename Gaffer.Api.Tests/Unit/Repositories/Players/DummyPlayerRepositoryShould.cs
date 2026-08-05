using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared;
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
    
    [Test]
    public async Task ReturnPlayer1WhenGuid1IsUsedAsQueryItem()
    {
        const string firstName = "John";
        const string lastName = "Doe";
        const string? secondLastName = "Doe";
        const Gender gender = Gender.MALE;
        Guid newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        DateTime birthDate = new DateTime(2000, 1, 1);
        var players = new List<Player>{ new(newGuid, firstName, lastName, secondLastName, birthDate, gender) };
        _repository = new DummyPlayerRepository(_connectionProvider, players);

        var player = await _repository.Get(newGuid);

        player.Id.Should().Be(newGuid);
        player.FirstName.Should().Be(firstName);
        player.LastName.Should().Be(lastName);
        player.SecondLastName.Should().Be(secondLastName);
        player.Birthdate.Should().Be(birthDate);
        player.Gender.Should().Be(gender);
    }
    
    [Test]
    public async Task ReturnPlayer1WhenGuid1IsUsedAsQueryItemForTwoPlayers()
    {
        const string firstName = "John";
        const string lastName = "Doe";
        const string? secondLastName = "Doe";
        const Gender gender = Gender.MALE;
        DateTime birthDate = new DateTime(2000, 1, 1);
        Guid player1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid player2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var player1 = new Player(player1Id, firstName, lastName, secondLastName, birthDate, gender);
        var player2 = new Player(player2Id, firstName, lastName, secondLastName, birthDate, gender);
        
        var players = new List<Player> { player2, player1 };
        _repository = new DummyPlayerRepository(_connectionProvider, players);

        var player = await _repository.Get(player1Id);

        player.Id.Should().Be(player1Id);
        player.FirstName.Should().Be(firstName);
        player.LastName.Should().Be(lastName);
        player.SecondLastName.Should().Be(secondLastName);
        player.Birthdate.Should().Be(birthDate);
        player.Gender.Should().Be(gender);
    }
}