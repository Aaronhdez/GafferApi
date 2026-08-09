using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Unit.Repositories.Players;

public class SqlPlayerRepositoryShould : BaseSqlTest
{
    public IDatabaseConnectionProvider _databaseConnectionProvider;
    private SqlPlayerRepository _repository;
    private static Player player1;
    private Player player2;
    private Player player3;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private const Gender Gender = TestDefaultValues.DefaultGender;
    private readonly DateTime _birthDate = new DateTime(2000, 1, 1);
    


    [SetUp]
    public async Task Setup()
    {
        _tablesToDelete = new List<string>
        {
            DbTableNames.PlayersTable,
        };
        await ClearDatabase(_tablesToDelete);
        _databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        _repository = new SqlPlayerRepository(_databaseConnectionProvider);
        
        player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, _birthDate);
        player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", string.Empty, Gender.MALE, _birthDate);
        player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", string.Empty, Gender.MALE, _birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);
    }

    [TearDown]
    public async Task TearDown()
    {
        await ClearDatabase(_tablesToDelete);
    }

    private async Task InsertAPlayerInDb(Player player)
    {
        var query = "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
                    "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);";
        await _databaseConnectionProvider.ExecuteAsync(query, new
        {
            Id = player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            Gender = player.Gender.ToString(),
            BirthDate = player.Birthdate
        });
    }

    [TestFixture]
    public class WhenGettingPlayers : SqlPlayerRepositoryShould
    {
        [Test]
        public async Task ReturnEmptyListWhenSearchCriteriaAndDatabaseAreBothEmpty()
        {
            await ClearDatabase(_tablesToDelete);
            
            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Should().BeEmpty();
        }

        [Test]
        public async Task ReturnAllPlayersWhenSearchCriteriaAreEmptyAndDatabaseIsNot()
        {

            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);

            result.Count.Should().Be(3);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.Id == player3.Id);
        }
        
        [Test]
        public async Task ReturnAllPlayersWhenGuidIsUsedAsSearchCriteria()
        {
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "Id", Value = $"{player1.Id}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
        }

        [Test]
        public async Task ReturnAPlayersWhenFirstNameIsUsedAsSearchCriteria()
        {
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "FirstName", Value = $"{player1.FirstName}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
        }

        [Test]
        public async Task ReturnAPlayerWhenLastNameIsUsedAsSearchCriteria()
        {
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "LastName", Value = $"{player1.LastName}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
            result.Should().Contain(p => p.LastName == player1.LastName);
        }

        [Test]
        public async Task ReturnAPlayerWhenDifferentFiltersAreUsedAsStrictSearchCriteria()
        {
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "LastName", Value = $"{player2.LastName}"});
            searchCriteriaList.Add(new SearchCriteria { Name = "FirstName", Value = $"{player2.FirstName}"});
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.FirstName == player2.FirstName);
            result.Should().Contain(p => p.LastName == player2.LastName);
        }
        
        [Test]
        public async Task ReturnDifferentPlayersWhenDifferentFiltersAreUsedAsConditionalSearchCriteria()
        {
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "FirstName", Value = $"{player1.FirstName}", CriteriaType = CriteriaType.AND});
            searchCriteriaList.Add(new SearchCriteria { Name = "LastName", Value = $"{player2.LastName}", CriteriaType = CriteriaType.OR });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(2);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
            result.Should().Contain(p => p.LastName == player1.LastName);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.FirstName == player2.FirstName);
            result.Should().Contain(p => p.LastName == player2.LastName);
        }
    }
    
    [TestFixture]
    public class WhenSavingAPlayer : SqlPlayerRepositoryShould
    {
        
        [Test]
        public async Task SaveAPlayer()
        {
            await ClearDatabase(_tablesToDelete);
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, Gender, _birthDate);

            await _repository.Save(player, CancellationToken.None);

            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Count.Should().Be(1);
            result[0].Id.Should().Be(player.Id);
            result[0].FirstName.Should().Be(player.FirstName);
            result[0].LastName.Should().Be(player.LastName);
            result[0].SecondLastName.Should().Be(player.SecondLastName);
            result[0].Birthdate.Should().Be(player.Birthdate);
            result[0].Gender.Should().Be(player.Gender);
        }
    }

    [TestFixture]
    public class WhenDeletingPlayers : SqlPlayerRepositoryShould
    {

        [Test]
        public async Task DeleteAPlayer()
        {
            await _repository.Delete(player1.Id, CancellationToken.None);

            var result = await _repository.Get(new PlayerSearchCriteria
            {
                criteriaList = new List<SearchCriteria>(){ new() { Name = "Id", Value = $"{player1.Id}" } }
            }, CancellationToken.None);
            
            result.Count.Should().Be(0);
        }
    }
}