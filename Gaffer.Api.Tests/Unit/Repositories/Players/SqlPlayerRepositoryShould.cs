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
    private Player player1;
    private Player player2;
    private Player player3;
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string SecondLastName = "Doe";
    private const Gender Gender = Shared.Gender.MALE;
    private readonly DateTime _birthDate = new DateTime(2000, 1, 1);
    


    [SetUp]
    public async Task Setup()
    {
        _tablesToDelete = new List<string>
        {
            DbTableNames.PlayersTable,
        };
        await ClearDatabase(_tablesToDelete);
        player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, _birthDate);
        player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, _birthDate);
        player3 = new Player(Guid.NewGuid(), "Michael", "Bacllack", null, Gender.MALE, _birthDate);
        _databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        _repository = new SqlPlayerRepository(_databaseConnectionProvider);
    }

    [TearDown]
    public async Task TearDown()
    {
        await ClearDatabase(_tablesToDelete);
    }

    [TestFixture]
    public class WhenGettingPlayersInSqlRepository : SqlPlayerRepositoryShould
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
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);

            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);

            result.Count.Should().Be(3);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.Id == player3.Id);
        }

        [Test]
        public async Task ReturnAllPlayersWhenGuidIsUsedAsSearchCriteria()
        {
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
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
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
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
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
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
        public async Task ReturnAPlayerWhenFirstAndLastNameAreUsedAsSearchCriteria()
        {
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
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
        public async Task ReturnAPlayerWhenFirstAndLastNameAreUsedAsConditionalSearchCriteria()
        {
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "FirstName", Value = $"{player1.FirstName}", CriteriaType = CriteriaType.AND});
            searchCriteriaList.Add(new SearchCriteria { Name = "LastName", Value = $"{player2.LastName}", CriteriaType = CriteriaType.OR });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(2);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.FirstName == player2.FirstName);
            result.Should().Contain(p => p.LastName == player2.LastName);
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
    }
}