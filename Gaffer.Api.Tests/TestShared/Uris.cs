namespace Gaffer.Api.Tests.TestShared;

public static class Uris
{
    public static string BaseAddress;
    private static Uri BaseUri => new(BaseAddress+"api/v1.0/");
    public static Uri CreatePlayer() => new(BaseUri, $"players/");
    public static Uri CreateFacility() => new(BaseUri, $"facilities/");
    public static Uri GetPlayer(string playerId) => new(BaseUri, $"players/{playerId}");
    public static Uri DeletePlayer(string playerId) => new(BaseUri, $"players/{playerId}");
    public static Uri UpdatePlayer(string playerId) => new(BaseUri, $"players/{playerId}");
}