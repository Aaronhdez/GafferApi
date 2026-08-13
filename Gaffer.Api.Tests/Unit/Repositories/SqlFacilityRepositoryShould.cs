using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Unit.Repositories;

public class SqlFacilityRepositoryShould : BaseSqlTest
{
    private IDatabaseConnectionProvider _databaseConnectionProvider;
    private SqlFacilityRepository _repository;

    [SetUp]
    public async Task Setup()
    {
        _tablesToDelete =
        [
            DbTableNames.PlayersTable,
            DbTableNames.FacilitiesTable,
        ];
        await ClearDatabase(_tablesToDelete);
        _databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        _repository = new SqlFacilityRepository(_databaseConnectionProvider);
    }

    [TearDown]
    public async Task TearDown()
    {
        await ClearDatabase(_tablesToDelete);
    }

    [TestFixture]
    public class WhenSavingAFacility : SqlFacilityRepositoryShould
    {
        [Test]
        public async Task SaveAFacilityWithoutStands()
        {
            var facility = new Facility(
                Guid.NewGuid(),
                "National Stadium",
                true,
                new DateTime(2018, 1, 5),
                FacilityType.Stadium,
                52000,
                new Address(
                    Guid.NewGuid(),
                    "Main Avenue",
                    "100",
                    "28001",
                    "Madrid",
                    "Madrid",
                    "Spain"),
                null);

            await _repository.Save(facility, CancellationToken.None);

            var savedFacilities = await _databaseConnectionProvider.QueryAsync<FacilityRow>(
                "SELECT Id, Name, Active, BuildDate, Type, Capacity FROM `Facilities`;"
            );
            var savedAddresses = await _databaseConnectionProvider.QueryAsync<AddressRow>(
                "SELECT Id, Street, StreetNumber, PostalCode, City, Region, Country FROM `Adresses`;"
            );
            var addressLinks = await _databaseConnectionProvider.QueryAsync<FacilityAddressRow>(
                "SELECT FacilityId, AddressId FROM `Facilities_Addresses`;"
            );
            var standLinksCount = await _databaseConnectionProvider.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM `Facilities_Stands`;"
            );

            savedFacilities.Should().ContainSingle();
            savedFacilities[0].Id.Should().Be(facility.Id);
            savedFacilities[0].Name.Should().Be(facility.Name);
            savedFacilities[0].Active.Should().Be(facility.Active);
            savedFacilities[0].BuildDate.Should().Be(facility.BuildDate);
            savedFacilities[0].Type.Should().Be(facility.Type.ToString());
            savedFacilities[0].Capacity.Should().Be(facility.Capacity);

            savedAddresses.Should().ContainSingle();
            savedAddresses[0].Id.Should().Be(facility.Address.Id);
            savedAddresses[0].Street.Should().Be(facility.Address.Street);
            savedAddresses[0].StreetNumber.Should().Be(facility.Address.StreetNumber);
            savedAddresses[0].PostalCode.Should().Be(facility.Address.PostalCode);
            savedAddresses[0].City.Should().Be(facility.Address.City);
            savedAddresses[0].Region.Should().Be(facility.Address.Region);
            savedAddresses[0].Country.Should().Be(facility.Address.Country);

            addressLinks.Should().ContainSingle();
            addressLinks[0].FacilityId.Should().Be(facility.Id);
            addressLinks[0].AddressId.Should().Be(facility.Address.Id);
            standLinksCount.Should().Be(0);
        }

        [Test]
        public async Task SaveAFacilityWithStands()
        {
            var stands = new List<Stand>
            {
                new(Guid.NewGuid(), "North", 4500),
                new(Guid.NewGuid(), "South", 4500),
            };
            var facility = new Facility(
                Guid.NewGuid(),
                "City Arena",
                true,
                new DateTime(2020, 7, 20),
                FacilityType.Arena,
                18000,
                new Address(
                    Guid.NewGuid(),
                    "Arena Road",
                    "50",
                    "08001",
                    "Barcelona",
                    "Catalonia",
                    "Spain"),
                stands);

            await _repository.Save(facility, CancellationToken.None);

            var savedStands = await _databaseConnectionProvider.QueryAsync<StandRow>(
                "SELECT Id, StandName, StandCapacity FROM `Stands`;"
            );
            var standLinks = await _databaseConnectionProvider.QueryAsync<FacilityStandRow>(
                "SELECT FacilityId, StandId FROM `Facilities_Stands`;"
            );

            savedStands.Count.Should().Be(2);
            savedStands.Should().Contain(stand => stand.Id == stands[0].Id && stand.StandName == stands[0].StandName && stand.StandCapacity == stands[0].StandCapacity);
            savedStands.Should().Contain(stand => stand.Id == stands[1].Id && stand.StandName == stands[1].StandName && stand.StandCapacity == stands[1].StandCapacity);

            standLinks.Count.Should().Be(2);
            standLinks.Should().Contain(link => link.FacilityId == facility.Id && link.StandId == stands[0].Id);
            standLinks.Should().Contain(link => link.FacilityId == facility.Id && link.StandId == stands[1].Id);
        }
    }

    private sealed class FacilityRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool Active { get; init; }
        public DateTime BuildDate { get; init; }
        public string Type { get; init; } = string.Empty;
        public int Capacity { get; init; }
    }

    private sealed class AddressRow
    {
        public Guid Id { get; init; }
        public string Street { get; init; } = string.Empty;
        public string StreetNumber { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
    }

    private sealed class StandRow
    {
        public Guid Id { get; init; }
        public string StandName { get; init; } = string.Empty;
        public int StandCapacity { get; init; }
    }

    private sealed class FacilityAddressRow
    {
        public Guid FacilityId { get; init; }
        public Guid AddressId { get; init; }
    }

    private sealed class FacilityStandRow
    {
        public Guid FacilityId { get; init; }
        public Guid StandId { get; init; }
    }
}