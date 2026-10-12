using AwesomeAssertions;

namespace Gaffer.Api.Tests.Unit.Startups;

public class EnvironmentNameResolutionShould {
    [TestCase("Staging", "Staging")]
    [TestCase("Production", "Production")]
    [TestCase("Test", "Test")]
    public void UseThePresentValue(string value, string expected) => Program.ResolveEnvironmentName(value).Should().Be(expected);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t  ")]
    public void DefaultToDevelopmentWhenTheValueIsAbsentOrBlank(string? value) => Program.ResolveEnvironmentName(value).Should().Be("Development");
}
