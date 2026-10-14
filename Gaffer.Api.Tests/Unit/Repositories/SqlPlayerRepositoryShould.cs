using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Repositories;

public class SqlPlayerRepositoryShould : BaseSqlTest {
    public IDatabaseConnectionProvider DatabaseConnectionProvider;
    private SqlPlayerRepository repository;
    private static Player player1;
    private Player player2;
    private Player player3;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private const Gender Gender = TestDefaultValues.DefaultGender;
    private readonly DateTime birthDate = new DateTime(2000, 1, 1);



    [SetUp]
    public async Task Setup() {
        TablesToDelete = new List<string>
        {
            DbTableNames.PlayersTable,
        };
        await ClearDatabase(TablesToDelete);
        DatabaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        repository = new SqlPlayerRepository(DatabaseConnectionProvider);

        player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, birthDate);
        player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", string.Empty, Gender.MALE, birthDate);
        player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", string.Empty, Gender.MALE, birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);
    }

    [TearDown]
    public async Task TearDown() {
        await ClearDatabase(TablesToDelete);
        await ((IAsyncDisposable)DatabaseConnectionProvider).DisposeAsync();
    }

    private async Task InsertAPlayerInDb(Player player) {
        var query = "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
                    "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);";
        await DatabaseConnectionProvider.ExecuteAsync(query, new {
            Id = player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            Gender = player.Gender.ToString(),
            BirthDate = player.Birthdate
        });
    }

    [Test]
    public async Task ReadAStoredPlayerThatBreaksTheCreationRulesWithoutValidatingIt() {
        var legacy = new Player(Guid.NewGuid(), new string('x', 150), "Legacy", string.Empty, Gender.OTHER,
            DateTime.UtcNow.Date.AddYears(-10));
        await InsertAPlayerInDb(legacy);

        var result = await repository.Get(new CriteriaSet {
            ValueCriteria = [new ValueCriteria { Name = "Id", Values = [$"{legacy.Id}"] }]
        }, CancellationToken.None);

        var stored = result.Should().ContainSingle().Subject;
        stored.FirstName.Should().Be(legacy.FirstName);
        stored.SecondLastName.Should().BeEmpty();
        stored.Birthdate.Should().Be(legacy.Birthdate);
    }

    [TestFixture]
    public class WhenPropagatingTheCancellationToken : SqlPlayerRepositoryShould {
        private TokenRecordingConnectionProvider recorder;
        private SqlPlayerRepository recordedRepository;
        private CancellationTokenSource cancellation;

        [SetUp]
        public void SetUpRecorder() {
            recorder = new TokenRecordingConnectionProvider(DatabaseConnectionProvider);
            recordedRepository = new SqlPlayerRepository(recorder);
            cancellation = new CancellationTokenSource();
        }

        [TearDown]
        public void DisposeCancellation() => cancellation.Dispose();

        [Test]
        public async Task PassItsTokenToTheProviderWhenGetting() {
            await recordedRepository.Get(new CriteriaSet(), cancellation.Token);

            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenSaving() {
            await recordedRepository.Save(
                new Player(Guid.NewGuid(), "Xavi", "Hernandez", string.Empty, Gender.MALE, birthDate), cancellation.Token);

            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenDeleting() {
            await recordedRepository.Delete(player1.Id, cancellation.Token);

            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 1);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenUpdating() {
            await recordedRepository.Update(player2, cancellation.Token);

            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 1);
        }
    }

    [TestFixture]
    public class WhenGettingPlayers : SqlPlayerRepositoryShould {
        [Test]
        public async Task ReturnEmptyListWhenSearchCriteriaAndDatabaseAreBothEmpty() {
            await ClearDatabase(TablesToDelete);

            var result = await repository.Get(new CriteriaSet(), CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ReturnAllPlayersWhenSearchCriteriaAreEmptyAndDatabaseIsNot() {

            var result = await repository.Get(new CriteriaSet(), CancellationToken.None);

            result.Count.Should().Be(3);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.Id == player3.Id);
        }

        [Test]
        public async Task ReturnAllPlayersWhenGuidIsUsedAsSearchCriteria() {
            var searchCriteriaList = new List<ValueCriteria>();
            searchCriteriaList.Add(new ValueCriteria { Name = "Id", Values = [$"{player1.Id}"] });
            var playerSearchCriteria = new CriteriaSet();
            playerSearchCriteria.ValueCriteria = searchCriteriaList;

            var result = await repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
        }

        [Test]
        public async Task ReturnAPlayersWhenFirstNameIsUsedAsSearchCriteria() {
            var searchCriteriaList = new List<ValueCriteria>();
            searchCriteriaList.Add(new ValueCriteria { Name = "FirstName", Values = [$"{player1.FirstName}"] });
            var playerSearchCriteria = new CriteriaSet();
            playerSearchCriteria.ValueCriteria = searchCriteriaList;

            var result = await repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
        }

        [Test]
        public async Task ReturnAPlayerWhenLastNameIsUsedAsSearchCriteria() {
            var searchCriteriaList = new List<ValueCriteria>();
            searchCriteriaList.Add(new ValueCriteria { Name = "LastName", Values = [$"{player1.LastName}"] });
            var playerSearchCriteria = new CriteriaSet();
            playerSearchCriteria.ValueCriteria = searchCriteriaList;

            var result = await repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
            result.Should().Contain(p => p.LastName == player1.LastName);
        }

        [Test]
        public async Task ReturnAPlayerWhenDifferentFiltersAreUsedAsStrictSearchCriteria() {
            var searchCriteriaList = new List<ValueCriteria>();
            searchCriteriaList.Add(new ValueCriteria { Name = "LastName", Values = [$"{player2.LastName}"] });
            searchCriteriaList.Add(new ValueCriteria { Name = "FirstName", Values = [$"{player2.FirstName}"] });
            var playerSearchCriteria = new CriteriaSet();
            playerSearchCriteria.ValueCriteria = searchCriteriaList;

            var result = await repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(1);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.FirstName == player2.FirstName);
            result.Should().Contain(p => p.LastName == player2.LastName);
        }

        [Test]
        public async Task ReturnDifferentPlayersWhenDifferentFiltersAreUsedAsConditionalSearchCriteria() {
            var searchCriteriaList = new List<ValueCriteria>();
            searchCriteriaList.Add(new ValueCriteria { Name = "FirstName", Values = [$"{player1.FirstName}"], Type = ValueCriteriaType.AND });
            searchCriteriaList.Add(new ValueCriteria { Name = "LastName", Values = [$"{player2.LastName}"], Type = ValueCriteriaType.OR });
            var playerSearchCriteria = new CriteriaSet();
            playerSearchCriteria.ValueCriteria = searchCriteriaList;

            var result = await repository.Get(playerSearchCriteria, CancellationToken.None);

            result.Count.Should().Be(2);
            result.Should().Contain(p => p.Id == player1.Id);
            result.Should().Contain(p => p.FirstName == player1.FirstName);
            result.Should().Contain(p => p.LastName == player1.LastName);
            result.Should().Contain(p => p.Id == player2.Id);
            result.Should().Contain(p => p.FirstName == player2.FirstName);
            result.Should().Contain(p => p.LastName == player2.LastName);
        }

        [Test]
        public async Task ReturnPlayersMatchingAnyValueOfAnAndCriterion() {
            var result = await repository.Get(
                Criteria(Value("FirstName", ValueCriteriaType.AND, player1.FirstName, player2.FirstName)),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player1.Id, player2.Id]);
        }

        [Test]
        public async Task ReturnTheIntersectionOfTwoAndCriteriaWithSeveralValues() {
            var result = await repository.Get(Criteria(
                    Value("FirstName", ValueCriteriaType.AND, player1.FirstName, player2.FirstName),
                    Value("LastName", ValueCriteriaType.AND, player2.LastName, player3.LastName)),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player2.Id]);
        }

        [Test]
        public async Task ReturnTheUnionOfAnAndCriterionAndAnOrCriterionWithSeveralValues() {
            var result = await repository.Get(Criteria(
                    Value("FirstName", ValueCriteriaType.AND, player2.FirstName),
                    Value("LastName", ValueCriteriaType.OR, player1.LastName, player3.LastName)),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player1.Id, player2.Id, player3.Id]);
        }

        [Test]
        public async Task ReturnPlayersWhoseFieldContainsTheLikeValue() {
            var result = await repository.Get(Criteria(Value("FirstName", ValueCriteriaType.LIKE, "ich")),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player3.Id]);
        }

        [Test]
        public async Task ReturnPlayersWhoseFieldContainsAnyOfTheLikeValues() {
            var result = await repository.Get(Criteria(Value("FirstName", ValueCriteriaType.LIKE, "ich", "au")),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player2.Id, player3.Id]);
        }

        [Test]
        public async Task RestrictWithAndALikeCriterionThatFollowsAnotherCriterion() {
            var result = await repository.Get(Criteria(
                    Value("LastName", ValueCriteriaType.AND, player2.LastName, player3.LastName),
                    Value("FirstName", ValueCriteriaType.LIKE, "ae", "av")),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([player3.Id]);
        }

        [Test]
        public async Task MatchAnUnderscoreInALikeValueLiterally() {
            var underscored = new Player(Guid.NewGuid(), "Jo_n", "Literal", string.Empty, Gender.MALE, birthDate);
            await InsertAPlayerInDb(underscored);

            var wildcardCandidates = await repository.Get(Criteria(Value("FirstName", ValueCriteriaType.LIKE, "P_u")),
                CancellationToken.None);
            var literalMatches = await repository.Get(Criteria(Value("FirstName", ValueCriteriaType.LIKE, "o_n")),
                CancellationToken.None);

            wildcardCandidates.Should().BeEmpty();
            literalMatches.Select(p => p.Id).Should().BeEquivalentTo([underscored.Id]);
        }

        [Test]
        public async Task MatchAPercentSignInALikeValueLiterally() {
            var percent = new Player(Guid.NewGuid(), "Max100%", "Literal", string.Empty, Gender.MALE, birthDate);
            await InsertAPlayerInDb(percent);

            var result = await repository.Get(Criteria(Value("FirstName", ValueCriteriaType.LIKE, "%")),
                CancellationToken.None);

            result.Select(p => p.Id).Should().BeEquivalentTo([percent.Id]);
        }

        private static CriteriaSet Criteria(params ValueCriteria[] valueCriteria) => new() {
            ValueCriteria = [.. valueCriteria]
        };

        private static ValueCriteria Value(string name, ValueCriteriaType type, params string[] values) => new() {
            Name = name,
            Type = type,
            Values = [.. values]
        };
    }

    [TestFixture]
    public class WhenFilteringPlayersWithUntrustedInput : SqlPlayerRepositoryShould {
        [Test]
        public async Task ReturnNoPlayersWhenTheFilterValueTriesToInjectSql() {
            var result = await repository.Get(CriteriaFor("FirstName", "x') OR ('1'='1"), CancellationToken.None);

            result.Should().BeEmpty();
        }

        [TestCase("FirstName IN ('x') OR 1=1 --")]
        [TestCase("SecondLastName")]
        [TestCase("Gender")]
        [TestCase("BirthDate")]
        public async Task ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted(string fieldName) {
            var connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
            var repository = new SqlPlayerRepository(connectionProvider);

            var action = async () => await repository.Get(CriteriaFor(fieldName, "x"), CancellationToken.None);

            (await action.Should().ThrowAsync<PlayerSearchFilterNotPermittedException>())
                .Which.FieldName.Should().Be(fieldName);
            await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
        }

        [Test]
        public async Task MatchFilterFieldNamesIgnoringCase() {
            var result = await repository.Get(CriteriaFor("firstname", player2.FirstName), CancellationToken.None);

            result.Should().ContainSingle(p => p.Id == player2.Id);
        }

        [Test]
        public async Task FindAPlayerWhenTheFilterValueContainsASingleQuote() {
            var player = new Player(Guid.NewGuid(), "Shaun", "O'Neill", string.Empty, Gender.MALE, birthDate);
            await InsertAPlayerInDb(player);

            var result = await repository.Get(CriteriaFor("LastName", "O'Neill"), CancellationToken.None);

            result.Should().ContainSingle(p => p.Id == player.Id);
        }

        private static CriteriaSet CriteriaFor(string fieldName, string value) => new() {
            ValueCriteria = [new ValueCriteria { Name = fieldName, Values = [value] }]
        };
    }

    [TestFixture]
    public class WhenSavingAPlayer : SqlPlayerRepositoryShould {

        [Test]
        public async Task SaveAPlayer() {
            await ClearDatabase(TablesToDelete);
            var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var player = new Player(newGuid, FirstName, LastName, SecondLastName, Gender, birthDate);

            await repository.Save(player, CancellationToken.None);

            var result = await repository.Get(new CriteriaSet(), CancellationToken.None);

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
    public class WhenDeletingPlayers : SqlPlayerRepositoryShould {

        [Test]
        public async Task DeleteAPlayer() {
            await repository.Delete(player1.Id, CancellationToken.None);

            var result = await repository.Get(new CriteriaSet {
                ValueCriteria = [new ValueCriteria { Name = "Id", Values = [$"{player1.Id}"] }]
            }, CancellationToken.None);

            result.Count.Should().Be(0);
        }
    }

    [TestFixture]
    public class WhenUpdatingPlayers : SqlPlayerRepositoryShould {

        [Test]
        public async Task UpdateAPlayer() {
            var updatedPlayer = new Player(player1.Id,
                "NewFirstName",
                "NewLastName",
                "NewSecondLastName",
                Gender.OTHER,
                new DateTime(1996, 1, 1));
            await repository.Update(updatedPlayer, CancellationToken.None);

            var result = await repository.Get(new CriteriaSet {
                ValueCriteria = [new ValueCriteria { Name = "Id", Values = [$"{player1.Id}"] }]
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
