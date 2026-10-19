using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Stands;

public record ReplaceFacilityStandCommand(Guid FacilityId, Guid StandId, string StandName, int StandCapacity) : ICommand;

public class ReplaceFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<ReplaceFacilityStandCommand> {
    public const string CannotUpdateStandTitle = "Cannot update stand";

    public async Task Handle(ReplaceFacilityStandCommand command, CancellationToken cancellationToken) {
        // Built before the repository is called: an invalid stand is a 400 before any 404 (Q21 a)).
        var stand = StandValidation.Create(command.StandId, command.StandName, command.StandCapacity, CannotUpdateStandTitle);
        try {
            await facilityRepository.UpdateStand(command.FacilityId, stand, cancellationToken);
        } catch (FacilityNotFoundException ex) {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }
    }
}
