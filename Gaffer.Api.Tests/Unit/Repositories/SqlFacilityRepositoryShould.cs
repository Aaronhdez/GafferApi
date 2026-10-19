using System.Reflection;
using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Facilities;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Repositories;

public class SqlFacilityRepositoryShould : BaseSqlTest {
    private IDatabaseConnectionProvider databaseConnectionProvider;
    private SqlFacilityRepository repository;

    [SetUp]
    public async Task Setup() {
        TablesToDelete =
        [
            DbTableNames.FacilitiesAddressesTable,
            DbTableNames.FacilitiesStandsTable,
            DbTableNames.AddressesTable,
            DbTableNames.StandsTable,
            DbTableNames.PlayersTable,
            DbTableNames.FacilitiesTable,
        ];
        await ClearDatabase(TablesToDelete);
        databaseConnectionProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        repository = new SqlFacilityRepository(databaseConnectionProvider);
    }

    [TearDown]
    public async Task TearDown() {
        await ClearDatabase(TablesToDelete);
        await ((IAsyncDisposable)databaseConnectionProvider).DisposeAsync();
    }

    [TestFixture]
    public class WhenSavingAFacility : SqlFacilityRepositoryShould {
        [Test]
        public async Task SaveAFacilityWithoutStands() {
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

            await repository.Save(facility, CancellationToken.None);

            var savedFacilities = await databaseConnectionProvider.QueryAsync<FacilityRow>(
                "SELECT Id, Name, Active, BuildDate, Type, Capacity FROM `Facilities`;"
            );
            var savedAddresses = await databaseConnectionProvider.QueryAsync<AddressRow>(
                "SELECT Id, Street, StreetNumber, PostalCode, City, Region, Country FROM `Addresses`;"
            );
            var addressLinks = await databaseConnectionProvider.QueryAsync<FacilityAddressRow>(
                "SELECT FacilityId, AddressId FROM `Facilities_Addresses`;"
            );
            var standLinksCount = await databaseConnectionProvider.QueryScalarAsync<int>(
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
        public async Task SaveAFacilityWithStands() {
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

            await repository.Save(facility, CancellationToken.None);

            var savedStands = await databaseConnectionProvider.QueryAsync<StandRow>(
                "SELECT Id, StandName, StandCapacity FROM `Stands`;"
            );
            var standLinks = await databaseConnectionProvider.QueryAsync<FacilityStandRow>(
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

    [TestFixture]
    public class WhenGettingFacilities : SqlFacilityRepositoryShould {
        [Test]
        public async Task ReturnThePreviouslySavedFacilityWithAddressAndStands() {
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
                new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
                stands);
            await repository.Save(facility, CancellationToken.None);

            var result = await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{facility.Id}" }]
            }, CancellationToken.None);

            result.Should().ContainSingle();
            result[0].Id.Should().Be(facility.Id);
            result[0].Name.Should().Be(facility.Name);
            result[0].Active.Should().Be(facility.Active);
            result[0].BuildDate.Should().Be(facility.BuildDate);
            result[0].Type.Should().Be(facility.Type);
            result[0].Capacity.Should().Be(facility.Capacity);
            result[0].Address.Id.Should().Be(facility.Address.Id);
            result[0].Address.Street.Should().Be(facility.Address.Street);
            result[0].Address.StreetNumber.Should().Be(facility.Address.StreetNumber);
            result[0].Address.PostalCode.Should().Be(facility.Address.PostalCode);
            result[0].Address.City.Should().Be(facility.Address.City);
            result[0].Address.Region.Should().Be(facility.Address.Region);
            result[0].Address.Country.Should().Be(facility.Address.Country);
            result[0].Stands.Count.Should().Be(2);
            result[0].Stands.Should().Contain(s => s.Id == stands[0].Id && s.StandName == stands[0].StandName && s.StandCapacity == stands[0].StandCapacity);
            result[0].Stands.Should().Contain(s => s.Id == stands[1].Id && s.StandName == stands[1].StandName && s.StandCapacity == stands[1].StandCapacity);
        }

        [Test]
        public async Task ReadAStoredFacilityThatBreaksTheCreationRulesWithoutValidatingIt() {
            var facilityId = Guid.NewGuid();
            var addressId = Guid.NewGuid();
            await databaseConnectionProvider.ExecuteAsync(
                "INSERT INTO `Facilities` (Id, Name, Active, BuildDate, Type, Capacity) VALUES (@Id, @Name, 1, @BuildDate, 'Arena', -5);",
                new { Id = facilityId, Name = new string('x', 150), BuildDate = new DateTime(1700, 1, 1) });
            await databaseConnectionProvider.ExecuteAsync(
                "INSERT INTO `Addresses` (Id, Street, StreetNumber, PostalCode, City, Region, Country) " +
                "VALUES (@Id, '', '1', @PostalCode, 'City', 'Region', 'Country');",
                new { Id = addressId, PostalCode = new string('7', 30) });
            await databaseConnectionProvider.ExecuteAsync(
                "INSERT INTO `Facilities_Addresses` (FacilityId, AddressId) VALUES (@FacilityId, @AddressId);",
                new { FacilityId = facilityId, AddressId = addressId });

            var result = await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{facilityId}" }]
            }, CancellationToken.None);

            var stored = result.Should().ContainSingle().Subject;
            stored.Name.Should().HaveLength(150);
            stored.Capacity.Should().Be(-5);
            stored.BuildDate.Should().Be(new DateTime(1700, 1, 1));
            stored.Address.Street.Should().BeEmpty();
            stored.Address.PostalCode.Should().HaveLength(30);
        }

