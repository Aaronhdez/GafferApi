using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/dummy")]
[ApiController]
public class DummyController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<string> DummyGet()
    {
        return await Task.FromResult("Ok");
    }    
    
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<string> DummyQuery([FromBody] string queryOptions)
    {
        return await Task.FromResult("Ok");
    }
}