using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IFacilityRepository {
    Task<List<Facility>> Get(FacilitySearchCriteria criteria, CancellationToken cancellationToken);
    Task Save(Facility facility, CancellationToken cancellationToken);

    Task Update(Guid id, Func<Facility, Facility> buildUpdated, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<List<Stand>> GetStands(Guid facilityId, CancellationToken cancellationToken);

    Task<Stand> GetStand(Guid facilityId, Guid standId, CancellationToken cancellationToken);

    Task AddStand(Guid facilityId, Stand stand, CancellationToken cancellationToken);

    Task UpdateStand(Guid facilityId, Stand stand, CancellationToken cancellationToken);

    Task DeleteStand(Guid facilityId, Guid standId, CancellationToken cancellationToken);
}

public class FacilityStandNotFoundException(Guid facilityId, Guid standId)
    : Exception($"Stand {standId} not found in facility {facilityId}") {
    public Guid FacilityId { get; } = facilityId;
    public Guid StandId { get; } = standId;
}

public class FacilityMaxStandsExceededException(Guid facilityId)
    : Exception($"Facility {facilityId} already has the maximum number of stands") {
    public Guid FacilityId { get; } = facilityId;
}

public class FacilitySearchCriteria {
    public List<SearchCriteria> CriteriaList { get; set; } = [];
}

public class FacilitySearchFilterNotPermittedException(string fieldName)
    : Exception("FilterNotPermitted") {
    public string FieldName { get; } = fieldName;
}

public class FacilityNotFoundException(string message, Guid id) : Exception(message) {
    public string Id { get; set; } = id.ToString();
    public string ErrorMessage { get; set; } = $"Facility with id {id} not found in Database";
}
