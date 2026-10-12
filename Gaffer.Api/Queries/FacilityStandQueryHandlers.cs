using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public record SearchFacilityStandsQuery(Guid FacilityId) : IQuery<List<SearchFacilityStandResult>>;

public record GetFacilityStandQuery(Guid FacilityId, Guid StandId) : IQuery<SearchFacilityStandResult>;

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
