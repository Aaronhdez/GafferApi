using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Stands;

public record AddFacilityStandResponse(Guid Id);

public record AddFacilityStandCommand(Guid FacilityId, string StandName, int StandCapacity)
    : ICommand<AddFacilityStandResponse>;

public class AddFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<AddFacilityStandCommand, AddFacilityStandResponse> {
    public const string CannotCreateStandTitle = "Cannot create stand";

    public async Task<AddFacilityStandResponse> Handle(AddFacilityStandCommand command, CancellationToken cancellationToken) {
        var stand = StandValidation.Create(Guid.NewGuid(), command.StandName, command.StandCapacity, CannotCreateStandTitle);
        try {
            await facilityRepository.AddStand(command.FacilityId, stand, cancellationToken);
        } catch (FacilityNotFoundException ex) {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }

        return new AddFacilityStandResponse(stand.Id);
    }
}
