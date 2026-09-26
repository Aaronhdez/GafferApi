using AwesomeAssertions;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Integration;

/// <summary>Schema of GafferDB_Test after running Gaffer.Api.Migrations.</summary>
public class DatabaseSchemaShould
{
    private SqlConnectionProvider _provider;

    [SetUp]
    public void SetUp() => _provider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);

    [TearDown]
    public async Task TearDown()
    {
        await _provider.ClearAsync(DbTableNames.TablesToDelete);
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task HaveAnAddressesTableAndNoAdressesTable()
    {
        var tables = await _provider.QueryAsync<string>(
            "SELECT TABLE_NAME FROM information_schema.TABLES " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ('Addresses', 'Adresses');");

        tables.Should().Equal("Addresses");
    }

    [Test]
    public async Task HaveAFacilitiesDeleteTriggerThatDeletesFromAddresses()
    {
        var statement = await _provider.QueryScalarAsync<string>(
            "SELECT ACTION_STATEMENT FROM information_schema.TRIGGERS " +
            "WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME = 'TRG_Facilities_BeforeDelete';");

        statement.Should().Contain("DELETE FROM Addresses").And.NotContain("Adresses");
    }

    [Test]
    public async Task HaveACascadingForeignKeyFromFacilitiesAddressesToAddresses()
    {
        var constraints = await _provider.QueryAsync<(string Name, string ReferencedTable, string DeleteRule)>(
            "SELECT CONSTRAINT_NAME, REFERENCED_TABLE_NAME, DELETE_RULE FROM information_schema.REFERENTIAL_CONSTRAINTS " +
            "WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'Facilities_Addresses' " +
            "AND CONSTRAINT_NAME LIKE 'FK_Facilities_Addresses_A%dresses';");

        constraints.Should().Equal(("FK_Facilities_Addresses_Addresses", "Addresses", "CASCADE"));
    }

    [Test]
    public async Task DeleteTheAddressOfAFacilityDeletedDirectly()
    {
        var facilityId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await _provider.ExecuteAsync(
            "INSERT INTO `Facilities` (Id, Name, Active, BuildDate, Type, Capacity) " +
            "VALUES (@FacilityId, 'Trigger Probe', 1, '2020-01-01', 'Arena', 10);" +
            "INSERT INTO `Addresses` (Id, Street, StreetNumber, PostalCode, City, Region, Country) " +
            "VALUES (@AddressId, 'S', '1', '0', 'C', 'R', 'K');" +
            "INSERT INTO `Facilities_Addresses` (FacilityId, AddressId) VALUES (@FacilityId, @AddressId);",
            new { FacilityId = facilityId, AddressId = addressId });

        await _provider.ExecuteAsync("DELETE FROM `Facilities` WHERE Id = @FacilityId;", new { FacilityId = facilityId });

        var remaining = await _provider.QueryScalarAsync<long>(
            "SELECT COUNT(*) FROM `Addresses` WHERE Id = @AddressId;", new { AddressId = addressId });
        remaining.Should().Be(0);
    }
}
