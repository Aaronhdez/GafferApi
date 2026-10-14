using AwesomeAssertions;
using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Repositories;

public class SqlPlayerRepositoryQueryShould {
    private IDatabaseConnectionProvider connectionProvider;
    private SqlPlayerRepository repository;
    private string? capturedQuery;
    private DynamicParameters? capturedParameters;

    [SetUp]
    public void Setup() {
        capturedQuery = null;
        capturedParameters = null;
        connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
        connectionProvider
            .QueryAsync<Player>(
                Arg.Do<string>(query => capturedQuery = query),
                Arg.Do<object?>(parameters => capturedParameters = parameters as DynamicParameters),
                Arg.Any<CancellationToken>())
            .Returns(new List<Player>());
        repository = new SqlPlayerRepository(connectionProvider);
    }

    private static IEnumerable<TestCaseData> ExpectedQueries() {
        yield return Case("NoCriteria",
            Criteria(),
            "SELECT * FROM `Players`;");
        yield return Case("OneAndCriterionWithTwoValues",
            Criteria(Value("FirstName", ValueCriteriaType.AND, "Paul", "Xavi")),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0, @v0_1);",
            ("v0_0", "Paul"), ("v0_1", "Xavi"));
        yield return Case("TwoAndCriteria",
            Criteria(
                Value("FirstName", ValueCriteriaType.AND, "Paul"),
                Value("LastName", ValueCriteriaType.AND, "Gascoigne")),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) AND `LastName` IN (@v1_0);",
            ("v0_0", "Paul"), ("v1_0", "Gascoigne"));
        yield return Case("AndThenOrCriterion",
            Criteria(
                Value("FirstName", ValueCriteriaType.AND, "Paul"),
                Value("LastName", ValueCriteriaType.OR, "Martel", "Hernandez")),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) OR `LastName` IN (@v1_0, @v1_1);",
            ("v0_0", "Paul"), ("v1_0", "Martel"), ("v1_1", "Hernandez"));
        yield return Case("FirstCriterionWithOr",
            Criteria(Value("LastName", ValueCriteriaType.OR, "Martel")),
            "SELECT * FROM `Players` WHERE `LastName` IN (@v0_0);",
            ("v0_0", "Martel"));
        yield return Case("FirstLikeCriterionWithOneValue",
            Criteria(Value("FirstName", ValueCriteriaType.LIKE, "au")),
            "SELECT * FROM `Players` WHERE `FirstName` LIKE @v0_0;",
            ("v0_0", "%au%"));
        yield return Case("FirstLikeCriterionWithSeveralValues",
            Criteria(Value("FirstName", ValueCriteriaType.LIKE, "au", "av")),
            "SELECT * FROM `Players` WHERE (`FirstName` LIKE @v0_0 OR `FirstName` LIKE @v0_1);",
            ("v0_0", "%au%"), ("v0_1", "%av%"));
        yield return Case("LikeCriterionWithOneValueAfterAnotherCriterion",
            Criteria(
                Value("LastName", ValueCriteriaType.AND, "Gascoigne"),
                Value("FirstName", ValueCriteriaType.LIKE, "Pa")),
            "SELECT * FROM `Players` WHERE `LastName` IN (@v0_0) AND `FirstName` LIKE @v1_0;",
            ("v0_0", "Gascoigne"), ("v1_0", "%Pa%"));
        yield return Case("LikeCriterionWithSeveralValuesAfterAnotherCriterion",
            Criteria(
                Value("LastName", ValueCriteriaType.AND, "Gascoigne"),
                Value("FirstName", ValueCriteriaType.LIKE, "Pa", "Xa")),
            "SELECT * FROM `Players` WHERE `LastName` IN (@v0_0) AND (`FirstName` LIKE @v1_0 OR `FirstName` LIKE @v1_1);",
            ("v0_0", "Gascoigne"), ("v1_0", "%Pa%"), ("v1_1", "%Xa%"));
        yield return Case("LikeValueWithEscapedCharacters",
            Criteria(Value("FirstName", ValueCriteriaType.LIKE, @"50%_a\b")),
            "SELECT * FROM `Players` WHERE `FirstName` LIKE @v0_0;",
            ("v0_0", @"%50\%\_a\\b%"));
        yield return Case("CanonicalColumnNameForACaseInsensitiveField",
            Criteria(Value("firstname", ValueCriteriaType.AND, "Paul")),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0);",
            ("v0_0", "Paul"));
    }

    [TestCaseSource(nameof(ExpectedQueries))]
    public async Task BuildTheExactQueryWithOneParameterPerValue(
        CriteriaSet criteria, string expectedQuery, Dictionary<string, string> expectedParameters) {
        await repository.Get(criteria, CancellationToken.None);

        capturedQuery.Should().Be(expectedQuery);
        capturedQuery.Should().NotContain("WHERE AND").And.NotContain("WHERE OR");
        capturedParameters.Should().NotBeNull();
        capturedParameters!.ParameterNames.Should().BeEquivalentTo(expectedParameters.Keys);
        foreach (var (name, value) in expectedParameters) {
            capturedParameters.Get<string>(name).Should().Be(value);
        }
    }

    [TestCase(ValueCriteriaType.AND)]
    [TestCase(ValueCriteriaType.OR)]
    [TestCase(ValueCriteriaType.LIKE)]
    public async Task KeepEveryCallerValueOutOfTheQueryText(ValueCriteriaType type) {
        const string injection = "x') OR ('1'='1";
        const string quoted = "O'Neill";

        await repository.Get(Criteria(Value("LastName", type, injection, quoted)), CancellationToken.None);

        capturedQuery.Should().NotContain(injection).And.NotContain(quoted).And.NotContain("'");
    }

    [Test]
    public async Task ThrowWithoutQueryingWhenACriterionHasNoValues() {
        var action = async () => await repository.Get(
            Criteria(new ValueCriteria { Name = "FirstName", Values = [] }), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    [Test]
    public async Task ThrowWithoutQueryingWhenACriterionHasNullValues() {
        var action = async () => await repository.Get(
            Criteria(new ValueCriteria { Name = "FirstName", Values = null! }), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    private static TestCaseData Case(string name, CriteriaSet criteria, string expectedQuery,
        params (string Name, string Value)[] expectedParameters) =>
        new TestCaseData(criteria, expectedQuery, expectedParameters.ToDictionary(p => p.Name, p => p.Value))
            .SetName($"BuildTheExactQueryWithOneParameterPerValue_{name}");

    private static CriteriaSet Criteria(params ValueCriteria[] valueCriteria) => new() {
        ValueCriteria = [.. valueCriteria]
    };

    private static ValueCriteria Value(string name, ValueCriteriaType type, params string[] values) => new() {
        Name = name,
        Type = type,
        Values = [.. values]
    };
}
