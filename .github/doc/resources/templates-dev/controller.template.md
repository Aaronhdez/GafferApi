# Controller Template

```csharp
using Microsoft.AspNetCore.Mvc;

namespace <ModuleName>.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class <ControllerName>Controller : ControllerBase
{
    private readonly <IQueryService> _queryService;
    private readonly <ICommandService> _commandService;

    public <ControllerName>Controller(<IQueryService> queryService, <ICommandService> commandService)
    {
        _queryService = queryService;
        _commandService = commandService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _queryService.ExecuteAsync(new <GetByIdQuery>(id), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] <CreateRequestDto> request, CancellationToken ct)
    {
        var created = await _commandService.ExecuteAsync(new <CreateCommand>(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
```
