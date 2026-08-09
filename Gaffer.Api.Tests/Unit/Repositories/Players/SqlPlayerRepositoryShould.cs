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
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
            
            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Count.Should().Be(3);
            result.Should().Contain(p => p.PlayerId == player1.PlayerId);
            result.Should().Contain(p => p.PlayerId == player2.PlayerId);
            result.Should().Contain(p => p.PlayerId == player3.PlayerId);
        }
        
        [Test]
        public async Task ReturnAllPlayersWhenGuidIsUsedAsSearchCriteria()
        {
            var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
            await InsertAPlayerInDb(player1);
            await InsertAPlayerInDb(player2);
            await InsertAPlayerInDb(player3);
            var searchCriteriaList = new List<SearchCriteria>();
            searchCriteriaList.Add(new SearchCriteria { Name = "Id", Value = $"{player1.PlayerId}" });
            var playerSearchCriteria = new PlayerSearchCriteria();
            playerSearchCriteria.criteriaList = searchCriteriaList;
            var result = await _repository.Get(playerSearchCriteria, CancellationToken.None);
            
            result.Count.Should().Be(1);
            result.Should().Contain(p => p.PlayerId == player1.PlayerId);
        }

        private async Task InsertAPlayerInDb(Player player)
        {
            var query = "INSERT INTO `Players` (PlayerId, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
                        "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);";
            await _databaseConnectionProvider.ExecuteAsync(query, new
            {
                Id = player.PlayerId,
                player.FirstName,
                player.LastName,
                player.SecondLastName,
                Gender = player.Gender.ToString(),
                BirthDate = player.Birthdate
            });
        }
    }
}