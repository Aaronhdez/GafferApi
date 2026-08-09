using DocumentFormat.OpenXml.Bibliography;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var baseQuery = "SELECT * FROM `Players`";
        baseQuery = AddFilters(baseQuery, criteria) +";";
        var result =  await dbConnectionProvider.QueryAsync<Player>(baseQuery, cancellationToken);
        return result.ToList();
    }

    private string AddFilters(string query, PlayerSearchCriteria criteria)
    {
        var guidFilters = AddGuidFilter("PlayerId", criteria.criteriaList.Where(c => c.Name == "Id").ToList());
        var nameFilters = AddFirstNameFilter("FirstName", criteria.criteriaList.Where(c => c.Name == "FirstName").ToList());
        return query + guidFilters + nameFilters;
    }

    private string AddGuidFilter(string fieldName, List<SearchCriteria>? guids)
    {
        if(guids == null || guids.Count == 0) return string.Empty;
        var filter = guids.Aggregate($" WHERE {fieldName} IN ('", (current, guid) => current + guid.Value + "', ");
        return filter.Remove(filter.Length - 2) + ")";
    }

    private string AddFirstNameFilter(string fieldName, List<SearchCriteria>? names)
    {
        if(names == null || names.Count == 0) return string.Empty;
        var filter = names.Aggregate($" WHERE {fieldName} IN ('", (current, firstName) => current + firstName.Value + "', ");
        return filter.Remove(filter.Length - 2) + ")";
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}