        [Test]
        public async Task ReadAStoredStandThatBreaksTheCreationRulesWithoutValidatingIt() {
            var facility = new Facility(Guid.NewGuid(), "Legacy Stands Arena", true, new DateTime(2020, 1, 1), FacilityType.Arena, 100,
                new Address(Guid.NewGuid(), "Street", "1", "08001", "City", "Region", "Country"), []);
            await repository.Save(facility, CancellationToken.None);
            var standId = Guid.NewGuid();
            await databaseConnectionProvider.ExecuteAsync(
                "INSERT INTO `Stands` (Id, StandName, StandCapacity) VALUES (@Id, '', -5);", new { Id = standId });
            await databaseConnectionProvider.ExecuteAsync(
                "INSERT INTO `Facilities_Stands` (FacilityId, StandId) VALUES (@FacilityId, @StandId);",
                new { FacilityId = facility.Id, StandId = standId });

            var stands = await repository.GetStands(facility.Id, CancellationToken.None);
            var stand = await repository.GetStand(facility.Id, standId, CancellationToken.None);

            stands.Should().ContainSingle().Which.Should().BeEquivalentTo(new Stand(standId, "", -5));
            stand.Should().BeEquivalentTo(new Stand(standId, "", -5));
        }

        [Test]
        public async Task ReturnEmptyListWhenNoFacilityMatchesCriteria() {
            var facility = new Facility(
                Guid.NewGuid(),
                "National Stadium",
                true,
                new DateTime(2018, 1, 5),
                FacilityType.Stadium,
                52000,
                new Address(Guid.NewGuid(), "Main Avenue", "100", "28001", "Madrid", "Madrid", "Spain"),
                null);
            await repository.Save(facility, CancellationToken.None);

            var result = await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{Guid.NewGuid()}" }]
            }, CancellationToken.None);

