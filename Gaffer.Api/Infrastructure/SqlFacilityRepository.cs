using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlFacilityRepository(IDatabaseConnectionProvider dbConnectionProvider) : IFacilityRepository {
    private static readonly HashSet<string> AllowedFilterColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Name", "Active", "BuildDate", "Type", "Capacity"
    };

    public async Task<List<Facility>> Get(FacilitySearchCriteria criteria, CancellationToken cancellationToken) {
        var (whereClause, parameters) = BuildFilters(criteria);
        var query = $"SELECT * FROM `Facilities` {whereClause};";
        var rows = await dbConnectionProvider.QueryAsync<FacilityRow>(query, parameters, cancellationToken);

        var facilities = new List<Facility>();
        foreach (var row in rows) {
            var address = await GetAddress(row.Id, cancellationToken);
            var stands = await ReadStands(row.Id, cancellationToken);
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

    private static (string whereClause, DynamicParameters parameters) BuildFilters(FacilitySearchCriteria criteria) {
        var parameters = new DynamicParameters();
        if (criteria.CriteriaList.Count == 0) return (string.Empty, parameters);

        var whereClause = string.Empty;
        for (var i = 0; i < criteria.CriteriaList.Count; i++) {
            var item = criteria.CriteriaList[i];
            if (!AllowedFilterColumns.Contains(item.Name)) {
                throw new FacilitySearchFilterNotPermittedException(item.Name ?? string.Empty);
            }

            var paramName = $"p{i}";
            var connector = i == 0 ? "WHERE" : item.CriteriaType.ToString();
            whereClause += $" {connector} `{item.Name}` = @{paramName}";
            parameters.Add(paramName, item.Value);
        }

        return (whereClause, parameters);
    }

    private async Task<Address?> GetAddress(Guid facilityId, CancellationToken cancellationToken) {
        const string query =
            "SELECT a.* FROM `Addresses` a " +
            "INNER JOIN `Facilities_Addresses` fa ON fa.AddressId = a.Id " +
            "WHERE fa.FacilityId = @FacilityId;";
        var rows = await dbConnectionProvider.QueryAsync<AddressRow>(query, new { FacilityId = facilityId }, cancellationToken);
        var row = rows.FirstOrDefault();
        if (row == null) return null;

        return new Address(row.Id, row.Street, row.StreetNumber, row.PostalCode, row.City, row.Region, row.Country);
    }

    private async Task<List<Stand>> ReadStands(Guid facilityId, CancellationToken cancellationToken) {
        const string query =
            "SELECT s.* FROM `Stands` s " +
            "INNER JOIN `Facilities_Stands` fs ON fs.StandId = s.Id " +
            "WHERE fs.FacilityId = @FacilityId;";
        var rows = await dbConnectionProvider.QueryAsync<StandRow>(query, new { FacilityId = facilityId }, cancellationToken);
        return rows.Select(row => new Stand(row.Id, row.StandName, row.StandCapacity)).ToList();
    }

    public async Task Save(Facility facility, CancellationToken cancellationToken) {
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

        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await dbConnectionProvider.ExecuteAsync(insertFacilityQuery, new {
                facility.Id,
                facility.Name,
                facility.Active,
                facility.BuildDate,
                Type = facility.Type.ToString(),
                facility.Capacity
            }, cancellationToken);

            await InsertAddress(facility, cancellationToken);
            await InsertStands(facility, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Writes the facility, its address and its stands. Only <see cref="Update(Guid, Func{Facility, Facility}, CancellationToken)"/>
    /// calls it, inside the transaction that holds the facility row lock, so it joins that ambient transaction.
    /// </summary>
    private async Task WriteFacility(Facility facility, CancellationToken cancellationToken) {
        const string updateFacilityQuery =
            "UPDATE `Facilities` SET " +
            "Name = @Name, " +
            "Active = @Active, " +
            "BuildDate = @BuildDate, " +
            "Type = @Type, " +
            "Capacity = @Capacity " +
            "WHERE Id = @Id;";
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await dbConnectionProvider.ExecuteAsync(updateFacilityQuery, new {
                facility.Id,
                facility.Name,
                facility.Active,
                facility.BuildDate,
                Type = facility.Type.ToString(),
                facility.Capacity
            }, cancellationToken);

            await UpdateAddress(facility, cancellationToken);
            await UpdateStands(facility, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>Updates in place the address linked to the facility; its <c>Id</c> never changes.</summary>
    private async Task UpdateAddress(Facility facility, CancellationToken cancellationToken) {
        const string updateAddressQuery =
            "UPDATE `Addresses` SET " +
            "Street = @Street, " +
            "StreetNumber = @StreetNumber, " +
            "PostalCode = @PostalCode, " +
            "City = @City, " +
            "Region = @Region, " +
            "Country = @Country " +
            "WHERE Id IN (SELECT AddressId FROM `Facilities_Addresses` WHERE FacilityId = @FacilityId);";

        await dbConnectionProvider.ExecuteAsync(updateAddressQuery, new {
            FacilityId = facility.Id,
            facility.Address.Street,
            facility.Address.StreetNumber,
            facility.Address.PostalCode,
            facility.Address.City,
            facility.Address.Region,
            facility.Address.Country
        }, cancellationToken);
    }

    /// <summary>
    /// Updates in place the stands already linked to the facility and inserts the others.
    /// Linked stands missing from <paramref name="facility"/> are left untouched (never deleted).
    /// </summary>
    private async Task UpdateStands(Facility facility, CancellationToken cancellationToken) {
        const string linkedStandIdsQuery = "SELECT StandId FROM `Facilities_Stands` WHERE FacilityId = @FacilityId;";
        var linkedStandIds = (await dbConnectionProvider.QueryAsync<Guid>(linkedStandIdsQuery, new { FacilityId = facility.Id }, cancellationToken))
            .ToHashSet();
        var standsByLinked = facility.Stands.ToLookup(stand => linkedStandIds.Contains(stand.Id));

        const string updateStandQuery =
            "UPDATE `Stands` SET StandName = @StandName, StandCapacity = @StandCapacity WHERE Id = @Id;";
        foreach (var stand in standsByLinked[true]) {
            await dbConnectionProvider.ExecuteAsync(updateStandQuery, new {
                stand.Id,
                stand.StandName,
                stand.StandCapacity
            }, cancellationToken);
        }

        foreach (var stand in standsByLinked[false]) {
            await InsertStand(facility.Id, stand, cancellationToken);
        }
    }

    private const int MaxStands = 4;

    public async Task Update(Guid id, Func<Facility, Facility> buildUpdated, CancellationToken cancellationToken) {
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await LockFacility(id, cancellationToken);
            var criteria = new FacilitySearchCriteria();
            criteria.CriteriaList.Add(new SearchCriteria { Name = "Id", Value = $"{id}" });
            var facility = (await Get(criteria, cancellationToken)).First();
            await WriteFacility(buildUpdated(facility), cancellationToken);
        }, cancellationToken);
    }

    public async Task<List<Stand>> GetStands(Guid facilityId, CancellationToken cancellationToken) {
        await EnsureFacilityExists(facilityId, cancellationToken);
        return await ReadStands(facilityId, cancellationToken);
    }

    public async Task<Stand> GetStand(Guid facilityId, Guid standId, CancellationToken cancellationToken) {
        var stands = await GetStands(facilityId, cancellationToken);
        return stands.FirstOrDefault(stand => stand.Id == standId)
               ?? throw new FacilityStandNotFoundException(facilityId, standId);
    }

    public async Task AddStand(Guid facilityId, Stand stand, CancellationToken cancellationToken) {
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await LockFacility(facilityId, cancellationToken);
            var currentStands = await CountStands(facilityId, cancellationToken);
            if (currentStands >= MaxStands) {
                throw new FacilityMaxStandsExceededException(facilityId);
            }

            await InsertStand(facilityId, stand, cancellationToken);
        }, cancellationToken);
    }

    public async Task UpdateStand(Guid facilityId, Stand stand, CancellationToken cancellationToken) {
        const string updateStandQuery =
            "UPDATE `Stands` SET StandName = @StandName, StandCapacity = @StandCapacity WHERE Id = @Id;";
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await LockFacility(facilityId, cancellationToken);
            await EnsureStandBelongsToFacility(facilityId, stand.Id, cancellationToken);
            await dbConnectionProvider.ExecuteAsync(updateStandQuery, new {
                stand.Id,
                stand.StandName,
                stand.StandCapacity
            }, cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteStand(Guid facilityId, Guid standId, CancellationToken cancellationToken) {
        const string deleteStandQuery = "DELETE FROM `Stands` WHERE Id = @StandId;";
        const string deleteRelationQuery = "DELETE FROM `Facilities_Stands` WHERE FacilityId = @FacilityId AND StandId = @StandId;";
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            await LockFacility(facilityId, cancellationToken);
            await EnsureStandBelongsToFacility(facilityId, standId, cancellationToken);
            await dbConnectionProvider.ExecuteAsync(deleteStandQuery, new { StandId = standId }, cancellationToken);
            await dbConnectionProvider.ExecuteAsync(deleteRelationQuery, new { FacilityId = facilityId, StandId = standId }, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>Takes the row lock of the facility until the ambient transaction ends, serializing writers of its stands.</summary>
    private async Task LockFacility(Guid facilityId, CancellationToken cancellationToken) {
        const string lockQuery = "SELECT Id FROM `Facilities` WHERE Id = @Id FOR UPDATE;";
        var rows = await dbConnectionProvider.QueryAsync<Guid>(lockQuery, new { Id = facilityId }, cancellationToken);
        if (rows.Count == 0) throw new FacilityNotFoundException($"Facility with id {facilityId} not found", facilityId);
    }

    private async Task EnsureFacilityExists(Guid facilityId, CancellationToken cancellationToken) {
        const string query = "SELECT Id FROM `Facilities` WHERE Id = @Id;";
        var rows = await dbConnectionProvider.QueryAsync<Guid>(query, new { Id = facilityId }, cancellationToken);
        if (rows.Count == 0) throw new FacilityNotFoundException($"Facility with id {facilityId} not found", facilityId);
    }

    private async Task EnsureStandBelongsToFacility(Guid facilityId, Guid standId, CancellationToken cancellationToken) {
        const string query = "SELECT StandId FROM `Facilities_Stands` WHERE FacilityId = @FacilityId AND StandId = @StandId;";
        var rows = await dbConnectionProvider.QueryAsync<Guid>(query, new { FacilityId = facilityId, StandId = standId }, cancellationToken);
        if (rows.Count == 0) throw new FacilityStandNotFoundException(facilityId, standId);
    }

    private async Task<int> CountStands(Guid facilityId, CancellationToken cancellationToken) {
        const string query = "SELECT COUNT(*) FROM `Facilities_Stands` WHERE FacilityId = @FacilityId;";
        return await dbConnectionProvider.QueryScalarAsync<int>(query, new { FacilityId = facilityId }, cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken) {
        await dbConnectionProvider.ExecuteInTransactionAsync(async () => {
            const string deleteFacilityQuery = "DELETE FROM `Facilities` WHERE Id = @Id;";
            await dbConnectionProvider.ExecuteAsync(deleteFacilityQuery, new { Id = id }, cancellationToken);
        }, cancellationToken);
    }

    private async Task InsertAddress(Facility facility, CancellationToken cancellationToken) {
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

        await dbConnectionProvider.ExecuteAsync(insertAddressQuery, new {
            facility.Address.Id,
            facility.Address.Street,
            facility.Address.StreetNumber,
            facility.Address.PostalCode,
            facility.Address.City,
            facility.Address.Region,
            facility.Address.Country
        }, cancellationToken);

        const string insertFacilityAddressRelationQuery =
            "INSERT INTO `Facilities_Addresses` (FacilityId, AddressId) VALUES (@FacilityId, @AddressId);";
        await dbConnectionProvider.ExecuteAsync(insertFacilityAddressRelationQuery, new {
            FacilityId = facility.Id,
            AddressId = facility.Address.Id
        }, cancellationToken);
    }

    private async Task InsertStands(Facility facility, CancellationToken cancellationToken) {
        foreach (var stand in facility.Stands) {
            await InsertStand(facility.Id, stand, cancellationToken);
        }
    }

    private async Task InsertStand(Guid facilityId, Stand stand, CancellationToken cancellationToken) {
        const string insertStandQuery =
            "INSERT INTO `Stands` (Id, StandName, StandCapacity) VALUES (@Id, @StandName, @StandCapacity);";
        await dbConnectionProvider.ExecuteAsync(insertStandQuery, new {
            stand.Id,
            stand.StandName,
            stand.StandCapacity
        }, cancellationToken);

        const string insertFacilityStandRelationQuery =
            "INSERT INTO `Facilities_Stands` (FacilityId, StandId) VALUES (@FacilityId, @StandId);";
        await dbConnectionProvider.ExecuteAsync(insertFacilityStandRelationQuery, new {
            FacilityId = facilityId,
            StandId = stand.Id
        }, cancellationToken);
    }

    private class FacilityRow {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public bool Active { get; set; }
        public DateTime BuildDate { get; set; }
        public string Type { get; set; } = null!;
        public int Capacity { get; set; }
    }

    private class AddressRow {
        public Guid Id { get; set; }
        public string Street { get; set; } = null!;
        public string StreetNumber { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Region { get; set; } = null!;
        public string Country { get; set; } = null!;
    }

    private class StandRow {
        public Guid Id { get; set; }
        public string StandName { get; set; } = null!;
        public int StandCapacity { get; set; }
    }
}
