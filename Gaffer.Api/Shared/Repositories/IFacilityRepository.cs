using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IFacilityRepository
{
    Task<List<Facility>> Get(FacilitySearchCriteria criteria, CancellationToken cancellationToken);
    Task Save(Facility facility, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the facility row, reads it inside the same transaction, writes the facility returned by
    /// <paramref name="buildUpdated"/> and commits, so the decision never rests on a stale read.
    /// It is the only way to update a facility: there is no unlocked write in the contract.
    /// </summary>
    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    Task Update(Guid id, Func<Facility, Facility> buildUpdated, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    Task<List<Stand>> GetStands(Guid facilityId, CancellationToken cancellationToken);

    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    /// <exception cref="FacilityStandNotFoundException">The stand does not exist or belongs to another facility.</exception>
    Task<Stand> GetStand(Guid facilityId, Guid standId, CancellationToken cancellationToken);

    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    /// <exception cref="FacilityMaxStandsExceededException">The facility already has the maximum number of stands.</exception>
    Task AddStand(Guid facilityId, Stand stand, CancellationToken cancellationToken);

    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    /// <exception cref="FacilityStandNotFoundException">The stand does not exist or belongs to another facility.</exception>
    Task UpdateStand(Guid facilityId, Stand stand, CancellationToken cancellationToken);

    /// <exception cref="FacilityNotFoundException">The facility does not exist.</exception>
    /// <exception cref="FacilityStandNotFoundException">The stand does not exist or belongs to another facility.</exception>
    Task DeleteStand(Guid facilityId, Guid standId, CancellationToken cancellationToken);
}

public class FacilityStandNotFoundException(Guid facilityId, Guid standId)
    : Exception($"Stand {standId} not found in facility {facilityId}")
{
    public Guid FacilityId { get; } = facilityId;
    public Guid StandId { get; } = standId;
}

public class FacilityMaxStandsExceededException(Guid facilityId)
    : Exception($"Facility {facilityId} already has the maximum number of stands")
{
    public Guid FacilityId { get; } = facilityId;
}

public class FacilitySearchCriteria
{
    public List<SearchCriteria> criteriaList { get; set; } = [];
}

public class FacilitySearchFilterNotPermittedException(string fieldName)
    : Exception("FilterNotPermitted")
{
    public string FieldName { get; } = fieldName;
}

public class FacilityNotFoundException : Exception
{
    public FacilityNotFoundException(string message, Guid id) : base(message)
    {
        Id = id.ToString();
        ErrorMessage = $"Facility with id {id} not found in Database";
    }

    public string Id { get; set; }
    public string ErrorMessage { get; set; }
}