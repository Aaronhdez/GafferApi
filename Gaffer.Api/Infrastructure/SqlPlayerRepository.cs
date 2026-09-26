using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    private static readonly IReadOnlyDictionary<string, string> AllowedFilterColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "Id" },
            { "FirstName", "FirstName" },
            { "LastName", "LastName" }
        };

    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var (whereClause, parameters) = BuildFilters(criteria);
        var query = $"SELECT * FROM `Players`{whereClause};";
        return await dbConnectionProvider.QueryAsync<Player>(query, parameters);
    }

    private static (string whereClause, DynamicParameters parameters) BuildFilters(PlayerSearchCriteria criteria)
    {
        var parameters = new DynamicParameters();
        var whereClause = string.Empty;
        var filters = criteria.criteriaList.Where(item => item != null).ToList();

        for (var i = 0; i < filters.Count; i++)
        {
            var item = filters[i];
            if (item.Name == null || !AllowedFilterColumns.TryGetValue(item.Name, out var column))
            {
                throw new PlayerSearchFilterNotPermittedException(item.Name ?? string.Empty);
            }

            var paramName = $"p{i}";
            var connector = i == 0 ? "WHERE" : item.CriteriaType.ToString();
            whereClause += $" {connector} `{column}` = @{paramName}";
            parameters.Add(paramName, item.Value);
        }

        return (whereClause, parameters);
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

    public async Task Update(Player player, CancellationToken cancellationToken)
    {
        const string query =
            "UPDATE `Players` SET " +
                "FirstName = @FirstName, " +
                "LastName = @LastName, " +
                "SecondLastName = @SecondLastName, " +
                "Gender = @Gender, " +
                "BirthDate = @BirthDate " +
        "WHERE " +
            "Id = @Id";
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
}