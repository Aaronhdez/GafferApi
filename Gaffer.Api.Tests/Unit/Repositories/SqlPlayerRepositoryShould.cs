using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Repositories;

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
        await ((IAsyncDisposable)_databaseConnectionProvider).DisposeAsync();
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

    [Test]
    public async Task ReadAStoredPlayerThatBreaksTheCreationRulesWithoutValidatingIt()
    {
        var legacy = new Player(Guid.NewGuid(), new string('x', 150), "Legacy", string.Empty, Gender.OTHER,
            DateTime.UtcNow.Date.AddYears(-10));
        await InsertAPlayerInDb(legacy);

        var result = await _repository.Get(new PlayerSearchCriteria
        {
            criteriaList = [new SearchCriteria { Name = "Id", Value = $"{legacy.Id}" }]
        }, CancellationToken.None);

        var stored = result.Should().ContainSingle().Subject;
        stored.FirstName.Should().Be(legacy.FirstName);
        stored.SecondLastName.Should().BeEmpty();
        stored.Birthdate.Should().Be(legacy.Birthdate);
    }

    [TestFixture]
    public class WhenPropagatingTheCancellationToken : SqlPlayerRepositoryShould
    {
        private TokenRecordingConnectionProvider _recorder;
        private SqlPlayerRepository _recordedRepository;
        private CancellationTokenSource _cancellation;

        [SetUp]
        public void SetUpRecorder()
        {
            _recorder = new TokenRecordingConnectionProvider(_databaseConnectionProvider);
            _recordedRepository = new SqlPlayerRepository(_recorder);
            _cancellation = new CancellationTokenSource();
        }

        [TearDown]
        public void DisposeCancellation() => _cancellation.Dispose();

        [Test]
        public async Task PassItsTokenToTheProviderWhenGetting()
        {
            await _recordedRepository.Get(new PlayerSearchCriteria(), _cancellation.Token);

            _recorder.ShouldHaveReceivedOnly(_cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenSaving()
        {
            await _recordedRepository.Save(
                new Player(Guid.NewGuid(), "Xavi", "Hernandez", string.Empty, Gender.MALE, _birthDate), _cancellation.Token);

            _recorder.ShouldHaveReceivedOnly(_cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenDeleting()
        {
            await _recordedRepository.Delete(player1.Id, _cancellation.Token);

            _recorder.ShouldHaveReceivedOnly(_cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenUpdating()
        {
            await _recordedRepository.Update(player2, _cancellation.Token);

            _recorder.ShouldHaveReceivedOnly(_cancellation.Token, minimumCalls: 1);
        }
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
    public class WhenFilteringPlayersWithUntrustedInput : SqlPlayerRepositoryShould
    {
        [Test]
        public async Task ReturnNoPlayersWhenTheFilterValueTriesToInjectSql()
        {
            var result = await _repository.Get(CriteriaFor("FirstName", "x') OR ('1'='1"), CancellationToken.None);

            result.Should().BeEmpty();
        }

        [TestCase("FirstName IN ('x') OR 1=1 --")]
        [TestCase("SecondLastName")]
        [TestCase("Gender")]
        [TestCase("BirthDate")]
        public async Task ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted(string fieldName)
        {
            var connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
            var repository = new SqlPlayerRepository(connectionProvider);

            var action = async () => await repository.Get(CriteriaFor(fieldName, "x"), CancellationToken.None);

            (await action.Should().ThrowAsync<PlayerSearchFilterNotPermittedException>())
                .Which.FieldName.Should().Be(fieldName);
            await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
        }

        [Test]
        public async Task MatchFilterFieldNamesIgnoringCase()
        {
            var result = await _repository.Get(CriteriaFor("firstname", player2.FirstName), CancellationToken.None);

            result.Should().ContainSingle(p => p.Id == player2.Id);
        }

        [Test]
        public async Task FindAPlayerWhenTheFilterValueContainsASingleQuote()
        {
            var player = new Player(Guid.NewGuid(), "Shaun", "O'Neill", string.Empty, Gender.MALE, _birthDate);
            await InsertAPlayerInDb(player);

            var result = await _repository.Get(CriteriaFor("LastName", "O'Neill"), CancellationToken.None);

            result.Should().ContainSingle(p => p.Id == player.Id);
        }

        private static PlayerSearchCriteria CriteriaFor(string fieldName, string value) => new()
        {
            criteriaList = [new SearchCriteria { Name = fieldName, Value = value }]
        };
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
    
    [TestFixture]
    public class WhenUpdatingPlayers : SqlPlayerRepositoryShould
    {

        [Test]
        public async Task UpdateAPlayer()
        {
            var updatedPlayer = new Player(player1.Id,
                "NewFirstName",
                "NewLastName",
                "NewSecondLastName",
                Gender.OTHER,
                new DateTime(1996, 1, 1));
            await _repository.Update(updatedPlayer, CancellationToken.None);

            var result = await _repository.Get(new PlayerSearchCriteria
            {
                criteriaList = new List<SearchCriteria>(){ new() { Name = "Id", Value = $"{player1.Id}" } }
            }, CancellationToken.None);
            result[0].Id.Should().Be(updatedPlayer.Id);
            result[0].FirstName.Should().Be(updatedPlayer.FirstName);
            result[0].LastName.Should().Be(updatedPlayer.LastName);
            result[0].SecondLastName.Should().Be(updatedPlayer.SecondLastName);
            result[0].Birthdate.Should().Be(updatedPlayer.Birthdate);
            result[0].Gender.Should().Be(updatedPlayer.Gender);
        }
    }
}