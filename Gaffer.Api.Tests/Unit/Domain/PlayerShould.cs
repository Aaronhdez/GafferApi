using System.Globalization;
using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Unit.Domain;

public class PlayerShould {
    private Player player;

    [SetUp]
    public void Setup() {
        player = new Player(TestDefaultValues.EmptyPlayerId,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultGender,
            TestDefaultValues.DefaultBirthDate);
    }

    [Test]
    public void ReturnFirstName() => player.GetFirstName().Should().Be(TestDefaultValues.DefaultFirstName);

    [Test]
    public void ReturnLastName() => player.GetLastName().Should().Be(TestDefaultValues.DefaultLastName);

    [Test]
    public void ReturnSecondLastName() => player.GetSecondLastName().Should().Be(TestDefaultValues.DefaultSecondLastName);

    [Test]
    public void ReturnBirthDate() => player.GetBirthDate().Should().Be(TestDefaultValues.DefaultBirthDate.ToString(System.Globalization.CultureInfo.InvariantCulture));

    [Test]
    public void ReturnGenderName() => player.GetGenderName().Should().Be(TestDefaultValues.DefaultGender.ToString());

    [Test]
    public void SetFirstName() {
        player.SetFirstName("Marcos");

        player.FirstName.Should().Be("Marcos");
    }

    [Test]
    public void SetLastName() {
        player.SetLastName("Sanchez");

        player.LastName.Should().Be("Sanchez");
    }

    [Test]
    public void SetSecondLastName() {
        player.SetSecondLastName("Lopez");

        player.SecondLastName.Should().Be("Lopez");
    }

    [Test]
    public void SetBirthDate() {
        var birthDate = new DateTime(1995, 5, 23);

        player.SetBirthDate(birthDate.ToString(CultureInfo.InvariantCulture));

        player.Birthdate.Should().Be(birthDate);
    }

    [Test]
    public void SetGender() {
        player.SetGender("Female");

        player.Gender.Should().Be(Gaffer.Api.Shared.Gender.FEMALE);
    }

    private static readonly DateTime Today = new(2026, 3, 15);
    private static readonly string Text100 = new('a', 100);
    private static readonly string Text101 = new('a', 101);

    private static Player CreateWith(string? firstName = "Ana", string? lastName = "Lopez", string? secondLastName = "Ruiz",
        Gender gender = Gender.FEMALE, DateTime? birthDate = null) =>
        Player.Create(Guid.NewGuid(), firstName, lastName, secondLastName, gender, birthDate ?? new DateTime(1990, 1, 1), Today);

    private static TestCaseData Creation(string name, Func<Player> create, string field) =>
        new TestCaseData(create, field).SetName($"RejectTheCreation({name})");

    public static IEnumerable<TestCaseData> InvalidCreations() {
        yield return Creation("FirstName null", () => CreateWith(firstName: null), "FirstName");
        yield return Creation("FirstName empty", () => CreateWith(firstName: ""), "FirstName");
        yield return Creation("FirstName whitespace", () => CreateWith(firstName: "  "), "FirstName");
        yield return Creation("FirstName 101", () => CreateWith(firstName: Text101), "FirstName");
        yield return Creation("LastName null", () => CreateWith(lastName: null), "LastName");
        yield return Creation("LastName whitespace", () => CreateWith(lastName: " "), "LastName");
        yield return Creation("LastName 101", () => CreateWith(lastName: Text101), "LastName");
        yield return Creation("SecondLastName empty", () => CreateWith(secondLastName: ""), "SecondLastName");
        yield return Creation("SecondLastName whitespace", () => CreateWith(secondLastName: "\t"), "SecondLastName");
        yield return Creation("SecondLastName 101", () => CreateWith(secondLastName: Text101), "SecondLastName");
        yield return Creation("BirthDate default", () => CreateWith(birthDate: default(DateTime)), "BirthDate");
        yield return Creation("BirthDate 1899-12-31", () => CreateWith(birthDate: new DateTime(1899, 12, 31)), "BirthDate");
        yield return Creation("BirthDate tomorrow", () => CreateWith(birthDate: Today.AddDays(1)), "BirthDate");
        yield return Creation("BirthDate one day under 16", () => CreateWith(birthDate: Today.AddYears(-16).AddDays(1)), "BirthDate");
        yield return Creation("Gender 99", () => CreateWith(gender: (Gender)99), "Gender");
    }

    [TestCaseSource(nameof(InvalidCreations))]
    public void RejectACreationThatBreaksARule(Func<Player> create, string field) {
        var act = () => create();

        act.Should().Throw<DomainValidationException>().Which.Errors.Keys.Should().Equal(field);
    }

    [Test]
    public void ReportEveryInvalidFieldOfACreationAtOnceWithoutTheRejectedValues() {
        var act = () => CreateWith(firstName: Text101, lastName: "", secondLastName: " ", gender: (Gender)7,
            birthDate: Today.AddDays(1));

        var errors = act.Should().Throw<DomainValidationException>().Which.Errors;
        errors.Keys.Should().BeEquivalentTo("FirstName", "LastName", "SecondLastName", "BirthDate", "Gender");
        errors.Values.SelectMany(messages => messages).Should().NotContain(message => message.Contains(Text101));
    }

