using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;

namespace Gaffer.Api.Tests.Unit.Startups;

public class StartupSecurityShould {
    [Test]
    public async Task NotRegisterAnyAuthenticationScheme() {
        using var serviceProvider = StartupServices.Build();

        var schemeProvider = serviceProvider.GetService<IAuthenticationSchemeProvider>();

        var schemes = schemeProvider is null ? [] : await schemeProvider.GetAllSchemesAsync();
        schemes.Select(scheme => scheme.Name).Should().BeEmpty();
    }
}
