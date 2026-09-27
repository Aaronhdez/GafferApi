namespace Gaffer.Api.Tests.TestShared;

public static class Uris
{
    public static string BaseAddress = string.Empty;
    private static Uri BaseUri => new(BaseAddress+"api/v1.0/");
    public static Uri CreatePlayer() => new(BaseUri, $"players/");
    public static Uri CreateFacility() => new(BaseUri, $"facilities/");
    public static Uri GetPlayer(string playerId) => new(BaseUri, $"players/{playerId}");
    public static Uri SearchPlayers() => new(BaseUri, $"players/search");
    public static Uri DeletePlayer(string playerId) => new(BaseUri, $"players/{playerId}");
    public static Uri UpdatePlayer(string playerId) => new(BaseUri, $"players/{playerId}");
    public static Uri GetFacility(string facilityId) => new(BaseUri, $"facilities/{facilityId}");
    public static Uri GetAllFacilities() => new(BaseUri, $"facilities/");
    public static Uri DeleteFacility(string facilityId) => new(BaseUri, $"facilities/{facilityId}");
    public static Uri UpdateFacility(string facilityId) => new(BaseUri, $"facilities/{facilityId}");
}