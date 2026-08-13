using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlFacilityRepository(IDatabaseConnectionProvider dbConnectionProvider) : IFacilityRepository
{
    public async Task Save(Facility facility, CancellationToken cancellationToken)
    {
        const string insertFacilityQuery =
            "INSERT INTO `Facilities` (" +
            "Id, " +
            "Name, " +
            "Active, " +
            "BuildDate, " +
            "Type, " +
            "Capacity) " +
            "VALUES (" +
            "@Id, " +
            "@Name, " +
            "@Active, " +
            "@BuildDate, " +
            "@Type, " +
            "@Capacity);";

        await dbConnectionProvider.ExecuteAsync(insertFacilityQuery, new
        {
            facility.Id,
            facility.Name,
            facility.Active,
            facility.BuildDate,
            Type = facility.Type.ToString(),
            facility.Capacity
        });

        const string insertAddressQuery =
            "INSERT INTO `Adresses` (" +
            "Id, " +
            "Street, " +
            "StreetNumber, " +
            "PostalCode, " +
            "City, " +
            "Region, " +
            "Country) " +
            "VALUES (" +
            "@Id, " +
            "@Street, " +
            "@StreetNumber, " +
            "@PostalCode, " +
            "@City, " +
            "@Region, " +
            "@Country);";

        await dbConnectionProvider.ExecuteAsync(insertAddressQuery, new
        {
            facility.Address.Id,
            facility.Address.Street,
            facility.Address.StreetNumber,
            facility.Address.PostalCode,
            facility.Address.City,
            facility.Address.Region,
            facility.Address.Country
        });

        const string insertFacilityAddressRelationQuery =
            "INSERT INTO `Facilities_Addresses` (FacilityId, AddressId) VALUES (@FacilityId, @AddressId);";
        await dbConnectionProvider.ExecuteAsync(insertFacilityAddressRelationQuery, new
        {
            FacilityId = facility.Id,
            AddressId = facility.Address.Id
        });

        foreach (var stand in facility.Stands)
        {
            const string insertStandQuery =
                "INSERT INTO `Stands` (Id, StandName, StandCapacity) VALUES (@Id, @StandName, @StandCapacity);";
            await dbConnectionProvider.ExecuteAsync(insertStandQuery, new
            {
                stand.Id,
                stand.StandName,
                stand.StandCapacity
            });

            const string insertFacilityStandRelationQuery =
                "INSERT INTO `Facilities_Stands` (FacilityId, StandId) VALUES (@FacilityId, @StandId);";
            await dbConnectionProvider.ExecuteAsync(insertFacilityStandRelationQuery, new
            {
                FacilityId = facility.Id,
                StandId = stand.Id
            });
        }
    }
}