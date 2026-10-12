using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/facilities/{facilityId}/stands")]
[ApiController]
public class FacilityStandsController(
    IQueryHandler<SearchFacilityStandsQuery, List<SearchFacilityStandResult>> searchStandsQueryHandler,
    IQueryHandler<GetFacilityStandQuery, SearchFacilityStandResult> getStandQueryHandler,
    ICommandHandler<AddFacilityStandCommand, AddFacilityStandResponse> addStandCommandHandler,
    ICommandHandler<ReplaceFacilityStandCommand> replaceStandCommandHandler,
    ICommandHandler<RemoveFacilityStandCommand> removeStandCommandHandler)
    : ControllerBase {
    [HttpGet("")]
    public async Task<List<SearchFacilityStandResult>> GetAll([FromRoute] Guid facilityId, CancellationToken cancellationToken = default)
        => await searchStandsQueryHandler.Handle(new SearchFacilityStandsQuery(facilityId), cancellationToken);

    [HttpGet("{standId}")]
    public async Task<SearchFacilityStandResult> GetBy([FromRoute] Guid facilityId, [FromRoute] Guid standId, CancellationToken cancellationToken = default)
        => await getStandQueryHandler.Handle(new GetFacilityStandQuery(facilityId, standId), cancellationToken);

    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromRoute] Guid facilityId, [FromBody] FacilityStandRequest request, CancellationToken cancellationToken = default) {
        var result = await addStandCommandHandler.Handle(
            new AddFacilityStandCommand(facilityId, request.StandName, request.StandCapacity), cancellationToken);
        return CreatedAtAction(nameof(GetBy), new { facilityId, standId = result.Id }, result.Id);
    }

    [HttpPut("{standId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([FromRoute] Guid facilityId, [FromRoute] Guid standId, [FromBody] FacilityStandRequest request, CancellationToken cancellationToken = default) {
        await replaceStandCommandHandler.Handle(
            new ReplaceFacilityStandCommand(facilityId, standId, request.StandName, request.StandCapacity), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{standId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid facilityId, [FromRoute] Guid standId, CancellationToken cancellationToken = default) {
        await removeStandCommandHandler.Handle(new RemoveFacilityStandCommand(facilityId, standId), cancellationToken);
        return NoContent();
    }
}

public class FacilityStandRequest {
    public string StandName { get; set; } = string.Empty;
    public int StandCapacity { get; set; }
}
