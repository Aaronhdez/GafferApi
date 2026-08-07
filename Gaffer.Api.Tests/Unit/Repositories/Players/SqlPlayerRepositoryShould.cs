using AwesomeAssertions;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Unit.Repositories.Players;

public class SqlPlayerRepositoryShould : BaseSqlTest
{
    public IDatabaseConnectionProvider _databaseConnectionProvider;
    private PlayerRepository _repository;

    [SetUp]
    public void Setup()
    {
        _tablesToDelete = new List<string>
        {
            DbTableNames.PlayersTable,
        };
        _databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        _repository = new PlayerRepository(_databaseConnectionProvider);
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
    }
}