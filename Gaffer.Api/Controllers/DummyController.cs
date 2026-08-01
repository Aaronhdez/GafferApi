using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/dummy")]
[ApiController]
public class DummyController : ControllerBase
{
    [HttpGet("")]
    public async Task<string> DummyGet()
    {
        return await Task.FromResult("Ok");
    }
}