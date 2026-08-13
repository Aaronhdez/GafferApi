# Repository Template

```csharp
namespace <ModuleName>.Shared.Repositories;

public interface I<RepositoryName>
{
    Task<<EntityType>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<<EntityType>>> SearchAsync(<SearchCriteria> criteria, CancellationToken ct);
    Task<<EntityType>> SaveAsync(<EntityType> entity, CancellationToken ct);
}

namespace <ModuleName>.Infrastructure.Repositories;

public sealed class <RepositoryName> : I<RepositoryName>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public <RepositoryName>(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<<EntityType>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Compose query and map persistence model to domain entity.
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<<EntityType>>> SearchAsync(<SearchCriteria> criteria, CancellationToken ct)
    {
        // Compose conditional query and return deterministic ordering.
        throw new NotImplementedException();
    }

    public Task<<EntityType>> SaveAsync(<EntityType> entity, CancellationToken ct)
    {
        // Execute insert/update and return persisted entity.
        throw new NotImplementedException();
    }
}
```
