using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public record UpdateFacilityAddressCommand(
    string Street,
    string StreetNumber,
    string PostalCode,
    string City,
    string Region,
    string Country);

public record UpdateFacilityStandCommand(string StandName, int StandCapacity);

public class UpdateFacilityCommand : ICommand
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public DateTime BuildDate { get; set; }
    public FacilityType Type { get; set; }
    public int Capacity { get; set; }
    public UpdateFacilityAddressCommand Address { get; set; }
    public List<UpdateFacilityStandCommand>? Stands { get; set; }
}

public class UpdateFacilityCommandHandler(IFacilityRepository facilityRepository) : ICommandHandler<UpdateFacilityCommand>
{
    public async Task Handle(UpdateFacilityCommand command, CancellationToken cancellationToken)
    {
        Facility facility;
        try
        {
            facility = await GetFacility(command, cancellationToken);
        }
        catch (FacilityNotFoundException ex)
        {
            throw new CannotUpdateFacilityNotFoundException(ex.Message, command.Id);
        }

        var updatedFacility = BuildUpdatedFacility(facility, command);

        await facilityRepository.Update(updatedFacility, cancellationToken);
    }

    private async Task<Facility> GetFacility(UpdateFacilityCommand command, CancellationToken cancellationToken)
    {
        var searchCriteria = new FacilitySearchCriteria();
        searchCriteria.criteriaList.Add(new SearchCriteria
        {
            Name = "Id",
            Value = $"{command.Id}",
            CriteriaType = CriteriaType.NONE
        });
        var queryResult = await facilityRepository.Get(searchCriteria, cancellationToken);
        var facility = queryResult.FirstOrDefault();
        return facility ?? throw new FacilityNotFoundException($"Facility with id {command.Id} not found", command.Id);
    }

    private static Facility BuildUpdatedFacility(Facility existing, UpdateFacilityCommand command)
    {
        var address = new Address(
            existing.Address.Id,
            command.Address.Street,
            command.Address.StreetNumber,
            command.Address.PostalCode,
            command.Address.City,
            command.Address.Region,
            command.Address.Country);

        var stands = command.Stands?.Select(stand =>
                new Stand(Guid.NewGuid(), stand.StandName, stand.StandCapacity))
            .ToList();

        return new Facility(
            existing.Id,
            command.Name,
            command.Active,
            command.BuildDate,
            command.Type,
            command.Capacity,
            address,
            stands);
    }
}

public class CannotUpdateFacilityNotFoundException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
}

public class CannotUpdateFacilityException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}
