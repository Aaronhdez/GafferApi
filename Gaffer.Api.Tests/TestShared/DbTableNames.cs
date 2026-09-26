namespace Gaffer.Api.Tests.TestShared;

public static class DbTableNames
{
    public static readonly string PlayersTable = "Players";
    public static readonly string FacilitiesTable = "Facilities";
    public static readonly string AddressesTable = "Addresses";
    public static readonly string StandsTable = "Stands";
    public static readonly string FacilitiesAddressesTable = "Facilities_Addresses";
    public static readonly string FacilitiesStandsTable = "Facilities_Stands";
    public static readonly List<String> TablesToDelete = new()
    {
        FacilitiesAddressesTable,
        FacilitiesStandsTable,
        AddressesTable,
        StandsTable,
        PlayersTable,
        FacilitiesTable,
    };
}