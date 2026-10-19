using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries.Facilities;

public class SearchFacilitiesQueryHandler(IFacilityRepository facilityRepository)
    : IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>> {
    public virtual async Task<List<SearchFacilityResult>> Handle(SearchFacilitiesQuery query, CancellationToken cancellationToken) {
        var searchCriteria = new FacilitySearchCriteria();
        foreach (var queryFilter in query.Filters) {
            searchCriteria.CriteriaList.Add(new SearchCriteria { Name = queryFilter.Name, Value = queryFilter.Value, });
        }
        var facilities = await facilityRepository.Get(searchCriteria, cancellationToken);
        return facilities.Select(facility
                => new SearchFacilityResult(
                    facility.Id,
                    facility.Name,
                    facility.Active,
                    facility.BuildDate,
                    facility.Type,
                    facility.Capacity,
                    new SearchFacilityAddressResult(
                        facility.Address.Id,
                        facility.Address.Street,
                        facility.Address.StreetNumber,
                        facility.Address.PostalCode,
                        facility.Address.City,
                        facility.Address.Region,
                        facility.Address.Country),
                    facility.Stands
                        .Select(stand => new SearchFacilityStandResult(stand.Id, stand.StandName, stand.StandCapacity))
                        .ToList()))
            .ToList();
    }
}

public record SearchFacilityResult(
    Guid Id,
    string Name,
    bool Active,
    DateTime BuildDate,
    FacilityType Type,
    int Capacity,
    SearchFacilityAddressResult Address,
    List<SearchFacilityStandResult> Stands);

public record SearchFacilityAddressResult(
    Guid Id,
    string Street,
    string StreetNumber,
    string PostalCode,
    string City,
    string Region,
    string Country);

public record SearchFacilityStandResult(Guid Id, string StandName, int StandCapacity);

public class SearchFacilitiesQuery : IQuery<List<SearchFacilityResult>> {
    public List<QueryFilter> Filters { get; } = [];
}
