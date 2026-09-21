using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IFacilityRepository
{
    Task<List<Facility>> Get(FacilitySearchCriteria criteria, CancellationToken cancellationToken);
    Task Save(Facility facility, CancellationToken cancellationToken);
    Task Update(Facility facility, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}

public class FacilitySearchCriteria
{
    public List<SearchCriteria> criteriaList { get; set; } = [];
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