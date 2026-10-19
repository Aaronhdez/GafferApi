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
        var valueClauses = ToClauses(criteria.ValueCriteria, ToValueCriteriaClause);
        var intervalClauses = ToClauses(criteria.IntervalCriteria, ToIntervalCriteriaClause);
        var statement = ComposeWhere(valueClauses, intervalClauses);
        return HasConditions(statement)
            ? ($" {statement}", CollectParameters(valueClauses.Concat(intervalClauses)))
            : (string.Empty, new DynamicParameters());
    }

    private static List<CriteriaClause> ToClauses<TCriteria>(IEnumerable<TCriteria> criteria,
        Func<TCriteria, int, CriteriaClause> toClause) =>
        criteria
            .Where(item => item != null)
            .Select(toClause)
            .ToList();

    private static string ComposeWhere(IReadOnlyList<CriteriaClause> valueClauses,
        IReadOnlyList<CriteriaClause> intervalClauses) {
        var valueStatement = valueClauses.Aggregate(WherePrefix, AddCriteria);
        return intervalClauses.Aggregate(
            GroupValuesBeforeIntervals(valueStatement, valueClauses, intervalClauses), AddCriteria);
    }

    private static DynamicParameters CollectParameters(IEnumerable<CriteriaClause> clauses) =>
        new(clauses
            .SelectMany(clause => clause.Parameters)
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value));

    private static bool HasConditions(string statement) => statement.Length > WherePrefix.Length;

    private static string AddCriteria(string statement, CriteriaClause clause) =>
        HasConditions(statement)
            ? $"{statement}{clause.Connector}{clause.Expression}"
            : $"{statement}{clause.Expression}";

    private static string GroupValuesBeforeIntervals(string valueStatement,
        IReadOnlyList<CriteriaClause> valueClauses, IReadOnlyList<CriteriaClause> intervalClauses) =>
        intervalClauses.Any() && HasOrBetweenValues(valueClauses)
            ? Parenthesize(valueStatement)
            : valueStatement;

    private static bool HasOrBetweenValues(IEnumerable<CriteriaClause> valueClauses) =>
        valueClauses.Skip(1).Any(clause => clause.Connector == OrConnector);

    private static string Parenthesize(string statement) =>
        $"{WherePrefix}({statement[WherePrefix.Length..]})";

    private static string ResolveColumn(IReadOnlyDictionary<string, string> allowedColumns, string? name) =>
        name != null && allowedColumns.TryGetValue(name, out var column)
            ? column
            : throw new PlayerSearchFilterNotPermittedException(name ?? string.Empty);

    private static CriteriaClause ToIntervalCriteriaClause(IIntervalCriteria criteria, int index) {
        var column = ResolveColumn(AllowedIntervalColumns, criteria.Name);
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

    private static string ToOrderTerm(OrderCriteria criteria) {
        var column = ResolveColumn(AllowedOrderColumns, criteria.Name);
        var direction = criteria.Direction switch {
            OrderDirection.ASC => "ASC",
            OrderDirection.DESC => "DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), criteria.Direction,
                "Unknown order direction.")
        };
        return $"`{column}` {direction}";
    }

    private static CriteriaClause ToValueCriteriaClause(ValueCriteria criteria, int index) {
        var column = ResolveColumn(AllowedFilterColumns, criteria.Name);
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

    private static string? ContainsPattern(string? value) =>
        value == null
            ? null
            : $"%{value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";


}
