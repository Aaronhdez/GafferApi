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
    private const string? SecondLastName = "Doe";
    private const Gender Gender = Shared.Gender.MALE;
    private readonly DateTime _birthDate = new DateTime(2000, 1, 1);

    [SetUp]
    public void Setup()
    {
        _tablesToDelete = new List<string>
        {
            DbTableNames.PlayersTable,
        };
        _databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        _repository = new SqlPlayerRepository(_databaseConnectionProvider);
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
            InsertAPlayerInDb(player1);
            InsertAPlayerInDb(player2);
            InsertAPlayerInDb(player3);
            
            var result = await _repository.Get(new PlayerSearchCriteria(), CancellationToken.None);
            
            result.Count.Should().Be(3);
            result.Should().Contain(player1);
            result.Should().Contain(player2);
            result.Should().Contain(player3);
        }

        private void InsertAPlayerInDb(Player player)
        {
            var query = "INSERT INTO Players (PlayerId, FirstName, LastName, SecondLastName, Gender, _birthDate) " +
                        "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate)";
            _databaseConnectionProvider.ExecuteAsync(query, new
            {
                player.PlayerId,
                player.FirstName,
                player.LastName,
                player.SecondLastName,
                Gender = player.Gender.ToString(),
                BirthDate = player.Birthdate
            });
        }
    }
}