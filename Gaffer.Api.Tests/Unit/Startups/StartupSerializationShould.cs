using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gaffer.Api.Tests.Unit.Startups;

public class StartupSerializationShould {
    [Test]
    public void LeaveTheSystemTextJsonOptionsWithTheFrameworkDefaults() {
        using var serviceProvider = StartupServices.Build();

        var jsonOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        jsonOptions.Converters.Should().BeEmpty();
        jsonOptions.PropertyNamingPolicy.Should().BeSameAs(new JsonOptions().JsonSerializerOptions.PropertyNamingPolicy);
    }

    [Test]
    public void SerializeWithNewtonsoftUsingUtcDatesAndEnumsAsText() {
        using var serviceProvider = StartupServices.Build();

        var settings = serviceProvider.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>().Value.SerializerSettings;

        settings.DateTimeZoneHandling.Should().Be(DateTimeZoneHandling.Utc);
        settings.Converters.Should().ContainSingle(converter => converter is StringEnumConverter);
    }
}
