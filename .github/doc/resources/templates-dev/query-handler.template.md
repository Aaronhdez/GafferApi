# Query Handler Template

```csharp
namespace <ModuleName>.Queries;

public sealed class <QueryName>
{
    public <QueryName>(<FilterType> filter)
    {
        Filter = filter;
    }

    public <FilterType> Filter { get; }
}

public interface I<QueryName>Handler
{
    Task<<ResultType>> HandleAsync(<QueryName> query, CancellationToken ct);
}

public sealed class <QueryName>Handler : I<QueryName>Handler
{
    private readonly I<RepositoryName> _repository;

    public <QueryName>Handler(I<RepositoryName> repository)
    {
        _repository = repository;
    }

    public async Task<<ResultType>> HandleAsync(<QueryName> query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _repository.<ReadMethodAsync>(query.Filter, ct);
    }
}
```
