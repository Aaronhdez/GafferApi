using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public record CreateFacilityResponse(Guid Id);

public record CreateFacilityAddressCommand(
    string Street,
    string StreetNumber,
    string PostalCode,
    string City,
    string Region,
    string Country);

public record CreateFacilityStandCommand(string StandName, int StandCapacity);

public record CreateFacilityCommand(
    string Name,
    bool Active,
    DateTime BuildDate,
    FacilityType Type,
    int Capacity,
    CreateFacilityAddressCommand Address,
    List<CreateFacilityStandCommand>? Stands)
    : ICommand<CreateFacilityResponse>;

public class CreateFacilityCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<CreateFacilityCommand, CreateFacilityResponse>
{
    public async Task<CreateFacilityResponse> Handle(CreateFacilityCommand command, CancellationToken cancellationToken)
    {
        var address = new Address(
            Guid.NewGuid(),
            command.Address.Street,
            command.Address.StreetNumber,
            command.Address.PostalCode,
            command.Address.City,
            command.Address.Region,
            command.Address.Country);

        var stands = command.Stands?.Select(stand =>
                new Stand(Guid.NewGuid(), stand.StandName, stand.StandCapacity))
            .ToList();

        var facility = new Facility(
            Guid.NewGuid(),
            command.Name,
            command.Active,
            command.BuildDate,
            command.Type,
            command.Capacity,
            address,
            stands);

        await facilityRepository.Save(facility, cancellationToken);
        return new CreateFacilityResponse(facility.Id);
    }
}