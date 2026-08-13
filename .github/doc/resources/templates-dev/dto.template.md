# DTO Template

```csharp
namespace <ModuleName>.Dtos;

public sealed record <EntityName>RequestDto(
    string <RequiredField>,
    string? <OptionalField>,
    DateTimeOffset? <Timestamp>
);

public sealed record <EntityName>ResponseDto(
    Guid Id,
    string <DisplayField>,
    string? <OptionalField>
);
```