    [Test]
    public void CreateAPlayerWhoseValuesAreAtTheLimits() {
        var player = CreateWith(firstName: Text100, lastName: Text100, secondLastName: Text100, birthDate: new DateTime(1900, 1, 1));

        player.FirstName.Should().Be(Text100);
        player.LastName.Should().Be(Text100);
        player.SecondLastName.Should().Be(Text100);
        player.Birthdate.Should().Be(new DateTime(1900, 1, 1));
    }

    [Test]
    public void CreateAPlayerWhoTurnsSixteenToday() => CreateWith(birthDate: Today.AddYears(-16)).Birthdate.Should().Be(Today.AddYears(-16));

    [Test]
    public void ReportAnAbsentBirthDateAsRequired() {
        var act = () => CreateWith(birthDate: default(DateTime));

        act.Should().Throw<DomainValidationException>().Which.Errors["BirthDate"].Should().Equal("BirthDate is required.");
    }

    [Test]
    public void CreateAPlayerWithoutSecondLastName() => CreateWith(secondLastName: null).SecondLastName.Should().BeNull();

    [Test]
    public void TreatAPlayerBornOnTheTwentyNinthOfFebruaryAsSixteenOnTheTwentyEighth() {
        // +16 years from a 29 February lands on a leap year except across a non-leap century (2100).
        var act = () => Player.Create(Guid.NewGuid(), "Ana", "Lopez", null, Gender.FEMALE, new DateTime(2084, 2, 29),
            new DateTime(2100, 2, 28));

        act.Should().NotThrow();
    }

    [Test]
    public void NotValidateWhenRehydratedThroughTheConstructor() {
        var act = () => new Player(Guid.NewGuid(), Text101, "", "", (Gender)99, Today.AddDays(1));

        act.Should().NotThrow();
    }

    private static TestCaseData Setter(string name, Action<Player> set, string field) =>
        new TestCaseData(set, field).SetName($"RejectTheSetter({name})");

    public static IEnumerable<TestCaseData> InvalidSetterCalls() {
        yield return Setter("FirstName empty", p => p.SetFirstName(""), "FirstName");
        yield return Setter("FirstName whitespace", p => p.SetFirstName("   "), "FirstName");
        yield return Setter("FirstName 101", p => p.SetFirstName(Text101), "FirstName");
        yield return Setter("LastName empty", p => p.SetLastName(""), "LastName");
        yield return Setter("LastName 101", p => p.SetLastName(Text101), "LastName");
        yield return Setter("SecondLastName empty", p => p.SetSecondLastName(""), "SecondLastName");
        yield return Setter("SecondLastName whitespace", p => p.SetSecondLastName("  "), "SecondLastName");
        yield return Setter("SecondLastName 101", p => p.SetSecondLastName(Text101), "SecondLastName");
        yield return Setter("BirthDate 1899-12-31", p => p.SetBirthDate(new DateTime(1899, 12, 31), Today), "BirthDate");
        yield return Setter("BirthDate tomorrow", p => p.SetBirthDate(Today.AddDays(1), Today), "BirthDate");
        yield return Setter("BirthDate one day under 16", p => p.SetBirthDate(Today.AddYears(-16).AddDays(1), Today), "BirthDate");
        yield return Setter("Gender 99", p => p.SetGender((Gender)99), "Gender");
    }

    [TestCaseSource(nameof(InvalidSetterCalls))]
    public void RejectAnInvalidValueInASetterAndKeepTheCurrentOne(Action<Player> set, string field) {
        var before = (player.FirstName, player.LastName, player.SecondLastName, player.Birthdate, player.Gender);

        var act = () => set(player);

        act.Should().Throw<DomainValidationException>().Which.Errors.Keys.Should().Equal(field);
        (player.FirstName, player.LastName, player.SecondLastName, player.Birthdate, player.Gender).Should().Be(before);
    }

    [Test]
    public void AcceptSetterValuesAtTheLimits() {
        player.SetFirstName(Text100);
        player.SetLastName(Text100);
        player.SetSecondLastName(Text100);
        player.SetBirthDate(new DateTime(1900, 1, 1), Today);
        player.SetBirthDate(Today.AddYears(-16), Today);
        player.SetGender(Gender.OTHER);

        player.FirstName.Should().Be(Text100);
        player.LastName.Should().Be(Text100);
        player.SecondLastName.Should().Be(Text100);
        player.Birthdate.Should().Be(Today.AddYears(-16));
        player.Gender.Should().Be(Gender.OTHER);
    }

    [Test]
    public void CollectTheErrorsOfSeveralSettersBeforeFailing() {
        var errors = new DomainValidationErrors();

        errors.Collect(() => player.SetFirstName(""));
        errors.Collect(() => player.SetLastName("Valido"));
        errors.Collect(() => player.SetGender((Gender)99));
        var act = () => errors.ThrowIfAny();

        act.Should().Throw<DomainValidationException>().Which.Errors.Keys.Should().BeEquivalentTo("FirstName", "Gender");
    }

    [Test]
    public void NotFailWhenNoErrorWasCollected() {
        var errors = new DomainValidationErrors();

        errors.Collect(() => player.SetFirstName("Valido"));

        errors.Invoking(e => e.ThrowIfAny()).Should().NotThrow();
    }
}
