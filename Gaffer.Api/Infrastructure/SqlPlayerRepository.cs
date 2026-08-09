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

    private string AddFilters(string query, PlayerSearchCriteria criteria)
    {
        if (criteria.criteriaList.Count == 0) return query;
        query += AddFilter(query, "PlayerId", criteria.criteriaList.Where(c => c.Name == "Id").ToList());
        query += AddFilter(query, "FirstName",
            criteria.criteriaList.Where(c => c.Name == "FirstName").ToList());
        query += AddFilter(query, "LastName", criteria.criteriaList.Where(c => c.Name == "LastName").ToList());
        return query;
    }
    
    private static string AddFilter(string query, string fieldName, List<SearchCriteria>? guids)
    {
        if (guids == null || guids.Count == 0) return string.Empty;
        return !query.Contains("WHERE") ? 
            "WHERE " + AppendFilter(fieldName, guids) 
            : "AND " + AppendFilter(fieldName, guids);
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