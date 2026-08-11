using Gaffer.Api.Versioning;

namespace Gaffer.Api.Tests.TestShared;

public static class Uris
{
    public static string BaseAddress;
    public static Uri BaseUri => new Uri(BaseAddress);
    public static Uri CreatePlayer => new Uri(BaseUri, $"api/v1.0/players/");
    public static Uri GetPlayer(string playerId) => new Uri(BaseUri, $"api/v1.0/players/{playerId}");
}