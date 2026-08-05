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
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string? SecondLastName = "Doe";
    private const Gender Gender = Shared.Gender.MALE;
    private readonly DateTime _birthDate = new DateTime(2000, 1, 1);
    private List<Player> _playersList;

    [SetUp]
    public void Setup()
    {
        _connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
        _playersList = new List<Player>();
        _repository = new DummyPlayerRepository(_connectionProvider, _playersList);
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
        Guid newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _playersList.Add(new Player(newGuid, FirstName, LastName, SecondLastName, _birthDate, Gender));

        var player = await _repository.Get(newGuid);

        player.Id.Should().Be(newGuid);
        player.FirstName.Should().Be(FirstName);
        player.LastName.Should().Be(LastName);
        player.SecondLastName.Should().Be(SecondLastName);
        player.Birthdate.Should().Be(_birthDate);
        player.Gender.Should().Be(Gender);
    }

    [Test]
    public async Task ReturnPlayer1WhenGuid1IsUsedAsQueryItemForTwoPlayers()
    {
        Guid player1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid player2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        _playersList.Add(new Player(player2Id, FirstName, LastName, SecondLastName, _birthDate, Gender));
        _playersList.Add(new Player(player1Id, FirstName, LastName, SecondLastName, _birthDate, Gender));

        var player = await _repository.Get(player1Id);

        player.Id.Should().Be(player1Id);
        player.FirstName.Should().Be(FirstName);
        player.LastName.Should().Be(LastName);
        player.SecondLastName.Should().Be(SecondLastName);
        player.Birthdate.Should().Be(_birthDate);
        player.Gender.Should().Be(Gender);
    }

    [Test]
    public async Task ReturnPlayer1WhenGuid1IsUsedAsQueryItemForThreePlayers()
    {
        Guid player1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid player2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid player3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        _playersList.Add(new Player(player3Id, FirstName, LastName, SecondLastName, _birthDate, Gender));
        _playersList.Add(new Player(player2Id, FirstName, LastName, SecondLastName, _birthDate, Gender));
        _playersList.Add(new Player(player1Id, FirstName, LastName, SecondLastName, _birthDate, Gender));

        var player = await _repository.Get(player1Id);

        player.Id.Should().Be(player1Id);
        player.FirstName.Should().Be(FirstName);
        player.LastName.Should().Be(LastName);
        player.SecondLastName.Should().Be(SecondLastName);
        player.Birthdate.Should().Be(_birthDate);
        player.Gender.Should().Be(Gender);
    }
}