using Gaffer.Api.Queries.Facilities;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries.Stands;

public record SearchFacilityStandsQuery(Guid FacilityId) : IQuery<List<SearchFacilityStandResult>>;

public class SearchFacilityStandsQueryHandler(IFacilityRepository facilityRepository)
    : IQueryHandler<SearchFacilityStandsQuery, List<SearchFacilityStandResult>> {
    public async Task<List<SearchFacilityStandResult>> Handle(SearchFacilityStandsQuery query, CancellationToken cancellationToken) {
        try {
            var stands = await facilityRepository.GetStands(query.FacilityId, cancellationToken);
            return stands.Select(stand => new SearchFacilityStandResult(stand.Id, stand.StandName, stand.StandCapacity)).ToList();
        } catch (FacilityNotFoundException ex) {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, query.FacilityId);
        }
    }
}
