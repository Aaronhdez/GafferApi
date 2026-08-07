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
    public class WhenSavingAPlayerInDummyRepository : DummyPlayerRepositoryShould
    {
        [Test]
        public async Task SaveAPlayer()
        {
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, Gender, _birthDate);

            await _repository.Save(player, _cancellationToken);

            _playersList.Count.Should().Be(1);
            _playersList[0].PlayerId.Should().Be(player.PlayerId);
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
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, Gender, _birthDate);

            var result = async () => await _repository.Save(player, _cancellationToken);

            await result.Should().ThrowAsync<DatabaseNotReadyException>();
        }
    }

    [TestFixture]
    public class WhenGettingPlayersUsingSearchCriteriaInDummyRepository : DummyPlayerRepositoryShould
    {
        [Test]
        public async Task ReturnEmptyListWhenSearchCriteriaAndDatabaseAreBothEmpty()
        {
            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Should().BeEmpty();
        }
        
        [Test]
        public async Task ReturnAllPlayersWhenSearchCriteriaAreEmptyAndDatabaseIsNot()
        {
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            _playersList.Add(player1);
            _playersList.Add(player2);
            _playersList.Add(player3);
            
            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Count.Should().Be(3);
            result.Should().Contain(player1);
            result.Should().Contain(player2);
            result.Should().Contain(player3);
        }
        
        [Test]
        public async Task ReturnAllPlayersWhenGuidIsUsedAsSearchCriteria()
        {
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            _playersList.Add(player1);
            _playersList.Add(player2);
            _playersList.Add(player3);
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "Id", Value = $"{player1.PlayerId}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);
            
            result.Count.Should().Be(1);
            result.Should().Contain(player1);
        }
        
        [Test]
        public async Task ReturnAPlayersWhenFirstNameIsUsedAsSearchCriteria()
        {
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), "Other", LastName, SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), "Other2", LastName, SecondLastName, Gender, _birthDate);
            _playersList.Add(player1);
            _playersList.Add(player2);
            _playersList.Add(player3);
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "FirstName", Value = $"{player1.FirstName}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);
            
            result.Count.Should().Be(1);
            result.Should().Contain(player1);
        }
        
        [Test]
        public async Task ReturnAPlayerWhenLastNameIsUsedAsSearchCriteria()
        {
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), FirstName, "Other", SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), FirstName, "Other2", SecondLastName, Gender, _birthDate);
            _playersList.Add(player1);
            _playersList.Add(player2);
            _playersList.Add(player3);
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "LastName", Value = $"{player1.LastName}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);
            
            result.Count.Should().Be(1);
            result.Should().Contain(player1);
        }
        
        [Test]
        public async Task ThrowExceptionWhenDatabaseIsNull()
        {
            _repository = new DummyPlayerRepository(_connectionProvider, null);
            
            var result = async () => await _repository.Get(new PlayerSearchCriteria(), _cancellationToken);

            await result.Should().ThrowAsync<DatabaseNotReadyException>();
        }
    }


}