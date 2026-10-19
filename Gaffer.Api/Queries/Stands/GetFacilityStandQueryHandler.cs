using Gaffer.Api.Queries.Facilities;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries.Stands;

public record GetFacilityStandQuery(Guid FacilityId, Guid StandId) : IQuery<SearchFacilityStandResult>;

public class GetFacilityStandQueryHandler(IFacilityRepository facilityRepository)
    : IQueryHandler<GetFacilityStandQuery, SearchFacilityStandResult> {
    public async Task<SearchFacilityStandResult> Handle(GetFacilityStandQuery query, CancellationToken cancellationToken) {
        try {
            var stand = await facilityRepository.GetStand(query.FacilityId, query.StandId, cancellationToken);
            return new SearchFacilityStandResult(stand.Id, stand.StandName, stand.StandCapacity);
        } catch (FacilityNotFoundException ex) {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, query.FacilityId);
        }
    }
}
