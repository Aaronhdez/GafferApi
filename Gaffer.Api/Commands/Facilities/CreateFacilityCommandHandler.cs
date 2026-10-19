using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Facilities;

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
    CreateFacilityAddressCommand? Address,
    IReadOnlyList<CreateFacilityStandCommand?>? Stands)
    : ICommand<CreateFacilityResponse>;

public class CreateFacilityCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<CreateFacilityCommand, CreateFacilityResponse> {
    public const string CannotCreateFacilityTitle = "Cannot create facility";

    public async Task<CreateFacilityResponse> Handle(CreateFacilityCommand command, CancellationToken cancellationToken) {
        var address = command.Address is null
            ? null
            : new Address(
                Guid.NewGuid(),
                command.Address.Street,
                command.Address.StreetNumber,
                command.Address.PostalCode,
                command.Address.City,
                command.Address.Region,
                command.Address.Country);

        var stands = command.Stands?.OfType<CreateFacilityStandCommand>().Select(stand =>
                new Stand(Guid.NewGuid(), stand.StandName, stand.StandCapacity))
            .ToList();

        Facility? facility = null;
        try {
            var errors = new DomainValidationErrors();
            Facility.CheckRequestedStands(errors, command.Stands?.Select(ToRequestedStand).ToList() ?? []);
            errors.Collect(() => facility = Facility.Create(
                Guid.NewGuid(),
                command.Name,
                command.Active,
                command.BuildDate,
                command.Type,
                command.Capacity,
                address,
                stands));
            errors.ThrowIfAny();
        } catch (DomainValidationException ex) {
            throw new ValidationFailedException(CannotCreateFacilityTitle, ValidationErrorKeys.FacilitiesCreate, ex.Errors);
        }

        await facilityRepository.Save(facility!, cancellationToken);
        return new CreateFacilityResponse(facility!.Id);
    }

    private static (string? StandName, int StandCapacity)? ToRequestedStand(CreateFacilityStandCommand? stand)
        => stand is null ? null : (stand.StandName, stand.StandCapacity);
}
