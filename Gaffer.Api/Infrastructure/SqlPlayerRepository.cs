using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository {
    private static readonly IReadOnlyDictionary<string, string> AllowedFilterColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "Id" },
            { "FirstName", "FirstName" },
            { "LastName", "LastName" }
        };

    private const string WherePrefix = "WHERE ";

    public async Task<List<Player>> Get(CriteriaSet criteria, CancellationToken cancellationToken) {
        var (whereClause, parameters) = BuildFilters(criteria);
        var query = $"SELECT * FROM `Players`{whereClause};";
        return await dbConnectionProvider.QueryAsync<Player>(query, parameters, cancellationToken);
    }

    private sealed record ValueCriteriaClause(
        string Connector,
        string Expression,
        IReadOnlyList<KeyValuePair<string, object?>> Parameters);

    private static (string whereClause, DynamicParameters parameters) BuildFilters(CriteriaSet criteria) {
        // ToList() evaluates every criterion (and its validation) in order before any SQL is built.
        var clauses = criteria.ValueCriteria
            .Where(item => item != null)
            .Select(ToValueCriteriaClause)
            .ToList();
        if (clauses.Count == 0) {
            return (string.Empty, new DynamicParameters());
        }

        var statement = clauses.Aggregate(WherePrefix, AddValueCriteria);
        var parameters = new DynamicParameters(clauses
            .SelectMany(clause => clause.Parameters)
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value));
        return ($" {statement}", parameters);
    }

    // The action prefix (AND/OR) is omitted while the statement only holds "WHERE ".
    private static string AddValueCriteria(string statement, ValueCriteriaClause clause) =>
        statement.Length > WherePrefix.Length
            ? $"{statement}{clause.Connector}{clause.Expression}"
            : $"{statement}{clause.Expression}";

    // Only whitelisted column names, fixed keywords and parameter names reach the SQL text; every caller value is a
    // parameter.
    private static ValueCriteriaClause ToValueCriteriaClause(ValueCriteria criteria, int index) {
        if (criteria.Name == null || !AllowedFilterColumns.TryGetValue(criteria.Name, out var column)) {
            throw new PlayerSearchFilterNotPermittedException(criteria.Name ?? string.Empty);
        }

        if (criteria.Values == null || criteria.Values.Count == 0) {
            throw new ArgumentException($"Search criterion '{column}' needs at least one value.", nameof(criteria));
        }

        var isLike = criteria.Type == ValueCriteriaType.LIKE;
        var parameters = criteria.Values
            .Select((value, valueIndex) => new KeyValuePair<string, object?>(
                $"v{index}_{valueIndex}",
                isLike ? ContainsPattern(value) : value))
            .ToList();
        var parameterNames = parameters.Select(parameter => $"@{parameter.Key}").ToList();

        return new ValueCriteriaClause(
            criteria.Type == ValueCriteriaType.OR ? " OR " : " AND ",
            isLike
                ? LikeExpression(column, parameterNames)
                : $"`{column}` IN ({string.Join(", ", parameterNames)})",
            parameters);
    }

    private static string LikeExpression(string column, IReadOnlyList<string> parameterNames) {
        var expression = string.Join(" OR ", parameterNames.Select(name => $"`{column}` LIKE {name}"));
        return parameterNames.Count > 1 ? $"({expression})" : expression;
    }

    // MariaDB's default LIKE escape character is '\': escape it first, then the wildcards, so the value matches literally.
    private static string? ContainsPattern(string? value) =>
        value == null
            ? null
            : $"%{value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";

    public async Task Save(Player player, CancellationToken cancellationToken) {
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
        await dbConnectionProvider.ExecuteAsync(query, new {
            player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            player.Gender,
            player.Birthdate
        }, cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken) {
        var query = "DELETE FROM `Players` WHERE ID = @Id";
        await dbConnectionProvider.ExecuteAsync(query, new { Id = id }, cancellationToken);
    }

    public async Task Update(Player player, CancellationToken cancellationToken) {
        const string query =
            "UPDATE `Players` SET " +
                "FirstName = @FirstName, " +
                "LastName = @LastName, " +
                "SecondLastName = @SecondLastName, " +
                "Gender = @Gender, " +
                "BirthDate = @BirthDate " +
        "WHERE " +
            "Id = @Id";
        await dbConnectionProvider.ExecuteAsync(query, new {
            player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            player.Gender,
            player.Birthdate
        }, cancellationToken);
    }
}
