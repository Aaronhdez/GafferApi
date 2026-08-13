namespace Gaffer.Api.Tests.TestShared;

public static class DbTableNames
{
    public static readonly string PlayersTable = "Players";
    public static readonly string FacilitiesTable = "Facilities";
    public static readonly List<String> TablesToDelete = new()
    {
        PlayersTable,
        FacilitiesTable,
    };
}