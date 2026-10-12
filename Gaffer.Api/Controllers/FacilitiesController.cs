using System.ComponentModel.DataAnnotations;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/facilities")]
[ApiController]
public class FacilitiesController(
    ICommandHandler<CreateFacilityCommand, CreateFacilityResponse> createFacilityCommandHandler,
    IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>> searchFacilitiesQueryHandler,
    ICommandHandler<DeleteFacilityCommand> deleteFacilityCommandHandler,
    ICommandHandler<UpdateFacilityCommand> updateFacilityCommandHandler)
    : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<SearchFacilityResult> GetBy([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{id}" });
        var queryResult = await searchFacilitiesQueryHandler.Handle(query, cancellationToken);
        var facility = queryResult.FirstOrDefault();
        return facility ?? throw new FacilityNotFoundException($"Facility with id {id} not found", id);
    }

    [HttpGet("")]
    public async Task<List<SearchFacilityResult>> GetAll(CancellationToken cancellationToken = default)
    {
        var query = new SearchFacilitiesQuery();
        return await searchFacilitiesQueryHandler.Handle(query, cancellationToken);
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFacilityRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateFacilityCommand(
            request.Name,
            request.Active,
            request.BuildDate,
            request.Type,
            request.Capacity,
            request.Address is null ? null : new CreateFacilityAddressCommand(
                request.Address.Street,
                request.Address.StreetNumber,
                request.Address.PostalCode,
                request.Address.City,
                request.Address.Region,
                request.Address.Country),
            request.Stands?.Select(stand => stand is null ? null : new CreateFacilityStandCommand(stand.StandName, stand.StandCapacity)).ToList());

        var result = await createFacilityCommandHandler.Handle(command, cancellationToken);
        return CreatedAtAction(nameof(GetBy), new { id = result.Id }, result.Id);
    }

    [HttpDelete("{id}")]
    public async Task Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        await deleteFacilityCommandHandler.Handle(new DeleteFacilityCommand(id), cancellationToken);
    }

    [HttpPut("{id}")]
    public async Task Update([FromRoute] Guid id, [FromBody] UpdateFacilityRequest request, CancellationToken cancellationToken = default)
    {
        await updateFacilityCommandHandler.Handle(new UpdateFacilityCommand
        {
            Id = id,
            Name = request.Name,
            Active = request.Active,
            BuildDate = request.BuildDate,
            Type = request.Type,
            Capacity = request.Capacity,
            Address = request.Address is null ? null : new UpdateFacilityAddressCommand(
                request.Address.Street,
                request.Address.StreetNumber,
                request.Address.PostalCode,
                request.Address.City,
                request.Address.Region,
                request.Address.Country),
            Stands = request.Stands?.Select(stand => stand is null ? null : new UpdateFacilityStandCommand(stand.StandName, stand.StandCapacity, stand.Id)).ToList(),
        }, cancellationToken);
    }
}

public class CreateFacilityRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime BuildDate { get; set; }
    public FacilityType Type { get; set; }
    public int Capacity { get; set; }

    [Required]
    public CreateFacilityAddressRequest Address { get; set; } = null!;

    [MaxLength(4)]
    public List<CreateFacilityStandRequest>? Stands { get; set; }
}

public class CreateFacilityAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class CreateFacilityStandRequest
{
    public string StandName { get; set; } = string.Empty;
    public int StandCapacity { get; set; }
}

public class UpdateFacilityRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime BuildDate { get; set; }
    public FacilityType Type { get; set; }
    public int Capacity { get; set; }

    [Required]
    public UpdateFacilityAddressRequest Address { get; set; } = null!;

    [MaxLength(4)]
    public List<UpdateFacilityStandRequest>? Stands { get; set; }
}

public class UpdateFacilityAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class UpdateFacilityStandRequest
{
    /// <summary>Id of an existing stand of the facility; omit it (or send null) to add a new stand.</summary>
    public Guid? Id { get; set; }
    public string StandName { get; set; } = string.Empty;
    public int StandCapacity { get; set; }
}