using Dapper;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Criteria;
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

    private static readonly IReadOnlyDictionary<string, string> AllowedIntervalColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BirthDate", "BirthDate" }
        };

    private static readonly IReadOnlyDictionary<string, string> AllowedOrderColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "FirstName", "FirstName" },
            { "LastName", "LastName" },
            { "BirthDate", "BirthDate" }
        };

    private const string WherePrefix = "WHERE ";
    private const string AndConnector = " AND ";
    private const string OrConnector = " OR ";

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

    public async Task<List<Player>> Get(CriteriaSet criteria, CancellationToken cancellationToken) {
        var (whereClause, parameters) = BuildFilters(criteria);
        var orderClause = BuildOrder(criteria);
        var query = $"SELECT * FROM `Players`{whereClause}{orderClause};";
        return await dbConnectionProvider.QueryAsync<Player>(query, parameters, cancellationToken);
    }

    private sealed record CriteriaClause(
        string Connector,
        string Expression,
        IReadOnlyList<KeyValuePair<string, object?>> Parameters);

    private static (string whereClause, DynamicParameters parameters) BuildFilters(CriteriaSet criteria) {
        // ToList() evaluates every criterion (and its validation) in order before any SQL is built.
        var valueClauses = criteria.ValueCriteria
            .Where(item => item != null)
            .Select(ToValueCriteriaClause)
            .ToList();
        var intervalClauses = criteria.IntervalCriteria
            .Where(item => item != null)
            .Select(ToIntervalCriteriaClause)
            .ToList();

        var valueStatement = valueClauses.Aggregate(WherePrefix, AddCriteria);
        var statement = intervalClauses.Aggregate(
            GroupValuesBeforeIntervals(valueStatement, valueClauses, intervalClauses), AddCriteria);
        if (statement.Length == WherePrefix.Length) {
            return (string.Empty, new DynamicParameters());
        }

        var parameters = new DynamicParameters(valueClauses
            .Concat(intervalClauses)
            .SelectMany(clause => clause.Parameters)
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value));
        return ($" {statement}", parameters);
    }

    // The action prefix (AND/OR) is omitted while the statement only holds "WHERE ".
    private static string AddCriteria(string statement, CriteriaClause clause) =>
        statement.Length > WherePrefix.Length
            ? $"{statement}{clause.Connector}{clause.Expression}"
            : $"{statement}{clause.Expression}";

    // Q4 b) + Q12 a): an OR connector emitted between value criteria would bind tighter to the following AND, so the
    // value block is parenthesized when intervals follow; without intervals the SQL is left as it was.
    private static string GroupValuesBeforeIntervals(string valueStatement,
        IReadOnlyList<CriteriaClause> valueClauses, IReadOnlyList<CriteriaClause> intervalClauses) =>
        intervalClauses.Any() && valueClauses.Skip(1).Any(clause => clause.Connector == OrConnector)
            ? $"{WherePrefix}({valueStatement[WherePrefix.Length..]})"
            : valueStatement;

    private static CriteriaClause ToIntervalCriteriaClause(IIntervalCriteria criteria, int index) {
        if (criteria.Name == null || !AllowedIntervalColumns.TryGetValue(criteria.Name, out var column)) {
            throw new PlayerSearchFilterNotPermittedException(criteria.Name ?? string.Empty);
        }

        return new CriteriaClause(
            AndConnector,
            $"`{column}` BETWEEN @i{index}_min AND @i{index}_max",
            [
                new KeyValuePair<string, object?>($"i{index}_min", criteria.Min),
                new KeyValuePair<string, object?>($"i{index}_max", criteria.Max)
            ]);
    }

    private static string BuildOrder(CriteriaSet criteria) {
        var orderTerms = criteria.OrderCriteria
            .Where(item => item != null)
            .Select(ToOrderTerm)
            .ToList();
        return orderTerms.Count == 0 ? string.Empty : $" ORDER BY {string.Join(", ", orderTerms)}";
    }

    // The column comes from the whitelist and the direction from a fixed keyword; nothing else reaches the SQL text.
    private static string ToOrderTerm(OrderCriteria criteria) {
        if (criteria.Name == null || !AllowedOrderColumns.TryGetValue(criteria.Name, out var column)) {
            throw new PlayerSearchFilterNotPermittedException(criteria.Name ?? string.Empty);
        }

        var direction = criteria.Direction switch {
            OrderDirection.ASC => "ASC",
            OrderDirection.DESC => "DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), criteria.Direction,
                "Unknown order direction.")
        };
        return $"`{column}` {direction}";
    }

    // Only whitelisted column names, fixed keywords and parameter names reach the SQL text; every caller value is a
    // parameter.
    private static CriteriaClause ToValueCriteriaClause(ValueCriteria criteria, int index) {
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

        return new CriteriaClause(
            criteria.Type == ValueCriteriaType.OR ? OrConnector : AndConnector,
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


}
