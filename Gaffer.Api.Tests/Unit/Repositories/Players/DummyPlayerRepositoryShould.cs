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
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _cancellationToken = CancellationToken.None;
        _connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
        _playersList = new List<Player>();
        _repository = new DummyPlayerRepository(_connectionProvider, _playersList);
    }


    [TestFixture]
    public class WhenCreatingAPlayerInDummyRepository : DummyPlayerRepositoryShould
    {
        [Test]
        public async Task ThrowExceptionWhenDatabaseIsNull()
        {
            _repository = new DummyPlayerRepository(_connectionProvider, null);
            var playerId = Guid.NewGuid();

            var result = async () => await _repository.Get(playerId, _cancellationToken);

            await result.Should().ThrowAsync<DatabaseNotReadyException>();
        }

        [Test]
        public async Task ThrowExceptionWhenDatabaseIsEmpty()
        {
            var players = new List<Player>();
            _repository = new DummyPlayerRepository(_connectionProvider, players);
            var playerId = Guid.NewGuid();

            var result = async () => await _repository.Get(playerId, _cancellationToken);

            await result.Should().ThrowAsync<PlayerNotFoundException>();
        }

        [Test]
        public async Task ThrowExceptionWhenPlayerIsNotFound()
        {
            var otherPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var playerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            _playersList.Add(new Player(playerId, FirstName, LastName, SecondLastName, _birthDate, Gender));

            var result = async () => await _repository.Get(otherPlayerId, _cancellationToken);

            await result.Should().ThrowAsync<PlayerNotFoundException>();
        }

        [Test]
        public async Task ReturnPlayerWhenGuidIsUsedAsQueryItem()
        {
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _playersList.Add(new Player(newGuid, FirstName, LastName, SecondLastName, _birthDate, Gender));

            var player = await _repository.Get(newGuid, _cancellationToken);

            player.Id.Should().Be(newGuid);
            player.FirstName.Should().Be(FirstName);
            player.LastName.Should().Be(LastName);
            player.SecondLastName.Should().Be(SecondLastName);
            player.Birthdate.Should().Be(_birthDate);
            player.Gender.Should().Be(Gender);
        }
    }


    [TestFixture]
    public class WhenSavingAPlayerInDummyRepository : DummyPlayerRepositoryShould
    {
        [Test]
        public async Task SaveAPlayer()
        {
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, _birthDate, Gender);

            await _repository.Save(player, _cancellationToken);

            _playersList.Count.Should().Be(1);
            _playersList[0].Id.Should().Be(player.Id);
            _playersList[0].FirstName.Should().Be(player.FirstName);
            _playersList[0].LastName.Should().Be(player.LastName);
            _playersList[0].SecondLastName.Should().Be(player.SecondLastName);
            _playersList[0].Birthdate.Should().Be(player.Birthdate);
            _playersList[0].Gender.Should().Be(player.Gender);
        }
        
        [Test]
        public async Task ThrowExceptionWhenDatabaseIsNull()
        {
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _repository = new DummyPlayerRepository(_connectionProvider, null);
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, _birthDate, Gender);

            var result = async () => await _repository.Save(player, _cancellationToken);

            await result.Should().ThrowAsync<DatabaseNotReadyException>();
        }
    }


}