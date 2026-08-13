using System.ComponentModel.DataAnnotations;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/facilities")]
[ApiController]
public class FacilitiesController(ICommandHandler<CreateFacilityCommand, CreateFacilityResponse> createFacilityCommandHandler)
    : ControllerBase
{
    [HttpPost("")]
    public async Task<Guid> Create([FromBody] CreateFacilityRequest request)
    {
        var command = new CreateFacilityCommand(
            request.Name,
            request.Active,
            request.BuildDate,
            request.Type,
            request.Capacity,
            new CreateFacilityAddressCommand(
                request.Address.Street,
                request.Address.StreetNumber,
                request.Address.PostalCode,
                request.Address.City,
                request.Address.Region,
                request.Address.Country),
            request.Stands?.Select(stand => new CreateFacilityStandCommand(stand.StandName, stand.StandCapacity)).ToList());

        var result = await createFacilityCommandHandler.Handle(command, CancellationToken.None);
        return result.Id;
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