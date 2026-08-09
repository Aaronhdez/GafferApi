using DocumentFormat.OpenXml.Bibliography;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = "SELECT * FROM `Players`";
        query = AddFilters(query, criteria);
        query += ";";
        var result =  await dbConnectionProvider.QueryAsync<Player>(query, cancellationToken);
        return result.ToList();
    }

    private string AddFilters(string query, PlayerSearchCriteria criteria)
    {
        var guidfilters = AddGuidFilter(criteria.criteriaList.Where(c => c.Name == "Id").ToList());
        return query + guidfilters;
    }

    private string AddGuidFilter(List<SearchCriteria> guids)
    {
        if(guids == null || guids.Count == 0) return string.Empty;
        var filter = guids.Aggregate(" WHERE PlayerId IN ('", (current, guid) => current + (guid.Value + "', "));
        filter = filter.Remove(filter.Length - 2);
        return filter + ")";
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}