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
        return query + guidFilters;
    }

    private string AddGuidFilter(string fieldName, List<SearchCriteria>? guids)
    {
        if(guids == null || guids.Count == 0) return string.Empty;
        var filter = guids.Aggregate($"{fieldName} WHERE PlayerId IN ('", (current, guid) => current + (guid.Value + "', "));
        return filter.Remove(filter.Length - 2) + ")";
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}