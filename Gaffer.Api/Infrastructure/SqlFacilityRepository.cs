using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlFacilityRepository(IDatabaseConnectionProvider dbConnectionProvider) : IFacilityRepository
{
    private static readonly HashSet<string> AllowedFilterColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Name", "Active", "BuildDate", "Type", "Capacity"
    };

    public async Task<List<Facility>> Get(FacilitySearchCriteria criteria, CancellationToken cancellationToken)
    {
        var (whereClause, parameters) = BuildFilters(criteria);
        var query = $"SELECT * FROM `Facilities` {whereClause};";
        var rows = await dbConnectionProvider.QueryAsync<FacilityRow>(query, parameters);

        var facilities = new List<Facility>();
        foreach (var row in rows)
        {
            var address = await GetAddress(row.Id, cancellationToken);
            var stands = await GetStands(row.Id, cancellationToken);
            facilities.Add(new Facility(
                row.Id,
                row.Name,
                row.Active,
                row.BuildDate,
                Enum.Parse<FacilityType>(row.Type),
                row.Capacity,
                address,
                stands));
        }

        return facilities;
    }

    private static (string whereClause, DynamicParameters parameters) BuildFilters(FacilitySearchCriteria criteria)
    {
        var parameters = new DynamicParameters();
        if (criteria.criteriaList.Count == 0) return (string.Empty, parameters);

        var whereClause = string.Empty;
        for (var i = 0; i < criteria.criteriaList.Count; i++)
        {
            var item = criteria.criteriaList[i];
            if (!AllowedFilterColumns.Contains(item.Name))
            {
                throw new ArgumentException($"Field '{item.Name}' is not a valid filter field", nameof(criteria));
            }

            var paramName = $"p{i}";
            var connector = i == 0 ? "WHERE" : item.CriteriaType.ToString();
            whereClause += $" {connector} `{item.Name}` = @{paramName}";
            parameters.Add(paramName, item.Value);
        }

        return (whereClause, parameters);
    }

    private async Task<Address> GetAddress(Guid facilityId, CancellationToken cancellationToken)
    {
        const string query =
            "SELECT a.* FROM `Addresses` a " +
            "INNER JOIN `Facilities_Addresses` fa ON fa.AddressId = a.Id " +
            "WHERE fa.FacilityId = @FacilityId;";
        var rows = await dbConnectionProvider.QueryAsync<AddressRow>(query, new { FacilityId = facilityId });
        var row = rows.FirstOrDefault();
        if (row == null) return null;

        return new Address(row.Id, row.Street, row.StreetNumber, row.PostalCode, row.City, row.Region, row.Country);
    }

    private async Task<List<Stand>> GetStands(Guid facilityId, CancellationToken cancellationToken)
    {
        const string query =
            "SELECT s.* FROM `Stands` s " +
            "INNER JOIN `Facilities_Stands` fs ON fs.StandId = s.Id " +
            "WHERE fs.FacilityId = @FacilityId;";
        var rows = await dbConnectionProvider.QueryAsync<StandRow>(query, new { FacilityId = facilityId });
        return rows.Select(row => new Stand(row.Id, row.StandName, row.StandCapacity)).ToList();
    }

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

        await dbConnectionProvider.ExecuteInTransactionAsync(async () =>
        {
            await dbConnectionProvider.ExecuteAsync(insertFacilityQuery, new
            {
                facility.Id,
                facility.Name,
                facility.Active,
                facility.BuildDate,
                Type = facility.Type.ToString(),
                facility.Capacity
            });

            await InsertAddress(facility, cancellationToken);
            await InsertStands(facility, cancellationToken);
        });
    }

    public async Task Update(Facility facility, CancellationToken cancellationToken)
    {
        const string updateFacilityQuery =
            "UPDATE `Facilities` SET " +
            "Name = @Name, " +
            "Active = @Active, " +
            "BuildDate = @BuildDate, " +
            "Type = @Type, " +
            "Capacity = @Capacity " +
            "WHERE Id = @Id;";
        await dbConnectionProvider.ExecuteInTransactionAsync(async () =>
        {
            await dbConnectionProvider.ExecuteAsync(updateFacilityQuery, new
            {
                facility.Id,
                facility.Name,
                facility.Active,
                facility.BuildDate,
                Type = facility.Type.ToString(),
                facility.Capacity
            });

            await DeleteStands(facility.Id, cancellationToken);
            await DeleteAddress(facility.Id, cancellationToken);
            await InsertAddress(facility, cancellationToken);
            await InsertStands(facility, cancellationToken);
        });
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await dbConnectionProvider.ExecuteInTransactionAsync(async () =>
        {
            await DeleteStands(id, cancellationToken);
            await DeleteAddress(id, cancellationToken);

            const string deleteFacilityQuery = "DELETE FROM `Facilities` WHERE Id = @Id;";
            await dbConnectionProvider.ExecuteAsync(deleteFacilityQuery, new { Id = id });
        });
    }

    private async Task InsertAddress(Facility facility, CancellationToken cancellationToken)
    {
        const string insertAddressQuery =
            "INSERT INTO `Addresses` (" +
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
    }

    private async Task InsertStands(Facility facility, CancellationToken cancellationToken)
    {
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

    private async Task DeleteStands(Guid facilityId, CancellationToken cancellationToken)
    {
        const string deleteStandsQuery =
            "DELETE FROM `Stands` WHERE Id IN (SELECT StandId FROM `Facilities_Stands` WHERE FacilityId = @FacilityId);";
        await dbConnectionProvider.ExecuteAsync(deleteStandsQuery, new { FacilityId = facilityId });

        const string deleteStandRelationsQuery = "DELETE FROM `Facilities_Stands` WHERE FacilityId = @FacilityId;";
        await dbConnectionProvider.ExecuteAsync(deleteStandRelationsQuery, new { FacilityId = facilityId });
    }

    private async Task DeleteAddress(Guid facilityId, CancellationToken cancellationToken)
    {
        const string deleteAddressQuery =
            "DELETE FROM `Addresses` WHERE Id IN (SELECT AddressId FROM `Facilities_Addresses` WHERE FacilityId = @FacilityId);";
        await dbConnectionProvider.ExecuteAsync(deleteAddressQuery, new { FacilityId = facilityId });

        const string deleteAddressRelationQuery = "DELETE FROM `Facilities_Addresses` WHERE FacilityId = @FacilityId;";
        await dbConnectionProvider.ExecuteAsync(deleteAddressRelationQuery, new { FacilityId = facilityId });
    }

    private class FacilityRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public DateTime BuildDate { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
    }

    private class AddressRow
    {
        public Guid Id { get; set; }
        public string Street { get; set; }
        public string StreetNumber { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
    }

    private class StandRow
    {
        public Guid Id { get; set; }
        public string StandName { get; set; }
        public int StandCapacity { get; set; }
    }
}