            result.Should().BeEmpty();
        }

        [TestCase("Street")]
        [TestCase("Name IN ('x') OR 1=1 --")]
        public async Task ThrowWithoutQueryingWhenTheFilterFieldIsNotPermitted(string fieldName) {
            var connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
            var repository = new SqlFacilityRepository(connectionProvider);

            var action = async () => await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = fieldName, Value = "x" }]
            }, CancellationToken.None);

            (await action.Should().ThrowExactlyAsync<FacilitySearchFilterNotPermittedException>())
                .Which.FieldName.Should().Be(fieldName);
            connectionProvider.ReceivedCalls().Should().BeEmpty();
        }
    }

    [TestFixture]
    public class WhenUpdatingAFacility : SqlFacilityRepositoryShould {
        [Test]
        public async Task UpdateFieldsAddressAndStandsInPlaceWithoutDeletingTheStandsNotSent() {
            var north = new Stand(Guid.NewGuid(), "North", 4500);
            var south = new Stand(Guid.NewGuid(), "South", 4500);
            var original = BuildCityArena([north, south]);
            await repository.Save(original, CancellationToken.None);

            var east = new Stand(Guid.NewGuid(), "East", 6000);
            var updated = new Facility(
                original.Id,
                "Renovated Arena",
                false,
                new DateTime(2023, 3, 15),
                FacilityType.Stadium,
                60000,
                new Address(original.Address.Id, "New Road", "99", "41001", "Seville", "Andalusia", "Spain"),
                [new Stand(north.Id, "North Renovated", 5000), east]);

            await repository.Update(original.Id, _ => updated, CancellationToken.None);

            var result = await GetById(original.Id);
            result.Name.Should().Be(updated.Name);
            result.Active.Should().Be(updated.Active);
            result.BuildDate.Should().Be(updated.BuildDate);
            result.Type.Should().Be(updated.Type);
            result.Capacity.Should().Be(updated.Capacity);
            result.Address.Id.Should().Be(original.Address.Id);
            result.Address.Street.Should().Be("New Road");
            result.Address.City.Should().Be("Seville");
            result.Stands.Select(Describe).Should().BeEquivalentTo([
                $"{north.Id}:North Renovated:5000",
                $"{south.Id}:South:4500",
                $"{east.Id}:East:6000"
            ]);

            var addresses = await databaseConnectionProvider.QueryAsync<AddressRow>(
                "SELECT Id, Street, StreetNumber, PostalCode, City, Region, Country FROM `Addresses`;");
            addresses.Select(address => address.Id).Should().Equal(original.Address.Id);
            var addressLinks = await databaseConnectionProvider.QueryAsync<FacilityAddressRow>(
                "SELECT FacilityId, AddressId FROM `Facilities_Addresses`;");
            addressLinks.Select(link => $"{link.FacilityId}:{link.AddressId}")
                .Should().Equal($"{original.Id}:{original.Address.Id}");
            var stands = await databaseConnectionProvider.QueryAsync<StandRow>(
                "SELECT Id, StandName, StandCapacity FROM `Stands`;");
            stands.Select(stand => stand.Id).Should().BeEquivalentTo([north.Id, south.Id, east.Id]);
            var standLinks = await databaseConnectionProvider.QueryAsync<FacilityStandRow>(
                "SELECT FacilityId, StandId FROM `Facilities_Stands`;");
            standLinks.Select(link => $"{link.FacilityId}:{link.StandId}").Should().BeEquivalentTo([
                $"{original.Id}:{north.Id}",
                $"{original.Id}:{south.Id}",
                $"{original.Id}:{east.Id}"
            ]);
        }

        [Test]
        public async Task LeaveEverythingUnchangedWhenAnOperationOfTheUpdateFails() {
            var north = new Stand(Guid.NewGuid(), "North", 4500);
            var south = new Stand(Guid.NewGuid(), "South", 4500);
            var original = BuildCityArena([north, south]);
            await repository.Save(original, CancellationToken.None);
            var standNameLongerThanItsColumn = new string('x', 300);
            var updated = new Facility(
                original.Id,
                "Renovated Arena",
                false,
                new DateTime(2023, 3, 15),
                FacilityType.Stadium,
                60000,
                new Address(original.Address.Id, "New Road", "99", "41001", "Seville", "Andalusia", "Spain"),
                [new Stand(north.Id, "North Renovated", 5000), new Stand(Guid.NewGuid(), standNameLongerThanItsColumn, 1000)]);

            var act = async () => await repository.Update(original.Id, _ => updated, CancellationToken.None);

            var failure = await act.Should().ThrowAsync<Exception>();
            TestContext.Out.WriteLine(failure.Which.Message);
            var result = await GetById(original.Id);
            result.Name.Should().Be("City Arena");
            result.Capacity.Should().Be(18000);
            result.Address.Id.Should().Be(original.Address.Id);
            result.Address.Street.Should().Be("Arena Road");
            result.Stands.Select(Describe).Should().BeEquivalentTo([
                $"{north.Id}:North:4500",
                $"{south.Id}:South:4500"
            ]);
            (await databaseConnectionProvider.QueryScalarAsync<int>("SELECT COUNT(*) FROM `Stands`;")).Should().Be(2);
            (await databaseConnectionProvider.QueryScalarAsync<int>("SELECT COUNT(*) FROM `Facilities_Stands`;")).Should().Be(2);
        }

        private static Facility BuildCityArena(List<Stand> stands) => new(
            Guid.NewGuid(),
            "City Arena",
            true,
            new DateTime(2020, 7, 20),
            FacilityType.Arena,
            18000,
            new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
            stands);

        private async Task<Facility> GetById(Guid id) {
            var result = await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{id}" }]
            }, CancellationToken.None);
            result.Should().ContainSingle();
            return result[0];
        }

        private static string Describe(Stand stand) => $"{stand.Id}:{stand.StandName}:{stand.StandCapacity}";

        [Test]
        public void NotAllowConstructingAFacilityWithMoreThanFourStandsSoDbIsNeverMutated() {
            var original = new Facility(
                Guid.NewGuid(),
                "City Arena",
                true,
                new DateTime(2020, 7, 20),
                FacilityType.Arena,
                18000,
                new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
                [new Stand(Guid.NewGuid(), "North", 4500)]);

            var invalidStands = Enumerable.Range(0, 5)
                .Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 1000))
                .ToList();

            var act = () => new Facility(
                original.Id,
                original.Name,
                original.Active,
                original.BuildDate,
                original.Type,
                original.Capacity,
                original.Address,
                invalidStands);

            act.Should().Throw<FacilityValidationException>();
        }
    }

    [TestFixture]
    public class WhenPropagatingTheCancellationToken : SqlFacilityRepositoryShould {
        private TokenRecordingConnectionProvider recorder;
        private SqlFacilityRepository recordedRepository;
        private CancellationTokenSource cancellation;

        [SetUp]
        public void SetUpRecorder() {
            recorder = new TokenRecordingConnectionProvider(databaseConnectionProvider);
            recordedRepository = new SqlFacilityRepository(recorder);
            cancellation = new CancellationTokenSource();
        }

        [TearDown]
        public void DisposeCancellation() => cancellation.Dispose();

        [Test]
        public async Task PassItsTokenToTheProviderWhenSaving() {
            await recordedRepository.Save(AFacilityWithTwoStands(), cancellation.Token);

            // Transaction + facility + address and its link + two stands and their links.
            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 8);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenGettingIncludingAddressAndStandsLookups() {
            var facility = AFacilityWithTwoStands();
            await repository.Save(facility, CancellationToken.None);

            var result = await recordedRepository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{facility.Id}" }]
            }, cancellation.Token);

            result.Should().ContainSingle();
            // Facilities + address lookup + stands lookup.
            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 3);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenUpdating() {
            var facility = AFacilityWithTwoStands();
            await repository.Save(facility, CancellationToken.None);
            var updated = new Facility(facility.Id, "Renamed Arena", facility.Active, facility.BuildDate, facility.Type,
                facility.Capacity, facility.Address,
                [facility.Stands[0], new Stand(Guid.NewGuid(), "East", 1000)]);

            await recordedRepository.Update(facility.Id, _ => updated, cancellation.Token);

            // Write alone: transaction + facility + address + linked stands lookup + one stand update + one stand insert and its link
            // (the row lock and the facility read inside the same transaction add more calls).
            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 7);
        }

        [Test]
        public async Task PassItsTokenToTheProviderWhenDeleting() {
            var facility = AFacilityWithTwoStands();
            await repository.Save(facility, CancellationToken.None);

            await recordedRepository.Delete(facility.Id, cancellation.Token);

            // Transaction + facility row; the schema (cascading FKs and TRG_Facilities_BeforeDelete) removes the rest.
            recorder.ShouldHaveReceivedOnly(cancellation.Token, minimumCalls: 2);
        }

        private static Facility AFacilityWithTwoStands() => new(
            Guid.NewGuid(),
            "City Arena",
            true,
            new DateTime(2020, 7, 20),
            FacilityType.Arena,
            18000,
            new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
            [new Stand(Guid.NewGuid(), "North", 4500), new Stand(Guid.NewGuid(), "South", 4500)]);
    }

    [TestFixture]
    public class WhenDeletingAFacility : SqlFacilityRepositoryShould {
        [Test]
        public async Task RemoveTheFacilityAndItsAddressAndStands() {
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
                new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
                stands);
            await repository.Save(facility, CancellationToken.None);

            await repository.Delete(facility.Id, CancellationToken.None);

            var result = await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{facility.Id}" }]
            }, CancellationToken.None);
            result.Should().BeEmpty();

            var remainingAddresses = await databaseConnectionProvider.QueryAsync<AddressRow>(
                "SELECT Id, Street, StreetNumber, PostalCode, City, Region, Country FROM `Addresses`;");
            remainingAddresses.Should().BeEmpty();

            var remainingStands = await databaseConnectionProvider.QueryAsync<StandRow>(
                "SELECT Id, StandName, StandCapacity FROM `Stands`;");
            remainingStands.Should().BeEmpty();

            var remainingAddressLinks = await databaseConnectionProvider.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM `Facilities_Addresses`;");
            remainingAddressLinks.Should().Be(0);

            var remainingStandLinks = await databaseConnectionProvider.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM `Facilities_Stands`;");
            remainingStandLinks.Should().Be(0);
        }

        /// <summary>
        /// Pins the schema dependency that lets <see cref="SqlFacilityRepository.Delete"/> remove only the
        /// <c>Facilities</c> row: the cascading foreign keys and <c>TRG_Facilities_BeforeDelete</c> clean the
        /// address, the stands and both link tables of that facility, and leave any other facility untouched.
        /// </summary>
        [Test]
        public async Task RemoveTheAddressStandsAndLinksThroughTheSchemaWhenOnlyTheFacilityRowIsDeleted() {
            var deleted = AFacilityWithStands("Deleted Arena", 2);
            var kept = AFacilityWithStands("Kept Arena", 3);
            await repository.Save(deleted, CancellationToken.None);
            await repository.Save(kept, CancellationToken.None);

            await databaseConnectionProvider.ExecuteAsync(
                "DELETE FROM `Facilities` WHERE Id = @Id;", new { Id = deleted.Id });

            (await RowsOf(deleted)).Should().Be((0, 0, 0, 0, 0));
            (await RowsOf(kept)).Should().Be((1, 1, 1, 3, 3));
        }

        private static Facility AFacilityWithStands(string name, int standCount) => new(
            Guid.NewGuid(),
            name,
            true,
            new DateTime(2020, 7, 20),
            FacilityType.Arena,
            18000,
            new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
            Enumerable.Range(0, standCount).Select(i => new Stand(Guid.NewGuid(), $"{name} {i}", 1000)).ToList());

        private async Task<(int Facilities, int Addresses, int AddressLinks, int Stands, int StandLinks)> RowsOf(Facility facility) {
            var standIds = facility.Stands.Select(stand => stand.Id).ToArray();
            return (
                await Count("SELECT COUNT(*) FROM `Facilities` WHERE Id = @Id;", new { facility.Id }),
                await Count("SELECT COUNT(*) FROM `Addresses` WHERE Id = @Id;", new { facility.Address.Id }),
                await Count("SELECT COUNT(*) FROM `Facilities_Addresses` WHERE FacilityId = @Id;", new { facility.Id }),
                await Count("SELECT COUNT(*) FROM `Stands` WHERE Id IN @Ids;", new { Ids = standIds }),
                await Count("SELECT COUNT(*) FROM `Facilities_Stands` WHERE FacilityId = @Id;", new { facility.Id }));
        }

        private Task<int> Count(string query, object parameters)
            => databaseConnectionProvider.QueryScalarAsync<int>(query, parameters);
    }

    [TestFixture]
    public class WhenManagingTheStandsOfAFacility : SqlFacilityRepositoryShould {
        private const string LockFacilityQuery = "SELECT Id FROM `Facilities` WHERE Id = @Id FOR UPDATE;";
        private SqlConnectionProvider holderProvider;
        private SqlConnectionProvider competitorProvider;

        [SetUp]
        public void SetUpProviders() {
            holderProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
            competitorProvider = new SqlConnectionProvider(TestDefaultValues.TestConnectionString);
        }

        [TearDown]
        public async Task DisposeProviders() {
            await holderProvider.DisposeAsync();
            await competitorProvider.DisposeAsync();
        }

        [Test]
        public async Task AddReadUpdateAndDeleteAStand() {
            var facility = await SaveAFacilityWithStands(1);
            var stand = new Stand(Guid.NewGuid(), "East", 3000);

            await repository.AddStand(facility.Id, stand, CancellationToken.None);
            (await repository.GetStands(facility.Id, CancellationToken.None)).Should().HaveCount(2);

            await repository.UpdateStand(facility.Id, new Stand(stand.Id, "East Renovated", 3500), CancellationToken.None);
            var updated = await repository.GetStand(facility.Id, stand.Id, CancellationToken.None);
            updated.StandName.Should().Be("East Renovated");
            updated.StandCapacity.Should().Be(3500);

            await repository.DeleteStand(facility.Id, stand.Id, CancellationToken.None);
            (await repository.GetStands(facility.Id, CancellationToken.None)).Should().ContainSingle()
                .Which.Id.Should().Be(facility.Stands[0].Id);
            (await databaseConnectionProvider.QueryScalarAsync<int>("SELECT COUNT(*) FROM `Stands`;")).Should().Be(1);
        }

        [Test]
        public async Task RejectAStandWhenTheFacilityAlreadyHasFour() {
            var facility = await SaveAFacilityWithStands(4);

            var act = async () => await repository.AddStand(facility.Id, new Stand(Guid.NewGuid(), "Fifth", 1000), CancellationToken.None);

            await act.Should().ThrowAsync<FacilityMaxStandsExceededException>();
            await AssertStandCount(facility.Id, 4);
        }

        [Test]
        public async Task ReportAMissingFacilityOnEveryStandOperation() {
            var missing = Guid.NewGuid();
            var stand = new Stand(Guid.NewGuid(), "East", 3000);

            await ((Func<Task>)(() => repository.GetStands(missing, CancellationToken.None))).Should().ThrowAsync<FacilityNotFoundException>();
            await ((Func<Task>)(() => repository.GetStand(missing, stand.Id, CancellationToken.None))).Should().ThrowAsync<FacilityNotFoundException>();
            await ((Func<Task>)(() => repository.AddStand(missing, stand, CancellationToken.None))).Should().ThrowAsync<FacilityNotFoundException>();
            await ((Func<Task>)(() => repository.UpdateStand(missing, stand, CancellationToken.None))).Should().ThrowAsync<FacilityNotFoundException>();
            await ((Func<Task>)(() => repository.DeleteStand(missing, stand.Id, CancellationToken.None))).Should().ThrowAsync<FacilityNotFoundException>();
        }

        [Test]
        public async Task ReportAMissingOrForeignStandWithoutTouchingIt() {
            var facility = await SaveAFacilityWithStands(1);
            var other = await SaveAFacilityWithStands(1);
            var foreign = other.Stands[0];

            await ((Func<Task>)(() => repository.GetStand(facility.Id, foreign.Id, CancellationToken.None))).Should().ThrowAsync<FacilityStandNotFoundException>();
            await ((Func<Task>)(() => repository.UpdateStand(facility.Id, new Stand(foreign.Id, "Hijacked", 1), CancellationToken.None))).Should().ThrowAsync<FacilityStandNotFoundException>();
            await ((Func<Task>)(() => repository.DeleteStand(facility.Id, foreign.Id, CancellationToken.None))).Should().ThrowAsync<FacilityStandNotFoundException>();
            await ((Func<Task>)(() => repository.DeleteStand(facility.Id, Guid.NewGuid(), CancellationToken.None))).Should().ThrowAsync<FacilityStandNotFoundException>();

            var untouched = await repository.GetStand(other.Id, foreign.Id, CancellationToken.None);
            untouched.StandName.Should().Be(foreign.StandName);
        }

        [Test]
        public async Task LeaveNoRowsWhenTheStandInsertFails() {
            var facility = await SaveAFacilityWithStands(2);
            var tooLong = new Stand(Guid.NewGuid(), new string('x', 300), 1000);

            var act = async () => await repository.AddStand(facility.Id, tooLong, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>();
            await AssertStandCount(facility.Id, 2);
            (await databaseConnectionProvider.QueryScalarAsync<int>("SELECT COUNT(*) FROM `Stands`;")).Should().Be(2);
            (await databaseConnectionProvider.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM `Facilities_Stands` WHERE StandId = @Id;", new { tooLong.Id })).Should().Be(0);
        }

        [Test]
        public async Task MakeAConcurrentStandCreationWaitForTheLockHolderAndThenRespectTheLimit() {
            var facility = await SaveAFacilityWithStands(3);
            var (holder, release) = await HoldTheFacilityLock(facility.Id, async () =>
                await new SqlFacilityRepository(holderProvider).AddStand(facility.Id, new Stand(Guid.NewGuid(), "Fourth", 1000), CancellationToken.None));
            var competitor = new SqlFacilityRepository(competitorProvider);

            var competing = competitor.AddStand(facility.Id, new Stand(Guid.NewGuid(), "Fifth", 1000), CancellationToken.None);

            (await Task.WhenAny(competing, Task.Delay(500))).Should().NotBeSameAs(competing, "the facility is locked by an open transaction");
            release.SetResult();
            await holder;
            await ((Func<Task>)(() => competing)).Should().ThrowAsync<FacilityMaxStandsExceededException>();
            await AssertStandCount(facility.Id, 4);
        }

        [Test]
        public async Task MakeAConcurrentFacilityUpdateWaitForTheLockHolderAndThenRespectTheLimit() {
            var facility = await SaveAFacilityWithStands(3);
            var (holder, release) = await HoldTheFacilityLock(facility.Id, async () =>
                await new SqlFacilityRepository(holderProvider).AddStand(facility.Id, new Stand(Guid.NewGuid(), "Fourth", 1000), CancellationToken.None));
            var handler = new UpdateFacilityCommandHandler(new SqlFacilityRepository(competitorProvider));

            var competing = handler.Handle(new UpdateFacilityCommand {
                Id = facility.Id,
                Name = "Renamed",
                BuildDate = facility.BuildDate,
                Type = facility.Type,
                Capacity = facility.Capacity,
                Address = new UpdateFacilityAddressCommand("Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
                Stands = [new UpdateFacilityStandCommand("Fifth", 1000)],
            }, CancellationToken.None);

            (await Task.WhenAny(competing, Task.Delay(500))).Should().NotBeSameAs(competing, "the facility is locked by an open transaction");
            release.SetResult();
            await holder;
            await competing;
            await AssertStandCount(facility.Id, 4);
            (await repository.Get(new FacilitySearchCriteria {
                CriteriaList = [new SearchCriteria { Name = "Id", Value = $"{facility.Id}" }]
            }, CancellationToken.None)).Single().Name.Should().Be("Renamed");
        }

        /// <summary>Opens a transaction that locks the facility, runs <paramref name="whileLocked"/> after the release signal and commits.</summary>
        private async Task<(Task Holder, TaskCompletionSource Release)> HoldTheFacilityLock(Guid facilityId, Func<Task> whileLocked) {
            var locked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var holder = holderProvider.ExecuteInTransactionAsync(async () => {
                await holderProvider.QueryAsync<Guid>(LockFacilityQuery, new { Id = facilityId });
                locked.SetResult();
                await release.Task;
                await whileLocked();
            });
            await Task.WhenAny(locked.Task, holder);
            locked.Task.IsCompletedSuccessfully.Should().BeTrue("the holder must own the lock before the competitor starts");
            return (holder, release);
        }

        private async Task<Facility> SaveAFacilityWithStands(int count) {
            var facility = new Facility(
                Guid.NewGuid(),
                "City Arena",
                true,
                new DateTime(2020, 7, 20),
                FacilityType.Arena,
                18000,
                new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
                Enumerable.Range(0, count).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList());
            await repository.Save(facility, CancellationToken.None);
            return facility;
        }

        private async Task AssertStandCount(Guid facilityId, int expected)
            => (await databaseConnectionProvider.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM `Facilities_Stands` WHERE FacilityId = @FacilityId;", new { FacilityId = facilityId })).Should().Be(expected);
    }

    private sealed class FacilityRow {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool Active { get; init; }
        public DateTime BuildDate { get; init; }
        public string Type { get; init; } = string.Empty;
        public int Capacity { get; init; }
    }

    private sealed class AddressRow {
        public Guid Id { get; init; }
        public string Street { get; init; } = string.Empty;
        public string StreetNumber { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
    }

    private sealed class StandRow {
        public Guid Id { get; init; }
        public string StandName { get; init; } = string.Empty;
        public int StandCapacity { get; init; }
    }

    private sealed class FacilityAddressRow {
        public Guid FacilityId { get; init; }
        public Guid AddressId { get; init; }
    }

    private sealed class FacilityStandRow {
        public Guid FacilityId { get; init; }
        public Guid StandId { get; init; }
    }
}
public class FacilityRepositoryContractShould {
    [Test]
    public void OnlyAcceptAWholeFacilityForCreationSoEveryUpdateGoesThroughTheLock() {
        var methodsTakingAFacility = typeof(IFacilityRepository).GetMethods()
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Facility)))
            .Select(method => method.Name);

        methodsTakingAFacility.Should().Equal(nameof(IFacilityRepository.Save));
    }

    [Test]
    public void ExposeNoPublicFacilityWriteWithoutTheIdAndTheBuildFunction() {
        var publicMethodsTakingAFacility = typeof(SqlFacilityRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Facility)))
            .Select(method => method.Name);

        publicMethodsTakingAFacility.Should().Equal(nameof(SqlFacilityRepository.Save));
        typeof(SqlFacilityRepository).GetMethod(nameof(SqlFacilityRepository.Update),
                [typeof(Guid), typeof(Func<Facility, Facility>), typeof(CancellationToken)])
            .Should().NotBeNull();
    }
}
