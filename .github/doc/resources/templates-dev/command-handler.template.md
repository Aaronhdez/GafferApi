# Command Handler Template

```csharp
namespace <ModuleName>.Commands;

public sealed class <CommandName>
{
    public <CommandName>(<PayloadType> payload)
    {
        Payload = payload;
    }

    public <PayloadType> Payload { get; }
}

public interface I<CommandName>Handler
{
    Task<<ResultType>> HandleAsync(<CommandName> command, CancellationToken ct);
}

public sealed class <CommandName>Handler : I<CommandName>Handler
{
    private readonly I<RepositoryName> _repository;

    public <CommandName>Handler(I<RepositoryName> repository)
    {
        _repository = repository;
    }

    public async Task<<ResultType>> HandleAsync(<CommandName> command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await _repository.<PersistMethodAsync>(command.Payload, ct);
    }
}
```
