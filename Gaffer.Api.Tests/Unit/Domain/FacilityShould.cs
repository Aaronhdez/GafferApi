using AwesomeAssertions;
using Gaffer.Api.Domain;

namespace Gaffer.Api.Tests.Unit.Domain;

public class FacilityShould
{
    private static readonly DateTime Today = new(2026, 10, 12);
    private static readonly string TooLongText = new('a', 101);
    private static readonly string TooLongCode = new('1', 21);

    public sealed record Values(
        string? Name = "City Arena",
        DateTime? BuildDate = null,
        FacilityType Type = FacilityType.Arena,
        int Capacity = 18000,
        bool WithAddress = true,
        string? Street = "Arena Road",
        string? StreetNumber = "50",
        string? PostalCode = "08001",
        string? City = "Barcelona",
        string? Region = "Catalonia",
        string? Country = "Spain");

    private static Facility Create(Values values, List<Stand>? stands = null) => Facility.Create(
        Guid.NewGuid(),
        values.Name,
        true,
        values.BuildDate ?? new DateTime(2020, 7, 20),
        values.Type,
        values.Capacity,
        values.WithAddress ? AddressOf(values) : null,
        stands,
        Today);

    private static Address AddressOf(Values values) => new(
        Guid.NewGuid(), values.Street!, values.StreetNumber!, values.PostalCode!, values.City!, values.Region!, values.Country!);

    public static IEnumerable<TestCaseData> InvalidValues()
    {
        TestCaseData Case(string name, string field, Values values) =>
            new TestCaseData(field, values).SetArgDisplayNames(name);

        var valid = new Values();
        yield return Case("Name null", "Name", valid with { Name = null });
        yield return Case("Name empty", "Name", valid with { Name = "" });
        yield return Case("Name whitespace", "Name", valid with { Name = "   " });
        yield return Case("Name 101", "Name", valid with { Name = TooLongText });
        yield return Case("BuildDate default", "BuildDate", valid with { BuildDate = default(DateTime) });
        yield return Case("BuildDate 1799-12-31", "BuildDate", valid with { BuildDate = new DateTime(1799, 12, 31) });
        yield return Case("BuildDate tomorrow", "BuildDate", valid with { BuildDate = Today.AddDays(1) });
        yield return Case("Type 99", "Type", valid with { Type = (FacilityType)99 });
        yield return Case("Capacity -1", "Capacity", valid with { Capacity = -1 });
        yield return Case("Address absent", "Address", valid with { WithAddress = false });
        foreach (var (field, change) in AddressFieldChanges())
        {
            yield return Case($"Address.{field} null", $"Address.{field}", change(valid, null));
            yield return Case($"Address.{field} empty", $"Address.{field}", change(valid, ""));
            yield return Case($"Address.{field} whitespace", $"Address.{field}", change(valid, "   "));
            var tooLong = field is "StreetNumber" or "PostalCode" ? TooLongCode : TooLongText;
            yield return Case($"Address.{field} {tooLong.Length}", $"Address.{field}", change(valid, tooLong));
        }
    }

    private static IEnumerable<(string Field, Func<Values, string?, Values> Change)> AddressFieldChanges()
    {
        yield return ("Street", (values, value) => values with { Street = value });
        yield return ("StreetNumber", (values, value) => values with { StreetNumber = value });
        yield return ("PostalCode", (values, value) => values with { PostalCode = value });
        yield return ("City", (values, value) => values with { City = value });
        yield return ("Region", (values, value) => values with { Region = value });
        yield return ("Country", (values, value) => values with { Country = value });
    }

    [TestCaseSource(nameof(InvalidValues))]
    public void RejectTheCreation(string field, Values values)
    {
        var act = () => Create(values);

        var exception = act.Should().ThrowExactly<DomainValidationException>().Which;
        exception.Errors.Keys.Should().Equal(field);
        exception.Errors[field].Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void CreateAFacilityWithEveryValueAtItsLimits()
    {
        var values = new Values(
            Name: new string('n', 100), BuildDate: new DateTime(1800, 1, 1), Capacity: 0,
            Street: new string('s', 100), StreetNumber: new string('9', 20), PostalCode: new string('8', 20),
            City: new string('c', 100), Region: new string('r', 100), Country: new string('p', 100));

        var facility = Create(values);

        facility.Name.Should().Be(values.Name);
        facility.BuildDate.Should().Be(new DateTime(1800, 1, 1));
        facility.Capacity.Should().Be(0);
        facility.Address.StreetNumber.Should().Be(values.StreetNumber);
        facility.Address.Country.Should().Be(values.Country);
    }

    [Test]
    public void AcceptABuildDateOfTodayAndAnyLargeCapacity()
    {
        var facility = Create(new Values(BuildDate: Today, Capacity: int.MaxValue));

        facility.BuildDate.Should().Be(Today);
        facility.Capacity.Should().Be(int.MaxValue);
    }

    [Test]
    public void UseTheCurrentUtcDateWhenNoReferenceDateIsGiven()
    {
        var act = () => Facility.Create(Guid.NewGuid(), "Arena", true, DateTime.UtcNow.Date.AddDays(1), FacilityType.Arena, 1,
            AddressOf(new Values()), null);

        act.Should().ThrowExactly<DomainValidationException>().Which.Errors.Keys.Should().Equal("BuildDate");
    }

    [Test]
    public void ReportEveryInvalidFieldOfTheFacilityAndItsAddressAtOnceWithoutEchoingTheValues()
    {
        var values = new Values(Name: TooLongText, BuildDate: Today.AddDays(1), Type: (FacilityType)99, Capacity: -1,
            Street: TooLongText, PostalCode: "");

        var act = () => Create(values);

        var errors = act.Should().ThrowExactly<DomainValidationException>().Which.Errors;
        errors.Keys.Should().BeEquivalentTo("Name", "BuildDate", "Type", "Capacity", "Address.Street", "Address.PostalCode");
        errors.Values.SelectMany(messages => messages).Should().NotContain(message => message.Contains(TooLongText));
    }

    [Test]
    public void KeepRejectingMoreThanFourStandsOnCreation()
    {
        var stands = Enumerable.Range(0, 5).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 100)).ToList();

        var act = () => Create(new Values(), stands);

        act.Should().ThrowExactly<FacilityValidationException>().WithMessage("*Stands cannot exceed 4*");
    }

    [Test]
    public void RehydrateAStoredFacilityThatBreaksTheRulesWithoutValidatingIt()
    {
        var address = new Address(Guid.NewGuid(), "", "1", new string('7', 30), "City", "Region", "Country");

        var facility = new Facility(Guid.NewGuid(), new string('x', 150), true, new DateTime(1700, 1, 1),
            (FacilityType)99, -5, address, null);

        facility.Name.Should().HaveLength(150);
        facility.Capacity.Should().Be(-5);
        facility.Address.PostalCode.Should().HaveLength(30);
    }

    [TestCase("Street")]
    [TestCase("PostalCode")]
    public void RejectAnInvalidAddressOnItsOwnWithTheAddressFieldName(string field)
    {
        var act = () => Address.Create(Guid.NewGuid(),
            field == "Street" ? "" : "Arena Road", "50", field == "PostalCode" ? TooLongCode : "08001",
            "Barcelona", "Catalonia", "Spain");

        act.Should().ThrowExactly<DomainValidationException>().Which.Errors.Keys.Should().Equal($"Address.{field}");
    }

    [Test]
    public void CreateAValidAddressOnItsOwn()
    {
        var id = Guid.NewGuid();

        var address = Address.Create(id, "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain");

        address.Should().BeEquivalentTo(new Address(id, "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"));
    }
}
