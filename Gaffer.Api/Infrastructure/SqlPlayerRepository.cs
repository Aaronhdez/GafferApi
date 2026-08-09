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
        return criteria.criteriaList.Aggregate(query, (current, criteriaItem)
            => current + AddFilter(current, criteriaItem));
    }

    private static string AddFilter(string query, SearchCriteria? criteria)
    {
        if (criteria == null) return string.Empty;
        return !query.Contains("WHERE")
            ? "WHERE" + AppendFilter(criteria.Name, criteria)
            : $"{criteria.CriteriaType.ToString()}" + AppendFilter(criteria.Name, criteria);
    }

    private static string AppendFilter(string fieldName, SearchCriteria searchCriteria)
    {
        var filter = $" {fieldName} IN ('{searchCriteria.Value}', ";
        return filter.Remove(filter.Length - 2) + ") ";
    }

    public async Task Save(Player player, CancellationToken cancellationToken)
    {
        const string query =
            "INSERT INTO `Players` (" +
                "Id, " +
                "FirstName, " +
                "LastName, " +
                "SecondLastName, " +
                "Gender, " +
                "BirthDate) " +
            "VALUES (" +
                "@Id, " +
                "@FirstName, " +
                "@LastName, " +
                "@SecondLastName, " +
                "@Gender, " +
                "@BirthDate);";
        await dbConnectionProvider.ExecuteAsync(query, new
        {
            player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            player.Gender,
            player.Birthdate
        });
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var query = "DELETE FROM `Players` WHERE ID = @Id";
        await dbConnectionProvider.ExecuteAsync(query, new { Id = id });
    }
}