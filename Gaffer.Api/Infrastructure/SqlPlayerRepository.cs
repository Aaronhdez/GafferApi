using DocumentFormat.OpenXml.Bibliography;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var baseQuery = $"SELECT * FROM `Players` {AddFilters(string.Empty, criteria)};";
        var result = await dbConnectionProvider.QueryAsync<Player>(baseQuery, cancellationToken);
        return result.ToList();
    }

    private static string AddFilters(string query, PlayerSearchCriteria criteria)
    {
        if (criteria.criteriaList.Count == 0) return query;
        query += AddFilter(query, "Id", criteria);
        query += AddFilter(query, "FirstName", criteria);
        query += AddFilter(query, "LastName", criteria);
        return query;
    }
    
    private static string AddFilter(string query, string fieldName, PlayerSearchCriteria searchCriteria)
    {
        var criteria = searchCriteria.criteriaList.Where(c => c.Name == fieldName).ToList();
        if (criteria.Count == 0) return string.Empty;
        return !query.Contains("WHERE") ? 
            "WHERE " + AppendFilter(fieldName, criteria) 
            : "AND " + AppendFilter(fieldName, criteria);
    }

    private static string AppendFilter(string fieldName, List<SearchCriteria> guids)
    {
        var filter = guids.Aggregate($" {fieldName} IN ('", (current, criteria) => current + criteria.Value + "', ");
        return filter.Remove(filter.Length - 2) + ") ";
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}