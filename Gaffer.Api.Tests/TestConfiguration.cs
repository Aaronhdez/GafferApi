using Microsoft.Extensions.Configuration;

namespace Gaffer.Api.Tests;

public static class TestConfiguration
{
    private static IConfiguration _configuration;

    private static IConfiguration Get()
    {
        if (_configuration != null)
            return _configuration;
        _configuration = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("testSettings.json", true, false)
            .Build();
        return _configuration;
    }

    public static string GetConnectionString(string connectionName)
    {
        return Get().GetConnectionString(connectionName);
    }
}