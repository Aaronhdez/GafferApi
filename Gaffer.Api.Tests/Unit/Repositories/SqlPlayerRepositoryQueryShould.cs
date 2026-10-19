using AwesomeAssertions;
using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Criteria;
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
        CriteriaSet criteria, string expectedQuery, Dictionary<string, object> expectedParameters) {
        await repository.Get(criteria, CancellationToken.None);

        capturedQuery.Should().Be(expectedQuery);
        capturedQuery.Should().NotContain("WHERE AND").And.NotContain("WHERE OR");
        capturedParameters.Should().NotBeNull();
        capturedParameters!.ParameterNames.Should().BeEquivalentTo(expectedParameters.Keys);
        foreach (var (name, value) in expectedParameters) {
            var actual = capturedParameters.Get<object>(name);
            actual.Should().BeOfType(value.GetType());
            actual.Should().Be(value);
        }
    }

    private static readonly DateTime From1990 = new(1990, 1, 1);
    private static readonly DateTime To1999 = new(1999, 12, 31);
    private static readonly DateTime From2000 = new(2000, 1, 1);
    private static readonly DateTime To2009 = new(2009, 12, 31);

    private static IEnumerable<TestCaseData> ExpectedIntervalAndOrderQueries() {
        yield return IntervalAndOrderCase("OnlyInterval",
            Criteria([], [Interval("BirthDate", From1990, To1999)], []),
            "SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("AndValueThenInterval",
            Criteria([Value("FirstName", ValueCriteriaType.AND, "Paul")], [Interval("BirthDate", From1990, To1999)], []),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) AND `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("v0_0", "Paul"), ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("ValuesWithOrThenInterval",
            Criteria(
                [
                    Value("FirstName", ValueCriteriaType.AND, "Paul"),
                    Value("LastName", ValueCriteriaType.OR, "Gascoigne")
                ],
                [Interval("BirthDate", From1990, To1999)],
                []),
            "SELECT * FROM `Players` WHERE (`FirstName` IN (@v0_0) OR `LastName` IN (@v1_0)) AND `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("v0_0", "Paul"), ("v1_0", "Gascoigne"), ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("LikeGroupWithoutOrConnectorThenInterval",
            Criteria([Value("FirstName", ValueCriteriaType.LIKE, "Pa", "Xa")], [Interval("BirthDate", From1990, To1999)], []),
            "SELECT * FROM `Players` WHERE (`FirstName` LIKE @v0_0 OR `FirstName` LIKE @v0_1) AND `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("v0_0", "%Pa%"), ("v0_1", "%Xa%"), ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("FirstValueWithOrThenInterval",
            Criteria([Value("LastName", ValueCriteriaType.OR, "Martel")], [Interval("BirthDate", From1990, To1999)], []),
            "SELECT * FROM `Players` WHERE `LastName` IN (@v0_0) AND `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("v0_0", "Martel"), ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("ValuesWithOrWithoutInterval",
            Criteria(
                [
                    Value("FirstName", ValueCriteriaType.AND, "Paul"),
                    Value("LastName", ValueCriteriaType.OR, "Gascoigne")
                ],
                [],
                []),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) OR `LastName` IN (@v1_0);",
            ("v0_0", "Paul"), ("v1_0", "Gascoigne"));
        yield return IntervalAndOrderCase("TwoIntervals",
            Criteria([], [Interval("BirthDate", From1990, To1999), Interval("BirthDate", From2000, To2009)], []),
            "SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max AND `BirthDate` BETWEEN @i1_min AND @i1_max;",
            ("i0_min", From1990), ("i0_max", To1999), ("i1_min", From2000), ("i1_max", To2009));
        yield return IntervalAndOrderCase("IntegerInterval",
            Criteria([], [new IntervalCriteria<int>("BirthDate", 1990, 1999)], []),
            "SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max;",
            ("i0_min", 1990), ("i0_max", 1999));
        yield return IntervalAndOrderCase("OnlyOrder",
            Criteria([], [], [Order("LastName", OrderDirection.ASC), Order("FirstName", OrderDirection.DESC)]),
            "SELECT * FROM `Players` ORDER BY `LastName` ASC, `FirstName` DESC;");
        yield return IntervalAndOrderCase("ValueIntervalAndOrder",
            Criteria(
                [Value("FirstName", ValueCriteriaType.AND, "Paul")],
                [Interval("BirthDate", From1990, To1999)],
                [Order("BirthDate", OrderDirection.DESC)]),
            "SELECT * FROM `Players` WHERE `FirstName` IN (@v0_0) AND `BirthDate` BETWEEN @i0_min AND @i0_max ORDER BY `BirthDate` DESC;",
            ("v0_0", "Paul"), ("i0_min", From1990), ("i0_max", To1999));
        yield return IntervalAndOrderCase("CanonicalColumnNamesForCaseInsensitiveIntervalAndOrderFields",
            Criteria([], [Interval("birthdate", From1990, To1999)], [Order("lastname", OrderDirection.ASC)]),
            "SELECT * FROM `Players` WHERE `BirthDate` BETWEEN @i0_min AND @i0_max ORDER BY `LastName` ASC;",
            ("i0_min", From1990), ("i0_max", To1999));
    }

    [TestCaseSource(nameof(ExpectedIntervalAndOrderQueries))]
    public async Task BuildTheExactQueryWithIntervalAndOrderCriteria(
        CriteriaSet criteria, string expectedQuery, Dictionary<string, object> expectedParameters) =>
        await BuildTheExactQueryWithOneParameterPerValue(criteria, expectedQuery, expectedParameters);

    [TestCase("FirstName")]
    [TestCase("Id")]
    [TestCase("LastName")]
    [TestCase("BirthDate; DROP TABLE Players")]
    public async Task ThrowWithoutQueryingWhenTheIntervalFieldIsNotPermitted(string fieldName) {
        var action = async () => await repository.Get(
            Criteria([], [Interval(fieldName, From1990, To1999)], []), CancellationToken.None);

        (await action.Should().ThrowAsync<PlayerSearchFilterNotPermittedException>())
            .Which.FieldName.Should().Be(fieldName);
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    [TestCase("Id")]
    [TestCase("Gender")]
    [TestCase("SecondLastName")]
    [TestCase("FirstName; DROP TABLE Players")]
    public async Task ThrowWithoutQueryingWhenTheOrderFieldIsNotPermitted(string fieldName) {
        var action = async () => await repository.Get(
            Criteria([], [], [Order(fieldName, OrderDirection.ASC)]), CancellationToken.None);

        (await action.Should().ThrowAsync<PlayerSearchFilterNotPermittedException>())
            .Which.FieldName.Should().Be(fieldName);
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    [Test]
    public async Task ThrowWithoutQueryingWhenBirthDateIsUsedAsAValueCriterion() {
        var action = async () => await repository.Get(
            Criteria(Value("BirthDate", ValueCriteriaType.AND, "2000-01-01")), CancellationToken.None);

        await action.Should().ThrowAsync<PlayerSearchFilterNotPermittedException>();
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    [Test]
    public async Task ThrowWithoutQueryingWhenTheOrderDirectionIsUndefined() {
        var action = async () => await repository.Get(
            Criteria([], [], [Order("LastName", (OrderDirection)42)]), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await connectionProvider.DidNotReceiveWithAnyArgs().QueryAsync<Player>(default!, default);
    }

    [Test]
    public async Task KeepEveryIntervalBoundOutOfTheQueryText() {
        const string injection = "x') OR ('1'='1";
        const string quoted = "O'Neill";

        await repository.Get(Criteria([], [
            Interval("BirthDate", From1990, To1999),
            new IntervalCriteria<string>("BirthDate", injection, quoted)
        ], []), CancellationToken.None);

        capturedQuery.Should().NotContain("1990").And.NotContain("1999")
            .And.NotContain(injection).And.NotContain(quoted).And.NotContain("'");
        capturedParameters!.Get<object>("i0_min").Should().Be(From1990);
        capturedParameters.Get<object>("i1_min").Should().Be(injection);
    }

    [Test]
    public void RejectAMissingMinimumBound() {
        var action = () => new IntervalCriteria<string>("BirthDate", null!, "2000-01-01");

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void RejectAMissingMaximumBound() {
        var action = () => new IntervalCriteria<string>("BirthDate", "1990-01-01", null!);

        action.Should().Throw<ArgumentNullException>();
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
        params (string Name, object Value)[] expectedParameters) =>
        new TestCaseData(criteria, expectedQuery, expectedParameters.ToDictionary(p => p.Name, p => p.Value))
            .SetName($"BuildTheExactQueryWithOneParameterPerValue_{name}");

    private static TestCaseData IntervalAndOrderCase(string name, CriteriaSet criteria, string expectedQuery,
        params (string Name, object Value)[] expectedParameters) =>
        Case(name, criteria, expectedQuery, expectedParameters)
            .SetName($"BuildTheExactQueryWithIntervalAndOrderCriteria_{name}");

    private static CriteriaSet Criteria(params ValueCriteria[] valueCriteria) => new() {
        ValueCriteria = [.. valueCriteria]
    };

    private static CriteriaSet Criteria(ValueCriteria[] valueCriteria, IIntervalCriteria[] intervalCriteria,
        OrderCriteria[] orderCriteria) => new() {
            ValueCriteria = [.. valueCriteria],
            IntervalCriteria = [.. intervalCriteria],
            OrderCriteria = [.. orderCriteria]
        };

    private static IntervalCriteria<DateTime> Interval(string name, DateTime min, DateTime max) => new(name, min, max);

    private static OrderCriteria Order(string name, OrderDirection direction) => new() {
        Name = name,
        Direction = direction
    };

    private static ValueCriteria Value(string name, ValueCriteriaType type, params string[] values) => new() {
        Name = name,
        Type = type,
        Values = [.. values]
    };
}
