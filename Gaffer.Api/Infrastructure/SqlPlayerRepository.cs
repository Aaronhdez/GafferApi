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
        if (criteria.criteriaList.Count > 0)
        {
            baseQuery = AddFilters(baseQuery, criteria) + ";";
        }

        var result = await dbConnectionProvider.QueryAsync<Player>(baseQuery, cancellationToken);
        return result.ToList();
    }

    private string AddFilters(string query, PlayerSearchCriteria criteria)
    {
        query += AddFilter(query, "PlayerId", criteria.criteriaList.Where(c => c.Name == "Id").ToList());
        query += AddFilter(query, "FirstName",
            criteria.criteriaList.Where(c => c.Name == "FirstName").ToList());
        query += AddFilter(query, "LastName", criteria.criteriaList.Where(c => c.Name == "LastName").ToList());
        return query;
    }
    
    private string AddFilter(string query, string fieldName, List<SearchCriteria>? guids)
    {
        if (guids == null || guids.Count == 0) return string.Empty;
        var filter = string.Empty;
        if (!query.Contains("WHERE"))
        {
            filter = guids.Aggregate($"WHERE {fieldName} IN ('", (current, criteria) => current + criteria.Value + "', ");
            filter = filter.Remove(filter.Length - 2) + ")";
            return filter;
        }
        filter = guids.Aggregate($" AND {fieldName} IN ('", (current, criteria) => current + criteria.Value + "', ");
        filter = filter.Remove(filter.Length - 2) + ")";
        return filter;
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}