using AwesomeAssertions;
using Gaffer.Api.Domain;

namespace Gaffer.Api.Tests.Unit.Domain;

/// <summary>Step 03b (OPT-037): the full matrix of the stand rules lives here (Q9 "Solo forma").</summary>
public class StandShould {
    private static readonly string TooLongName = new('a', 101);

    public static IEnumerable<TestCaseData> InvalidStands() {
        TestCaseData Case(string name, string field, string? standName, int standCapacity) =>
            new TestCaseData(field, standName, standCapacity).SetArgDisplayNames(name);

        yield return Case("StandName empty", "StandName", "", 10);
        yield return Case("StandName whitespace", "StandName", "   ", 10);
        yield return Case("StandName 101 chars", "StandName", TooLongName, 10);
        yield return Case("StandName null", "StandName", null, 10);
        yield return Case("StandCapacity -1", "StandCapacity", "East", -1);
        yield return Case("StandCapacity int.MinValue", "StandCapacity", "East", int.MinValue);
    }

    [TestCaseSource(nameof(InvalidStands))]
    public void RejectAnInvalidStandNamingTheField(string field, string? standName, int standCapacity) {
        var act = () => Stand.Create(Guid.NewGuid(), standName, standCapacity);

        var errors = act.Should().ThrowExactly<DomainValidationException>().Which.Errors;
        errors.Keys.Should().Equal(field);
        errors[field].Should().ContainSingle();
    }

    [TestCase(1, 0)]
    [TestCase(100, 0)]
    [TestCase(50, int.MaxValue)]
    public void AcceptAStandAtTheLimits(int nameLength, int standCapacity) {
        var id = Guid.NewGuid();
        var standName = new string('s', nameLength);

        var stand = Stand.Create(id, standName, standCapacity);

        stand.Should().BeEquivalentTo(new Stand(id, standName, standCapacity));
    }

    [Test]
    public void ReportEveryInvalidFieldAtOnceWithoutEchoingTheValues() {
        var act = () => Stand.Create(Guid.NewGuid(), TooLongName, -7);

        var errors = act.Should().ThrowExactly<DomainValidationException>().Which.Errors;
        errors.Keys.Should().BeEquivalentTo("StandName", "StandCapacity");
        errors.Values.SelectMany(messages => messages).Should()
            .NotContain(message => message.Contains(TooLongName) || message.Contains("-7"));
    }

    [Test]
    public void NameTheFieldsWithThePrefixGivenByWhoBuildsTheList() {
        var errors = new DomainValidationErrors();

        Stand.CheckFields(errors, "Stands[2].", null, -1);

        var act = () => errors.ThrowIfAny();
        var reported = act.Should().ThrowExactly<DomainValidationException>().Which.Errors;
        reported.Keys.Should().BeEquivalentTo("Stands[2].StandName", "Stands[2].StandCapacity");
        reported["Stands[2].StandName"].Should().Equal("Stands[2].StandName is required.");
        reported["Stands[2].StandCapacity"].Should().Equal("Stands[2].StandCapacity must not be negative.");
    }

    [Test]
    public void RehydrateAStoredStandThatBreaksTheRulesWithoutValidatingIt() {
        var stand = new Stand(Guid.NewGuid(), new string('x', 300), -5);

        stand.StandName.Should().HaveLength(300);
        stand.StandCapacity.Should().Be(-5);
    }
}
